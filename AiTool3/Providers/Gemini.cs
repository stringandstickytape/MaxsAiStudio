using AiTool3.ApiManagement;
using AiTool3.Conversations;
using AiTool3.Interfaces;
using AiTool3.Tools;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Text;

namespace AiTool3.Providers
{
    internal class Gemini : IAiService
    {
        public ToolManager ToolManager { get; set; }
        HttpClient client = new HttpClient();

        public event EventHandler<string> StreamingTextReceived;
        public event EventHandler<string> StreamingComplete;
        public Gemini()
        {
        }

        public async Task<AiResponse> FetchResponse(Model apiModel, Conversation conversation, string base64image, string base64ImageType, CancellationToken cancellationToken, SettingsSet currentSettings, bool mustNotUseEmbedding, List<string> toolIDs, bool useStreaming = false, bool addEmbeddings = false)
        {
            string url = $"{apiModel.Url}{apiModel.ModelName}:{(useStreaming ? "streamGenerateContent" : "generateContent")}?key={apiModel.Key}";

            var obj = new JObject
            {
                ["contents"] = new JArray(
                    conversation.messages.Select(m => new JObject
                    {
                        ["role"] = m.role == "assistant" ? "model" : m.role,
                        ["parts"] = new JArray()
                    })
                )
            };

            // Add message content and images from conversation history
            for (int i = 0; i < conversation.messages.Count; i++)
            {
                var message = conversation.messages[i];
                var contentArray = (JArray)((JArray)obj["contents"])[i]["parts"];

                // Add image if present in message history


                // Add text content
                contentArray.Add(new JObject
                {
                    ["text"] = message.content
                });

                if (!string.IsNullOrEmpty(message.base64image))
                {
                    contentArray.Add(new JObject
                    {
                        ["inline_data"] = new JObject
                        {
                            ["mime_type"] = message.base64type,
                            ["data"] = message.base64image
                        }
                    });
                }

            }

            AddFakeSystemPrompt(conversation, obj);

            if (addEmbeddings)
            {
                var newInput = await OllamaEmbeddingsHelper.AddEmbeddingsToInput(conversation, currentSettings, conversation.messages.Last().content, mustNotUseEmbedding);

                // does the last content array thing have a text prop?
                var lastContent = ((JArray)obj["contents"]).Last;
                if (lastContent["parts"].Last["text"] != null)
                {
                    lastContent["parts"].Last["text"] = newInput;
                }
                else
                {
                    // set the text prop on the last-but-one content instead
                    var lastButOneContent = ((JArray)obj["contents"]).Reverse().Skip(1).First();
                    lastButOneContent["parts"].Last["text"] = newInput;
                }
            }

            var jsonPayload = JsonConvert.SerializeObject(obj);

            using (HttpClient client = new HttpClient())
            {
                using (HttpContent content = new StringContent(jsonPayload, Encoding.UTF8, "application/json"))
                {
                    if (useStreaming)
                    {
                        return await StreamResponse(client, url, content, cancellationToken);
                    }
                    else
                    {
                        return await NonStreamingResponse(client, url, content, cancellationToken);
                    }
                }
            }
        }

        private static void AddFakeSystemPrompt(Conversation conversation, JObject obj)
        {
            ((JArray)obj["contents"]).Insert(0, new JObject
            {
                ["role"] = "model",
                ["parts"] = new JArray(new JObject
                {
                    ["text"] = "Understood."
                })
            });

            ((JArray)obj["contents"]).Insert(0, new JObject
            {
                ["role"] = "user",
                ["parts"] = new JArray(new JObject
                {
                    ["text"] = conversation.SystemPromptWithDateTime()
                })
            });
        }

