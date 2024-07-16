﻿using AiTool3.ApiManagement;
using AiTool3.Conversations;
using AiTool3.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using static System.Windows.Forms.Design.AxImporter;

namespace AiTool3.Providers
{
    internal class Gemini : IAiService
    {
        HttpClient client = new HttpClient();

        public event EventHandler<string> StreamingTextReceived;
        public event EventHandler<string> StreamingComplete;
        public Gemini()
        {
        }

        public async Task<AiResponse> FetchResponse(Model apiModel, Conversation conversation, string base64image, string base64ImageType, CancellationToken cancellationToken, SettingsSet currentSettings, bool useStreaming = false)
        {
            string url = $"{apiModel.Url}{apiModel.ModelName}:{(useStreaming ? "streamGenerateContent" : "generateContent")}?key={apiModel.Key}";

            var obj = new JObject
            {
                ["contents"] = new JArray(
                    conversation.messages.Select(m => new JObject
                    {
                        ["role"] = m.role == "assistant" ? "model" : m.role,
                        ["parts"] = new JArray(new JObject
                        {
                            ["text"] = m.content
                        })
                    })
                )
            };

            AddFakeSystemPrompt(conversation, obj);

            var newInput = await OllamaEmbeddingsHelper.AddEmbeddingsToInput(conversation, currentSettings, conversation.messages.Last().content);
            (obj["contents"] as JArray).Last()["parts"].Last()["text"] = newInput;

            if (base64image != null)
            {
                var lastContent = ((JArray)obj["contents"]).Last;
                ((JArray)lastContent["parts"]).Add(new JObject
                {
                    ["inline_data"] = new JObject
                    {
                        ["mime_type"] = "image/jpeg",
                        ["data"] = base64image
                    }
                });
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

                                if(c == '{')
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

                        if (jsonBuffer.Length > 0)
                        {
                            var json = jsonBuffer.ToString().Substring(jsonBuffer.ToString().IndexOf('{'));

                            await ProcessJsonObject(json, fullResponse);
                        }

                        StreamingComplete?.Invoke(this, null);

                        return new AiResponse { ResponseText = fullResponse.ToString(), Success = true };
                    }
                }
            }
        }

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
                        {Debug.WriteLine(textChunk);
                            fullResponse.Append(textChunk);

                            StreamingTextReceived?.Invoke(this, textChunk);
                        }
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