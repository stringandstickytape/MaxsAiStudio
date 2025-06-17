

// This is a test comment added by AI on user request
using AiStudio4.Core.Exceptions;


using SharedClasses.Providers;
using AiStudio4.AiServices;
using AiStudio4.DataModels;
using AiStudio4.Convs;

using System.Linq;
using System.Text.Json;



using AiStudio4.Core.Tools;
using Microsoft.Extensions.DependencyInjection;

namespace AiStudio4.Services
{
    public class DefaultChatService : IChatService
    {
        private readonly ILogger<DefaultChatService> _logger;
        
        private readonly IToolService _toolService;
        private readonly IMcpService _mcpService;
        private readonly ISystemPromptService _systemPromptService;
        private readonly IToolProcessorService _toolProcessorService;
        private readonly IWebSocketNotificationService _notificationService;
        private readonly IGeneralSettingsService _generalSettingsService;
        private readonly IStatusMessageService _statusMessageService;
        private readonly IServiceProvider _serviceProvider;
        private readonly AiStudio4.Services.CostingStrategies.ITokenCostStrategyFactory _strategyFactory;

        
        
        

        public DefaultChatService(ILogger<DefaultChatService> logger, IToolService toolService, ISystemPromptService systemPromptService, IMcpService mcpService, IToolProcessorService toolProcessorService, IWebSocketNotificationService notificationService, IGeneralSettingsService generalSettingsService, IStatusMessageService statusMessageService, IServiceProvider serviceProvider, AiStudio4.Services.CostingStrategies.ITokenCostStrategyFactory strategyFactory)
        {
            _logger = logger;
            _toolService = toolService;
            _systemPromptService = systemPromptService;
            _mcpService = mcpService;
            _toolProcessorService = toolProcessorService;
            _notificationService = notificationService;
            _generalSettingsService = generalSettingsService;
            _statusMessageService = statusMessageService;
            _serviceProvider = serviceProvider;
            _strategyFactory = strategyFactory;
        }

