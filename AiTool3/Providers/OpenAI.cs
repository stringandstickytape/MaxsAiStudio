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

        public async Task<AiResponse> FetchResponse(Model apiModel, Conversation conversation, string base64image, string base64ImageType, CancellationToken cancellationToken, Control textbox = null, bool useStreaming = false)
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
                return await HandleStreamingResponse(response, textbox, cancellationToken);
            }
            else
            {
                return await HandleNonStreamingResponse(response);
            }
        }

        private async Task<AiResponse> HandleStreamingResponse(HttpResponseMessage response, Control textbox, CancellationToken cancellationToken)
        {
            var stream = await response.Content.ReadAsStreamAsync();
            var reader = new StreamReader(stream);

            StringBuilder fullResponse = new StringBuilder();
            string line;
            int inputTokens = 0;
            int outputTokens = 0;

            while ((line = await reader.ReadLineAsync()) != null)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                if (line.StartsWith("data: "))
                {
                    string jsonData = line.Substring("data: ".Length).Trim();
                    if (jsonData == "[DONE]")
                        break;

                    try
                    {
                        var chunk = JsonConvert.DeserializeObject<JObject>(jsonData);
                        var content2 = chunk["choices"]?[0]?["delta"]?["content"]?.ToString();
                        if (!string.IsNullOrEmpty(content2))
                        {
                            fullResponse.Append(content2);
                            if (textbox != null)
                            {
                                textbox.Invoke((MethodInvoker)delegate {
                                    textbox.Text = fullResponse.ToString();
                                });
                            }
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

            return new AiResponse
            {
                ResponseText = fullResponse.ToString(),
                Success = true,
                TokenUsage = new TokenUsage(inputTokens.ToString(), outputTokens.ToString())
            };
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