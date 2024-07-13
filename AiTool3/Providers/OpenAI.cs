using AiTool3.ApiManagement;
using AiTool3.Conversations;
using AiTool3.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;

namespace AiTool3.Providers
{
    internal class OpenAI : IAiService
    {
        HttpClient client = new HttpClient();

        public async Task<AiResponse> FetchResponse(Model apiModel, Conversation conversation, string base64image, string base64ImageType, CancellationToken cancellationToken, bool useStreaming = false)
        {
            if (client.DefaultRequestHeaders.Authorization == null)
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiModel.Key);

            var req = new JObject
            {
                ["model"] = apiModel.ModelName,
                ["messages"] = new JArray
                {
                    new JObject
                    {
                        ["role"] = "system",
                        ["content"] = new JArray
                        {
                            new JObject
                            {
                                ["type"] = "text",
                                ["text"] = conversation.SystemPromptWithDateTime()
                            }
                        }
                    }
                },
                ["stream"] = useStreaming
            };

            foreach (var m in conversation.messages)
            {
                req["messages"].Last.AddAfterSelf(new JObject
                {
                    ["role"] = m.role,
                    ["content"] = new JArray
                    {
                        new JObject
                        {
                            ["type"] = "text",
                            ["text"] = m.content
                        }
                    }
                });
            }

            if (!string.IsNullOrWhiteSpace(base64image))
            {
                ((JArray)req["messages"].Last["content"]).Add(new JObject
                {
                    ["type"] = "image_url",
                    ["image_url"] = new JObject
                    {
                        ["url"] = $"data:{base64ImageType};base64,{base64image}"
                    }
                });
            }

            var json = JsonConvert.SerializeObject(req, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync(apiModel.Url, content, cancellationToken).ConfigureAwait(false);

            if (useStreaming)
            {
                return await HandleStreamingResponse(response, cancellationToken);
            }
            else
            {
                return await HandleNonStreamingResponse(response);
            }
        }

        private async Task<AiResponse> HandleStreamingResponse(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            using (var stream = await response.Content.ReadAsStreamAsync())
            {
                byte[] buffer = new byte[48];
                var decoder = new UTF8Encoding(false).GetDecoder();
                StringBuilder fullResponse = new StringBuilder();
                StringBuilder lineBuilder = new StringBuilder();
                int inputTokens = 0;
                int outputTokens = 0;

                while (true)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                    if (bytesRead == 0) break;

                    char[] chars = new char[decoder.GetCharCount(buffer, 0, bytesRead)];
                    int charsDecodedCount = decoder.GetChars(buffer, 0, bytesRead, chars, 0);

                    for (int i = 0; i < charsDecodedCount; i++)
                    {
                        char c = chars[i];
                        if (c == '\n')
                        {
                            ProcessLine(lineBuilder.ToString(), fullResponse, ref inputTokens, ref outputTokens);
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
                    ProcessLine(lineBuilder.ToString(), fullResponse, ref inputTokens, ref outputTokens);
                }

                return new AiResponse
                {
                    ResponseText = fullResponse.ToString(),
                    Success = true,
                    TokenUsage = new TokenUsage(inputTokens.ToString(), outputTokens.ToString())
                };
            }
        }

        private void ProcessLine(string line, StringBuilder fullResponse, ref int inputTokens, ref int outputTokens)
        {
            if (line.StartsWith("data: "))
            {
                string jsonData = line.Substring("data: ".Length).Trim();
                
                if (jsonData == "[DONE]")
                    return;

                try
                {
                    var chunk = JsonConvert.DeserializeObject<JObject>(jsonData);
                    var content = chunk["choices"]?[0]?["delta"]?["content"]?.ToString();
                    if (!string.IsNullOrEmpty(content))
                    {
                        Debug.Write(content);
                        fullResponse.Append(content);
                    }

                    // Update token counts if available
                    var usage = chunk["usage"];
                    if (usage != null)
                    {
                        inputTokens = usage["prompt_tokens"]?.Value<int>() ?? inputTokens;
                        outputTokens = usage["completion_tokens"]?.Value<int>() ?? outputTokens;
                    }
                }
                catch (JsonException)
                {
                    // Handle JSON parsing errors
                }
            }
        }

        private async Task<AiResponse> HandleNonStreamingResponse(HttpResponseMessage response)
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            var jsonResponse = JsonConvert.DeserializeObject<JObject>(responseContent);

            var responseText = jsonResponse["choices"]?[0]?["message"]?["content"]?.ToString();
            var usage = jsonResponse["usage"];
            var inputTokens = usage?["prompt_tokens"]?.Value<int>() ?? 0;
            var outputTokens = usage?["completion_tokens"]?.Value<int>() ?? 0;

            return new AiResponse
            {
                ResponseText = responseText,
                Success = true,
                TokenUsage = new TokenUsage(inputTokens.ToString(), outputTokens.ToString())
            };
        }
    }
}