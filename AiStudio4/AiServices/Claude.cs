using AiStudio4.Convs;
using AiStudio4.Core.Tools;
using AiStudio4.DataModels;
using AiStudio4.Core.Models;
using AiStudio4.InjectedDependencies;


using SharedClasses.Providers;


using System.Net.Http;

using System.Text.RegularExpressions;

using static System.Net.Mime.MediaTypeNames;

namespace AiStudio4.AiServices
{
    internal class Claude : AiServiceBase
    {
        private string oneOffPreFill;
        private readonly ClaudeToolResponseProcessor _toolResponseProcessor;
        private readonly Queue<string> _toolIdQueue = new Queue<string>();
        
        public Claude()
        {
            _toolResponseProcessor = new ClaudeToolResponseProcessor();
        }
        
        public void SetOneOffPreFill(string prefill) => oneOffPreFill = prefill;

        protected override ProviderFormat GetProviderFormat() => ProviderFormat.Claude;

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

        protected override async Task AddEmbeddingsToRequest(JObject request, LinearConv conv, ApiSettings apiSettings, bool mustNotUseEmbedding)
        {
            // Claude-specific embeddings implementation (currently empty in original)
            await Task.CompletedTask;
        }

        protected override JObject CreateRequestPayload(string modelName, LinearConv conv, ApiSettings apiSettings)
        {
            return RequestPayloadBuilder.Create(ProviderFormat.Claude)
                .WithModel(modelName)
                .WithConversation(conv)
                .WithApiSettings(apiSettings)
                .WithSystemPrompt(conv.systemprompt)
                .WithGenerationConfig()
                .WithMessages()
                .WithPromptCaching(apiSettings.UsePromptCaching)
                .WithOneOffPreFill(oneOffPreFill)
                .Build();
        }

        // Override the FetchResponseInternal method to implement Claude-specific logic
        protected override async Task<AiResponse> FetchResponseInternal(AiRequestOptions options, bool forceNoTools = false)
        {
            // Reset ToolResponseSet for each new request
            ToolResponseSet = new ToolResponse { Tools = new List<ToolResponseItem>() };
            
            InitializeHttpClient(options.ServiceProvider, options.Model, options.ApiSettings);

            return await MakeStandardApiCall(options, async (content) =>
            {
                while (true)
                {
                    try
                    {
                        if (oneOffPreFill != null)
                        {
                            oneOffPreFill = null;
                        }
                        return await HandleResponse(options, content);
                    }
                    catch (NotEnoughTokensForCachingException)
                    {
                        if (options.ApiSettings.UsePromptCaching)
                        {
                            var json = await content.ReadAsStringAsync();
                            json = RemoveCachingFromJson(json);
                            options.ApiSettings.UsePromptCaching = false;
                            content = new StringContent(json, Encoding.UTF8, "application/json");
                        }
                        else
                            throw;
                    }
                }
            }, forceNoTools);
        }

        protected override async Task CustomizeRequest(JObject request, AiRequestOptions options)
        {
            // Claude-specific request customizations can go here
            await base.CustomizeRequest(request, options);
        }

        public override async Task<AiResponse> FetchResponseWithToolLoop(AiRequestOptions options, Core.Interfaces.IToolExecutor toolExecutor, v4BranchedConv branchedConv, string parentMessageId, string assistantMessageId, string clientId)
        {
            InitializeHttpClient(options.ServiceProvider, options.Model, options.ApiSettings);

            return await ExecuteCommonToolLoop(
                options,
                toolExecutor,
                makeApiCall: async (opts) => await MakeClaudeApiCall(opts),
                createAssistantMessage: CreateClaudeAssistantMessage,
                createToolResultMessage: CreateClaudeToolResultMessage,
                options.MaxToolIterations ?? 10);
        }

