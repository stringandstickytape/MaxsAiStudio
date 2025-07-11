using AiStudio4.Convs;
using AiStudio4.Core.Tools;
using AiStudio4.DataModels;
using AiStudio4.Core.Models;

using OpenAIChatCompletionOptions = OpenAI.Chat.ChatCompletionOptions;
using OpenAIChatToolChoice = OpenAI.Chat.ChatToolChoice;
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
using System.Runtime.CompilerServices;


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
        private readonly Queue<string> _toolIdQueue = new Queue<string>();

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
                //if (options.AddEmbeddings)
                //{
                //    var lastMessage = options.Conv.messages.Last();
                //    var newInput = await AddEmbeddingsIfRequired(
                //        options.Conv,
                //        options.ApiSettings,
                //        options.MustNotUseEmbedding,
                //        options.AddEmbeddings,
                //        lastMessageContent);
                //
                //    // Update the last message content with embeddings
                //    if (messages.Count > 0 && messages.Last() is UserChatMessage userMessage)
                //    {
                //        // Replace the last message with the new content
                //        int lastIndex = messages.Count - 1;
                //        messages[lastIndex] = new UserChatMessage(newInput);
                //    }
                //}
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
            for (int i = 0; i < options.Conv.messages.Count; i++)
            {
                var message = options.Conv.messages[i];
                ChatMessage chatMessage = CreateChatMessage(message);
                messages.Add(chatMessage);
                System.Diagnostics.Debug.WriteLine($"🔧 OPENAI Added message [{i}] to API call: Role={message.role}, Type={chatMessage.GetType().Name}, ContentBlocks={message.contentBlocks?.Count ?? 0}");
                
                // Debug content blocks
                if (message.contentBlocks != null)
                {
                    for (int j = 0; j < message.contentBlocks.Count; j++)
                    {
                        var block = message.contentBlocks[j];
                        System.Diagnostics.Debug.WriteLine($"🔧 OPENAI   ContentBlock [{j}]: Type={block.ContentType}, ToolId={block.ToolId}, Content preview: {block.Content?.Substring(0, Math.Min(100, block.Content.Length /* this does not have a value property! */))}...");
                    }
                }
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

            if (chatOptions.Tools.Any())
            {
                chatOptions.ToolChoice = ChatToolChoice.CreateAutoChoice();
            }

            // Process embeddings if needed
            if (options.AddEmbeddings)
            {
                var lastMessage = options.Conv.messages.Last();
                var lastMessageContent = string.Join("\n\n", 
                    lastMessage.contentBlocks?.Where(b => b.ContentType == ContentType.Text)?.Select(b => b.Content) ?? new string[0]);
                var newInput = await AddEmbeddingsIfRequired(
                    options.Conv,
                    options.ApiSettings,
                    options.MustNotUseEmbedding,
                    options.AddEmbeddings,
                    lastMessageContent);

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
            var contentBlocks = new List<ContentBlock>();
            _toolIdQueue.Clear(); // Clear previous tool IDs
            
            // Add text content if any
            var textContent = response.ContentBlocks?.FirstOrDefault(c => c.ContentType == Core.Models.ContentType.Text)?.Content;
            if (!string.IsNullOrEmpty(textContent))
            {
                contentBlocks.Add(new ContentBlock
                {
                    ContentType = ContentType.Text,
                    Content = textContent
                });
            }

            // Add tool calls as a single structured block if present
            if (response.ToolResponseSet?.Tools?.Any() == true)
            {
                var toolCallsArray = new JArray();
                
                foreach (var tool in response.ToolResponseSet.Tools)
                {
                    var toolCallId = tool.ToolId ?? $"call_{Guid.NewGuid():N}".Substring(0, 24); // Use existing ID or generate new
                    _toolIdQueue.Enqueue(toolCallId); // Store in order for later use
                    
                    System.Diagnostics.Debug.WriteLine($"🔧 OPENAI ASSISTANT: Creating tool_call with id: {toolCallId}, tool: {tool.ToolName}, queue_count: {_toolIdQueue.Count}");
                    
                    var toolCallData = new JObject
                    {
                        ["type"] = "tool_call",
                        ["id"] = toolCallId,
                        ["function"] = new JObject
                        {
                            ["name"] = tool.ToolName,
                            ["arguments"] = tool.ResponseText
                        }
                    };
                    
                    toolCallsArray.Add(toolCallData);
                    
                    // Update the tool ID in the response set for later reference
                    tool.ToolId = toolCallId;
                }
                
                // Add all tool calls as a single ContentBlock with Tool type
                contentBlocks.Add(new ContentBlock
                {
                    ContentType = ContentType.Tool,
                    Content = toolCallsArray.ToString(),
                    ToolId = response.ToolResponseSet.Tools.FirstOrDefault()?.ToolId
                });
            }

            var assistantMessage = new LinearConvMessage
            {
                role = "assistant",
                contentBlocks = contentBlocks
            };

            System.Diagnostics.Debug.WriteLine($"🔧 OPENAI ASSISTANT MESSAGE: {contentBlocks.Count} content blocks");
            return assistantMessage;
        }

        private LinearConvMessage CreateOpenAIToolResultMessage(List<ContentBlock> toolResultBlocks)
        {
            var contentBlocks = new List<ContentBlock>();
            
            foreach (var block in toolResultBlocks)
            {
                if (block.ContentType == Core.Models.ContentType.ToolResponse)
                {
                    var toolData = JsonConvert.DeserializeObject<dynamic>(block.Content);
                    var toolName = toolData.toolName?.ToString();
                    var result = toolData.result?.ToString();
                    
                    // Use the next tool ID from the queue (preserves order)
                    var toolCallId = _toolIdQueue.Count > 0
                        ? _toolIdQueue.Dequeue()
                        : $"call_{Guid.NewGuid():N}".Substring(0, 24);
                    
                    System.Diagnostics.Debug.WriteLine($"🔧 OPENAI TOOL RESULT: Creating tool result with tool_call_id: {toolCallId}, tool: {toolName}, queue_count: {_toolIdQueue.Count}");
                    
                    // For OpenAI, each tool result becomes a separate tool message
                    // We'll create individual messages in the CreateChatMessage method
                    contentBlocks.Add(new ContentBlock
                    {
                        ContentType = ContentType.ToolResponse,
                        Content = result ?? "",
                        ToolId = toolCallId // Store the tool_call_id for matching
                    });
                }
            }
            
            var toolResultMessage = new LinearConvMessage
            {
                role = "tool", // Use tool role for OpenAI tool results
                contentBlocks = contentBlocks
            };
            
            System.Diagnostics.Debug.WriteLine($"🔧 OPENAI TOOL RESULT MESSAGE: {contentBlocks.Count} content blocks");
            return toolResultMessage;
        }

        protected override LinearConvMessage CreateUserInterjectionMessage(string interjectionText)
        {
            return new LinearConvMessage
            {
                role = "user",
                contentBlocks = new List<ContentBlock>
                {
                    new ContentBlock
                    {
                        ContentType = ContentType.Text,
                        Content = interjectionText
                    }
                }
            };
        }

        private ChatMessage CreateChatMessage(LinearConvMessage message)
        {
            var contentBlocks = message.contentBlocks ?? new List<ContentBlock>();
            System.Diagnostics.Debug.WriteLine($"🔧 OPENAI CreateChatMessage: role={message.role}, content blocks count={contentBlocks.Count}");
            
            // Debug all content blocks
            for (int i = 0; i < contentBlocks.Count; i++)
            {
                var block = contentBlocks[i];
                System.Diagnostics.Debug.WriteLine($"🔧 OPENAI   Block [{i}]: Type={block.ContentType}, ToolId={block.ToolId}");
            }
            
            // Handle assistant messages with tool calls
            if (message.role.ToLower() == "assistant")
            {
                var textContent = "";
                var toolCalls = new List<ChatToolCall>();
                
                foreach (var block in contentBlocks)
                {
                    System.Diagnostics.Debug.WriteLine($"🔧 OPENAI Assistant message block type: {block.ContentType}");
                    
                    if (block.ContentType == ContentType.Text)
                    {
                        textContent += block.Content ?? "";
                    }
                    else if (block.ContentType == ContentType.Tool)
                    {
                        // Parse tool calls from JSON content - could be single object or array
                        try
                        {
                            var content = block.Content ?? "{}";
                            
                            // Try to parse as array first (new format)
                            if (content.TrimStart().StartsWith("["))
                            {
                                var toolCallsArray = JArray.Parse(content);
                                foreach (var toolCallToken in toolCallsArray)
                                {
                                    if (toolCallToken is JObject toolData)
                                    {
                                        var toolCallId = toolData["id"]?.ToString();
                                        var functionName = toolData["function"]?["name"]?.ToString();
                                        var functionArgs = toolData["function"]?["arguments"]?.ToString();
                                        
                                        System.Diagnostics.Debug.WriteLine($"🔧 OPENAI Found tool_call from array: id={toolCallId}, name={functionName}");
                                        
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
                            }
                            else
                            {
                                // Try to parse as single object (legacy format)
                                var toolData = JObject.Parse(content);
                                var toolCallId = toolData["id"]?.ToString();
                                var functionName = toolData["function"]?["name"]?.ToString();
                                var functionArgs = toolData["function"]?["arguments"]?.ToString();
                                
                                // If the new format doesn't work, try the very old format: {"toolName":"...", "parameters":"..."}
                                if (string.IsNullOrEmpty(functionName))
                                {
                                    functionName = toolData["toolName"]?.ToString();
                                    functionArgs = toolData["parameters"]?.ToString();
                                    
                                    // Generate a tool call ID if missing
                                    if (string.IsNullOrEmpty(toolCallId))
                                    {
                                        toolCallId = block.ToolId ?? $"call_{Guid.NewGuid():N}".Substring(0, 24);
                                    }
                                    
                                    // Store this ID in the queue for later tool result matching
                                    _toolIdQueue.Enqueue(toolCallId);
                                    
                                    System.Diagnostics.Debug.WriteLine($"🔧 OPENAI Found tool_call from legacy object: id={toolCallId}, name={functionName}, added to queue_count: {_toolIdQueue.Count}");
                                }
                                else
                                {
                                    System.Diagnostics.Debug.WriteLine($"🔧 OPENAI Found tool_call from object: id={toolCallId}, name={functionName}");
                                }
                                
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
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Failed to parse tool call JSON: {ex.Message}");
                        }
                    }
                }
                
                // Create assistant message with tool calls if any
                if (toolCalls.Any())
                {
                    System.Diagnostics.Debug.WriteLine($"🔧 OPENAI Creating AssistantChatMessage with {toolCalls.Count} tool calls");
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
            
            // Handle tool messages (OpenAI tool results)
            if (message.role.ToLower() == "tool")
            {
                // For tool role messages, each ContentBlock represents a tool result
                var toolResponseBlock = contentBlocks.FirstOrDefault(b => b.ContentType == ContentType.ToolResponse);
                if (toolResponseBlock != null)
                {
                    var toolCallId = toolResponseBlock.ToolId ?? "unknown";
                    var content = toolResponseBlock.Content ?? "";
                    System.Diagnostics.Debug.WriteLine($"🔧 OPENAI Creating ToolChatMessage: tool_call_id={toolCallId}");
                    return new ToolChatMessage(toolCallId, ChatMessageContentPart.CreateTextPart(content));
                }
            }
            
            // Handle user messages with tool results (legacy format)
            if (message.role.ToLower() == "user")
            {
                var toolResponseBlock = contentBlocks.FirstOrDefault(b => b.ContentType == ContentType.ToolResponse);
                if (toolResponseBlock != null)
                {
                    // Use the next tool ID from the queue - queue must be populated by preceding assistant message
                    var toolCallId = _toolIdQueue.Dequeue();
                    
                    // For tool results, the content might be in different formats
                    string content;
                    try
                    {
                        var toolData = JObject.Parse(toolResponseBlock.Content ?? "{}");
                        content = toolData["content"]?.ToString() ?? toolData["result"]?.ToString() ?? toolResponseBlock.Content ?? "";
                    }
                    catch
                    {
                        content = toolResponseBlock.Content ?? "";
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"🔧 OPENAI Found tool_result: tool_call_id={toolCallId}, queue_count: {_toolIdQueue.Count}");
                    return new ToolChatMessage(toolCallId, ChatMessageContentPart.CreateTextPart(content));
                }
            }
            
            // Handle old function role messages for backward compatibility
            if (message.role.ToLower() == "function")
            {
                var textContent = string.Join("\n\n", contentBlocks.Where(b => b.ContentType == ContentType.Text).Select(b => b.Content));
                return new ToolChatMessage($"call_{message.name ?? "unknown"}", ChatMessageContentPart.CreateTextPart(textContent));
            }
            
            // Handle old assistant messages with function_call (backward compatibility)
            if (message.role.ToLower() == "assistant" && !string.IsNullOrEmpty(message.function_call))
            {
                var textContent = string.Join("\n\n", contentBlocks.Where(b => b.ContentType == ContentType.Text).Select(b => b.Content));
                return new AssistantChatMessage(textContent);
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
                            System.Diagnostics.Debug.WriteLine($"Failed to add image attachment: {ex.Message}");
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"Skipping unsupported attachment type: {attachment.Type}");
                    }
                }
            }

            // Handle text content blocks
            foreach (var block in contentBlocks.Where(b => b.ContentType == ContentType.Text))
            {
                if (!string.IsNullOrEmpty(block.Content))
                {
                    contentParts.Add(ChatMessageContentPart.CreateTextPart(block.Content));
                }
            }

            // For messages without content parts, add empty string
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
                    var toolContent = string.Join("\n\n", contentBlocks.Where(b => b.ContentType == ContentType.Text).Select(b => b.Content));
                    return new ToolChatMessage("tool_id", ChatMessageContentPart.CreateTextPart(toolContent));
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
                //OpenAISdkHelper.SetStreamOptionsToNull(options);
                //OpenAISdkHelper.SetMaxTokens(options, 32768);
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
                                    ResponseText = "",
                                    ToolId = toolCall.ToolCallId // Store the tool_call_id from OpenAI response
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


    public static class OpenAISdkHelper
    {
        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set__deprecatedMaxTokens")]
        public static extern void SetMaxTokens(OpenAIChatCompletionOptions options, int? deprecatedMaxTokens);

        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_predefinedValue")]
        public static extern ref string? GetToolChoicePredefinedValue(OpenAIChatToolChoice toolChoice);

        private static PropertyInfo? StreamOptionsProperty { get; } = typeof(OpenAIChatCompletionOptions).GetProperty("StreamOptions", BindingFlags.NonPublic | BindingFlags.Instance)!;

        public static void SetStreamOptionsToNull(OpenAIChatCompletionOptions options)
        {
            StreamOptionsProperty?.SetValue(options, null);
        }
    }

}
