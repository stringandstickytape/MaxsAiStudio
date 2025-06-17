using AiStudio4.Convs;
using AiStudio4.Core.Tools;
using AiStudio4.DataModels;
using AiStudio4.Core.Models;


using OpenAI;
using OpenAI.Chat;
using OpenAI.Audio;
using OpenAI.Images;
using OpenAI.Embeddings;
using OpenAI.Assistants;
using SharedClasses.Providers;




using System.Net.Http;

using System.Threading;

using System.ClientModel;

using AiStudio4.Services;
using Azure.Core;


namespace AiStudio4.AiServices
{
    public class NetOpenAi: AiServiceBase
    {
        private OpenAIClient _openAIClient;
        private ChatClient _chatClient;
        private AudioClient _audioClient;
        private ImageClient _imageClient;
        private EmbeddingClient _embeddingClient;
        private readonly List<GeneratedImage> _generatedImages = new List<GeneratedImage>();

        public ToolResponse ToolResponseSet { get; set; } = new ToolResponse { Tools = new List<ToolResponseItem>() };

        public NetOpenAi() { }

        protected override void ConfigureHttpClientHeaders(ApiSettings apiSettings)
        {
            // Not using the base HttpClient as we're using the OpenAI .NET client
        }

        private void InitializeOpenAIClients(string model)
        {
            var cred = new ApiKeyCredential(ApiKey ?? "ollama");

            _openAIClient = new OpenAIClient(cred, new OpenAIClientOptions { Endpoint =  new Uri(ApiUrl) , NetworkTimeout = TimeSpan.FromMinutes(10)});
            
            _chatClient = _openAIClient.GetChatClient(model);
            _audioClient = _openAIClient.GetAudioClient("whisper-1"); // Default audio model
            _imageClient = _openAIClient.GetImageClient("dall-e-3"); // Default image model
            _embeddingClient = _openAIClient.GetEmbeddingClient("text-embedding-3-small"); // Default embedding model
        }

        protected override async Task<AiResponse> FetchResponseInternal(AiRequestOptions options, bool forceNoTools = false)
        {
            // Reset ToolResponseSet for each new request
            ToolResponseSet = new ToolResponse { Tools = new List<ToolResponseItem>() };
            {
                // Initialize OpenAI clients
                InitializeHttpClient(options.ServiceProvider, options.Model, options.ApiSettings);
                InitializeOpenAIClients(ApiModel);

                // Apply custom system prompt if provided
                if (!string.IsNullOrEmpty(options.CustomSystemPrompt))
                {
                    options.Conv.systemprompt = options.CustomSystemPrompt;
                }

                // Create list of messages for the chat completion
                List<ChatMessage> messages = new List<ChatMessage>();

                // Add system message if present
                if (!string.IsNullOrEmpty(options.Conv.systemprompt))
                {
                    messages.Add(new SystemChatMessage(options.Conv.SystemPromptWithDateTime()));
                }

                // Add conversation messages
                foreach (var message in options.Conv.messages)
                { 
                    ChatMessage chatMessage = CreateChatMessage(message);
                    messages.Add(chatMessage);
                }

                float temp = options.Model.Requires1fTemp ? 1f : options.ApiSettings.Temperature;

                // for o3, o4 mini and possibly others
                ChatCompletionOptions chatOptions = new ChatCompletionOptions
                {
                   Temperature = temp
                };

                // Add TopP from options.TopP (which was populated from ApiSettings)
                // The value in options.TopP should be pre-validated (0.0 to 1.0).
                if (options.Model.AllowsTopP && options.TopP.HasValue && options.TopP.Value > 0.0f && options.TopP.Value <= 1.0f)
                {
                    chatOptions.TopP = options.TopP.Value;
                }

                // Set ReasoningEffortLevel if Model.ReasoningEffort is not 'none'
                if (!string.IsNullOrEmpty(options.Model.ReasoningEffort) && options.Model.ReasoningEffort != "none")
                {
                    switch (options.Model.ReasoningEffort)
                    {
                        case "low":
                            chatOptions.ReasoningEffortLevel = ChatReasoningEffortLevel.Low;
                            break;
                        case "medium":
                            chatOptions.ReasoningEffortLevel = ChatReasoningEffortLevel.Medium;
                            break;
                        case "high":
                            chatOptions.ReasoningEffortLevel = ChatReasoningEffortLevel.High;
                            break;
                        default:
                            chatOptions.ReasoningEffortLevel = ChatReasoningEffortLevel.Medium;
                            break;
                    }
                }

                // Add tools if specified or if using MCP service tools
                if (!forceNoTools)
                {
                    await AddToolsToChatOptions(chatOptions, options.ToolIds);
                }

                // Process embeddings if needed
                if (options.AddEmbeddings)
                {
                    var lastMessage = options.Conv.messages.Last();
                    var newInput = await AddEmbeddingsIfRequired(
                        options.Conv,
                        options.ApiSettings,
                        options.MustNotUseEmbedding,
                        options.AddEmbeddings,
                        lastMessage.content);

                    // Update the last message content with embeddings
                    if (messages.Count > 0 && messages.Last() is UserChatMessage userMessage)
                    {
                        // Replace the last message with the new content
                        int lastIndex = messages.Count - 1;
                        messages[lastIndex] = new UserChatMessage(newInput);
                    }
                }
                if (chatOptions.Tools.Any())
                {
                    chatOptions.ToolChoice = ChatToolChoice.CreateRequiredChoice();
                }
                try
                {
                    // Handle streaming vs non-streaming requests
                    return await HandleStreamingChatCompletion(messages, chatOptions, options.CancellationToken, options.OnStreamingUpdate, options.OnStreamingComplete);
                }
                catch (Exception ex)
                {
                    return HandleError(ex);
                }
            }
        }

