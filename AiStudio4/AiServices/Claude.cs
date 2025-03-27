using AiStudio4.Convs;
using AiStudio4.Core.Tools;
using AiStudio4.DataModels;
using ModelContextProtocol.Protocol.Messages;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharedClasses.Providers;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using AiStudio4.Core.Models;

namespace AiStudio4.AiServices
{
    internal class Claude : AiServiceBase
    {
        public ToolResponse ToolResponseSet { get; set; } = new ToolResponse { Tools = new List<ToolResponseItem>() };

        private string oneOffPreFill;
        
        public void SetOneOffPreFill(string prefill) => oneOffPreFill = prefill;

        protected override void ConfigureHttpClientHeaders(ApiSettings apiSettings)
        {
            base.ConfigureHttpClientHeaders(apiSettings);
            client.DefaultRequestHeaders.Add("x-api-key", ApiKey);
            client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

            var betaFeatures = new List<string>();

            //if (apiSettings.UsePromptCaching)
            //    betaFeatures.Add("prompt-caching-2024-07-31");

            if (ApiModel == "claude-3-7-sonnet-20250219" || ApiModel == "claude-3-7-sonnet-latest")
                betaFeatures.Add("output-128k-2025-02-19");

            betaFeatures.Add("token-efficient-tools-2025-02-19");

            if (betaFeatures.Any())
                client.DefaultRequestHeaders.Add("anthropic-beta", string.Join(", ", betaFeatures));
        }

