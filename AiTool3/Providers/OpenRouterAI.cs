using AiTool3.Conversations;
using AiTool3.DataModels;
using AiTool3.Embeddings;
using AiTool3.Interfaces;
using AiTool3.Tools;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Text;

namespace AiTool3.Providers
{
    internal class OpenRouterAI : IAiService
    {
        public ToolManager ToolManager { get; set; }
        private readonly HttpClient client = new HttpClient();
        public event EventHandler<string> StreamingTextReceived;
        public event EventHandler<string> StreamingComplete;
        bool clientInitialised = false;

        private readonly string apiKey;
        private readonly string baseUrl = "https://openrouter.ai/api/v1/chat/completions";

        public async Task<AiResponse> FetchResponse(Model apiModel, Conversation conversation, string base64image, string base64ImageType, CancellationToken cancellationToken, SettingsSet currentSettings, bool mustNotUseEmbedding, List<string> toolIDs, bool useStreaming = false, bool addEmbeddings = false)
        {
            if (!clientInitialised)
            {
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiModel.Key}");
                client.DefaultRequestHeaders.Add("HTTP-Referer", "https://github.com/stringandstickytape/MaxsAiStudio/");
                client.DefaultRequestHeaders.Add("X-Title", "MaxsAiStudio");
                clientInitialised = true;
            }

            var req = new JObject
            {
                ["model"] = apiModel.ModelName,
                ["messages"] = new JArray(),
                ["stream"] = useStreaming
            };

            // Add system message
            ((JArray)req["messages"]).Add(new JObject
            {
                ["role"] = "system",
                ["content"] = conversation.SystemPromptWithDateTime()
            });

            // Add conversation messages
            foreach (var m in conversation.messages)
            {
                var messageObj = new JObject
                {
                    ["role"] = m.role,
                    ["content"] = m.content
                };

                if (!string.IsNullOrEmpty(m.base64image) && !string.IsNullOrEmpty(m.base64type))
                {
                    messageObj["content"] = new JArray
                    {
                        new JObject
                        {
                            ["type"] = "text",
                            ["text"] = m.content
                        },
                        new JObject
                        {
                            ["type"] = "image_url",
                            ["image_url"] = new JObject
                            {
                                ["url"] = $"data:{m.base64type};base64,{m.base64image}"
                            }
                        }
                    };
                }

                ((JArray)req["messages"]).Add(messageObj);
            }

            if (addEmbeddings)
            {
                var newInput = await OllamaEmbeddingsHelper.AddEmbeddingsToInput(conversation, currentSettings, conversation.messages.Last().content, mustNotUseEmbedding);
                ((JObject)((JArray)req["messages"]).Last)["content"] = newInput;
            }

            var json = JsonConvert.SerializeObject(req);
            var requestContent = new StringContent(json, Encoding.UTF8, "application/json");

            if (useStreaming)
            {
                return await HandleStreamingResponse(requestContent, cancellationToken);
            }
            else
            {
                return await HandleNonStreamingResponse(requestContent, cancellationToken);
            }
        }

        private async Task<AiResponse> HandleStreamingResponse(StringContent requestContent, CancellationToken cancellationToken)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, baseUrl);
            request.Content = requestContent;
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

            StringBuilder fullResponse = new StringBuilder();
            using var reader = new StreamReader(stream);

            TokenUsage tokenUsage = null;

            var context = UIThreadHelper.IsOnUIThread();


                while (!reader.EndOfStream)
                {
                    await Task.Yield();
                    var line = await reader.ReadLineAsync();
                    Debug.WriteLine(line);


                    if (string.IsNullOrEmpty(line) || line.StartsWith(": OPENROUTER PROCESSING"))
                    {
                        StreamingTextReceived?.Invoke(this, "");

                        continue;
                    }

                    if (line.StartsWith("data: "))
                    {
                        var data = line.Substring(6);
                        if (data == "[DONE]")
                        {

                            break;
                        }

                        try
                        {
                            var jsonData = JObject.Parse(data);
                            var content = jsonData["choices"]?[0]?["delta"]?["content"]?.ToString();
                            if (!string.IsNullOrEmpty(content))
                            {
                                fullResponse.Append(content);
                                StreamingTextReceived?.Invoke(this, content);
                            }

                            // Check for usage information
                            var usage = jsonData["usage"];
                            if (usage != null)
                            {
                                tokenUsage = new TokenUsage(
                                    usage["prompt_tokens"]?.ToString() ?? "N/A",
                                    usage["completion_tokens"]?.ToString() ?? "N/A"
                                );
                            }
                        }
                        catch (JsonException)
                        {
                            // Handle or log JSON parsing errors
                        }
                    }
                }

                StreamingComplete?.Invoke(this, null);

            return new AiResponse
            {
                ResponseText = fullResponse.ToString(),
                Success = true,
                TokenUsage = tokenUsage ?? new TokenUsage("N/A", "N/A")
            };
        }

        private async Task<AiResponse> HandleNonStreamingResponse(StringContent requestContent, CancellationToken cancellationToken)
        {
            var response = await client.PostAsync(baseUrl, requestContent, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JObject.Parse(responseContent);

            return new AiResponse
            {
                ResponseText = result["choices"]?[0]?["message"]?["content"]?.ToString(),
                Success = true,
                TokenUsage = new TokenUsage(
                    result["usage"]?["prompt_tokens"]?.ToString() ?? "N/A",
                    result["usage"]?["completion_tokens"]?.ToString() ?? "N/A"
                )
            };
        }
    }
}