        private async Task<AiResponse> StreamResponse(HttpClient client, string url, HttpContent content, CancellationToken cancellationToken)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Post, url))
            {
                request.Content = content;
                using (var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
                {
                    response.EnsureSuccessStatusCode();
                    using (var stream = await response.Content.ReadAsStreamAsync())
                    {
                        byte[] buffer = new byte[48];
                        Decoder decoder = new UTF8Encoding(false).GetDecoder();
                        StringBuilder fullResponse = new StringBuilder();
                        StringBuilder jsonBuffer = new StringBuilder();
                        char[] charBuffer = new char[1024];

                        var indent = 0;

                        while (true)
                        {
                            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                            if (bytesRead == 0) break;

                            int charsDecodedCount = decoder.GetChars(buffer, 0, bytesRead, charBuffer, 0);

                            for (int i = 0; i < charsDecodedCount; i++)
                            {
                                char c = charBuffer[i];
                                jsonBuffer.Append(c);

                                if (c == '{')
                                {
                                    indent++;
                                }

                                if (c == '}')
                                {
                                    indent--;
                                    if (indent == 0)
                                    {
                                        var json = jsonBuffer.ToString().Substring(jsonBuffer.ToString().IndexOf('{'));

                                        await ProcessJsonObject(json, fullResponse);
                                        jsonBuffer.Clear();
                                    }
                                }
                            }
                        }

                        if (jsonBuffer.Length > 0 && jsonBuffer.ToString().Contains("{"))
                        {
                            var json = jsonBuffer.ToString().Substring(jsonBuffer.ToString().IndexOf('{'));


                            try
                            {
                                await ProcessJsonObject(json, fullResponse);
                            }
                            catch (Exception)
                            {

                            }
                        }

                        StreamingComplete?.Invoke(this, null);

                        return new AiResponse { ResponseText = fullResponse.ToString(), Success = true, TokenUsage = new TokenUsage(inputTokenCount, outputTokenCount) };
                    }
                }
            }
        }
        private string inputTokenCount = "";
        private string outputTokenCount = "";

        private async Task ProcessJsonObject(string jsonString, StringBuilder fullResponse)
        {
            if (!string.IsNullOrWhiteSpace(jsonString))
            {
                Debug.WriteLine(jsonString);
                try
                {
                    var streamData = JObject.Parse(jsonString);
                    if (streamData["candidates"] != null)
                    {
                        var textChunk = streamData["candidates"][0]["content"]["parts"][0]["text"]?.ToString();
                        if (!string.IsNullOrEmpty(textChunk))
                        {
                            Debug.WriteLine(textChunk);
                            fullResponse.Append(textChunk);

                            StreamingTextReceived?.Invoke(this, textChunk);
                        }
                    }
                    /*   "usageMetadata": {
    "promptTokenCount": 73,
    "candidatesTokenCount": 1,
    "totalTokenCount": 74
  } */
                    if (streamData["usageMetadata"] != null)
                    {
                        inputTokenCount = streamData["usageMetadata"]?["promptTokenCount"]?.ToString();
                        outputTokenCount = streamData["usageMetadata"]?["candidatesTokenCount"]?.ToString();
                    }
                }
                catch (JsonReaderException)
                {
                }
            }
        }

        private async Task<AiResponse> NonStreamingResponse(HttpClient client, string url, HttpContent content, CancellationToken cancellationToken)
        {
            HttpResponseMessage response = await client.PostAsync(url, content, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                string responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine(responseContent);

                var completion = JsonConvert.DeserializeObject<JObject>(responseContent);

                var inputTokens = completion["usageMetadata"]?["promptTokenCount"]?.ToString();
                var outputTokens = completion["usageMetadata"]?["candidatesTokenCount"]?.ToString();

                return new AiResponse
                {
                    ResponseText = completion["candidates"][0]["content"]["parts"][0]["text"].ToString(),
                    Success = true,
                    TokenUsage = new TokenUsage(inputTokens, outputTokens)
                };
            }
            else
            {
                Console.WriteLine($"Error: {response.StatusCode}");
                string errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine(errorContent);
                return new AiResponse { ResponseText = errorContent, Success = false };
            }
        }
    }
}