        protected override JObject CreateRequestPayload(string modelName, LinearConv conv, bool useStreaming, ApiSettings apiSettings)
        {
            var req = new JObject
            {
                ["model"] = modelName,
                ["max_tokens"] = (ApiModel == "claude-3-7-sonnet-20250219" || ApiModel == "claude-3-7-sonnet-latest") ? 64000 : 8192,
                ["stream"] = useStreaming,
                ["temperature"] = apiSettings.Temperature,
            };

            if (!string.IsNullOrWhiteSpace(conv.systemprompt))
                req["system"] = conv.systemprompt;

            var messagesArray = new JArray();
            int userMessageCount = 0;
            int totalUserMessages = conv.messages.Count(m => m.role.ToLower() == "user");

            foreach (var message in conv.messages)
            {
                var contentArray = new JArray();

                // Handle legacy single image
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
                
                // Handle multiple attachments
                if (message.attachments != null && message.attachments.Any())
                {
                    foreach (var attachment in message.attachments)
                    {
                        if (attachment.Type.StartsWith("image/") || attachment.Type == "application/pdf")
                        {
                            contentArray.Add(new JObject
                            {
                                ["type"] = attachment.Type == "application/pdf" ? "document" : "image",
                                ["source"] = new JObject
                                {
                                    ["type"] = "base64",
                                    ["media_type"] = attachment.Type,
                                    ["data"] = attachment.Content
                                }
                            });
                        }
                        // Additional attachment types could be handled here
                    }
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

                if (apiSettings.UsePromptCaching && message.role.ToLower() == "user")
                {
                    userMessageCount++;
                }

                // Add cache_control to the last 4 user messages
                if (apiSettings.UsePromptCaching && message.role.ToLower() == "user" && 
                    (totalUserMessages - userMessageCount) < 4)
                {
                    messageObject["content"][0]["cache_control"] = new JObject { ["type"] = "ephemeral" };
                }
                
                messagesArray.Add(messageObject);
            }

            if (oneOffPreFill != null)
            {
                messagesArray.Add(new JObject
                {
                    ["role"] = "assistant",
                    ["content"] = new JArray { new JObject { ["type"] = "text", ["text"] = oneOffPreFill.Trim() } }
                });
            }

            req["messages"] = messagesArray;
            return req;
        }

        // Override the FetchResponseInternal method to implement Claude-specific logic
        protected override async Task<AiResponse> FetchResponseInternal(AiRequestOptions options)
        {
            // Reset ToolResponseSet for each new request
            ToolResponseSet = new ToolResponse { Tools = new List<ToolResponseItem>() };
            
            InitializeHttpClient(options.ServiceProvider, options.Model, options.ApiSettings);

            // Apply custom system prompt if provided
            if (!string.IsNullOrEmpty(options.CustomSystemPrompt))
                options.Conv.systemprompt = options.CustomSystemPrompt;

            var req = CreateRequestPayload(ApiModel, options.Conv, options.UseStreaming, options.ApiSettings);

                await AddToolsToRequestAsync(req, options.ToolIds);

                if (req["tools"] != null)
                    req["tool_choice"] = new JObject { ["type"] = "any" };

            if (options.AddEmbeddings)
                await AddEmbeddingsToRequest(req, options.Conv, options.ApiSettings, options.MustNotUseEmbedding);

            // bodge for Everything MCP

            if (req["tools"] != null && req["tools"][0]["description"].ToString().StartsWith("Universal file search tool"))
            {
                req["tools"][0]["input_schema"] = (JObject)(JsonConvert.DeserializeObject(@"
                {
                ""type"": ""object"",
                ""$defs"": {
                    ""WindowsSortOption"": {
                        ""description"": ""Sort options for Windows Everything search."",
                        ""enum"": [1, 2, 3, 4, 5, 6, 7, 8, 11, 12, 13, 14],
                        ""title"": ""WindowsSortOption"",
                        ""type"": ""integer""
                    }
                },
                ""properties"": {
                    ""base"": {
                        ""description"": ""Base search parameters common to all platforms."",
                        ""properties"": {
                            ""query"": {
                                ""description"": ""Search query string. See platform-specific documentation for syntax details."",
                                ""title"": ""Query"",
                                ""type"": ""string""
                            },
                            ""max_results"": {
                                ""default"": 100,
                                ""description"": ""Maximum number of results to return (1-1000)"",
                                ""maximum"": 1000,
                                ""minimum"": 1,
                                ""title"": ""Max Results"",
                                ""type"": ""integer""
                            }
                        },
                        ""required"": [""query""],
                        ""title"": ""BaseSearchQuery"",
                        ""type"": ""object""
                    },
                    ""windows_params"": {
                        ""description"": ""Windows-specific search parameters for Everything SDK."",
                        ""properties"": {
                            ""match_path"": {
                                ""default"": false,
                                ""description"": ""Match against full path instead of filename only"",
                                ""title"": ""Match Path"",
                                ""type"": ""boolean""
                            },
                            ""match_case"": {
                                ""default"": false,
                                ""description"": ""Enable case-sensitive search"",
                                ""title"": ""Match Case"",
                                ""type"": ""boolean""
                            },
                            ""match_whole_word"": {
                                ""default"": false,
                                ""description"": ""Match whole words only"",
                                ""title"": ""Match Whole Word"",
                                ""type"": ""boolean""
                            },
                            ""match_regex"": {
                                ""default"": false,
                                ""description"": ""Enable regex search"",
                                ""title"": ""Match Regex"",
                                ""type"": ""boolean""
                            },
                            ""sort_by"": {
                                ""$ref"": ""#/$defs/WindowsSortOption"",
                                ""default"": 1,
                                ""description"": ""Sort order for results""
                            }
                        },
                        ""title"": ""WindowsSpecificParams"",
                        ""type"": ""object""
                    }
                },
                ""required"": [""base""]
            }"));
            }

            var json = JsonConvert.SerializeObject(req);//, Formatting.Indented).Replace("\r\n","\n");
            //File.WriteAllText($"request_{DateTime.Now:yyyyMMddHHmmss}.json", json);



            var content = new StringContent(json, Encoding.UTF8, "application/json");

            while (true)
            {
                try
                {
                    var response = await HandleResponse(content, options.UseStreaming, options.CancellationToken);
                    if (oneOffPreFill != null)
                    {
                        response.ResponseText = $"{oneOffPreFill}{response.ResponseText}";
                        oneOffPreFill = null;
                    }
                    return response;
                }
                catch (NotEnoughTokensForCachingException)
                {
                    if (options.ApiSettings.UsePromptCaching)
                    {
                        json = RemoveCachingFromJson(json);
                        options.ApiSettings.UsePromptCaching = false;
                        content = new StringContent(json, Encoding.UTF8, "application/json");
                    }
                    else
                        throw;
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
                ),
                ChosenTool = streamProcessor.ChosenTool,
                ToolResponseSet = streamProcessor.ToolResponseSet
            };
        }

        protected override async Task<AiResponse> HandleNonStreamingResponse(HttpContent content, CancellationToken cancellationToken)
        {
            using var response = await SendRequest(content, cancellationToken);
            var responseString = await response.Content.ReadAsStringAsync(cancellationToken);
            var completion = JsonConvert.DeserializeObject<JObject>(responseString);

            if (completion["type"]?.ToString() == "error")
            {
                var errorMsg = completion["error"]["message"].ToString();
                if (errorMsg.Contains("at least 1024 tokens"))
                    throw new NotEnoughTokensForCachingException(errorMsg);
                else if (completion["error"]["message"].ToString().StartsWith("Overloaded"))
                {
                    //var result = MessageBox.Show("Claude reports that it's overloaded. Would you like to retry?", "Server Overloaded", MessageBoxButtons.YesNo);
                    //if (result == DialogResult.Yes)
                    //{
                    //    return await HandleNonStreamingResponse( content, cancellationToken);
                    //}
                }

                return new AiResponse { ResponseText = "error - " + errorMsg, Success = false };
            }

            var chosenTool = ExtractChosenToolFromCompletion(completion);
            
            // Process tool calls if present
            if (chosenTool != null)
            {
                // Handle tool calls and populate ToolResponseSet
                if (completion["content"] != null && completion["content"][0]["type"]?.ToString() == "tool_use")
                {
                    string toolName = completion["content"][0]["name"]?.ToString();
                    string toolArguments = completion["content"][0]["input"].ToString();
                    
                    ToolResponseSet.Tools.Add(new ToolResponseItem
                    {
                        ToolName = toolName,
                        ResponseText = toolArguments
                    });
                }
                else if (completion["tool_calls"] != null && completion["tool_calls"].Any())
                {
                    // Process all tool calls in the response
                    foreach (var toolCall in completion["tool_calls"])
                    {
                        string toolName = toolCall["function"]["name"]?.ToString();
                        string toolArguments = toolCall["function"]["arguments"]?.ToString();
                        
                        ToolResponseSet.Tools.Add(new ToolResponseItem
                        {
                            ToolName = toolName,
                            ResponseText = toolArguments
                        });
                    }
                }
            }
            
            return new AiResponse
            {
                ResponseText = ExtractResponseTextFromCompletion(completion),
                Success = true,
                TokenUsage = ExtractTokenUsageFromCompletion(completion),
                ChosenTool = chosenTool,
                ToolResponseSet = ToolResponseSet
            };
        }

        private string RemoveCachingFromJson(string json)
        {
            var jObject = JObject.Parse(json);
            var messages = (JArray)jObject["messages"];

            if (messages != null)
            {
                foreach (var message in messages)
                {
                    var content = message["content"] as JArray;
                    if (content != null)
                    {
                        foreach (var item in content)
                            item["cache_control"]?.Parent.Remove();
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

        private string ExtractChosenToolFromCompletion(JObject completion)
        {
            if (completion["content"] != null && completion["content"][0]["type"]?.ToString() == "tool_use")
            {
                return completion["content"][0]["name"]?.ToString();
            }
            else if (completion["tool_calls"] != null && completion["tool_calls"].Any())
            {
                return completion["tool_calls"][0]["function"]["name"]?.ToString();
            }
            return null;
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

        private async Task AddEmbeddingsToRequest(JObject req, LinearConv conv, ApiSettings apiSettings, bool mustNotUseEmbedding)
        {
            // Implementation commented out in original code
        }

        protected override async Task AddToolsToRequestAsync(JObject req, List<string> toolIDs)
        {


            var toolRequestBuilder = new ToolRequestBuilder(ToolService, McpService);

            foreach (var toolId in toolIDs)
                await toolRequestBuilder.AddToolToRequestAsync(req, toolId, GetToolFormat());

            await toolRequestBuilder.AddMcpServiceToolsToRequestAsync(req, GetToolFormat());



        }

        protected override ToolFormat GetToolFormat() => ToolFormat.Claude;
    }

    internal class NotEnoughTokensForCachingException : Exception
    {
        public NotEnoughTokensForCachingException(string message) : base(message) { }
    }

    internal class StreamProcessor
    {
        private readonly bool usePromptCaching;
        public event EventHandler<string> StreamingTextReceived;

        public ToolResponse ToolResponseSet { get; set; } = new ToolResponse { Tools = new List<ToolResponseItem>() };

        public StreamProcessor(bool usePromptCaching) => this.usePromptCaching = usePromptCaching;

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
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                if (bytesRead == 0) break;

                char[] chars = new char[decoder.GetCharCount(buffer, 0, bytesRead)];
                decoder.GetChars(buffer, 0, bytesRead, chars, 0);

                foreach (char c in chars)
                {
                    if (c == '\n')
                    {
                        ProcessLine(lineBuilder.ToString(), responseBuilder, ref inputTokens, ref outputTokens,
                            ref cacheCreationInputTokens, ref cacheReadInputTokens);
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
                ProcessLine(lineBuilder.ToString(), responseBuilder, ref inputTokens, ref outputTokens,
                    ref cacheCreationInputTokens, ref cacheReadInputTokens);
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

        public string ChosenTool { get; set; } = null;

        private ToolResponseItem currentResponseItem = null;

        private void ProcessLine(string line, StringBuilder responseBuilder, ref int? inputTokens, ref int? outputTokens,
            ref int? cacheCreationInputTokens, ref int? cacheReadInputTokens)
        {
            if (!line.StartsWith("data: ")) return;

            var data = line.Substring(6);
            if (data == "[DONE]") return;

            try
            {
                var eventData = JsonConvert.DeserializeObject<JObject>(data);
                var eventType = eventData["type"].ToString();

                switch (eventType)
                {/*{
                    "type": "content_block_start",
  "index": 0,
  "content_block": {
                        "type": "tool_use",
    "id": "toolu_01FbXDHWtGJh7WqjbBEGyQxR",
    "name": "codeblock",
    "input": { }
                    }*/
                    case "content_block_start":
                        var contentBlockType = eventData["content_block"]?["type"];
                        if(contentBlockType.ToString() == "tool_use")
                        {
                            ChosenTool = eventData["content_block"]?["name"].ToString();
                            
                            // Create a new ToolResponseItem when a tool is chosen
                            var toolResponseItem = new ToolResponseItem
                            {
                                ToolName = ChosenTool,
                                ResponseText = ""
                            };

                            currentResponseItem = toolResponseItem;

                            ToolResponseSet.Tools.Add(toolResponseItem);

                        }
                        break;
                    case "content_block_delta":
                        var text = eventData["delta"]["text"]?.ToString() ?? eventData["delta"]["partial_json"]?.ToString();
                        
                        Debug.WriteLine(text);

                        if (currentResponseItem != null)
                            currentResponseItem.ResponseText += text;

                        StreamingTextReceived?.Invoke(this, text);
                        responseBuilder.Append(text);
                        break;
                    case "content_block_end":

                        break;
                    case "message_start":
                        inputTokens = eventData["message"]["usage"]["input_tokens"].Value<int>();

                        if (eventData["message"]["usage"]["output_tokens"] != null)
                            outputTokens = eventData["message"]["usage"]["output_tokens"].Value<int>();

                        if (eventData["message"]["usage"]["cache_creation_input_tokens"] != null)
                            cacheCreationInputTokens = eventData["message"]["usage"]["cache_creation_input_tokens"].Value<int>();

                        if (eventData["message"]["usage"]["cache_read_input_tokens"] != null)
                            cacheReadInputTokens = eventData["message"]["usage"]["cache_read_input_tokens"].Value<int>();
                        break;

                    case "message_delta":
                        outputTokens = eventData["usage"]["output_tokens"].Value<int>();

                        if (eventData["usage"]["cache_creation_input_tokens"] != null)
                            cacheCreationInputTokens = eventData["usage"]["cache_creation_input_tokens"].Value<int>();

                        if (eventData["usage"]["cache_read_input_tokens"] != null)
                            cacheReadInputTokens = eventData["usage"]["cache_read_input_tokens"].Value<int>();
                        break;

                    case "error":
                        var errorMessage = eventData["error"]["message"].ToString();
                        if (usePromptCaching && errorMessage.Contains("at least 1024 tokens"))
                            throw new NotEnoughTokensForCachingException(errorMessage);

                        StreamingTextReceived?.Invoke(this, errorMessage);
                        responseBuilder.Append(errorMessage);
                        break;
                }
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Error parsing JSON: {ex.Message}");
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