        public async Task<AiResponse> FetchResponseWithToolLoop(AiRequestOptions options, Core.Interfaces.IToolExecutor toolExecutor, string clientId)
        {
            // Initialize OpenAI clients
            InitializeHttpClient(options.ServiceProvider, options.Model, options.ApiSettings);
            InitializeOpenAIClients(ApiModel);

            var linearConv = options.Conv;
            if (!string.IsNullOrEmpty(options.CustomSystemPrompt))
                linearConv.systemprompt = options.CustomSystemPrompt;
            
            var maxIterations = options.MaxToolIterations ?? 10;
            
            for (int iteration = 0; iteration < maxIterations; iteration++)
            {
                // 1. Reset tool response set for this iteration
                ToolResponseSet = new ToolResponse { Tools = new List<ToolResponseItem>() };

                // 2. Create list of messages for the chat completion
                List<ChatMessage> messages = new List<ChatMessage>();

                // Add system message if present
                if (!string.IsNullOrEmpty(linearConv.systemprompt))
                {
                    messages.Add(new SystemChatMessage(linearConv.SystemPromptWithDateTime()));
                }

                // Add conversation messages
                foreach (var message in linearConv.messages)
                { 
                    ChatMessage chatMessage = CreateChatMessage(message);
                    messages.Add(chatMessage);
                }

                // 3. Configure chat completion options
                float temp = options.Model.Requires1fTemp ? 1f : options.ApiSettings.Temperature;
                
                ChatCompletionOptions chatOptions = new ChatCompletionOptions
                {
                   Temperature = temp
                };

                // Add TopP if supported
                if (options.Model.AllowsTopP && options.TopP.HasValue && options.TopP.Value > 0.0f && options.TopP.Value <= 1.0f)
                {
                    chatOptions.TopP = options.TopP.Value;
                }

                // Set reasoning effort level if applicable
                if (!string.IsNullOrEmpty(options.Model.ReasoningEffort) && options.Model.ReasoningEffort != "none")
                {
                    switch (options.Model.ReasoningEffort)
                    {
                        case "low":
                            chatOptions.ReasoningEffortLevel = ChatReasoningEffortLevel.Low;
                            break;
                        case "medium":
                            chatOptions.ReasoningEffortLevel = ChatReasoningEffortLevel.Medium;
                            break;
                        case "high":
                            chatOptions.ReasoningEffortLevel = ChatReasoningEffortLevel.High;
                            break;
                        default:
                            chatOptions.ReasoningEffortLevel = ChatReasoningEffortLevel.Medium;
                            break;
                    }
                }

                // 4. Add all available tools to the request
                var availableTools = await toolExecutor.GetAvailableToolsAsync(options.ToolIds);
                if (availableTools.Any())
                {
                    await AddToolsToChatOptions(chatOptions, options.ToolIds);
                    chatOptions.ToolChoice = ChatToolChoice.CreateAutoChoice();
                }

                // Process embeddings if needed
                if (options.AddEmbeddings)
                {
                    var lastMessage = linearConv.messages.Last();
                    var newInput = await AddEmbeddingsIfRequired(
                        linearConv,
                        options.ApiSettings,
                        options.MustNotUseEmbedding,
                        options.AddEmbeddings,
                        lastMessage.content);

                    // Update the last message content with embeddings
                    if (messages.Count > 0 && messages.Last() is UserChatMessage userMessage)
                    {
                        int lastIndex = messages.Count - 1;
                        messages[lastIndex] = new UserChatMessage(newInput);
                    }
                }

                // 5. Call OpenAI API
                AiResponse response;
                try
                {
                    response = await HandleStreamingChatCompletion(messages, chatOptions, options.CancellationToken, options.OnStreamingUpdate, options.OnStreamingComplete);
                }
                catch (Exception ex)
                {
                    response = HandleError(ex);
                    if (!response.Success)
                        return response;
                }

                // 6. Check for final answer (no tool calls)
                if (ToolResponseSet == null || !ToolResponseSet.Tools.Any())
                {
                    return response; // We're done, return the final text response
                }

                // 7. Add the AI's response with function calls to conversation history
                var textContent = response.ContentBlocks?.FirstOrDefault(c => c.ContentType == Core.Models.ContentType.Text)?.Content;
                var assistantMessage = new LinearConvMessage
                {
                    role = "assistant",
                    content = textContent ?? ""
                };

                // Add function call information if present
                if (ToolResponseSet.Tools.Any())
                {
                    // For OpenAI, we need to track function calls differently
                    var firstTool = ToolResponseSet.Tools.First();
                    assistantMessage.function_call = JsonConvert.SerializeObject(new {
                        name = firstTool.ToolName,
                        arguments = firstTool.ResponseText
                    });
                }

                linearConv.messages.Add(assistantMessage);

                // 8. Execute tools and collect results
                var shouldStopLoop = false;

                foreach (var toolCall in ToolResponseSet.Tools)
                {
                    var context = new Core.Interfaces.ToolExecutionContext
                    {
                        ClientId = clientId,
                        CancellationToken = options.CancellationToken,
                        Conversation = null, // We don't have the branched conversation here
                        CurrentIteration = iteration
                    };

                    var executionResult = await toolExecutor.ExecuteToolAsync(toolCall.ToolName, toolCall.ResponseText, context);
                    
                    // Check if tool execution indicates we should stop the loop
                    if (!executionResult.ContinueProcessing || executionResult.UserInterjection != null)
                    {
                        shouldStopLoop = true;
                        
                        // If there's a user interjection, add it to the conversation
                        if (executionResult.UserInterjection != null)
                        {
                            linearConv.messages.Add(new LinearConvMessage
                            {
                                role = "user",
                                content = executionResult.UserInterjection
                            });
                            break; // Don't execute remaining tools, process the interjection
                        }
                    }

                    // Add function result to conversation
                    linearConv.messages.Add(new LinearConvMessage
                    {
                        role = "function",
                        name = toolCall.ToolName,
                        content = executionResult.ResultMessage
                    });
                }

                // 9. Check if we should stop the loop
                if (shouldStopLoop)
                {
                    // Continue the loop to let the AI respond to the interjection or tool stop
                    continue;
                }
            }

            // If we've exceeded max iterations, return an error response
            return new AiResponse 
            { 
                Success = false, 
                ContentBlocks = new List<ContentBlock> 
                { 
                    new ContentBlock 
                    { 
                        Content = $"Exceeded maximum tool iterations ({maxIterations}). The AI may be stuck in a tool loop.",
                        ContentType = Core.Models.ContentType.Text 
                    } 
                } 
            };
        }

