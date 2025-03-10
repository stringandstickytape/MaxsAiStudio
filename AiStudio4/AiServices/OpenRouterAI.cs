﻿using AiStudio4.Conversations;
using AiStudio4.DataModels;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharedClasses.Providers;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace AiStudio4.AiServices
{
    internal class OpenRouterAI : AiServiceBase
    {
        private readonly string baseUrl = "https://openrouter.ai/api/v1/chat/completions";

        public OpenRouterAI()
        {
        }

        protected override void ConfigureHttpClientHeaders(ApiSettings apiSettings)
        {
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {ApiKey}");
            client.DefaultRequestHeaders.Add("HTTP-Referer", "https://github.com/stringandstickytape/MaxsAiStudio/");
            client.DefaultRequestHeaders.Add("X-Title", "MaxsAiStudio");
        }
        protected override async Task<AiResponse> FetchResponseInternal(AiRequestOptions options)
        {
            InitializeHttpClient(options.ServiceProvider, options.Model, options.ApiSettings);
            
            // Apply custom system prompt if provided
            if (!string.IsNullOrEmpty(options.CustomSystemPrompt))
            {
                options.Conversation.systemprompt = options.CustomSystemPrompt;
            }
            
            var requestPayload = CreateRequestPayload(ApiModel, options.Conversation, options.UseStreaming, options.ApiSettings);
            // Add system message
            ((JArray)requestPayload["messages"]).Add(new JObject
            {
                ["role"] = "system",
                ["content"] = options.Conversation.SystemPromptWithDateTime()
            });

            // Add conversation messages
            foreach (var m in options.Conversation.messages)
            {
                var messageObj = CreateMessageObject(m);
                ((JArray)requestPayload["messages"]).Add(messageObj);
            }

            if (options.AddEmbeddings)
            {
                var lastMessage = options.Conversation.messages.Last().content;
                var newInput = await AddEmbeddingsIfRequired(options.Conversation, options.ApiSettings, options.MustNotUseEmbedding, options.AddEmbeddings, lastMessage);
                ((JObject)((JArray)requestPayload["messages"]).Last)["content"] = newInput;
            }


            var jsonPayload = JsonConvert.SerializeObject(requestPayload);
            using (var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json"))
            {
                return await HandleResponse(content, options.UseStreaming, options.CancellationToken);
            }
        }
        protected override JObject CreateRequestPayload(
             string modelName,
             LinearConversation conversation,
            bool useStreaming,
             ApiSettings apiSettings)
        {
            return new JObject
            {
                ["model"] = modelName,
                ["messages"] = new JArray(),
                ["stream"] = useStreaming
            };
        }
        protected override JObject CreateMessageObject(LinearConversationMessage message)
        {
            var messageObj = new JObject
            {
                ["role"] = message.role,
                ["content"] = message.content
            };

            if (!string.IsNullOrEmpty(message.base64image) && !string.IsNullOrEmpty(message.base64type))
            {
                messageObj["content"] = new JArray
                {
                    new JObject
                    {
                        ["type"] = "text",
                        ["text"] = message.content
                    },
                    new JObject
                    {
                        ["type"] = "image_url",
                         ["image_url"] = new JObject
                        {
                           ["url"] = $"data:{message.base64type};base64,{message.base64image}"
                       }
                    }
                };
            }
            return messageObj;
        }
        protected override async Task<AiResponse> HandleStreamingResponse(HttpContent content, CancellationToken cancellationToken)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, baseUrl);
            request.Content = content;
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

            StringBuilder fullResponse = new StringBuilder();
            using var reader = new StreamReader(stream);

            TokenUsage tokenUsage = null;

            while (!reader.EndOfStream)
            {
                await Task.Yield();
                var line = await reader.ReadLineAsync(cancellationToken);
                System.Diagnostics.Debug.WriteLine(line);


                if (string.IsNullOrEmpty(line) || line.StartsWith(": OPENROUTER PROCESSING"))
                {
                    OnStreamingDataReceived("");

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
                        var contentChunk = jsonData["choices"]?[0]?["delta"]?["content"]?.ToString();
                        if (!string.IsNullOrEmpty(contentChunk))
                        {
                            fullResponse.Append(contentChunk);
                            OnStreamingDataReceived(contentChunk);
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

            OnStreamingComplete();

            return new AiResponse
            {
                ResponseText = fullResponse.ToString(),
                Success = true,
                TokenUsage = tokenUsage ?? new TokenUsage("N/A", "N/A")
            };
        }

        protected override async Task<AiResponse> HandleNonStreamingResponse( HttpContent content, CancellationToken cancellationToken)
        {
            var response = await client.PostAsync(baseUrl, content, cancellationToken);
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

        protected override TokenUsage ExtractTokenUsage(JObject response)
        {
            return new TokenUsage(
                   response["usage"]?["prompt_tokens"]?.ToString() ?? "N/A",
                   response["usage"]?["completion_tokens"]?.ToString() ?? "N/A"
               );
        }
    }
}