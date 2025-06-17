using AiStudio4.Convs;
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

                // 2. Make API call
                var response = await makeApiCall(options);

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
                            response.Attachments);
                        
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
                
                // Notify about assistant message with tool calls
                if (options.OnAssistantMessageCreated != null && options.BranchedConversation != null)
                {
                    var message = options.BranchedConversation.AddOrUpdateMessage(
                        v4BranchedConvMessageRole.Assistant,
                        options.AssistantMessageId,
                        contentBlocks,
                        options.ParentMessageId,
                        response.Attachments);
                    
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

                    var executionResult = await toolExecutor.ExecuteToolAsync(toolCall.ToolName, toolCall.ResponseText, context);
                    
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
                            success = executionResult.WasProcessed
                        })
                    });
                }
                
                // 8. Add tool results to conversations
                if (toolResultBlocks.Any())
                {
                    // Add to linear conversation for API (provider-specific format)
                    var toolResultMessage = createToolResultMessage(toolResultBlocks);
                    linearConv.messages.Add(toolResultMessage);
                    
                    // Add to branched conversation as a user message containing tool results
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
                        
                        // Update parent if we're not stopping the loop
                        if (!shouldStopLoop)
                        {
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
                    errorResponse.Attachments);
                
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
            
            return new JObject();
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

        protected virtual AiResponse HandleError(Exception ex, string additionalInfo = "")
        {
            string errorMessage = $"Error: {ex.Message}";
            if (!string.IsNullOrEmpty(additionalInfo))
            {
                errorMessage += $" Additional info: {additionalInfo}";
            }
            return new AiResponse { Success = false, ContentBlocks = new List<ContentBlock> { new ContentBlock { Content = errorMessage, ContentType = ContentType.Text } } };
        }
    }
}
