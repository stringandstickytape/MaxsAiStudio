
using AiStudio4.Conversations;
using AiStudio4.Core.Tools;
using AiStudio4.DataModels;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharedClasses.Helpers;
using SharedClasses.Providers;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using static System.Net.Mime.MediaTypeNames;

namespace AiStudio4.AiServices
{
    internal class Claude : AiServiceBase
    {
        private string oneOffPreFill { get; set; }

        public void SetOneOffPreFill(string prefill)
        {
            oneOffPreFill = prefill;
        }

        protected override void ConfigureHttpClientHeaders(ApiSettings apiSettings)
        {
            base.ConfigureHttpClientHeaders(apiSettings);
            client.DefaultRequestHeaders.Add("x-api-key", ApiKey);
            client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

            if (apiSettings.UsePromptCaching)
            {
                client.DefaultRequestHeaders.Add("anthropic-beta", "prompt-caching-2024-07-31");
            }
            
            if (ApiModel == "claude-3-7-sonnet-latest")
            {
                client.DefaultRequestHeaders.Add("anthropic-beta", "output-128k-2025-02-19");
            }
        }

        protected override JObject CreateRequestPayload(string modelName, LinearConversation conversation, bool useStreaming, ApiSettings apiSettings)
        {
            var req = new JObject
            {
                ["model"] = modelName,
                ["system"] = conversation.systemprompt ?? "",
                ["max_tokens"] = ApiModel == "claude-3-7-sonnet-latest" ? 64000 : 8192,
                ["stream"] = useStreaming,
                ["temperature"] = apiSettings.Temperature,
            };

            var messagesArray = new JArray();
            int userMessageCount = 0;

            foreach (var message in conversation.messages)
            {
                var contentArray = new JArray();

                if (message.base64image != null)
                {
                    contentArray.Add(new JObject
                    {
                        ["type"] = "image",
                        ["source"] = new JObject
                        {
                            ["type"] = "base64",
                            ["media_type"] = message.base64type,
                            ["data"] = message.base64image
                        }
                    });
                }

                contentArray.Add(new JObject
                {
                    ["type"] = "text",
                    ["text"] = message.content.Replace("\r", "")
                });

                var messageObject = new JObject
                {
                    ["role"] = message.role,
                    ["content"] = contentArray
                };

                if (apiSettings.UsePromptCaching && message.role.ToLower() == "user" && userMessageCount < 4)
                {
                    messageObject["content"][0]["cache_control"] = new JObject
                    {
                        ["type"] = "ephemeral"
                    };
                    userMessageCount++;
                }

                messagesArray.Add(messageObject);
            }

            if (oneOffPreFill != null)
            {
                messagesArray.Add(new JObject
                {
                    ["role"] = "assistant",
                    ["content"] = new JArray
                {
                    new JObject
                    {
                        ["type"] = "text",
                        ["text"] = oneOffPreFill.Trim()
                    }
                }
                });
            }

            req["messages"] = messagesArray;
            return req;
        }

        public override async Task<AiResponse> FetchResponse(SharedClasses.Providers.ServiceProvider serviceProvider,
            Model model, LinearConversation conversation, string base64image, string base64ImageType, CancellationToken cancellationToken, ApiSettings apiSettings, bool mustNotUseEmbedding, List<string> toolIDs, bool useStreaming = false, bool addEmbeddings = false)
        {
            InitializeHttpClient(serviceProvider, model, apiSettings);

            var req = CreateRequestPayload(ApiModel, conversation, useStreaming, apiSettings);

            if (toolIDs?.Any() == true)
            {
                AddToolsToRequest(req, toolIDs);
            }

            if (addEmbeddings)
            {
                await AddEmbeddingsToRequest(req, conversation, apiSettings, mustNotUseEmbedding);
            }

            var json = JsonConvert.SerializeObject(req);
            File.WriteAllText($"request_{DateTime.Now:yyyyMMddHHmmss}.json", json);

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            while (true)
            {
                try
                {
                    var response = await HandleResponse(content, useStreaming, cancellationToken);
                    if (oneOffPreFill != null)
                    {
                        response.ResponseText = $"{oneOffPreFill}{response.ResponseText}";
                        oneOffPreFill = null;
                    }
                    return response;
                }
                catch (NotEnoughTokensForCachingException)
                {
                    if (apiSettings.UsePromptCaching)
                    {
                        json = RemoveCachingFromJson(json);
                        apiSettings.UsePromptCaching = false;
                        content = new StringContent(json, Encoding.UTF8, "application/json");
                    }
                    else
                    {
                        throw;
                    }
                }
            }
        }

