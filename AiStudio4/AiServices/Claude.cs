using AiStudio4.Convs;
using AiStudio4.Core.Tools;
using AiStudio4.DataModels;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharedClasses.Providers;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using AiStudio4.Core.Models;
using static System.Net.Mime.MediaTypeNames;

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

        protected override JObject CreateRequestPayload(string modelName, LinearConv conv, ApiSettings apiSettings)
        {
            var req = new JObject
            {
                ["model"] = modelName,
                ["max_tokens"] = (ApiModel == "claude-3-7-sonnet-20250219" || ApiModel == "claude-3-7-sonnet-latest") ? 64000 : 8192,
                ["stream"] = true,
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
        protected override async Task<AiResponse> FetchResponseInternal(AiRequestOptions options, bool forceNoTools = false)
        {
            // Reset ToolResponseSet for each new request
            ToolResponseSet = new ToolResponse { Tools = new List<ToolResponseItem>() };
            
            InitializeHttpClient(options.ServiceProvider, options.Model, options.ApiSettings);

            // Apply custom system prompt if provided
            if (!string.IsNullOrEmpty(options.CustomSystemPrompt))
                options.Conv.systemprompt = options.CustomSystemPrompt;

            var req = CreateRequestPayload(ApiModel, options.Conv, options.ApiSettings);

            if (options.Model.AllowsTopP && options.ApiSettings.TopP > 0.0f && options.ApiSettings.TopP <= 1.0f)
            {
                req["top_p"] = options.ApiSettings.TopP;
            }

            if (!forceNoTools)
            {
                await AddToolsToRequestAsync(req, options.ToolIds);
            }

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
                    var response = await HandleResponse(options, content); // Pass options
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

        protected override async Task<AiResponse> HandleStreamingResponse(
            HttpContent content, 
            CancellationToken cancellationToken,
            Action<string> onStreamingUpdate, 
            Action onStreamingComplete)
        {
            using var response = await SendRequest(content, cancellationToken);
            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

            var streamProcessor = new StreamProcessor(true);
            streamProcessor.StreamingTextReceived += (s, e) => onStreamingUpdate?.Invoke(e); // Use callback

            StreamProcessingResult result = null;
            try
            {
                result = await streamProcessor.ProcessStream(stream, cancellationToken);
                // Normal completion
                onStreamingComplete?.Invoke(); // Use callback
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
                    ToolResponseSet = streamProcessor.ToolResponseSet,
                    IsCancelled = false // Explicitly false on normal completion
                };
            }
            catch (OperationCanceledException)
            {
                System.Diagnostics.Debug.WriteLine("Claude streaming cancelled.");
                // Cancellation happened, use the partial result from the processor
                result = streamProcessor.GetPartialResult(); // Need to add this method to StreamProcessor
                return new AiResponse
                {
                    ResponseText = result.ResponseText,
                    Success = true, // Indicate successful handling of cancellation
                    TokenUsage = new TokenUsage(
                        result.InputTokens?.ToString() ?? "0",
                        result.OutputTokens?.ToString() ?? "0",
                        result.CacheCreationInputTokens?.ToString() ?? "0",
                        result.CacheReadInputTokens?.ToString() ?? "0"
                    ),
                    ChosenTool = streamProcessor.ChosenTool,
                    ToolResponseSet = streamProcessor.ToolResponseSet,
                    IsCancelled = true
                };
            }
            catch (Exception ex)
            {
                // Handle other errors
                return HandleError(ex, "Error during streaming response");
            }
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

        private AiResponse HandleError(Exception ex, string additionalInfo = "")
        {
            string errorMessage = $"Error: {ex.Message}";
            if (!string.IsNullOrEmpty(additionalInfo))
            {
                errorMessage += $" Additional info: {additionalInfo}";
            }
            return new AiResponse { Success = false, ResponseText = errorMessage };
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
            StreamingTextReceived?.Invoke(this, "");
            var responseBuilder = new StringBuilder();
            var lineBuilder = new StringBuilder();
            var buffer = new byte[48];
            var decoder = Encoding.UTF8.GetDecoder();

            int? inputTokens = null;
            int? outputTokens = null;
            int? cacheCreationInputTokens = null;
            int? cacheReadInputTokens = null;

            try
            {
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
            }
            catch (OperationCanceledException)
            {
                // Process any remaining data in the line builder before returning
                if (lineBuilder.Length > 0)
                {
                    var line = lineBuilder.ToString();
                    if (!line.StartsWith("data: "))
                        line = "data: " + line;
                    ProcessLine(line, responseBuilder, ref inputTokens, ref outputTokens,
                        ref cacheCreationInputTokens, ref cacheReadInputTokens);
                }

                responseBuilder.AppendLine("\n\n<Cancelled>\n");

                // Return partial results on cancellation
                return new StreamProcessingResult
                {
                    ResponseText = responseBuilder.ToString(),
                    InputTokens = this.inputTokens,
                    OutputTokens = this.outputTokens,
                    CacheCreationInputTokens = this.cacheCreationInputTokens,
                    CacheReadInputTokens = this.cacheReadInputTokens,
                };
            }


            if (lineBuilder.Length > 0)
            {
                var line = lineBuilder.ToString();
                if (!line.StartsWith("data: "))
                    line = "data: " + line;

                ProcessLine(line, responseBuilder, ref inputTokens, ref outputTokens,
                    ref cacheCreationInputTokens, ref cacheReadInputTokens);
            }

            return new StreamProcessingResult
            {
                ResponseText = responseBuilder.ToString(),
                InputTokens = this.inputTokens,
                OutputTokens = this.outputTokens,
                CacheCreationInputTokens = this.cacheCreationInputTokens,
                CacheReadInputTokens = this.cacheReadInputTokens
            };
        }

        public string ChosenTool { get; set; } = null;

        // Fields to store partial results
        private StringBuilder responseBuilder = new StringBuilder();
        private int? inputTokens = null;
        private int? outputTokens = null;
        private int? cacheCreationInputTokens = null;
        private int? cacheReadInputTokens = null;

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
                {
                    case "content_block_start":
                        var contentBlockType = eventData["content_block"]?["type"];
                        if (contentBlockType.ToString() == "tool_use")
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
                        else
                            ChosenTool = null;
                        break;
                    case "content_block_delta":
                        var text = eventData["delta"]["text"]?.ToString() ?? eventData["delta"]["partial_json"]?.ToString();
                        
                        Debug.WriteLine(text);

                        StreamingTextReceived?.Invoke(this, text);

                        if (currentResponseItem != null)
                            currentResponseItem.ResponseText += text;

                        else
                        {
                            responseBuilder.Append(text); // Append to the class-level builder
                        }
                        break;
                    case "content_block_end":

                        break;
                    case "message_start":
                        this.inputTokens = eventData["message"]["usage"]["input_tokens"].Value<int>();

                        if (eventData["message"]["usage"]["output_tokens"] != null)
                            this.outputTokens = eventData["message"]["usage"]["output_tokens"].Value<int>();

                        if (eventData["message"]["usage"]["cache_creation_input_tokens"] != null)
                            this.cacheCreationInputTokens = eventData["message"]["usage"]["cache_creation_input_tokens"].Value<int>();

                        if (eventData["message"]["usage"]["cache_read_input_tokens"] != null)
                            this.cacheReadInputTokens = eventData["message"]["usage"]["cache_read_input_tokens"].Value<int>();
                        break;

                    case "message_delta":
                        this.outputTokens = eventData["usage"]["output_tokens"].Value<int>();

                        if (eventData["usage"]["cache_creation_input_tokens"] != null)
                            this.cacheCreationInputTokens = eventData["usage"]["cache_creation_input_tokens"].Value<int>();

                        if (eventData["usage"]["cache_read_input_tokens"] != null)
                            this.cacheReadInputTokens = eventData["usage"]["cache_read_input_tokens"].Value<int>();
                        break;

                    case "error":
                        var errorMessage = eventData["error"]["message"].ToString();
                        if (usePromptCaching && errorMessage.Contains("at least 1024 tokens"))
                            throw new NotEnoughTokensForCachingException(errorMessage);

                        StreamingTextReceived?.Invoke(this, errorMessage);
                        responseBuilder.Append(errorMessage); // Append error to the class-level builder
                        break;
                }
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Error parsing JSON: {ex.Message}");
            }
        }

        // Method to get the partial result if cancelled
        public StreamProcessingResult GetPartialResult()
        {
            return new StreamProcessingResult
            {
                ResponseText = responseBuilder.ToString(),
                InputTokens = inputTokens,
                OutputTokens = outputTokens,
                CacheCreationInputTokens = cacheCreationInputTokens,
                CacheReadInputTokens = cacheReadInputTokens,
                IsCancelled = true // Indicate this is a partial result due to cancellation
            };
        }
    }

    internal class StreamProcessingResult
    {
        public string ResponseText { get; set; }
        public int? InputTokens { get; set; }
        public int? OutputTokens { get; set; }
        public int? CacheCreationInputTokens { get; set; }
        public int? CacheReadInputTokens { get; set; }
        public bool IsCancelled { get; set; } = false; // Add IsCancelled flag
    }
}