using AiTool3.ApiManagement;
using AiTool3.Conversations;
using AiTool3.Interfaces;
using System;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net;

namespace AiTool3.Providers
{
    internal class Claude : IAiService
    {
        HttpClient client = new HttpClient();
        bool clientInitialised = false;


        // streaming text received callback event
        public event EventHandler<string> StreamingTextReceived;
        public event EventHandler<string> StreamingComplete;

        public async Task<AiResponse> FetchResponse(Model apiModel, Conversation conversation, string base64image, string base64ImageType, CancellationToken cancellationToken, Settings.Settings currentSettings, bool useStreaming = false)
        {
            var input = new List<string> { conversation.messages.Last().content };

            bool useEmbedding = false;
            var all = new List<CodeSnippet>();

            if (useEmbedding)
            {
                var inputEmbedding = await EmbeddingsHelper.CreateEmbeddingsAsync(input, currentSettings.EmbeddingKey);
                
                // deserialize from C:\Users\maxhe\source\repos\CloneTest\MaxsAiTool\AiTool3\OpenAIEmbedFragged.embeddings.json
                var codeEmbedding = JsonConvert.DeserializeObject<List<Embedding>>(System.IO.File.ReadAllText("C:\\Users\\maxhe\\source\\repos\\CloneTest\\MaxsAiTool\\AiTool3\\OpenAIEmbedFragged2.embeddings.json"));
                
                var embeddingHelper = new EmbeddingHelper();
                
                var s = embeddingHelper.FindSimilarCodeSnippets(inputEmbedding[0], codeEmbedding, 5);
                
                foreach(var snippet in s)
                {
                    var subInputEmbedding = await EmbeddingsHelper.CreateEmbeddingsAsync(new List<string> { snippet.Code }, currentSettings.EmbeddingKey);
                    var subs = embeddingHelper.FindSimilarCodeSnippets(subInputEmbedding[0], codeEmbedding, 3);
                    all.AddRange(subs);
                }
                
                all = all.GroupBy(x => x.Code).Select(x => x.First()).ToList();
            }
            if (!clientInitialised)
            {
                client.DefaultRequestHeaders.Add("x-api-key", apiModel.Key);
                client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
                clientInitialised = true;
            }


            var req = new JObject
            {
                ["model"] = apiModel.ModelName,
                ["max_tokens"] = 4096,
                ["stream"] = useStreaming,
                ["temperature"] = currentSettings.Temperature,
                ["messages"] = new JArray(
                    conversation.messages.Select(m => new JObject
                    {
                        ["role"] = m.role,
                        ["content"] = new JArray(
                            new JObject
                            {
                                ["type"] = "text",
                                ["text"] = m.content
                            }
                        )
                    })
                ),
            };

            if (useEmbedding)
            {
                var lastMsg = $"{conversation.messages.Last().content}{Environment.NewLine}{Environment.NewLine}Here's some related content:{Environment.NewLine}{string.Join(Environment.NewLine, all.Select(x => $"```{x.Filename} line {x.LineNumber}{Environment.NewLine}{x.Code}{Environment.NewLine}```"))}";
                conversation.messages.Last().content = lastMsg;
                req["messages"].Last["content"].Last["text"] = lastMsg;
            }
            if (!string.IsNullOrWhiteSpace(base64image))
            {
                var imageContent = new JObject
                {
                    ["type"] = "image",
                    ["source"] = new JObject
                    {
                        ["type"] = "base64",
                        ["media_type"] = base64ImageType,
                        ["data"] = base64image
                    }
                };

                req["messages"].Last["content"].Last.AddAfterSelf(imageContent);
            }

            var json = JsonConvert.SerializeObject(req);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            if (useStreaming)
            {
                return await HandleStreamingResponse(apiModel, content, cancellationToken);
            }
            else
            {
                return await HandleNonStreamingResponse(apiModel, content, cancellationToken);
            }
        }

        private async Task<AiResponse> HandleStreamingResponse(Model apiModel, StringContent content, CancellationToken cancellationToken)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, apiModel.Url) { Content = content };
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var responseBuilder = new StringBuilder();
            var lineBuilder = new StringBuilder();
            var buffer = new byte[48];
            var decoder = Encoding.UTF8.GetDecoder();

            while (true)
            {
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                if (bytesRead == 0) break;

                char[] chars = new char[decoder.GetCharCount(buffer, 0, bytesRead)];
                decoder.GetChars(buffer, 0, bytesRead, chars, 0);

                foreach (char c in chars)
                {
                    if (c == '\n')
                    {
                        ProcessLine(lineBuilder.ToString(), responseBuilder);
                        lineBuilder.Clear();
                    }
                    else
                    {
                        lineBuilder.Append(c);
                    }
                }
            }