        public async Task<SimpleChatResponse> ProcessSimpleChatRequest(string chatMessage)
        {
            var startTime = DateTime.UtcNow;
            try
            {
                
                
                var secondaryModelName = _generalSettingsService.CurrentSettings.SecondaryModel;
                if (string.IsNullOrEmpty(secondaryModelName))
                {
                    return new SimpleChatResponse
                    {
                        Success = false,
                        Error = "No secondary model configured",
                        ProcessingTime = DateTime.UtcNow - startTime
                    };
                }

                
                var model = _generalSettingsService.CurrentSettings.ModelList.FirstOrDefault(x => x.ModelName == secondaryModelName);
                if (model == null)
                {
                    return new SimpleChatResponse
                    {
                        Success = false,
                        Error = $"Secondary model '{secondaryModelName}' not found",
                        ProcessingTime = DateTime.UtcNow - startTime
                    };
                }

                var service = SharedClasses.Providers.ServiceProvider.GetProviderForGuid(_generalSettingsService.CurrentSettings.ServiceProviders, model.ProviderGuid);
                var aiService = AiServiceResolver.GetAiService(service.ServiceName, _toolService, _mcpService);

                
                var conv = new LinearConv(DateTime.Now)
                {
                    systemprompt = "You are a helpful assistant.",
                    messages = new List<LinearConvMessage>
                    {
                        new LinearConvMessage
                        {
                            role = "user",
                            content = chatMessage
                        }
                    }
                };

                var requestOptions = new AiRequestOptions
                {
                    ServiceProvider = service,
                    Model = model,
                    Conv = conv,
                    CancellationToken = new CancellationToken(false),
                    ApiSettings = _generalSettingsService.CurrentSettings.ToApiSettings(),
                    TopP = _generalSettingsService.CurrentSettings.ToApiSettings().TopP, // Added TopP
                    MustNotUseEmbedding = true,

                };
                
                var response = await aiService.FetchResponse(requestOptions, true);
                
                return new SimpleChatResponse
                {
                    Success = response.Success,

                    ResponseText = string.Join("\n\n",response.ContentBlocks.Where(x => x.ContentType == ContentType.Text).Select(x => x.Content)),
                    Error = response.Success ? null : "Failed to process chat request",
                    ProcessingTime = DateTime.UtcNow - startTime
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing simple chat request");
                return new SimpleChatResponse
                {
                    Success = false,
                    Error = ex.Message,
                    ProcessingTime = DateTime.UtcNow - startTime
                };
            }
        }

        public async Task<ChatResponse> ProcessChatRequest(ChatRequest request, string assistantMessageId)
        {
            try
            {

                await _statusMessageService.SendStatusMessageAsync(request.ClientId, $"Processing request...");

                
                var clearInterjectionService = _serviceProvider.GetService<IInterjectionService>();
                if (clearInterjectionService != null)
                {
                    await clearInterjectionService.GetAndClearInterjectionAsync(request.ClientId);
                }


                _logger.LogInformation("Processing chat request for conv {ConvId}", request.BranchedConv.ConvId);

                var model = _generalSettingsService.CurrentSettings.ModelList.First(x => x.FriendlyName == request.Model);
                var service = SharedClasses.Providers.ServiceProvider.GetProviderForGuid(_generalSettingsService.CurrentSettings.ServiceProviders, model.ProviderGuid);
                var aiService = AiServiceResolver.GetAiService(service.ServiceName, _toolService, _mcpService);

                string systemPromptContent = await GetSystemPrompt(request);

                // Get the tool executor service for provider-managed tool loop
                var toolExecutor = _serviceProvider.GetRequiredService<Core.Interfaces.IToolExecutor>();
                
                // Create linear conversation from branched conversation
                var linearConversation = new LinearConv(DateTime.Now)
                {
                    systemprompt = systemPromptContent,
                    messages = new List<LinearConvMessage>()
                };

                // Convert branched conversation to linear format
                var history = request.BranchedConv.GetMessageHistory(request.MessageId);
                var messageHistory = history
                    .Select(msg => new MessageHistoryItem
                    {
                        Role = msg.Role.ToString().ToLower(),
                        Content = string.Join("\n\n", (msg.ContentBlocks?.Where(x => x.ContentType == ContentType.Text || x.ContentType == ContentType.AiHidden || x.ContentType == ContentType.Tool) ?? new List<ContentBlock>()).Select(cb => cb.Content)),
                        Attachments = msg.Attachments
                    }).ToList();

                foreach (var historyItem in messageHistory.Where(x => x.Role != "system"))
                {
                    var message = new LinearConvMessage
                    {
                        role = historyItem.Role,
                        content = historyItem.Content,
                        attachments = historyItem.Attachments?.ToList() ?? new List<Attachment>()
                    };
                    linearConversation.messages.Add(message);
                }

                // Ensure the last user message is included
                var lastUserMessage = messageHistory.LastOrDefault(m => m.Role == "user");
                if (lastUserMessage != null && !linearConversation.messages.Any(m => m.role == "user" && m.content == lastUserMessage.Content))
                {
                    linearConversation.messages.Add(new LinearConvMessage
                    {
                        role = "user",
                        content = lastUserMessage.Content,
                        attachments = lastUserMessage.Attachments?.ToList() ?? new List<Attachment>()
                    });
                }

                // Add default tools if any tools are specified
                var toolIds = request.ToolIds ?? new List<string>();
                if (toolIds.Any() || (await _mcpService.GetAllServerDefinitionsAsync()).Any(x => x.IsEnabled))
                {
                    var stopTool = (await _toolService.GetToolByToolNameAsync("PresentResultsAndAwaitUserInput")).Guid;
                    if (!toolIds.Contains(stopTool))
                        toolIds.Add(stopTool);

                    stopTool = (await _toolService.GetToolByToolNameAsync("Stop")).Guid;
                    if (!toolIds.Contains(stopTool))
                        toolIds.Add(stopTool);
                }

                // Create request options for provider-managed tool loop
                var requestOptions = new AiRequestOptions
                {
                    ServiceProvider = service,
                    Model = model,
                    Conv = linearConversation,
                    CancellationToken = request.CancellationToken,
                    ApiSettings = _generalSettingsService.CurrentSettings.ToApiSettings(),
                    TopP = _generalSettingsService.CurrentSettings.ToApiSettings().TopP,
                    MustNotUseEmbedding = true,
                    ToolIds = toolIds,
                    MaxToolIterations = 50, // Could be configurable
                    AllowInterjections = true,
                    // New properties for branched conversation updates
                    BranchedConversation = request.BranchedConv,
                    ParentMessageId = request.MessageId,
                    AssistantMessageId = assistantMessageId,
                    ClientId = request.ClientId
                };

                // Set up callbacks that reference requestOptions after it's created
                requestOptions.GetCurrentAssistantMessageId = () => requestOptions.AssistantMessageId ?? assistantMessageId;
                requestOptions.OnStreamingUpdate = (text) => _notificationService.NotifyStreamingUpdate(request.ClientId, new StreamingUpdateDto 
                { 
                    MessageId = requestOptions.GetCurrentAssistantMessageId?.Invoke() ?? assistantMessageId, 
                    MessageType = "cfrag", 
                    Content = text 
                });
                requestOptions.OnStreamingComplete = () => _notificationService.NotifyStreamingUpdate(request.ClientId, new StreamingUpdateDto 
                { 
                    MessageId = requestOptions.GetCurrentAssistantMessageId?.Invoke() ?? assistantMessageId, 
                    MessageType = "endstream", 
                    Content = "" 
                });
                
                requestOptions.OnAssistantMessageCreated = async (message) =>
                {
                    // Message is already added to branched conversation by the callback
                    // Just notify the client
                    await _notificationService.NotifyConvUpdate(request.ClientId, new ConvUpdateDto
                    {
                        ConvId = request.BranchedConv.ConvId,
                        MessageId = message.Id,
                        ContentBlocks = message.ContentBlocks,
                        ParentId = message.ParentId,
                        Timestamp = new DateTimeOffset(DateTime.Now).ToUnixTimeMilliseconds(),
                        Source = "assistant",
                        Attachments = message.Attachments,
                        DurationMs = 0,
                        TokenUsage = null, // Will be updated later with final usage
                        Temperature = message.Temperature
                    });
                };
                
                requestOptions.OnToolCallsGenerated = async (messageId, contentBlocks, toolCalls) =>
                {
                    // Notify client about tool calls
                    await _notificationService.NotifyStreamingUpdate(request.ClientId, new StreamingUpdateDto
                    {
                        MessageId = messageId,
                        MessageType = "toolcalls",
                        Content = JsonConvert.SerializeObject(toolCalls.Select(t => new { name = t.ToolName, status = "pending" }))
                    });
                };
                
                requestOptions.OnToolExecuted = async (messageId, toolName, result) =>
                {
                    // Notify client about tool execution result
                    await _notificationService.NotifyStreamingUpdate(request.ClientId, new StreamingUpdateDto
                    {
                        MessageId = messageId,
                        MessageType = "toolresult",
                        Content = JsonConvert.SerializeObject(new 
                        { 
                            toolName = toolName,
                            success = result.WasProcessed,
                            result = result.ResultMessage
                        })
                    });
                };
                
                requestOptions.OnUserInterjection = async (interjectionId, content) =>
                {
                    // Add interjection to branched conversation
                    var interjectionMessage = request.BranchedConv.AddOrUpdateMessage(
                        v4BranchedConvMessageRole.User,
                        interjectionId,
                        content,
                        assistantMessageId); // Parent is the current assistant message
                    
                    // Notify client
                    await _notificationService.NotifyConvUpdate(request.ClientId, new ConvUpdateDto
                    {
                        ConvId = request.BranchedConv.ConvId,
                        MessageId = interjectionId,
                        ContentBlocks = interjectionMessage.ContentBlocks,
                        ParentId = assistantMessageId,
                        Timestamp = new DateTimeOffset(DateTime.Now).ToUnixTimeMilliseconds(),
                        Source = "user",
                        DurationMs = 0
                    });
                };
                
                requestOptions.OnUserMessageCreated = async (message) =>
                {
                    // Message is already added to branched conversation by the callback
                    // Just notify the client
                    await _notificationService.NotifyConvUpdate(request.ClientId, new ConvUpdateDto
                    {
                        ConvId = request.BranchedConv.ConvId,
                        MessageId = message.Id,
                        ContentBlocks = message.ContentBlocks,
                        ParentId = message.ParentId,
                        Timestamp = new DateTimeOffset(DateTime.Now).ToUnixTimeMilliseconds(),
                        Source = "user",
                        Attachments = message.Attachments,
                        DurationMs = 0
                    });
                };

                await _statusMessageService.SendStatusMessageAsync(request.ClientId, $"Sending request...");

                // *** THE BIG CHANGE: Single call replaces entire tool loop ***
                AiResponse response = await aiService.FetchResponseWithToolLoop(requestOptions, toolExecutor, request.BranchedConv, request.MessageId, assistantMessageId, request.ClientId);

                // Process the final response
                var costStrategy = _strategyFactory.GetStrategy(service.ChargingStrategy);
                var costInfo = new TokenCost(response.TokenUsage, model, costStrategy);
                
                // Handle final message - use the CURRENT assistant message ID from the loop
                // The assistantMessageId may have been updated during the tool loop
                var finalAssistantMessageId = requestOptions.AssistantMessageId ?? assistantMessageId;
                var existingMessage = request.BranchedConv.Messages.FirstOrDefault(m => m.Id == finalAssistantMessageId);
                
                _logger.LogInformation("🏁 DEFAULTCHATSERVICE: Final message processing - OriginalId: {OriginalId}, FinalId: {FinalId}, ExistingMessage: {Exists}, FinalParent: {FinalParent}", 
                    assistantMessageId, finalAssistantMessageId, existingMessage != null, requestOptions.ParentMessageId);
                
                v4BranchedConvMessage finalMessage;
                
                if (existingMessage != null)
                {
                    // Assistant message already exists - check if we need to append final response content
                    // For tool loops, the final response often contains content that was already streamed/added
                    // So we need to be careful not to duplicate content
                    
                    List<ContentBlock> finalContentBlocks;
                    
                    // Check if the final response has any content that's not already in the existing message
                    if (response.ContentBlocks != null && response.ContentBlocks.Any())
                    {
                        var newContentBlocks = new List<ContentBlock>();
                        var existingTextContent = string.Join("", existingMessage.ContentBlocks?
                            .Where(cb => cb.ContentType == ContentType.Text)
                            .Select(cb => cb.Content) ?? new List<string>());
                        
                        foreach (var newBlock in response.ContentBlocks)
                        {
                            // Only add text blocks that aren't already included in existing content
                            if (newBlock.ContentType == ContentType.Text)
                            {
                                if (!existingTextContent.Contains(newBlock.Content ?? ""))
                                {
                                    newContentBlocks.Add(newBlock);
                                }
                            }
                            else
                            {
                                // Always add non-text blocks (they should be unique)
                                newContentBlocks.Add(newBlock);
                            }
                        }
                        
                        if (newContentBlocks.Any())
                        {
                            // Append only truly new content
                            finalContentBlocks = new List<ContentBlock>(existingMessage.ContentBlocks);
                            finalContentBlocks.AddRange(newContentBlocks);
                            
                            _logger.LogInformation("🏁 FINAL: Appending new content - Original blocks: {OriginalCount}, New blocks: {NewCount}, Total: {TotalCount}", 
                                existingMessage.ContentBlocks?.Count ?? 0, newContentBlocks.Count, finalContentBlocks.Count);
                        }
                        else
                        {
                            // No truly new content, use existing
                            finalContentBlocks = existingMessage.ContentBlocks;
                            
                            _logger.LogInformation("🏁 FINAL: No new content detected (likely duplicates) - Using existing blocks: {ExistingCount}", 
                                existingMessage.ContentBlocks?.Count ?? 0);
                        }
                    }
                    else
                    {
                        // No new content to add, just use existing content
                        finalContentBlocks = existingMessage.ContentBlocks;
                        
                        _logger.LogInformation("🏁 FINAL: No final response content - Using existing blocks: {ExistingCount}", 
                            existingMessage.ContentBlocks?.Count ?? 0);
                    }
                    
                    finalMessage = request.BranchedConv.AddOrUpdateMessage(
                        role: v4BranchedConvMessageRole.Assistant,
                        newMessageId: finalAssistantMessageId,
                        contentBlocks: finalContentBlocks,
                        parentMessageId: existingMessage.ParentId, // Keep original parent
                        attachments: (existingMessage.Attachments ?? new List<Attachment>()).Concat(response.Attachments ?? new List<Attachment>()).ToList(),
                        costInfo: costInfo
                    );
                }
                else
                {
                    // No existing message - create new one (fallback case)
                    _logger.LogInformation("🏁 FINAL: Creating new final message - Blocks: {BlockCount}", response.ContentBlocks?.Count ?? 0);
                    
                    finalMessage = request.BranchedConv.AddOrUpdateMessage(
                        role: v4BranchedConvMessageRole.Assistant,
                        newMessageId: finalAssistantMessageId,
                        contentBlocks: response.ContentBlocks,
                        parentMessageId: requestOptions.ParentMessageId ?? request.MessageId,
                        attachments: response.Attachments,
                        costInfo: costInfo
                    );
                }

                finalMessage.Temperature = requestOptions.ApiSettings.Temperature;

                
                // Always send final update with complete cost information
                await _notificationService.NotifyConvUpdate(request.ClientId, new ConvUpdateDto
                {
                    ConvId = request.BranchedConv.ConvId,
                    MessageId = finalAssistantMessageId,
                    ContentBlocks = finalMessage.ContentBlocks, // Use the complete content blocks
                    ParentId = finalMessage.ParentId,
                    Timestamp = new DateTimeOffset(DateTime.Now).ToUnixTimeMilliseconds(),
                    Source = "assistant",
                    Attachments = finalMessage.Attachments,
                    DurationMs = 0,
                    CostInfo = costInfo,
                    CumulativeCost = finalMessage.CumulativeCost,
                    TokenUsage = response.TokenUsage,
                    Temperature = finalMessage.Temperature
                });

                _logger.LogInformation("Successfully processed chat request using provider-managed tool loop.");

                // DEBUG: Dump final conversation structure
                DumpConversationStructure(request.BranchedConv);

                return new ChatResponse
                {
                    Success = true
                };


            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing chat request");
                throw new ChatProcessingException("Failed to process chat request", ex);
            }
        }

        private async Task<string> GetSystemPrompt(ChatRequest request)
        {
            
            
            string systemPromptContent = "You are a helpful chatbot.";

            if (!string.IsNullOrEmpty(request.SystemPromptContent))
            {
                
                systemPromptContent = request.SystemPromptContent;
            }
            else if (!string.IsNullOrEmpty(request.SystemPromptId))
            {
                
                var systemPrompt = await _systemPromptService.GetSystemPromptByIdAsync(request.SystemPromptId);
                if (systemPrompt != null)
                {
                    systemPromptContent = systemPrompt.Content;
                }
            }
            else if (request.BranchedConv != null)
            {
                
                var systemPrompt = await _systemPromptService.GetConvSystemPromptAsync(request.BranchedConv.ConvId);
                if (systemPrompt != null)
                {
                    systemPromptContent = systemPrompt.Content;
                }
            }

            systemPromptContent = systemPromptContent.Replace("{ProjectPath}", _generalSettingsService.CurrentSettings.ProjectPath);

            
            if(systemPromptContent.Contains("{CommonAiMistakes}"))
            {
                string commonMistakesPath = Path.Combine(_generalSettingsService.CurrentSettings.ProjectPath, "CommonAiMistakes.md");
                if(File.Exists(commonMistakesPath))
                {
                    string mistakesContent = File.ReadAllText(commonMistakesPath);
                    systemPromptContent = systemPromptContent.Replace("{CommonAiMistakes}", mistakesContent);
                }
                else
                {
                    
                    systemPromptContent = systemPromptContent.Replace("{CommonAiMistakes}", "");
                }
            }

            if(systemPromptContent.Contains("{ToolList}"))
            {
                StringBuilder sb = new StringBuilder();

                foreach(var toolId in request.ToolIds)
                {
                    var tool = await _toolService.GetToolByIdAsync(toolId);
                    sb.AppendLine($"{tool.Name} : {tool.Description}");
                }

                systemPromptContent = systemPromptContent.Replace("{ToolList}", sb.ToString());
            }

            return systemPromptContent;
        }

        private void DumpConversationStructure(v4BranchedConv conversation)
        {
            System.Diagnostics.Debug.WriteLine("📊 ===== FINAL CONVERSATION STRUCTURE =====");
            System.Diagnostics.Debug.WriteLine($"📊 Conversation ID: {conversation.ConvId}");
            System.Diagnostics.Debug.WriteLine($"📊 Total Messages: {conversation.Messages.Count}");
            System.Diagnostics.Debug.WriteLine("📊");

            // Sort messages by creation order (or try to build tree)
            var rootMessages = conversation.Messages.Where(m => string.IsNullOrEmpty(m.ParentId) || m.Role == v4BranchedConvMessageRole.System).ToList();
            
            foreach (var root in rootMessages)
            {
                DumpMessageAndChildren(conversation, root, 0);
            }

            System.Diagnostics.Debug.WriteLine("📊 ===== END CONVERSATION STRUCTURE =====");
        }

        private void DumpMessageAndChildren(v4BranchedConv conversation, v4BranchedConvMessage message, int depth)
        {
            var indent = new string(' ', depth * 2);
            var roleIcon = message.Role switch
            {
                v4BranchedConvMessageRole.User => "👤",
                v4BranchedConvMessageRole.Assistant => "🤖",
                v4BranchedConvMessageRole.System => "⚙️",
                _ => "❓"
            };

            var contentPreview = message.ContentBlocks?.FirstOrDefault()?.Content?.Substring(0, Math.Min(50, message.ContentBlocks.FirstOrDefault()?.Content?.Length ?? 0)) ?? "";
            if (contentPreview.Length == 50) contentPreview += "...";

            System.Diagnostics.Debug.WriteLine($"📊 {indent}{roleIcon} {message.Role} | ID: {message.Id} | Parent: {message.ParentId ?? "null"} | Blocks: {message.ContentBlocks?.Count ?? 0} | \"{contentPreview}\"");

            // Find and dump children
            var children = conversation.Messages.Where(m => m.ParentId == message.Id).ToList();
            foreach (var child in children)
            {
                DumpMessageAndChildren(conversation, child, depth + 1);
            }
        }
    }