        private async Task<AiResponse> MakeClaudeApiCall(AiRequestOptions options)
        {
            return await MakeStandardApiCall(options, async (content) =>
            {
                // Handle potential caching errors
                while (true)
                {
                    try
                    {
                        return await HandleStreamingResponse(content, options.CancellationToken, options.OnStreamingUpdate, options.OnStreamingComplete);
                    }
                    catch (NotEnoughTokensForCachingException)
                    {
                        if (options.ApiSettings.UsePromptCaching)
                        {
                            var json = await content.ReadAsStringAsync();
                            json = RemoveCachingFromJson(json);
                            options.ApiSettings.UsePromptCaching = false;
                            content = new StringContent(json, Encoding.UTF8, "application/json");
                        }
                        else
                            throw;
                    }
                }
            });
        }

        protected override async Task ConfigureToolChoice(JObject request)
        {
            if (request["tools"] != null)
                request["tool_choice"] = new JObject { ["type"] = "auto" };
            await Task.CompletedTask;
        }

        private LinearConvMessage CreateClaudeAssistantMessage(AiResponse response)
        {
            var assistantContent = new JArray();
            _toolIdQueue.Clear(); // Clear previous tool IDs
            
            // Add any text content first
            var textContent = response.ContentBlocks?.FirstOrDefault(c => c.ContentType == Core.Models.ContentType.Text)?.Content;
            if (!string.IsNullOrEmpty(textContent))
            {
                assistantContent.Add(new JObject 
                { 
                    ["type"] = "text", 
                    ["text"] = textContent 
                });
            }

            // Add tool use blocks
            foreach (var toolCall in response.ToolResponseSet.Tools)
            {
                var toolId = toolCall.ToolId ?? $"tool_{Guid.NewGuid():N}"[..15]; // Use Claude's ID or fallback
                _toolIdQueue.Enqueue(toolId); // Store in order for later use
                System.Diagnostics.Debug.WriteLine($"🔧 CLAUDE ASSISTANT: Creating tool_use with id: {toolId}, tool: {toolCall.ToolName}");
                
                assistantContent.Add(new JObject
                {
                    ["type"] = "tool_use",
                    ["id"] = toolId,
                    ["name"] = toolCall.ToolName,
                    ["input"] = JObject.Parse(toolCall.ResponseText)
                });
            }

            var result = new LinearConvMessage
            {
                role = "assistant",
                content = assistantContent.ToString()
            };
            
            System.Diagnostics.Debug.WriteLine($"🔧 CLAUDE ASSISTANT MESSAGE: {result.content}");
            return result;
        }

        private LinearConvMessage CreateClaudeToolResultMessage(List<ContentBlock> toolResultBlocks)
        {
            var toolResults = new JArray();
            
            foreach (var block in toolResultBlocks)
            {
                if (block.ContentType == ContentType.ToolResponse)
                {
                    var toolData = JsonConvert.DeserializeObject<dynamic>(block.Content);
                    var toolName = toolData.toolName?.ToString();
                    
                    // Use the next tool ID from the queue (preserves order)
                    var toolResultId = _toolIdQueue.Count > 0 
                        ? _toolIdQueue.Dequeue() 
                        : $"tool_{Guid.NewGuid():N}"[..15];
                    
                    System.Diagnostics.Debug.WriteLine($"🔧 CLAUDE TOOL RESULT: Creating tool_result with tool_use_id: {toolResultId}, tool: {toolName}");
                    
                    toolResults.Add(new JObject
                    {
                        ["type"] = "tool_result",
                        ["tool_use_id"] = toolResultId,
                        ["content"] = toolData.result?.ToString(),
                        ["is_error"] = !(bool)(toolData.success ?? false)
                    });
                }
            }
            
            var result = new LinearConvMessage
            {
                role = "user",
                content = toolResults.ToString()
            };
            
            System.Diagnostics.Debug.WriteLine($"🔧 CLAUDE TOOL RESULT MESSAGE: {result.content}");
            return result;
        }

