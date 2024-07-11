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

        public async Task<AiResponse> FetchResponse(Model apiModel, Conversation conversation, string base64image, string base64ImageType, CancellationToken cancellationToken, Control textbox = null, bool useStreaming = false)
        {
            
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
                )
            };

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
                return await HandleStreamingResponse(apiModel, content, cancellationToken, textbox);
            }
            else
            {
                return await HandleNonStreamingResponse(apiModel, content, cancellationToken);
            }
        }

        private async Task<AiResponse> HandleStreamingResponse(Model apiModel, StringContent content, CancellationToken cancellationToken, Control textbox)
        {
            var response = await client.PostAsync(apiModel.Url, content, cancellationToken);
            var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var reader = new StreamReader(stream);
            var responseBuilder = new StringBuilder();
            string line;

            while ((line = await reader.ReadLineAsync()) != null)
            {
                if (line.StartsWith("data: "))
                {
                    var data = line.Substring(6);
                    if (data == "[DONE]") break;

                    var eventData = JsonConvert.DeserializeObject<JObject>(data);
                    if (eventData["type"].ToString() == "content_block_delta")
                    {
                        var text = eventData["delta"]["text"].ToString();
                        responseBuilder.Append(text);
                        if (textbox != null)
                        {
                            textbox.Invoke((MethodInvoker)delegate {
                                textbox.Text = responseBuilder.ToString();
                            });
                        }
                    }
                }
            }

            return new AiResponse { ResponseText = responseBuilder.ToString(), Success = true };
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
}