        private ChatMessage CreateChatMessage(LinearConvMessage message)
        {
            List<ChatMessageContentPart> contentParts = new List<ChatMessageContentPart>();

            // Handle multiple attachments (images only, as OpenAI supports only images for now)
            if (message.attachments != null && message.attachments.Any())
            {
                foreach (var attachment in message.attachments)
                {
                    if (!string.IsNullOrEmpty(attachment.Type) && attachment.Type.StartsWith("image/"))
                    {
                        try
                        {
                            byte[] imageData = Convert.FromBase64String(attachment.Content);
                            contentParts.Add(ChatMessageContentPart.CreateImagePart(
                                BinaryData.FromBytes(imageData), attachment.Type));
                        }
                        catch (Exception ex)
                        {
                            // Log or handle invalid base64/image data
                            System.Diagnostics.Debug.WriteLine($"Failed to add image attachment: {ex.Message}");
                        }
                    }
                    else
                    {
                        // Only image attachments are supported by OpenAI as of 2024
                        System.Diagnostics.Debug.WriteLine($"Skipping unsupported attachment type: {attachment.Type}");
                    }
                }
            }

            // Handle text content (always add after images, to match Claude logic)
            if (!string.IsNullOrEmpty(message.content))
            {
                contentParts.Add(ChatMessageContentPart.CreateTextPart(message.content));
            }

            // Create appropriate message type based on role
            switch (message.role.ToLower())
            {
                case "system":
                    return new SystemChatMessage(contentParts);
                case "user":
                    return new UserChatMessage(contentParts);
                case "assistant":
                    return new AssistantChatMessage(contentParts);
                case "tool":
                    // Tool messages require additional parameters
                    return new ToolChatMessage("tool_id", message.content);
                default:
                    return new UserChatMessage(contentParts);
            }
        }