    public class CustomJsonParser
    {
        public static Dictionary<string, object> ParseJson(string json)
        {
            using (JsonDocument doc = JsonDocument.Parse(json))
            {
                return ProcessJsonElement(doc.RootElement) as Dictionary<string, object>;
            }
        }

        private static object ProcessJsonElement(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    var dictionary = new Dictionary<string, object>();
                    foreach (var property in element.EnumerateObject())
                    {
                        dictionary[property.Name] = ProcessJsonElement(property.Value);
                    }
                    return dictionary;

                case JsonValueKind.Array:
                    
                    bool allNumbers = true;
                    foreach (var item in element.EnumerateArray())
                    {
                        if (item.ValueKind != JsonValueKind.Number)
                        {
                            allNumbers = false;
                            break;
                        }
                    }

                    if (allNumbers)
                    {
                        return element.EnumerateArray()
                            .Select(e => e.GetSingle())
                            .ToArray();
                    }
                    else
                    {
                        return element.EnumerateArray()
                            .Select(ProcessJsonElement)
                            .ToArray();
                    }

                case JsonValueKind.String:
                    return element.GetString();

                case JsonValueKind.Number:
                    
                    return element.GetSingle();

                case JsonValueKind.True:
                    return true;

                case JsonValueKind.False:
                    return false;

                case JsonValueKind.Null:
                    return null;

                default:
                    return null;
            }
        }
    }
}
