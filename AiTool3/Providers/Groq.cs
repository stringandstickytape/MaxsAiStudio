using AiTool3.Conversations;
using AiTool3.DataModels;
using AiTool3.Embeddings;
using AiTool3.Interfaces;
using AiTool3.Tools;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;

namespace AiTool3.Providers
{
    internal class Groq : IAiService
    {
        public ToolManager ToolManager { get; set; }
        public event EventHandler<string> StreamingTextReceived;
        public event EventHandler<string> StreamingComplete;


        HttpClient client = new HttpClient();
        public Groq()
        {
        }

        public async Task<AiResponse> FetchResponse(Model apiModel, Conversation conversation, string base64image, string base64ImageType, CancellationToken cancellationToken, SettingsSet currentSettings, bool mustNotUseEmbedding, List<string> toolIDs, bool useStreaming = false, bool addEmbeddings = false)
        {
            useStreaming = true;
            if (client.DefaultRequestHeaders.Authorization == null)
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiModel.Key);

            var req = new JObject
            {
                ["model"] = apiModel.ModelName,
                ["max_tokens"] = 4000,
                ["messages"] = new JArray(
                    conversation.messages.Select(m => new JObject
                    {
                        ["role"] = m.role,
                        ["content"] = m.content
                    })
                )
            };

            ((JArray)req["messages"]).Insert(0, new JObject
            {
                ["role"] = "system",
                ["content"] = conversation.SystemPromptWithDateTime()
            });

            if (useStreaming)
            {
                req["stream"] = true;
            }

            if (addEmbeddings)
            {
                var newInput = await OllamaEmbeddingsHelper.AddEmbeddingsToInput(conversation, currentSettings, conversation.messages.Last().content, mustNotUseEmbedding);
                req["messages"].Last["content"] = newInput;
            }

            var json = JsonConvert.SerializeObject(req);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            HttpResponseMessage response;
            if (useStreaming)
            {
                response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Post, apiModel.Url)
                {
                    Content = content
                }, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                response = await client.PostAsync(apiModel.Url, content, cancellationToken).ConfigureAwait(false);
            }

            response.EnsureSuccessStatusCode();

            if (useStreaming)
            {
                return await HandleStreamingResponse(response, cancellationToken);
            }
            else
            {
                return await HandleNonStreamingResponse(response, cancellationToken);
            }
        }
        private async Task<AiResponse> HandleStreamingResponse(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var buffer = new byte[48];
            var decoder = Encoding.UTF8.GetDecoder();
            var sb = new StringBuilder();
            var lineSb = new StringBuilder();
            var charBuffer = new char[1024];

            while (true)
            {
                var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                if (bytesRead == 0) break;

                var charsRead = decoder.GetChars(buffer, 0, bytesRead, charBuffer, 0);

                for (int i = 0; i < charsRead; i++)
                {
                    char c = charBuffer[i];
                    lineSb.Append(c);

                    if (c == '\n')
                    {

                        ProcessLine(lineSb.ToString(), sb);
                        lineSb.Clear();
                    }
                }
            }

            // Process any remaining content
            if (lineSb.Length > 0)
            {
                ProcessLine(lineSb.ToString(), sb);
            }
            StreamingComplete?.Invoke(this, null);
            return new AiResponse { ResponseText = sb.ToString(), Success = true };
        }

        private void ProcessLine(string line, StringBuilder sb)
        {
            if (line.StartsWith("data: "))
            {
                var data = line.Substring(6).Trim();
                if (data == "[DONE]") return;

                try
                {
                    var jsonData = JsonConvert.DeserializeObject<JObject>(data);
                    var content = jsonData["choices"]?[0]?["delta"]?["content"]?.ToString();

                    if (!string.IsNullOrEmpty(content))
                    {
                        Debug.WriteLine(content);
                        sb.Append(content);
                    }
                }
                catch (Exception ex)
                {
                    // Handle JSON parsing error
                    Console.WriteLine($"Error parsing JSON: {ex.Message}");
                }
            }
        }

        private async Task<AiResponse> HandleNonStreamingResponse(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var completion = JsonConvert.DeserializeObject<JObject>(responseContent);

            var inputTokens = completion["usage"]?["prompt_tokens"]?.ToString();
            var outputTokens = completion["usage"]?["completion_tokens"]?.ToString();

            if (completion["choices"] == null)
            {
                return null;
            }

            return new AiResponse
            {
                ResponseText = completion["choices"][0]["message"]["content"].ToString(),
                Success = true,
                TokenUsage = new TokenUsage(inputTokens, outputTokens)
            };
        }
    }
}