        protected override async Task<AiResponse> HandleStreamingResponse(
            HttpContent content,
            CancellationToken cancellationToken,
            Action<string> onStreamingUpdate,
            Action onStreamingComplete)
        {
            // Not used in this implementation as we're using the OpenAI .NET client directly
            throw new NotImplementedException();
        }



        private async Task<AiResponse> HandleStreamingChatCompletion(
            List<ChatMessage> messages,
            ChatCompletionOptions options,
            CancellationToken cancellationToken,
            Action<string> onStreamingUpdate,
            Action onStreamingComplete)
        {
            StringBuilder responseBuilder = new StringBuilder();
            string chosenTool = null;
            int inputTokens = 0;
            int outputTokens = 0;
            int cachedTokens = 0;

            try
            {
                AsyncCollectionResult<StreamingChatCompletionUpdate> completionUpdates =
                    _chatClient.CompleteChatStreamingAsync(messages, options, cancellationToken);

                await foreach (StreamingChatCompletionUpdate update in completionUpdates.WithCancellation(cancellationToken))
                {
                    // Handle content updates (text)
                    if (update.ContentUpdate != null && update.ContentUpdate.Count > 0 && !string.IsNullOrEmpty(update.ContentUpdate[0].Text))
                    {
                        string textChunk = update.ContentUpdate[0].Text;
                        responseBuilder.Append(textChunk);
                        onStreamingUpdate?.Invoke(textChunk); // Use callback
                    }

                    // Handle tool call updates
                    if (update.ToolCallUpdates != null && update.ToolCallUpdates.Count > 0)
                    {
                        foreach (var toolCall in update.ToolCallUpdates)
                        {
                            if (!string.IsNullOrEmpty(toolCall.FunctionName))
                            {
                                chosenTool = toolCall.FunctionName;

                                // Create a new tool response item when we first identify the tool
                                var toolResponseItem = new ToolResponseItem
                                {
                                    ToolName = toolCall.FunctionName,
                                    ResponseText = ""
                                };
                                ToolResponseSet.Tools.Add(toolResponseItem);

                                // Clear the response builder when a tool is chosen
                                //responseBuilder.Clear();
                            }
                            if (toolCall.FunctionArgumentsUpdate != null && toolCall.FunctionArgumentsUpdate.ToArray().Length > 0 && !string.IsNullOrEmpty(toolCall.FunctionArgumentsUpdate.ToString()))
                            {
                                string argumentUpdate = toolCall.FunctionArgumentsUpdate.ToString();
                                // Don't append to responseBuilder for tool calls
                                // responseBuilder.Append(argumentUpdate);
                                onStreamingUpdate?.Invoke(argumentUpdate); // Use callback

                                // Update the tool response text
                                if (ToolResponseSet.Tools.Count > 0)
                                {
                                    var lastToolResponse = ToolResponseSet.Tools.LastOrDefault(t => t.ToolName == chosenTool);
                                    if (lastToolResponse != null)
                                    {
                                        lastToolResponse.ResponseText += argumentUpdate;
                                        if (lastToolResponse.ResponseText.Length > 200 && !lastToolResponse.ResponseText.Substring(lastToolResponse.ResponseText.Length - 200, 200).Any(x => x != '\t' && x != ' ' && x != '\r' && x != '\n'))
                                        {
                                            // request cancellation on cancellationtoken

                                            onStreamingComplete?.Invoke(); // Use callback
                                            lastToolResponse.ResponseText = lastToolResponse.ResponseText.Trim();
                                            return new AiResponse
                                            {
                                                ContentBlocks = new List<ContentBlock> { new ContentBlock { Content = responseBuilder.ToString().TrimEnd(), ContentType = Core.Models.ContentType.Text } },
                                                Success = true,
                                                TokenUsage = new TokenUsage(inputTokens.ToString(), outputTokens.ToString()),
                                                ChosenTool = chosenTool,
                                                ToolResponseSet = ToolResponseSet
                                            };
                                        }
                                    }
                                }
                            }
                            else
                            {
                                // Update the tool response text
                                if (ToolResponseSet.Tools.Count > 0)
                                {
                                    var lastToolResponse = ToolResponseSet.Tools.LastOrDefault(t => t.ToolName == chosenTool);
                                    if (lastToolResponse != null)
                                    {
                                        lastToolResponse.ResponseText += "";
                                    }
                                }
                            }
                        }
                    }

                    // Update token usage if available
                    if (update.Usage != null)
                    {
                        inputTokens = update.Usage.InputTokenCount;
                        outputTokens = update.Usage.OutputTokenCount;
                        cachedTokens = update.Usage.InputTokenDetails?.CachedTokenCount ?? 0;
                        
                    }

                    if (cancellationToken.IsCancellationRequested)
                    {
                        // Throwing ensures we jump to the catch block
                        throw new OperationCanceledException(cancellationToken);
                    }
                }

                onStreamingComplete?.Invoke(); // Use callback

                return new AiResponse
                {
                    ContentBlocks = new List<ContentBlock> { new ContentBlock { Content = responseBuilder.ToString().TrimEnd(), ContentType = Core.Models.ContentType.Text } },
                    Success = true,
                    TokenUsage = new TokenUsage(inputTokens.ToString(), outputTokens.ToString(), "0", cachedTokens.ToString()),
                    ChosenTool = chosenTool,
                    ToolResponseSet = ToolResponseSet,
                };
            }
            catch (OperationCanceledException)
            {
                // Return partial results when cancellation occurs
                onStreamingComplete?.Invoke(); // Still invoke completion callback

                // Trim any tool response text
                if (ToolResponseSet.Tools.Count > 0 && !string.IsNullOrEmpty(chosenTool))
                {
                    var lastToolResponse = ToolResponseSet.Tools.LastOrDefault(t => t.ToolName == chosenTool);
                    if (lastToolResponse != null)
                    {
                        lastToolResponse.ResponseText = lastToolResponse.ResponseText.Trim();
                    }
                }

                responseBuilder.AppendLine("\n\n<Cancelled>\n");

                return new AiResponse
                {
                    ContentBlocks = new List<ContentBlock> { new ContentBlock { Content = responseBuilder.ToString().TrimEnd(), ContentType = Core.Models.ContentType.Text } },
                    Success = true, // Still consider it a success, just partial
                    TokenUsage = new TokenUsage(inputTokens.ToString(), outputTokens.ToString(), "0", cachedTokens.ToString()),
                    ChosenTool = chosenTool,
                    ToolResponseSet = ToolResponseSet,
                };
            }
            catch (ClientResultException ex)
            {
                // Return partial results when cancellation occurs
                onStreamingComplete?.Invoke(); // Still invoke completion callback

                // Trim any tool response text
                if (ToolResponseSet.Tools.Count > 0 && !string.IsNullOrEmpty(chosenTool))
                {
                    var lastToolResponse = ToolResponseSet.Tools.LastOrDefault(t => t.ToolName == chosenTool);
                    if (lastToolResponse != null)
                    {
                        lastToolResponse.ResponseText = lastToolResponse.ResponseText.Trim();
                    }
                }

                responseBuilder.AppendLine("\n\n<Error>\n");

                return new AiResponse
                {
                    ContentBlocks = new List<ContentBlock> { new ContentBlock { Content = responseBuilder.ToString().TrimEnd() + "\n" + ex.ToString(), ContentType = Core.Models.ContentType.Text } },
                    Success = true, // Still consider it a success, just partial
                    TokenUsage = new TokenUsage(inputTokens.ToString(), outputTokens.ToString()),
                    ChosenTool = chosenTool,
                    ToolResponseSet = ToolResponseSet,
                };
            }
            catch (Exception ex)
            {
                return HandleError(ex);
            }
        }