            if (lineBuilder.Length > 0)
            {
                ProcessLine(lineBuilder.ToString(), responseBuilder);
            }

            // call streaming complete
            StreamingComplete?.Invoke(this, null);

            return new AiResponse { ResponseText = responseBuilder.ToString(), Success = true };
        }

        private void ProcessLine(string line, StringBuilder responseBuilder)
        {
            if (line.StartsWith("data: "))
            {
                var data = line.Substring(6);
                if (data == "[DONE]") return;

                try
                {
                    var eventData = JsonConvert.DeserializeObject<JObject>(data);
                    if (eventData["type"].ToString() == "content_block_delta")
                    {
                        var text = eventData["delta"]["text"].ToString();
                        Debug.WriteLine(text);
                        //call streamingtextreceived
                        StreamingTextReceived?.Invoke(this, text);
                        responseBuilder.Append(text);
                    }
                }
                catch (JsonException ex)
                {
                    // Handle JSON parsing error
                    Console.WriteLine($"Error parsing JSON: {ex.Message}");
                }
            }
        }

        private async Task<AiResponse> HandleNonStreamingResponse(Model apiModel, StringContent content, CancellationToken cancellationToken)
        {
            var response = await client.PostAsync(apiModel.Url, content, cancellationToken);
            var responseString = await response.Content.ReadAsStringAsync(cancellationToken);
            var completion = JsonConvert.DeserializeObject<JObject>(responseString);

            if (completion["type"]?.ToString() == "error")
            {
                return new AiResponse { ResponseText = "error - " + completion["error"]["message"].ToString(), Success = false };
            }

            var inputTokens = completion["usage"]?["input_tokens"]?.ToString();
            var outputTokens = completion["usage"]?["output_tokens"]?.ToString();
            var responseText = completion["content"][0]["text"].ToString();

            return new AiResponse { ResponseText = responseText, Success = true, TokenUsage = new TokenUsage(inputTokens, outputTokens) };
        }
    }


    public class CodeSnippet
    {
        public List<float> Embedding { get; set; }
        public string Code { get; set; }

        public string Filename { get; set; }
        public int LineNumber { get; internal set; }
    }

    public class EmbeddingHelper
    {
        public List<CodeSnippet> FindSimilarCodeSnippets(Embedding promptEmbedding, List<Embedding> codeEmbeddings, int numberOfSnippetsToReturn = 3)
        {
            var similarSnippets = new List<CodeSnippet>();

            for (int i = 0; i < codeEmbeddings.Count; i++)
            {
                var codeEmbedding = codeEmbeddings[i];
                var similarity = CalculateCosineSimilarity(promptEmbedding.Value, codeEmbedding.Value);

                var snippet = new CodeSnippet
                {
                    Embedding = codeEmbedding.Value,
                    Code = codeEmbedding.Code,
                    Filename = codeEmbedding.Filename,
                    LineNumber = codeEmbedding.LineNumber
                };

                if (similarSnippets.Count < numberOfSnippetsToReturn)
                {
                    similarSnippets.Add(snippet);
                }
                else if (similarity > similarSnippets.Min(s => CalculateCosineSimilarity(promptEmbedding.Value, s.Embedding)))
                {
                    similarSnippets.Remove(similarSnippets.OrderBy(s => CalculateCosineSimilarity(promptEmbedding.Value, s.Embedding)).First());
                    similarSnippets.Add(snippet);
                }
            }

            return similarSnippets.OrderByDescending(s => CalculateCosineSimilarity(promptEmbedding.Value, s.Embedding)).ToList();
        }

        private float CalculateCosineSimilarity(List<float> embedding1, List<float> embedding2)
        {
            if (embedding1.Count != embedding2.Count)
            {
                throw new ArgumentException("Embeddings must have the same dimension.");
            }

            float dotProduct = 0;
            float magnitude1 = 0;
            float magnitude2 = 0;

            for (int i = 0; i < embedding1.Count; i++)
            {
                dotProduct += embedding1[i] * embedding2[i];
                magnitude1 += embedding1[i] * embedding1[i];
                magnitude2 += embedding2[i] * embedding2[i];
            }

            magnitude1 = (float)Math.Sqrt(magnitude1);
            magnitude2 = (float)Math.Sqrt(magnitude2);

            if (magnitude1 == 0 || magnitude2 == 0)
            {
                return 0;
            }
            else
            {
                return dotProduct / (magnitude1 * magnitude2);
            }
        }
    }
}