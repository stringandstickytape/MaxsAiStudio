using AiTool3.Conversations;
using AiTool3.DataModels;
using AiTool3.Embeddings;
using AiTool3.Interfaces;
using AiTool3.Tools;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;

namespace AiTool3.Providers
{
    internal class OpenRouterAI : IAiService
    {
        public ToolManager ToolManager { get; set; }
        private readonly HttpClient client = new HttpClient();
        public event EventHandler<string> StreamingTextReceived;
        public event EventHandler<string> StreamingComplete;

        private const string API_URL = "https://openrouter.ai/api/v1/chat/completions";
        private readonly string SITE_URL;
        private readonly string SITE_NAME;

        public async Task<AiResponse> FetchResponse(Model apiModel, Conversation conversation, string base64image, string base64ImageType, CancellationToken cancellationToken, SettingsSet currentSettings, bool mustNotUseEmbedding, List<string> toolIDs, bool useStreaming = false, bool addEmbeddings = false)
        {
            var req = new JObject
            {
                ["model"] = apiModel.ModelName,
                ["messages"] = new JArray()
                {
                    (new JObject
                    {
                        ["role"] = "system",
                        ["content"] = conversation.SystemPromptWithDateTime()
                    })
                },
                ["stream"] = useStreaming
            };

            // Add conversation messages
            foreach (var m in conversation.messages)
            {
                var messageObj = new JObject
                {
                    ["role"] = m.role,
                    ["content"] = m.content
                };

                if (!string.IsNullOrEmpty(m.base64image))
                {
                    messageObj["images"] = new JArray { m.base64image };
                }

                req["messages"].Last.AddAfterSelf(messageObj);
            }

            if (addEmbeddings)
            {
                var newInput = await OllamaEmbeddingsHelper.AddEmbeddingsToInput(conversation, currentSettings, conversation.messages.Last().content, mustNotUseEmbedding);
                req["messages"].Last()["content"] = newInput;
            }

            var json = JsonConvert.SerializeObject(req);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiModel.Key}");
            client.DefaultRequestHeaders.Add("HTTP-Referer", SITE_URL);
            client.DefaultRequestHeaders.Add("X-Title", SITE_NAME);

            if (useStreaming)
            {
                return await HandleStreamingResponse(content, cancellationToken);
            }
            else
            {
                return await HandleNonStreamingResponse(content, cancellationToken);
            }
        }

        private async Task<AiResponse> HandleStreamingResponse(StringContent content, CancellationToken cancellationToken)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, API_URL);
            request.Content = content;
            client.Timeout = TimeSpan.FromSeconds(1800);
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

            StringBuilder fullResponse = new StringBuilder();
            StringBuilder lineBuilder = new StringBuilder();

            byte[] buffer = new byte[1024];
            var decoder = Encoding.UTF8.GetDecoder();

            while (true)
            {
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                if (bytesRead == 0) break;

                char[] chars = new char[decoder.GetCharCount(buffer, 0, bytesRead)];
                int charsDecodedCount = decoder.GetChars(buffer, 0, bytesRead, chars, 0);

                for (int i = 0; i < charsDecodedCount; i++)
                {
                    char c = chars[i];
                    lineBuilder.Append(c);

                    if (c == '\n')
                    {
                        ProcessLine(lineBuilder.ToString().Trim(), fullResponse);
                        lineBuilder.Clear();
                    }
                }
            }

            // Process any remaining content
            if (lineBuilder.Length > 0)
            {
                ProcessLine(lineBuilder.ToString().Trim(), fullResponse);
            }

            StreamingComplete?.Invoke(this, null);
            return new AiResponse
            {
                ResponseText = fullResponse.ToString(),
                Success = true,
                TokenUsage = new TokenUsage("0", "0") // OpenRouter doesn't provide token usage in the same way
            };
        }

        private void ProcessLine(string line, StringBuilder fullResponse)
        {
            if (string.IsNullOrEmpty(line) || line == "data: [DONE]") return;

            try
            {
                if (line.StartsWith("data: "))
                {
                    line = line.Substring(6);
                }

                var chunkResponse = JObject.Parse(line);

                if (chunkResponse["choices"] != null && chunkResponse["choices"].Any())
                {
                    var content = chunkResponse["choices"][0]["delta"]["content"]?.ToString();
                    if (!string.IsNullOrEmpty(content))
                    {
                        fullResponse.Append(content);
                        StreamingTextReceived?.Invoke(this, content);
                    }
                }
            }
            catch (JsonException)
            {
                // Handle or log JSON parsing errors
            }
        }

        private async Task<AiResponse> HandleNonStreamingResponse(StringContent content, CancellationToken cancellationToken)
        {
            var response = await client.PostAsync(API_URL, content, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JObject.Parse(responseContent);

            return new AiResponse
            {
                ResponseText = result["choices"]?[0]?["message"]?["content"]?.ToString(),
                Success = true,
                TokenUsage = new TokenUsage(
                    result["usage"]?["prompt_tokens"]?.ToString() ?? "0",
                    result["usage"]?["completion_tokens"]?.ToString() ?? "0"
                )
            };
        }
    }
}