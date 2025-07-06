using AiStudio4.Convs;
using System.Diagnostics;
using System.Text.RegularExpressions;
using AiStudio4.Core.Models;
using AiStudio4.Core.Tools;
using AiStudio4.DataModels;
using AiStudio4.InjectedDependencies;
using AiStudio4.Services.Interfaces;
using Newtonsoft.Json;
using SharedClasses.Providers;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;



namespace AiStudio4.AiServices
{
    public abstract class AiServiceBase : IAiService
    {
        public IToolService ToolService { get; set; }
        public IMcpService McpService { get; set; }
        
        
        

        public string ChosenTool { get; set; } = null;

        public ToolResponse ToolResponseSet { get; set; } = new ToolResponse { Tools = new List<ToolResponseItem>() };

        protected HttpClient client = new HttpClient();
        protected bool clientInitialised = false;

        public string ApiKey { get; set; }
        public string ApiUrl { get; set; }
        public string AdditionalParams { get; set; }
        public string ApiModel { get; set; }

        private bool _isInitialized { get; set; } = false;

        protected virtual void InitializeHttpClient(ServiceProvider serviceProvider,
            Model model, ApiSettings apiSettings, int timeout = 300)
        {
            if (!clientInitialised)
            {
                
                ApiKey = serviceProvider.ApiKey;
                ApiModel = model.ModelName;
                ApiUrl = serviceProvider.Url;
                AdditionalParams = model.AdditionalParams ?? "";

                if (clientInitialised && client.DefaultRequestHeaders.Authorization?.Parameter == ApiKey && client.DefaultRequestHeaders.Authorization?.Scheme == "Bearer")
                {
                    
                    
                    return;
                }

                ConfigureHttpClientHeaders(apiSettings); 

                client.Timeout = TimeSpan.FromSeconds(timeout);
                clientInitialised = true;
            }
        }

