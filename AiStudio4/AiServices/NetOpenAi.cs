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
        private readonly Queue<string> _toolCallIdQueue = new Queue<string>();

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

                // Add TopP if supported
                if (options.Model.AllowsTopP && options.ApiSettings.TopP > 0.0f && options.ApiSettings.TopP <= 1.0f)
                {
                    chatOptions.TopP = options.ApiSettings.TopP;
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
                    chatOptions.ToolChoice = ChatToolChoice.CreateAutoChoice();
                }
                try
                {
                    // Handle streaming vs non-streaming requests
                    return await HandleStreamingChatCompletion(messages, chatOptions, options.CancellationToken, options.OnStreamingUpdate, options.OnStreamingComplete);
                }
                catch (Exception ex)
                {
                    return HandleError(ex, "Error during OpenAI API call");
                }
            }
        }

        public override async Task<AiResponse> FetchResponseWithToolLoop(AiRequestOptions options, Core.Interfaces.IToolExecutor toolExecutor, v4BranchedConv branchedConv, string parentMessageId, string assistantMessageId, string clientId)
        {
            InitializeHttpClient(options.ServiceProvider, options.Model, options.ApiSettings);
            InitializeOpenAIClients(ApiModel);

            return await ExecuteCommonToolLoop(
                options,
                toolExecutor,
                makeApiCall: async (opts) => await MakeOpenAIApiCall(opts),
                createAssistantMessage: CreateOpenAIAssistantMessage,
                createToolResultMessage: CreateOpenAIToolResultMessage,
                options.MaxToolIterations ?? 10);
        }

        private async Task<AiResponse> MakeOpenAIApiCall(AiRequestOptions options)
        {
            ToolResponseSet = new ToolResponse { Tools = new List<ToolResponseItem>() };
            
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
                System.Diagnostics.Debug.WriteLine($"🔧 OPENAI Added message to API call: Type={chatMessage.GetType().Name}");
            }

            // Configure chat completion options
            float temp = options.Model.Requires1fTemp ? 1f : options.ApiSettings.Temperature;
            
            ChatCompletionOptions chatOptions = new ChatCompletionOptions
            {
               Temperature = temp
            };

            // Add TopP if supported
            if (options.Model.AllowsTopP && options.ApiSettings.TopP > 0.0f && options.ApiSettings.TopP <= 1.0f)
            {
                chatOptions.TopP = options.ApiSettings.TopP;
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

            // Add tools if present
                await AddToolsToChatOptions(chatOptions, options.ToolIds);
                chatOptions.ToolChoice = ChatToolChoice.CreateAutoChoice();

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
                    int lastIndex = messages.Count - 1;
                    messages[lastIndex] = new UserChatMessage(newInput);
                }
            }

            return await HandleStreamingChatCompletion(messages, chatOptions, options.CancellationToken, options.OnStreamingUpdate, options.OnStreamingComplete);
        }

        private LinearConvMessage CreateOpenAIAssistantMessage(AiResponse response)
        {
            _toolCallIdQueue.Clear(); // Clear previous tool IDs
            
            // Build content similar to Claude - as a JSON array
            var contentArray = new JArray();
            
            // Add text content if any
            var textContent = response.ContentBlocks?.FirstOrDefault(c => c.ContentType == Core.Models.ContentType.Text)?.Content;
            if (!string.IsNullOrEmpty(textContent))
            {
                contentArray.Add(new JObject
                {
                    ["type"] = "text",
                    ["text"] = textContent
                });
            }

            // Add tool calls if present
            if (response.ToolResponseSet?.Tools?.Any() == true)
            {
                foreach (var tool in response.ToolResponseSet.Tools)
                {
                    var toolCallId = $"call_{Guid.NewGuid():N}".Substring(0, 24); // Generate a unique ID
                    _toolCallIdQueue.Enqueue(toolCallId); // Store in order for later use
                    
                    System.Diagnostics.Debug.WriteLine($"🔧 OPENAI ASSISTANT: Creating tool_call with id: {toolCallId}, tool: {tool.ToolName}");
                    
                    contentArray.Add(new JObject
                    {
                        ["type"] = "tool_call",
                        ["id"] = toolCallId,
                        ["function"] = new JObject
                        {
                            ["name"] = tool.ToolName,
                            ["arguments"] = tool.ResponseText
                        }
                    });
                }
            }

            var assistantMessage = new LinearConvMessage
            {
                role = "assistant",
                content = contentArray.ToString()
            };

            System.Diagnostics.Debug.WriteLine($"🔧 OPENAI ASSISTANT MESSAGE: {assistantMessage.content}");
            return assistantMessage;
        }

        private LinearConvMessage CreateOpenAIToolResultMessage(List<ContentBlock> toolResultBlocks)
        {
            // Build content similar to Claude - as a JSON array
            var contentArray = new JArray();
            
            foreach (var block in toolResultBlocks)
            {
                if (block.ContentType == Core.Models.ContentType.ToolResponse)
                {
                    var toolData = JsonConvert.DeserializeObject<dynamic>(block.Content);
                    var toolName = toolData.toolName?.ToString();
                    var result = toolData.result?.ToString();
                    
                    // Use the next tool call ID from the queue (preserves order)
                    var toolCallId = _toolCallIdQueue.Count > 0 
                        ? _toolCallIdQueue.Dequeue() 
                        : $"call_{Guid.NewGuid():N}".Substring(0, 24);
                    
                    System.Diagnostics.Debug.WriteLine($"🔧 OPENAI TOOL RESULT: Creating tool result with tool_call_id: {toolCallId}, tool: {toolName}");
                    
                    contentArray.Add(new JObject
                    {
                        ["type"] = "tool_result",
                        ["tool_call_id"] = toolCallId,
                        ["content"] = result ?? ""
                    });
                }
            }
            
            var toolResultMessage = new LinearConvMessage
            {
                role = "user", // Tool results go in user messages for OpenAI
                content = contentArray.ToString()
            };
            
            System.Diagnostics.Debug.WriteLine($"🔧 OPENAI TOOL RESULT MESSAGE: {toolResultMessage.content}");
            return toolResultMessage;
        }

        protected override LinearConvMessage CreateUserInterjectionMessage(string interjectionText)
        {
            return new LinearConvMessage
            {
                role = "user",
                content = interjectionText
            };
        }

        private ChatMessage CreateChatMessage(LinearConvMessage message)
        {
            System.Diagnostics.Debug.WriteLine($"🔧 OPENAI CreateChatMessage: role={message.role}, content length={message.content?.Length ?? 0}");
            if (!string.IsNullOrEmpty(message.content) && message.content.Length < 500)
            {
                System.Diagnostics.Debug.WriteLine($"🔧 OPENAI Message content: {message.content}");
            }
            
            // Try to parse content as JSON array to detect structured messages
            if (!string.IsNullOrEmpty(message.content) && message.content.Trim().StartsWith("["))
            {
                try
                {
                    var contentArray = JArray.Parse(message.content);
                    System.Diagnostics.Debug.WriteLine($"🔧 OPENAI Parsed JSON array with {contentArray.Count} items");
                    
                    // Handle assistant messages with tool calls
                    if (message.role.ToLower() == "assistant")
                    {
                        // Extract text content and tool calls
                        var textContent = "";
                        var toolCalls = new List<ChatToolCall>();
                        
                        foreach (var item in contentArray)
                        {
                            var itemType = item["type"]?.ToString();
                            System.Diagnostics.Debug.WriteLine($"🔧 OPENAI Assistant message item type: {itemType}");
                            
                            if (itemType == "text")
                            {
                                textContent = item["text"]?.ToString() ?? "";
                            }
                            else if (itemType == "tool_call")
                            {
                                var toolCallId = item["id"]?.ToString();
                                var functionName = item["function"]?["name"]?.ToString();
                                var functionArgs = item["function"]?["arguments"]?.ToString();
                                
                                System.Diagnostics.Debug.WriteLine($"🔧 OPENAI Found tool_call: id={toolCallId}, name={functionName}");
                                
                                if (!string.IsNullOrEmpty(toolCallId) && !string.IsNullOrEmpty(functionName))
                                {
                                    toolCalls.Add(ChatToolCall.CreateFunctionToolCall(
                                        toolCallId,
                                        functionName,
                                        BinaryData.FromString(functionArgs ?? "{}")
                                    ));
                                }
                            }
                        }
                        
                        // Create assistant message with tool calls if any
                        if (toolCalls.Any())
                        {
                            System.Diagnostics.Debug.WriteLine($"🔧 OPENAI Creating AssistantChatMessage with {toolCalls.Count} tool calls");
                            // OpenAI .NET SDK requires creating the message first, then adding tool calls
                            var assistantMsg = new AssistantChatMessage(textContent);
                            foreach (var toolCall in toolCalls)
                            {
                                assistantMsg.ToolCalls.Add(toolCall);
                            }
                            return assistantMsg;
                        }
                        else
                        {
                            return new AssistantChatMessage(textContent);
                        }
                    }
                    
                    // Handle user messages with tool results
                    if (message.role.ToLower() == "user")
                    {
                        // Check if this is a tool result message
                        var firstItem = contentArray.FirstOrDefault();
                        if (firstItem?["type"]?.ToString() == "tool_result")
                        {
                            var toolCallId = firstItem["tool_call_id"]?.ToString() ?? "unknown";
                            var content = firstItem["content"]?.ToString() ?? "";
                            System.Diagnostics.Debug.WriteLine($"🔧 OPENAI Found tool_result: tool_call_id={toolCallId}");
                            return new ToolChatMessage(toolCallId, ChatMessageContentPart.CreateTextPart(content));
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to parse message content as JSON: {ex.Message}");
                }
            }
            
            // Handle old function role messages for backward compatibility
            if (message.role.ToLower() == "function")
            {
                // Function messages have a special format - convert to tool message
                return new ToolChatMessage($"call_{message.name ?? "unknown"}", ChatMessageContentPart.CreateTextPart(message.content ?? ""));
            }
            
            // Handle old assistant messages with function_call (backward compatibility)
            if (message.role.ToLower() == "assistant" && !string.IsNullOrEmpty(message.function_call))
            {
                var assistantMsg = new AssistantChatMessage(message.content ?? "");
                return assistantMsg;
            }

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

            // For assistant messages without content parts, add empty string
            if (contentParts.Count == 0)
            {
                contentParts.Add(ChatMessageContentPart.CreateTextPart(""));
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
                    return new ToolChatMessage("tool_id", ChatMessageContentPart.CreateTextPart(message.content ?? ""));
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
                                                TokenUsage = CreateTokenUsage(inputTokens.ToString(), outputTokens.ToString(), "0", cachedTokens.ToString()),
                                                ChosenTool = chosenTool,
                                                ToolResponseSet = ToolResponseSet,
                                                IsCancelled = false
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
                    TokenUsage = CreateTokenUsage(inputTokens.ToString(), outputTokens.ToString(), "0", cachedTokens.ToString()),
                    ChosenTool = chosenTool,
                    ToolResponseSet = ToolResponseSet,
                    IsCancelled = false
                };
            }
            catch (OperationCanceledException)
            {
                System.Diagnostics.Debug.WriteLine("OpenAI streaming cancelled.");
                
                // Trim any tool response text
                if (ToolResponseSet.Tools.Count > 0 && !string.IsNullOrEmpty(chosenTool))
                {
                    var lastToolResponse = ToolResponseSet.Tools.LastOrDefault(t => t.ToolName == chosenTool);
                    if (lastToolResponse != null)
                    {
                        lastToolResponse.ResponseText = lastToolResponse.ResponseText.Trim();
                    }
                }

                var tokenUsage = CreateTokenUsage(
                    inputTokens.ToString(),
                    outputTokens.ToString(),
                    "0",
                    cachedTokens.ToString()
                );
                
                return HandleCancellation(
                    responseBuilder.ToString(),
                    tokenUsage,
                    ToolResponseSet,
                    chosenTool
                );
            }
            catch (ClientResultException ex)
            {
                return HandleError(ex, "OpenAI API client error");
            }
            catch (Exception ex)
            {
                return HandleError(ex, "Error during streaming response");
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

            await toolRequestBuilder.AddMcpServiceToolsToRequestAsync(requestObj, ToolFormat.OpenAI);

            // Add user-selected tools
            if (toolIDs?.Any() == true)
            {
                foreach (var toolId in toolIDs)
                {
                    await toolRequestBuilder.AddToolToRequestAsync(requestObj, toolId, ToolFormat.OpenAI);
                }
            }
            
            // Add MCP service tools
            
            
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


    }


}
