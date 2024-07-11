using AiTool3.ApiManagement;
using AiTool3.Conversations;
using AiTool3.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using static System.Windows.Forms.Design.AxImporter;

namespace AiTool3.Providers
{
    internal class Groq : IAiService
    {
        HttpClient client = new HttpClient();
        public Groq()
        {
        }

        public async Task<AiResponse> FetchResponse(Model apiModel, Conversation conversation, string base64image, string base64ImageType, CancellationToken cancellationToken, Control textbox = null, bool useStreaming = false)
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
                return await HandleStreamingResponse(response, textbox, cancellationToken);
            }
            else
            {
                return await HandleNonStreamingResponse(response, cancellationToken);
            }
        }
        private async Task<AiResponse> HandleStreamingResponse(HttpResponseMessage response, Control textbox, CancellationToken cancellationToken)
        {
            var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var reader = new StreamReader(stream);
            var sb = new StringBuilder();
            string line;

            while ((line = await reader.ReadLineAsync()) != null)
            {
                if (line.StartsWith("data: "))
                {
                    var data = line.Substring(6);
                    if (data == "[DONE]") break;

                    var jsonData = JsonConvert.DeserializeObject<JObject>(data);
                    var content = jsonData["choices"]?[0]?["delta"]?["content"]?.ToString();

                    if (!string.IsNullOrEmpty(content))
                    {
                        sb.Append(content);
                        if (textbox != null)
                        {
                            textbox.Invoke((MethodInvoker)delegate {
                                textbox.Text = sb.ToString();
                            });
                        }
                    }
                }
            }

            return new AiResponse { ResponseText = sb.ToString(), Success = true };
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