        protected virtual void ConfigureHttpClientHeaders(ApiSettings apiSettings)
        {
            if (!string.IsNullOrEmpty(ApiKey))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ApiKey);
            }
        }

        
        public virtual async Task<AiResponse> FetchResponse(AiRequestOptions options, bool forceNoTools = false)
        {
            
            return await FetchResponseInternal(options, forceNoTools);
        }

        /// <summary>
        /// Default implementation that falls back to legacy FetchResponse method.
        /// Override this in provider-specific implementations for provider-managed tool loops.
        /// </summary>
        public virtual async Task<AiResponse> FetchResponseWithToolLoop(AiRequestOptions options, Core.Interfaces.IToolExecutor toolExecutor, v4BranchedConv branchedConv, string parentMessageId, string assistantMessageId, string clientId)
        {
            // Fallback implementation - just use the legacy single-call method
            // This will work for providers that don't implement tool loops yet
            return await FetchResponse(options, forceNoTools: false);
        }

        /// <summary>
        /// Common tool loop implementation that can be used by provider-specific implementations.
        /// This method handles the common pattern of:
        /// 1. Making API calls with tools
        /// 2. Executing returned tool calls
        /// 3. Adding results to conversation
        /// 4. Repeating until no more tool calls or max iterations reached
        /// </summary>
        protected virtual async Task<AiResponse> ExecuteCommonToolLoop(
            AiRequestOptions options, 
            Core.Interfaces.IToolExecutor toolExecutor, 
            Func<AiRequestOptions, Task<AiResponse>> makeApiCall,
            Func<AiResponse, LinearConvMessage> createAssistantMessage,
            Func<List<ContentBlock>, LinearConvMessage> createToolResultMessage,
            int maxIterations = 10)
        {
            var linearConv = options.Conv;
            if (!string.IsNullOrEmpty(options.CustomSystemPrompt))
                linearConv.systemprompt = options.CustomSystemPrompt;
            
            for (int iteration = 0; iteration < maxIterations; iteration++)
            {
                // 1. Reset tool response set for this iteration
                ToolResponseSet = new ToolResponse { Tools = new List<ToolResponseItem>() };

                // Notify about assistant message with tool calls
                if (options.OnAssistantMessageCreated != null && options.BranchedConversation != null)
                {
                    var message = options.BranchedConversation.AddOrUpdateMessage(
                        v4BranchedConvMessageRole.Assistant,
                        options.AssistantMessageId,
                        new List<ContentBlock> { new ContentBlock { Content = "", ContentType = ContentType.Text } },
                        options.ParentMessageId,
                        new List<Attachment> { });

                    await options.OnAssistantMessageCreated(message);
                }

                // 2. Make API call
                var response = await makeApiCall(options);

                // Calculate cost info if calculator is provided
                TokenCost costInfo = null;
                if (options.CalculateCost != null && response.TokenUsage != null)
                {
                    costInfo = options.CalculateCost(response.TokenUsage, options.Model);
                    Console.WriteLine($"🔢 TOOL LOOP: Calculated cost for iteration {iteration} - InputTokens: {response.TokenUsage.InputTokens}, OutputTokens: {response.TokenUsage.OutputTokens}, TotalCost: {costInfo?.TotalCost}");
                }
                else
                {
                    Console.WriteLine($"🔢 TOOL LOOP: No cost calculated for iteration {iteration} - CalculateCost: {options.CalculateCost != null}, TokenUsage: {response.TokenUsage != null}");
                }

                // 3. Check for final answer (no tool calls)
                if (response.ToolResponseSet == null || !response.ToolResponseSet.Tools.Any())
                {
                    // This is the final response with no tool calls - add it to branched conversation
                    if (options.OnAssistantMessageCreated != null && options.BranchedConversation != null)
                    {
                        var message = options.BranchedConversation.AddOrUpdateMessage(
                            v4BranchedConvMessageRole.Assistant,
                            options.AssistantMessageId,
                            response.ContentBlocks,
                            options.ParentMessageId,
                            response.Attachments,
                            costInfo);
                        
                        await options.OnAssistantMessageCreated(message);
                    }
                    
                    return response; // We're done, return the final text response
                }

                // 4. Build complete content blocks including tool calls
                var contentBlocks = new List<ContentBlock>();
                
                // Add any existing content blocks first
                if (response.ContentBlocks != null)
                {
                    contentBlocks.AddRange(response.ContentBlocks);
                }
                
                // Extract task descriptions from tool calls and add System blocks
                foreach (var toolCall in response.ToolResponseSet.Tools)
                {
                    string taskDescription = null;
                    try
                    {
                        var toolCallArgs = JObject.Parse(toolCall.ResponseText);
                        if (toolCallArgs.TryGetValue("task_description", out JToken taskDescriptionToken))
                        {
                            taskDescription = taskDescriptionToken.ToString();
                            
                            // Add task description as System content block
                            contentBlocks.Add(new ContentBlock
                            {
                                ContentType = ContentType.System,
                                Content = $"{toolCall.ToolName}: {taskDescription}\n\n"
                            });
                        }
                    }
                    catch (JsonException)
                    {
                        // The response might not be a JSON object, which is fine for some tools.
                    }
                }
                
                // Only add tool call blocks if they're not already in the content blocks
                // Check if we already have tool blocks
                var hasToolBlocks = contentBlocks.Any(cb => cb.ContentType == ContentType.Tool);
                
                if (!hasToolBlocks)
                {
                    // Add tool call blocks
                    foreach (var toolCall in response.ToolResponseSet.Tools)
                    {
                        contentBlocks.Add(new ContentBlock
                        {
                            ContentType = ContentType.Tool,
                            Content = JsonConvert.SerializeObject(new
                            {
                                toolName = toolCall.ToolName,
                                parameters = toolCall.ResponseText
                            })
                        });
                    }
                }
                
                // Notify about assistant message with tool calls
                if (options.OnAssistantMessageCreated != null && options.BranchedConversation != null)
                {
                    var message = options.BranchedConversation.AddOrUpdateMessage(
                        v4BranchedConvMessageRole.Assistant,
                        options.AssistantMessageId,
                        contentBlocks,
                        options.ParentMessageId,
                        response.Attachments,
                        costInfo);
                    
                    Console.WriteLine($"🔢 TOOL LOOP: Created assistant message {options.AssistantMessageId} with cost info: {message.CostInfo?.TotalCost}");
                    
                    await options.OnAssistantMessageCreated(message);
                }

                // 5. Notify about tool calls
                if (options.OnToolCallsGenerated != null)
                {
                    await options.OnToolCallsGenerated(
                        options.AssistantMessageId,
                        contentBlocks,
                        response.ToolResponseSet.Tools);
                }

                // 6. Add the AI's response with tool calls to conversation history
                var assistantMessage = createAssistantMessage(response);
                linearConv.messages.Add(assistantMessage);

                // 7. Execute tools and collect results
                var toolResultBlocks = new List<ContentBlock>();
                var shouldStopLoop = false;

                foreach (var toolCall in response.ToolResponseSet.Tools)
                {
                    var context = new Core.Interfaces.ToolExecutionContext
                    {
                        ClientId = options.ClientId ?? "",
                        CancellationToken = options.CancellationToken,
                        BranchedConversation = options.BranchedConversation,
                        LinearConversation = linearConv,
                        CurrentIteration = iteration,
                        AssistantMessageId = options.AssistantMessageId,
                        ParentMessageId = options.ParentMessageId
                    };

                    var executionResult = await toolExecutor.ExecuteToolAsync(toolCall.ToolName, toolCall.ResponseText, context, options.CancellationToken);
                    
                    // Notify about tool execution
                    if (options.OnToolExecuted != null)
                    {
                        await options.OnToolExecuted(options.AssistantMessageId, toolCall.ToolName, executionResult);
                    }
                    
                    // Check if tool execution indicates we should stop the loop
                    if (!executionResult.ContinueProcessing || executionResult.UserInterjection != null)
                    {
                        shouldStopLoop = true;
                        
                        // If there's a user interjection, add it to the conversation
                        if (executionResult.UserInterjection != null)
                        {
                            var interjectionId = Guid.NewGuid().ToString();
                            
                            // Notify about interjection
                            if (options.OnUserInterjection != null)
                            {
                                await options.OnUserInterjection(interjectionId, executionResult.UserInterjection);
                            }
                            
                            // Add to linear conversation (provider-specific format)
                            var interjectionMessage = CreateUserInterjectionMessage(executionResult.UserInterjection);
                            linearConv.messages.Add(interjectionMessage);
                            
                            // Update parent for next iteration
                            if (options.BranchedConversation != null)
                            {
                                options.ParentMessageId = interjectionId;
                                options.AssistantMessageId = Guid.NewGuid().ToString();
                            }
                            
                            break; // Don't execute remaining tools, process the interjection
                        }
                    }

                    toolResultBlocks.Add(new ContentBlock
                    {
                        ContentType = ContentType.ToolResponse,
                        Content = JsonConvert.SerializeObject(new
                        {
                            toolName = toolCall.ToolName,
                            result = executionResult.ResultMessage,
                            success = executionResult.WasProcessed,
                            statusMessage = executionResult.StatusMessage,
                            outputFileType = executionResult.OutputFileType
                        })
                    });
                }
                
                // 8. Add tool results to conversations
                if (toolResultBlocks.Any())
                {
                    // Add to linear conversation for API (provider-specific format)
                    var toolResultMessage = createToolResultMessage(toolResultBlocks);
                    linearConv.messages.Add(toolResultMessage);
                    
                    if (shouldStopLoop)
                    {
                        // If stopping the loop, append tool results to the final AI message
                        if (options.BranchedConversation != null)
                        {
                            // Get the current assistant message and append tool results to it
                            var existingMessage = options.BranchedConversation.Messages.FirstOrDefault(m => m.Id == options.AssistantMessageId);
                            if (existingMessage != null)
                            {
                                // Combine existing content blocks with tool result blocks
                                var updatedContentBlocks = new List<ContentBlock>(existingMessage.ContentBlocks);
                                updatedContentBlocks.AddRange(toolResultBlocks);
                                
                                var updatedMessage = options.BranchedConversation.AddOrUpdateMessage(
                                    v4BranchedConvMessageRole.Assistant,
                                    options.AssistantMessageId,
                                    updatedContentBlocks,
                                    options.ParentMessageId,
                                    response.Attachments,
                                    costInfo);

                                // Notify about the updated assistant message
                                if (options.OnAssistantMessageCreated != null)
                                {
                                    await options.OnAssistantMessageCreated(updatedMessage);
                                }
                            }
                        }
                    }
                    else
                    {
                        // If continuing the loop, add tool results as a user message
                        if (options.BranchedConversation != null)
                        {
                            var toolResultMessageId = Guid.NewGuid().ToString();
                            
                            var toolResultBranchedMessage = options.BranchedConversation.AddOrUpdateMessage(
                                v4BranchedConvMessageRole.User,
                                toolResultMessageId,
                                toolResultBlocks,
                                options.AssistantMessageId);

                            // Notify client about the tool result message
                            if (options.OnUserMessageCreated != null)
                            {
                                await options.OnUserMessageCreated(toolResultBranchedMessage);
                            }
                            
                            options.ParentMessageId = toolResultMessageId;
                        }
                    }
                }

                // 9. Check if we should stop the loop
                if (shouldStopLoop)
                {
                    return response;
                }
                
                // 10. Prepare for next iteration - generate new assistant message ID
                if (options.BranchedConversation != null)
                {
                    options.AssistantMessageId = Guid.NewGuid().ToString();
                }
            }

            // If we've exceeded max iterations, create error response
            var errorResponse = new AiResponse 
            { 
                Success = false, 
                ContentBlocks = new List<ContentBlock> 
                { 
                    new ContentBlock 
                    { 
                        Content = $"Exceeded maximum tool iterations ({maxIterations}). The AI may be stuck in a tool loop.",
                        ContentType = ContentType.Text 
                    } 
                } 
            };
            
            // Add error message to branched conversation
            if (options.OnAssistantMessageCreated != null && options.BranchedConversation != null)
            {
                var message = options.BranchedConversation.AddOrUpdateMessage(
                    v4BranchedConvMessageRole.Assistant,
                    options.AssistantMessageId,
                    errorResponse.ContentBlocks,
                    options.ParentMessageId,
                    errorResponse.Attachments,
                    null); // No cost info for error messages
                
                await options.OnAssistantMessageCreated(message);
            }
            
            return errorResponse;
        }

        /// <summary>
        /// Creates a user interjection message in provider-specific format.
        /// Override this in provider implementations to match their message format.
        /// </summary>
        protected virtual LinearConvMessage CreateUserInterjectionMessage(string interjectionText)
        {
            return new LinearConvMessage
            {
                role = "user",
                content = interjectionText
            };
        }

        
        public Task<AiResponse> FetchResponse(
            ServiceProvider serviceProvider,
            Model model,
            LinearConv conv,
            string base64image,
            string base64ImageType,
            CancellationToken cancellationToken,
            ApiSettings apiSettings,
            bool mustNotUseEmbedding,
            List<string> toolIDs,
            bool addEmbeddings = false,
            string customSystemPrompt = null)
        {
            
            var options = AiRequestOptions.Create(
                serviceProvider, model, conv, base64image, base64ImageType,
                cancellationToken, apiSettings, mustNotUseEmbedding, toolIDs,
                addEmbeddings, customSystemPrompt);
            
            
            options.OnStreamingUpdate = null;
            options.OnStreamingComplete = null;
            
            return FetchResponse(options);
        }
        
        
        protected abstract Task<AiResponse> FetchResponseInternal(AiRequestOptions options, bool forceNoTools = false);

        protected virtual async Task<string> AddEmbeddingsIfRequired(
            LinearConv conv,
            ApiSettings apiSettings,
            bool mustNotUseEmbedding,
            bool addEmbeddings,
            string content)
        {
            
            return content;
            
            
            
            
            
            
        }
        
        protected virtual JArray CreateAttachmentsArray(List<Attachment> attachments)
        {
            var result = new JArray();
            
            if (attachments == null || !attachments.Any())
                return result;
                
            foreach (var attachment in attachments)
            {
                if (attachment.Type.StartsWith("image/") || attachment.Type == "application/pdf")
                {
                    result.Add(new JObject
                    {
                        ["type"] = "image_url",
                        ["image_url"] = new JObject
                        {
                            ["url"] = $"data:{attachment.Type};base64,{attachment.Content}"
                        }
                    });
                }
                
            }
            
            return result;
        }

        protected abstract JObject CreateRequestPayload(string modelName, LinearConv conv, ApiSettings apiSettings);

        protected virtual async Task<AiResponse> HandleResponse(
            AiRequestOptions options,
            HttpContent content)
        {
            return await HandleStreamingResponse(content, options.CancellationToken, options.OnStreamingUpdate, options.OnStreamingComplete);
        }

        protected abstract Task<AiResponse> HandleStreamingResponse(
            HttpContent content,
            CancellationToken cancellationToken,
            Action<string> onStreamingUpdate, 
            Action onStreamingComplete);

        protected virtual async Task<HttpResponseMessage> SendRequest(
            HttpContent content,
            CancellationToken cancellationToken)
        {
            var sendOption = HttpCompletionOption.ResponseHeadersRead;
            var request = new HttpRequestMessage(HttpMethod.Post, ApiUrl)
            {
                Content = content
            };

            var response = await client.SendAsync(request, sendOption, cancellationToken);
            
            return response;
        }

        protected virtual TokenUsage ExtractTokenUsage(JObject response)
        {
            return new TokenUsage("0", "0");  
        }

        protected virtual void ValidateResponse(HttpResponseMessage response)
        {
            
        }

        protected virtual async Task<string> ExtractResponseText(JObject response)
        {
            return await Task.FromResult(string.Empty);  
        }

        protected virtual async Task ProcessStreamingData(
            Stream stream,
            StringBuilder responseBuilder,
            CancellationToken cancellationToken)
        {
            
            await Task.CompletedTask;
        }

        protected virtual JObject CreateMessageObject(LinearConvMessage message)
        {
            return MessageBuilder.CreateMessage(message, GetProviderFormat());
        }

        protected virtual async Task AddToolsToRequestAsync(JObject request, List<string> toolIDs)
        {
            var toolRequestBuilder = new ToolRequestBuilder(ToolService, McpService);
            
            
            foreach (var toolID in toolIDs)
            {
                await toolRequestBuilder.AddToolToRequestAsync(request, toolID, GetToolFormat());
            }

            await toolRequestBuilder.AddMcpServiceToolsToRequestAsync(request, GetToolFormat());
        }

        protected virtual ToolFormat GetToolFormat()
        {
            return ToolFormat.OpenAI; 
        }

        protected virtual ProviderFormat GetProviderFormat()
        {
            return ProviderFormat.OpenAI;
        }

        protected virtual AiResponse HandleError(Exception ex, string additionalInfo = "")
        {
            string errorMessage = $"Error: {ex.Message}";
            if (!string.IsNullOrEmpty(additionalInfo))
            {
                errorMessage += $" Additional info: {additionalInfo}";
            }
            return new AiResponse { Success = false, ContentBlocks = new List<ContentBlock> { new ContentBlock { Content = errorMessage, ContentType = ContentType.Text } } };
        }

        /// <summary>
        /// Common request building pattern used by most providers.
        /// Handles system prompt, TopP, tools, and embeddings in a standardized way.
        /// </summary>
        protected virtual async Task<JObject> BuildCommonRequest(AiRequestOptions options, bool forceNoTools = false)
        {
            // Apply custom system prompt if provided
            if (!string.IsNullOrEmpty(options.CustomSystemPrompt))
                options.Conv.systemprompt = options.CustomSystemPrompt;

            var request = CreateRequestPayload(ApiModel, options.Conv, options.ApiSettings);

            // Add TopP if supported
            if (options.Model.AllowsTopP && options.ApiSettings.TopP > 0.0f && options.ApiSettings.TopP <= 1.0f)
            {
                await AddTopPToRequest(request, options.ApiSettings.TopP);
            }

            // Add tools if not forcing no tools
            if (!forceNoTools)
            {
                await AddToolsToRequestAsync(request, options.ToolIds);
                await ConfigureToolChoice(request);
            }

            // Add embeddings if required
            if (options.AddEmbeddings)
            {
                await AddEmbeddingsToRequest(request, options.Conv, options.ApiSettings, options.MustNotUseEmbedding);
            }

            return request;
        }

        /// <summary>
        /// Template method for making API calls with common patterns.
        /// Providers can override specific steps while sharing the common flow.
        /// </summary>
        protected virtual async Task<AiResponse> MakeStandardApiCall(
            AiRequestOptions options, 
            Func<StringContent, Task<AiResponse>> handleResponse,
            bool forceNoTools = false)
        {
            var request = await BuildCommonRequest(options, forceNoTools);
            
            // Allow provider-specific request modifications
            await CustomizeRequest(request, options);

            var json = JsonConvert.SerializeObject(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            return await handleResponse(content);
        }

        /// <summary>
        /// Common cancellation response handler.
        /// Providers can override to add provider-specific data (like attachments).
        /// </summary>
        protected virtual AiResponse HandleCancellation(
            string responseText, 
            TokenUsage tokenUsage, 
            ToolResponse toolResponseSet, 
            string chosenTool = null,
            List<Attachment> attachments = null)
        {
            return new AiResponse
            {
                ContentBlocks = new List<ContentBlock> 
                { 
                    new ContentBlock 
                    { 
                        Content = responseText + "\n\n<Cancelled>\n", 
                        ContentType = ContentType.Text 
                    } 
                },
                Success = true, // Indicate successful handling of cancellation
                TokenUsage = tokenUsage ?? new TokenUsage("0", "0", "0", "0"),
                ChosenTool = chosenTool,
                ToolResponseSet = toolResponseSet ?? new ToolResponse { Tools = new List<ToolResponseItem>() },
                Attachments = attachments,
                IsCancelled = true
            };
        }

        /// <summary>
        /// Provider-specific TopP addition. Override for custom TopP handling.
        /// </summary>
        protected virtual async Task AddTopPToRequest(JObject request, float topP)
        {
            request["top_p"] = topP;
            await Task.CompletedTask;
        }

        /// <summary>
        /// Provider-specific tool choice configuration. Override for custom tool choice logic.
        /// </summary>
        protected virtual async Task ConfigureToolChoice(JObject request)
        {
            if (request["tools"] != null)
            {
                // Default implementation - providers can override
                await Task.CompletedTask;
            }
        }

        /// <summary>
        /// Provider-specific request customization hook. Override for provider-specific modifications.
        /// </summary>
        protected virtual async Task CustomizeRequest(JObject request, AiRequestOptions options)
        {
            await Task.CompletedTask;
        }

        /// <summary>
        /// Provider-specific embeddings addition. Override for custom embeddings handling.
        /// </summary>
        protected virtual async Task AddEmbeddingsToRequest(JObject request, LinearConv conv, ApiSettings apiSettings, bool mustNotUseEmbedding)
        {
            await Task.CompletedTask;
        }

        /// <summary>
        /// Common token usage creation with fallback handling for missing values.
                /// <summary>
                /// Attempts to process malformed tool calls found in response text.
                /// This handles cases where AI models output tool calls as JSON blocks in text instead of proper tool call format.
                /// </summary>
                /// <param name="responseText">The raw response text from the AI service</param>
                /// <returns>Processed AiResponse with cleaned text and extracted tool calls, or null if no malformed tool calls found</returns>
                protected async Task<AiResponse> TryProcessMalformedToolCallsAsync(string responseText)
                {
                    try
                    {
                        var extractedCalls = ExtractMalformedToolCalls(responseText);
        
                        if (extractedCalls.Count == 0)
                            return null; // No malformed tool calls found
        
                        Debug.WriteLine($"🔧 FOUND {extractedCalls.Count} MALFORMED TOOL CALLS in response");
        
                        var contentBlocks = new List<ContentBlock>();
                        var toolResponseSet = new ToolResponse { Tools = new List<ToolResponseItem>() };
        
                        // Remove all tool call JSON blocks from the text
                        var cleanText = responseText;
        
                        // Apply all patterns to remove matched text
                        var allPatterns = new[]
                        {
                            @"\{""toolName""\s*:\s*""([^""]+)""\s*,\s*""parameters""\s*:\s*""([^""]*(?:\\.[^""]*)*)""\s*\}",
                            @"\{""toolName""\s*:\s*""([^""]+)""\s*,\s*""parameters""\s*:\s*(\{[^}]*\})\s*\}",
                            @"\{""tool""\s*:\s*""([^""]+)""\s*,\s*""args""\s*:\s*""([^""]*(?:\\.[^""]*)*)""\s*\}"
                        };
        
                        foreach (var pattern in allPatterns)
                        {
                            var matches = Regex.Matches(cleanText, pattern, RegexOptions.Multiline);
                            foreach (Match match in matches.Cast<Match>().Reverse()) // Reverse to maintain string positions
                            {
                                cleanText = cleanText.Remove(match.Index, match.Length);
                            }
                        }
        
                        // Clean up extra whitespace and newlines
                        cleanText = Regex.Replace(cleanText.Trim(), @"\s*\n\s*\n\s*", "\n\n");
                        cleanText = Regex.Replace(cleanText, @"[ \t]+", " ");
        


                        // Add cleaned text as content block if not empty
                        if (!string.IsNullOrWhiteSpace(cleanText))
                        {
                            contentBlocks.Add(new ContentBlock
                            {
                                Content = cleanText,
                                ContentType = ContentType.Text
                            });
                        }
        
                        // Process each extracted tool call
                        foreach (var (toolName, parametersJson) in extractedCalls)
                        {
                            Debug.WriteLine($"🔧 EXTRACTED TOOL: {toolName} with parameters: {parametersJson}");
        
                            if (!string.IsNullOrEmpty(toolName))
                            {
                                // Validate that this is a known tool name
                                bool isValidTool = await IsValidToolNameAsync(toolName);
                                
                                if (!isValidTool)
                                {
                                    Debug.WriteLine($"⚠️ Skipping unknown tool name: {toolName}");
                                    continue; // Skip this tool as it's not recognized
                                }
                                
                                // Validate that parameters is valid JSON or empty
                                var validatedParameters = "{}";
                                if (!string.IsNullOrEmpty(parametersJson))
                                {
                                    try
                                    {
                                        // Try to parse to validate JSON
                                        JObject.Parse(parametersJson.Replace("\r",""));
                                        validatedParameters = parametersJson;
                                    }
                                    catch (JsonException)
                                    {
                                        Debug.WriteLine($"⚠️ Invalid JSON parameters for tool {toolName}, using empty object");
                                        validatedParameters = "{}";
                                    }
                                }
        
                                // Add tool call content block
                                contentBlocks.Add(new ContentBlock
                                {
                                    Content = JsonConvert.SerializeObject(new { toolName = toolName, parameters = validatedParameters }),
                                    ContentType = ContentType.Tool
                                });
        
                                // Add to tool response set for execution
                                toolResponseSet.Tools.Add(new ToolResponseItem
                                {
                                    ToolName = toolName,
                                    ResponseText = validatedParameters
                                });
                            }
                        }
        
                        Debug.WriteLine($"🔧 PROCESSED MALFORMED TOOL CALLS: {contentBlocks.Count} content blocks, {toolResponseSet.Tools.Count} tools extracted");
        
                        return new AiResponse
                        {
                            ContentBlocks = contentBlocks,
                            Success = true,
                            ToolResponseSet = toolResponseSet,
                            TokenUsage = new TokenUsage("0", "0", "0", "0"), // Unknown from malformed format
                            IsCancelled = false
                        };
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"❌ Failed to process malformed tool calls: {ex.Message}");
                        return null;
                    }
                }
        
                /// <summary>
                /// Extracts malformed tool calls from response text using various regex patterns.
                /// Supports multiple formats that AI models might use when outputting tool calls incorrectly.
                /// </summary>
                /// <param name="responseText">The response text to search for malformed tool calls</param>
                /// <returns>List of tuples containing tool name and parameters JSON</returns>
                protected List<(string toolName, string parameters)> ExtractMalformedToolCalls(string responseText)
                {
                    var extractedCalls = new List<(string toolName, string parameters)>();
        
                    // Pattern 1: {"toolName":"name","parameters":"params"}
                    var pattern1 = @"\{""toolName""\s*:\s*""([^""]+)""\s*,\s*""parameters""\s*:\s*""([^""]*(?:\\.[^""]*)*)""\s*\}";
                    var matches1 = Regex.Matches(responseText, pattern1, RegexOptions.Multiline);
        
                    foreach (Match match in matches1)
                    {
                        extractedCalls.Add((match.Groups[1].Value, match.Groups[2].Value.Replace("\\\"", "\"")));
                    }
        
                    // Pattern 2: {"toolName":"name","parameters":{...}} (unescaped JSON object)
                    var pattern2 = @"\{""toolName""\s*:\s*""([^""]+)""\s*,\s*""parameters""\s*:\s*(\{[^}]*\})\s*\}";
                    var matches2 = Regex.Matches(responseText, pattern2, RegexOptions.Multiline);
        
                    foreach (Match match in matches2)
                    {
                        extractedCalls.Add((match.Groups[1].Value, match.Groups[2].Value));
                    }
        
                    // Pattern 3: {"tool":"name","args":"params"} (alternative field names)
                    var pattern3 = @"\{""tool""\s*:\s*""([^""]+)""\s*,\s*""args""\s*:\s*""([^""]*(?:\\.[^""]*)*)""\s*\}";
                    var matches3 = Regex.Matches(responseText, pattern3, RegexOptions.Multiline);
        
                    foreach (Match match in matches3)
                    {
                        extractedCalls.Add((match.Groups[1].Value, match.Groups[2].Value.Replace("\\\"", "\"")));
                    }
        
                    return extractedCalls;
                }
        /// </summary>
        protected virtual TokenUsage CreateTokenUsage(
            string inputTokens, 
            string outputTokens, 
            string cacheCreationTokens = "0", 
            string cacheReadTokens = "0")
        {
            return new TokenUsage(
                inputTokens ?? "0",
                outputTokens ?? "0", 
                cacheCreationTokens ?? "0",
                cacheReadTokens ?? "0"
            );
        }

        /// <summary>
        /// Validates if a tool name is a valid tool or MCP tool
        /// </summary>
        /// <param name="toolName">The tool name to validate</param>
        /// <returns>True if the tool name is valid, false otherwise</returns>
        protected virtual async Task<bool> IsValidToolNameAsync(string toolName)
        {
            try
            {
                // Check if it's a built-in tool
                if (ToolService != null)
                {
                    var tool = await ToolService.GetToolByToolNameAsync(toolName);
                    if (tool != null)
                    {
                        return true;
                    }
                }

                // Check if it's an MCP tool
                if (McpService != null)
                {
                    // Check all enabled MCP servers for this tool
                    var allServers = await McpService.GetAllServerDefinitionsAsync();
                    foreach (var server in allServers.Where(s => s.IsEnabled))
                    {
                        try
                        {
                            var tools = await McpService.ListToolsAsync(server.Id);
                            if (tools.Any(t => t.Name == toolName))
                            {
                                return true;
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Error checking MCP server {server.Id} for tool {toolName}: {ex.Message}");
                            // Continue checking other servers
                        }
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error validating tool name {toolName}: {ex.Message}");
                return false; // On error, assume invalid to be safe
            }
        }
    }
}