        private TokenUsage ExtractTokenUsage(ChatCompletion completion)
        {
            // Extract token usage from the completion
            if (completion.Usage != null)
            {
                return new TokenUsage(completion.Usage.InputTokenCount.ToString(), completion.Usage.OutputTokenCount.ToString());
            }
            return new TokenUsage("0", "0");
        }

        private async Task AddToolsToChatOptions(ChatCompletionOptions options, List<string> toolIDs)
        {
            // Create a ToolRequestBuilder to handle tool construction
            var toolRequestBuilder = new ToolRequestBuilder(ToolService, McpService);
            
            // Create a JObject that will mimic the request structure expected by ToolRequestBuilder
            JObject requestObj = new JObject
            {
                ["tools"] = new JArray()
            };



            // Add user-selected tools
            if (toolIDs?.Any() == true)
            {
                foreach (var toolId in toolIDs)
                {
                    await toolRequestBuilder.AddToolToRequestAsync(requestObj, toolId, ToolFormat.OpenAI);
                }
            }
            
            // Add MCP service tools
            await toolRequestBuilder.AddMcpServiceToolsToRequestAsync(requestObj, ToolFormat.OpenAI);
            
            // Convert the JArray of tools to ChatTool objects
            if (requestObj["tools"] is JArray toolsArray && toolsArray.Count > 0)
            {


                foreach (JObject toolObj in toolsArray)
                {
                    if (toolObj["type"]?.ToString() == "function" && toolObj["function"] is JObject functionObj)
                    {
                        string name = functionObj["name"]?.ToString();
                        string description = functionObj["description"]?.ToString();
                        JObject parameters = functionObj["parameters"] as JObject;
                        
                        if (!string.IsNullOrEmpty(name) && parameters != null)
                        {
                            options.Tools.Add(ChatTool.CreateFunctionTool(
                                functionName: name,
                                functionDescription: description,
                                functionParameters: BinaryData.FromString(parameters.ToString()),
                                functionSchemaIsStrict: false
                            ));
                        }
                    }
                }
            }
        }

        
        protected override JObject CreateRequestPayload(string modelName, LinearConv conv, ApiSettings apiSettings)
        {
            // Not used in this implementation as we're using the OpenAI .NET client directly
            return new JObject();
        }

        protected override JObject CreateMessageObject(LinearConvMessage message)
        {
            // Not used in this implementation as we're using the OpenAI .NET client directly
            return new JObject();
        }





        protected override TokenUsage ExtractTokenUsage(JObject response)
        {
            // Not used in this implementation as we're using the OpenAI .NET client directly
            return new TokenUsage("0", "0");
        }

        protected override ToolFormat GetToolFormat()
        {
            return ToolFormat.OpenAI;
        }

        // Helper method for handling errors
        private AiResponse HandleError(Exception ex, string additionalInfo = "")
        {
            string errorMessage = $"Error: {ex.Message}";
            if (!string.IsNullOrEmpty(additionalInfo))
            {
                errorMessage += $" Additional info: {additionalInfo}";
            }
            return new AiResponse { Success = false, ContentBlocks = new List<ContentBlock> { new ContentBlock { Content = errorMessage, ContentType = Core.Models.ContentType.Text } }};
        }

    }


}