        protected override async Task<AiResponse> HandleStreamingResponse(HttpContent content, CancellationToken cancellationToken)
        {
            using var response = await SendRequest(content, cancellationToken, true);
            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

            var streamProcessor = new StreamProcessor(true);
            streamProcessor.StreamingTextReceived += (s, e) => OnStreamingDataReceived(e);

            var result = await streamProcessor.ProcessStream(stream, cancellationToken);
            OnStreamingComplete();

            return new AiResponse
            {
                ResponseText = result.ResponseText,
                Success = true,
                TokenUsage = new TokenUsage(
                    result.InputTokens?.ToString(),
                    result.OutputTokens?.ToString(),
                    result.CacheCreationInputTokens?.ToString(),
                    result.CacheReadInputTokens?.ToString()
                )
            };
        }

        protected override async Task<AiResponse> HandleNonStreamingResponse(HttpContent content, CancellationToken cancellationToken)
        {
            using var response = await SendRequest(content, cancellationToken);
            var responseString = await response.Content.ReadAsStringAsync(cancellationToken);
            var completion = JsonConvert.DeserializeObject<JObject>(responseString);

            if (completion["type"]?.ToString() == "error")
            {
                if (completion["error"]["message"].ToString().Contains("at least 1024 tokens"))
                {
                    throw new NotEnoughTokensForCachingException(completion["error"]["message"].ToString());
                }
                else if (completion["error"]["message"].ToString().StartsWith("Overloaded"))
                {
                    //var result = MessageBox.Show("Claude reports that it's overloaded. Would you like to retry?", "Server Overloaded", MessageBoxButtons.YesNo);
                    //if (result == DialogResult.Yes)
                    //{
                    //    return await HandleNonStreamingResponse( content, cancellationToken);
                    //}
                }
                return new AiResponse { ResponseText = "error - " + completion["error"]["message"].ToString(), Success = false };
            }

            var responseText = ExtractResponseTextFromCompletion(completion);
            var tokenUsage = ExtractTokenUsageFromCompletion(completion);

            return new AiResponse
            {
                ResponseText = responseText,
                Success = true,
                TokenUsage = tokenUsage
            };
        }

        private string RemoveCachingFromJson(string json)
        {
            var jObject = JObject.Parse(json);
            var messages = jObject["messages"] as JArray;
            if (messages != null)
            {
                foreach (var message in messages)
                {
                    var content = message["content"] as JArray;
                    if (content != null)
                    {
                        foreach (var item in content)
                        {
                            item["cache_control"]?.Parent.Remove();
                        }
                    }
                }
            }
            return jObject.ToString();
        }

        private string ExtractResponseTextFromCompletion(JObject completion)
        {
            if (completion["content"] != null)
            {
                return completion["content"][0]["type"].ToString() == "tool_use"
                    ? completion["content"][0]["input"].First().ToString()
                    : completion["content"][0]["text"].ToString();
            }
            else if (completion["tool_calls"] != null && completion["tool_calls"][0]["function"]["name"].ToString() == "Find-and-replaces")
            {
                return completion["tool_calls"][0]["function"]["arguments"].ToString();
            }
            return string.Empty;
        }

        private TokenUsage ExtractTokenUsageFromCompletion(JObject completion)
        {
            return new TokenUsage(
                completion["usage"]?["input_tokens"]?.ToString(),
                completion["usage"]?["output_tokens"]?.ToString(),
                completion["usage"]?["cache_creation_input_tokens"]?.ToString(),
                completion["usage"]?["cache_read_input_tokens"]?.ToString()
            );
        }

        private async Task AddEmbeddingsToRequest(JObject req, LinearConversation conversation, ApiSettings apiSettings, bool mustNotUseEmbedding)
        {
            //var newInput = await OllamaEmbeddingsHelper.AddEmbeddingsToInput(
            //    conversation,
            //    apiSettings,
            //    conversation.messages.Last().content,
            //    mustNotUseEmbedding
            //);
            //req["messages"].Last["content"].Last["text"] = newInput;
        }

        protected override void AddToolsToRequest(JObject req, List<string> toolIDs)
        {
            if (!toolIDs.Any()) return;

            var toolRequestBuilder = new ToolRequestBuilder(ToolService);
            toolRequestBuilder.AddToolToRequest(req, toolIDs[0], GetToolFormat());
        }

        protected override ToolFormat GetToolFormat()
        {
            // Claude uses OpenAI format for tools
            return ToolFormat.Claude;
        }
    }
    internal class NotEnoughTokensForCachingException : Exception
    {
        public NotEnoughTokensForCachingException(string message) : base(message) { }
    }

    internal class StreamProcessor
    {
        private bool usePromptCaching;
        public event EventHandler<string> StreamingTextReceived;

        public StreamProcessor(bool usePromptCaching)
        {
            this.usePromptCaching = usePromptCaching;
        }