        protected override LinearConvMessage CreateUserInterjectionMessage(string interjectionText)
        {
            return new LinearConvMessage
            {
                role = "user",
                content = interjectionText
            };
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
                // Check if response text contains JSON array with tool calls
                var parsedResponse = TryParseJsonArrayResponse(result.ResponseText);
                if (parsedResponse != null)
                {
                    return parsedResponse;
                }

                return new AiResponse
                {
                    ContentBlocks = new List<ContentBlock> { new ContentBlock { Content = result.ResponseText, ContentType = ContentType.Text } },
                    Success = true,
                    TokenUsage = CreateTokenUsage(
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
                
                var tokenUsage = CreateTokenUsage(
                    result.InputTokens?.ToString(),
                    result.OutputTokens?.ToString(),
                    result.CacheCreationInputTokens?.ToString(),
                    result.CacheReadInputTokens?.ToString()
                );
                
                return HandleCancellation(
                    result.ResponseText,
                    tokenUsage,
                    streamProcessor.ToolResponseSet,
                    streamProcessor.ChosenTool
                );
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


        protected override async Task AddToolsToRequestAsync(JObject req, List<string> toolIDs)
        {


            var toolRequestBuilder = new ToolRequestBuilder(ToolService, McpService);

            foreach (var toolId in toolIDs)
                await toolRequestBuilder.AddToolToRequestAsync(req, toolId, GetToolFormat());

            await toolRequestBuilder.AddMcpServiceToolsToRequestAsync(req, GetToolFormat());



        }

        protected override ToolFormat GetToolFormat() => ToolFormat.Claude;

        private AiResponse TryParseJsonArrayResponse(string responseText)
        {
            try
            {
                // Check if response is a JSON array
                if (!responseText.Trim().StartsWith("[") || !responseText.Trim().EndsWith("]"))
                    return null;

                var jsonArray = JArray.Parse(responseText);
                var contentBlocks = new List<ContentBlock>();
                var toolResponseSet = new ToolResponse { Tools = new List<ToolResponseItem>() };

                foreach (var item in jsonArray)
                {
                    var itemType = item["type"]?.ToString();
                    
                    if (itemType == "text")
                    {
                        // Add text content block
                        var textContent = item["text"]?.ToString();
                        if (!string.IsNullOrEmpty(textContent))
                        {
                            contentBlocks.Add(new ContentBlock
                            {
                                Content = textContent,
                                ContentType = ContentType.Text
                            });
                        }
                    }
                    else if (itemType == "tool_use")
                    {
                        // Add tool call content block
                        var toolName = item["name"]?.ToString();
                        var toolInput = item["input"]?.ToString();
                        
                        if (!string.IsNullOrEmpty(toolName))
                        {
                            contentBlocks.Add(new ContentBlock
                            {
                                Content = JsonConvert.SerializeObject(new { toolName = toolName, parameters = toolInput }),
                                ContentType = ContentType.Tool
                            });

                            // Add to tool response set for execution
                            toolResponseSet.Tools.Add(new ToolResponseItem
                            {
                                ToolName = toolName,
                                ResponseText = toolInput ?? "{}"
                            });
                        }
                    }
                }

                Debug.WriteLine($"🔍 PARSED JSON ARRAY: {contentBlocks.Count} content blocks, {toolResponseSet.Tools.Count} tools");

                return new AiResponse
                {
                    ContentBlocks = contentBlocks,
                    Success = true,
                    ToolResponseSet = toolResponseSet,
                    TokenUsage = new TokenUsage("0", "0", "0", "0"), // Unknown from JSON format
                    IsCancelled = false
                };
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Failed to parse JSON array response: {ex.Message}");
                return null;
            }
        }
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
                            var claudeToolId = eventData["content_block"]?["id"]?.ToString();
                            System.Diagnostics.Debug.WriteLine($"🔧 CLAUDE STREAMING: Received tool_use with id: {claudeToolId}, tool: {ChosenTool}");

                            // Create a new ToolResponseItem when a tool is chosen
                            var toolResponseItem = new ToolResponseItem
                            {
                                ToolName = ChosenTool,
                                ResponseText = "",
                                ToolId = claudeToolId // Store Claude's actual tool ID
                            };

                            currentResponseItem = toolResponseItem;

                            ToolResponseSet.Tools.Add(toolResponseItem);

                        }
                        else
                            ChosenTool = null;
                        break;
                    case "content_block_delta":
                        var text = eventData["delta"]["text"]?.ToString() ?? eventData["delta"]["partial_json"]?.ToString();
                        
                        //Debug.WriteLine(text);

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
                Debug.WriteLine($"Error parsing JSON: {ex.Message}");
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