        public async Task<StreamProcessingResult> ProcessStream(Stream stream, CancellationToken cancellationToken)
        {
            var responseBuilder = new StringBuilder();
            var lineBuilder = new StringBuilder();
            var buffer = new byte[48];
            var decoder = Encoding.UTF8.GetDecoder();

            int? inputTokens = null;
            int? outputTokens = null;
            int? cacheCreationInputTokens = null;
            int? cacheReadInputTokens = null;

            while (true)
            {
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken); // throws if cancelled...
                if (bytesRead == 0) break;

                char[] chars = new char[decoder.GetCharCount(buffer, 0, bytesRead)];
                decoder.GetChars(buffer, 0, bytesRead, chars, 0);

                foreach (char c in chars)
                {
                    if (c == '\n')
                    {
                        ProcessLine(lineBuilder.ToString(), responseBuilder, ref inputTokens, ref outputTokens, ref cacheCreationInputTokens, ref cacheReadInputTokens);
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
                ProcessLine(lineBuilder.ToString(), responseBuilder, ref inputTokens, ref outputTokens, ref cacheCreationInputTokens, ref cacheReadInputTokens);
            }

            return new StreamProcessingResult
            {
                ResponseText = responseBuilder.ToString(),
                InputTokens = inputTokens,
                OutputTokens = outputTokens,
                CacheCreationInputTokens = cacheCreationInputTokens,
                CacheReadInputTokens = cacheReadInputTokens
            };
        }

        private void ProcessLine(string line, StringBuilder responseBuilder, ref int? inputTokens, ref int? outputTokens, ref int? cacheCreationInputTokens, ref int? cacheReadInputTokens)
        {

            // These variables are now passed as ref parameters
            // could contain data: {"type":"error","error":{"details":null,"type":"overloaded_error","message":"Overloaded"}              }

            if (line.StartsWith("data: "))
            {
                var data = line.Substring(6);
                if (data == "[DONE]") return;

                try
                {
                    var eventData = JsonConvert.DeserializeObject<JObject>(data);
                    if (eventData["type"].ToString() == "content_block_delta")
                    {
                        var text = eventData["delta"]["text"]?.ToString();
                        if (text == null)
                        {
                            text = eventData["delta"]["partial_json"]?.ToString();
                        }

                        Debug.WriteLine(text);
                        
                        StreamingTextReceived?.Invoke(this, text);
                        responseBuilder.Append(text);
                    }
                    else if (eventData["type"].ToString() == "message_start")
                    {
                        inputTokens = eventData["message"]["usage"]["input_tokens"].Value<int>();

                        if (eventData["message"]["usage"]["output_tokens"] != null)
                        {
                            outputTokens = eventData["message"]["usage"]["output_tokens"].Value<int>();
                        }

                        if (eventData["message"]["usage"]["cache_creation_input_tokens"] != null)
                        {
                            cacheCreationInputTokens = eventData["message"]["usage"]["cache_creation_input_tokens"].Value<int>();
                        }

                        if (eventData["message"]["usage"]["cache_read_input_tokens"] != null)
                        {
                            cacheReadInputTokens = eventData["message"]["usage"]["cache_read_input_tokens"].Value<int>();
                        }
                    }
                    else if (eventData["type"].ToString() == "message_delta")
                    {
                        outputTokens = eventData["usage"]["output_tokens"].Value<int>();
                        if (eventData["usage"]["cache_creation_input_tokens"] != null)
                        {
                            cacheCreationInputTokens = eventData["usage"]["cache_creation_input_tokens"].Value<int>();
                        }

                        if (eventData["usage"]["cache_read_input_tokens"] != null)
                        {
                            cacheReadInputTokens = eventData["usage"]["cache_read_input_tokens"].Value<int>();
                        }
                    }
                    else if (eventData["type"].ToString() == "error")
                    {
                        var errorMessage = eventData["error"]["message"].ToString();
                        if (usePromptCaching && errorMessage.Contains("at least 1024 tokens"))
                        {
                            // Input isn't long enough to cache. We need to restart the stream without caching.
                            throw new NotEnoughTokensForCachingException(errorMessage);
                        }
                        StreamingTextReceived?.Invoke(this, errorMessage);
                        responseBuilder.Append(errorMessage);
                    }
                    else
                    {

                    }
                }
                catch (JsonException ex)
                {
                    // Handle JSON parsing error
                    Console.WriteLine($"Error parsing JSON: {ex.Message}");
                }
            }
        }
    }


    internal class StreamProcessingResult
    {
        public string ResponseText { get; set; }
        public int? InputTokens { get; set; }
        public int? OutputTokens { get; set; }
        public int? CacheCreationInputTokens { get; set; }
        public int? CacheReadInputTokens { get; set; }
    }
}