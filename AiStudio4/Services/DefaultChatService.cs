using Microsoft.Extensions.Logging;
using AiStudio4.InjectedDependencies;
using AiStudio4.Core.Exceptions;
using AiStudio4.Core.Models;
using AiStudio4.Core.Interfaces;
using SharedClasses.Providers;
using AiStudio4.AiServices;
using AiStudio4.DataModels;
using AiStudio4.Convs;
using Newtonsoft.Json;
using System.Text.Json;
using Newtonsoft.Json.Linq;
using System.Text;
using System.IO;
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

        // Events removed
        // public event EventHandler<string> StreamingTextReceived;
        // public event EventHandler<string> StreamingComplete;

        public DefaultChatService(ILogger<DefaultChatService> logger, IToolService toolService, ISystemPromptService systemPromptService, IMcpService mcpService, IToolProcessorService toolProcessorService, IWebSocketNotificationService notificationService, IGeneralSettingsService generalSettingsService, IStatusMessageService statusMessageService, IServiceProvider serviceProvider)
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
        }

        public async Task<SimpleChatResponse> ProcessSimpleChatRequest(string chatMessage)
        {
            var startTime = DateTime.UtcNow;
            try
            {
                _logger.LogInformation("Processing simple chat request");
                
                // Get the secondary model
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

                // Find the model and service provider
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

                // Create a simple chat request
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
                    MustNotUseEmbedding = true,
                    UseStreaming = false
                };
                
                var response = await aiService.FetchResponse(requestOptions, true);
                
                return new SimpleChatResponse
                {
                    Success = response.Success,
                    ResponseText = response.ResponseText,
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

        public async Task<ChatResponse> ProcessChatRequest(ChatRequest request)
        {
            try
            {
                await _statusMessageService.SendStatusMessageAsync(request.ClientId, $"Processing request...");

                _logger.LogInformation("Processing chat request for conv {ConvId}", request.BranchedConv.ConvId);

                var model = _generalSettingsService.CurrentSettings.ModelList.First(x => x.ModelName == request.Model);
                var service = SharedClasses.Providers.ServiceProvider.GetProviderForGuid(_generalSettingsService.CurrentSettings.ServiceProviders, model.ProviderGuid);
                var aiService = AiServiceResolver.GetAiService(service.ServiceName, _toolService, _mcpService);

                string systemPromptContent = await GetSystemPrompt(request);

                const int MAX_ITERATIONS = 50; // Maximum number of tool call iterations


                int currentIteration = 0;
                bool continueLoop = true;
                AiResponse response = null; // Store the latest response
                AiStudio4.Core.Models.TokenCost accumulatedCostInfo = null;
                List<Attachment> finalAttachments = new List<Attachment>(); // Initialize here


                StringBuilder collatedResponse = new StringBuilder();


                // --- Tool Use Loop ---
                string previousToolRequested = null; // Track previous tool request for loop detection
                while (continueLoop && currentIteration < MAX_ITERATIONS)
                {
                    if(currentIteration != 0)
                        await _statusMessageService.SendStatusMessageAsync(request.ClientId, $"Looping...");
                        

                    // Check for cancellation at the start of each loop iteration
                    if (request.CancellationToken.IsCancellationRequested)
                    {
                        _logger.LogInformation("Cancellation requested during tool loop. Exiting loop.");
                        break;
                    }

                    collatedResponse = new StringBuilder();
                    currentIteration++;

                    // Prepare initial conversation state
                    var linearConversation = new LinearConv(DateTime.Now)
                    {
                        systemprompt = systemPromptContent,
                        messages = new List<LinearConvMessage>()
                    };

                    var messageHistory = request.BranchedConv.GetMessageHistory(request.MessageId)
                        .Select(msg => new MessageHistoryItem
                        {
                            Role = msg.Role.ToString().ToLower(),
                            Content = msg.UserMessage,
                            Attachments = msg.Attachments
                        }).ToList();


                    // Add all messages from history first
                    foreach (var historyItem in messageHistory.Where(x => x.Role != "system"))
                    {
                        var message = new LinearConvMessage
                        {
                            role = historyItem.Role,
                            content = historyItem.Content,
                            attachments = historyItem.Attachments?.ToList() ?? new List<Attachment>()
                            // Assuming LinearConvMessage doesn't explicitly store tool calls/results from history
                            // If it does, map historyItem.ToolCalls and historyItem.ToolResults here
                        };
                        linearConversation.messages.Add(message);
                    }

                    // Add the latest user message (the trigger for this request)
                    var lastUserMessage = messageHistory.LastOrDefault(m => m.Role == "user");
                    if (lastUserMessage != null && !linearConversation.messages.Any(m => m.role == "user" && m.content == lastUserMessage.Content)) // Avoid duplicates if history includes the trigger
                    {
                        linearConversation.messages.Add(new LinearConvMessage
                        {
                            role = "user",
                            content = lastUserMessage.Content,
                            attachments = lastUserMessage.Attachments?.ToList() ?? new List<Attachment>()
                        });
                    }

                    _logger.LogInformation("Processing chat request - Iteration {Iteration}", currentIteration);

                    // we always make the stop tool available.
                    if (request.ToolIds.Any())
                    {
                        var stopTool = (await _toolService.GetToolByToolNameAsync("Stop")).Guid;
                        if (!request.ToolIds.Contains(stopTool))
                            request.ToolIds.Add(stopTool);
                    }

                    var requestOptions = new AiRequestOptions
                    {
                        ServiceProvider = service,
                        Model = model,
                        Conv = linearConversation, // Use the current state of the conversation
                        CancellationToken = request.CancellationToken,
                        ApiSettings = _generalSettingsService.CurrentSettings.ToApiSettings(),
                        MustNotUseEmbedding = true,
                        ToolIds = request.ToolIds ?? new List<string>(), // Pass available tools
                        UseStreaming = true, 
                        OnStreamingUpdate = request.OnStreamingUpdate,
                        OnStreamingComplete = request.OnStreamingComplete
                    };

                    await _statusMessageService.SendStatusMessageAsync(request.ClientId, $"Sending request...");

                    response = await aiService.FetchResponse(requestOptions);

                    await _statusMessageService.SendStatusMessageAsync(request.ClientId, $"Response received...");

                    // Accumulate cost
                    //if (response.CostInfo != null)
                    //{
                    //    if (accumulatedCostInfo == null)
                    //        accumulatedCostInfo = new AiStudio4.Core.Models.TokenCost(0, 0, response.CostInfo.Model);
                    //    accumulatedCostInfo.Add(response.CostInfo);
                    //}
                    if (response.Attachments != null && response.Attachments.Any())
                    {
                        finalAttachments.AddRange(response.Attachments); // Accumulate attachments
                    }

                    // Add assistant message to conversation history
                    //var assistantMessage = new LinearConvMessage
                    //{
                    //    role = "assistant",
                    //    content = response.ResponseText,
                    //    // TODO: Map response.ToolResponseSet.Tools to a ToolCall structure if LinearConvMessage supports it
                    //    // tool_calls = response.ToolResponseSet?.Tools.Select(t => new { id = t.ToolCallId, type = "function", function = new { name = t.ToolName, arguments = t.ParametersJson } }).ToList()
                    //};
                    //linearConversation.messages.Add(assistantMessage);

                    var newAssistantMessageId = $"msg_{Guid.NewGuid()}";
                    




                    var toolResult = await _toolProcessorService.ProcessToolsAsync(response, linearConversation, collatedResponse, request.CancellationToken, request.ClientId);

                    bool duplicateDetection = false;
                    // --- Tool Loop Detection ---
                    if (previousToolRequested != null && toolResult.ToolRequested != null && toolResult.ToolRequested.Trim() == previousToolRequested.Trim())
                    {
                        _logger.LogError("Detected identical consecutive tool requests: {ToolRequested}. Aborting tool loop as AI is stuck.", toolResult.ToolRequested);
                        await _statusMessageService.SendStatusMessageAsync(request.ClientId, $"Error: AI requested the same tool(s) twice in a row. Tool loop aborted.");
                        duplicateDetection = true;
                    }
                    previousToolRequested = toolResult.ToolRequested;
                    // --- End Tool Loop Detection ---

                    continueLoop = toolResult.ContinueProcessing;

                    if (request.ToolIds.Count == 2) // one of which must be "Stop", so the user has only selected 1 tool
                    {
                        continueLoop = false;
                    }

                    if (toolResult.Attachments?.Count > 0)
                    {
                        if (finalAttachments == null) finalAttachments = new List<Attachment>();
                        finalAttachments.AddRange(toolResult.Attachments);
                    }

                    continueLoop = continueLoop && currentIteration < MAX_ITERATIONS && !duplicateDetection;

                    var costInfo = new TokenCost(response.TokenUsage, model);

                    // If the loop should continue, add a user message to prompt the next step
                    if (continueLoop)
                    {
                        // Check for interjections before continuing the loop
                        var interjectionService = _serviceProvider.GetService<IInterjectionService>();
                        if (interjectionService != null && await interjectionService.HasInterjectionAsync(request.ClientId))
                        {
                            string interjection = await interjectionService.GetAndClearInterjectionAsync(request.ClientId);
                            if (!string.IsNullOrEmpty(interjection))
                            {
                                // Prepend the interjection to the collated response
                                collatedResponse.Insert(0, $"User interjection: {interjection}\n\n");

                                // Notify the client that the interjection was processed
                                await _statusMessageService.SendStatusMessageAsync(request.ClientId, "Your interjection has been added to the conversation.");
                            }
                        }

                        var msg = request.BranchedConv.AddNewMessage(role: v4BranchedConvMessageRole.Assistant, newMessageId: newAssistantMessageId,
                            userMessage: response.ResponseText, parentMessageId: request.MessageId,
                            attachments: response.Attachments, costInfo: new TokenCost(response.TokenUsage, model));

                        await _notificationService.NotifyConvUpdate(request.ClientId, new ConvUpdateDto
                        {
                            ConvId = request.BranchedConv.ConvId,
                            MessageId = newAssistantMessageId,
                            Content = toolResult.ToolRequested,
                            ParentId = request.MessageId,
                            Timestamp = new DateTimeOffset(DateTime.Now).ToUnixTimeMilliseconds(),
                            Source = "assistant",
                            Attachments = response.Attachments,
                            DurationMs = 0,
                            CostInfo = costInfo,
                            CumulativeCost = msg.CumulativeCost,
                            TokenUsage = response.TokenUsage
                        });

                        var newUserMessageId = $"msg_{Guid.NewGuid()}";
                        
                        request.BranchedConv.AddNewMessage(role: v4BranchedConvMessageRole.User, newMessageId: newUserMessageId,
                            userMessage: collatedResponse.ToString(), parentMessageId: newAssistantMessageId, attachments: response.Attachments);
                        request.MessageId = newUserMessageId;

                        await _notificationService.NotifyConvUpdate(request.ClientId, new ConvUpdateDto
                        {
                            ConvId = request.BranchedConv.ConvId,
                            MessageId = newUserMessageId,
                            Content = collatedResponse.ToString(),
                            ParentId = newAssistantMessageId,
                            Timestamp = new DateTimeOffset(DateTime.Now).ToUnixTimeMilliseconds(),
                            Source = "user",
                            Attachments = response.Attachments,
                            DurationMs = 0 
                        });

                    }
                    else 
                    {
                        string duplicateDetectionText = "";
                        if(duplicateDetection)
                        {
                            duplicateDetectionText = $"AI requested the same tool(s) twice in a row: {toolResult.ToolRequested}. Tool loop aborted.\n\n";
                        }

                        string userMessage = $"{duplicateDetectionText}{response.ResponseText}\n{toolResult.ToolResult}";

                        v4BranchedConvMessage msg = request.BranchedConv.AddNewMessage(role: v4BranchedConvMessageRole.Assistant, newMessageId: newAssistantMessageId,
                            userMessage: userMessage, parentMessageId: request.MessageId,
                            attachments: response.Attachments, costInfo: new TokenCost(response.TokenUsage, model));

                        await _notificationService.NotifyConvUpdate(request.ClientId, new ConvUpdateDto
                        {
                            ConvId = request.BranchedConv.ConvId,
                            MessageId = newAssistantMessageId,
                            Content = userMessage,
                            ParentId = request.MessageId,
                            Timestamp = new DateTimeOffset(DateTime.Now).ToUnixTimeMilliseconds(),
                            Source = "assistant",
                            Attachments = response.Attachments,
                            DurationMs = 0,
                            CostInfo = costInfo,
                            CumulativeCost = msg.CumulativeCost,
                            TokenUsage = response.TokenUsage
                        });

                   }

                } // --- End of Tool Use Loop ---

                return new ChatResponse
                {
                    Success = true,
                    ResponseText = collatedResponse.ToString()
                };

                _logger.LogInformation("Successfully processed chat request after {Iterations} iterations.", currentIteration);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing chat request");
                throw new ChatProcessingException("Failed to process chat request", ex);
            }
        }

        private async Task<string> GetSystemPrompt(ChatRequest request)
        {
            
            // Get the appropriate system prompt
            string systemPromptContent = "You are a helpful chatbot.";

            if (!string.IsNullOrEmpty(request.SystemPromptContent))
            {
                // Use custom system prompt content provided in the request
                systemPromptContent = request.SystemPromptContent;
            }
            else if (!string.IsNullOrEmpty(request.SystemPromptId))
            {
                // Use specified system prompt ID
                var systemPrompt = await _systemPromptService.GetSystemPromptByIdAsync(request.SystemPromptId);
                if (systemPrompt != null)
                {
                    systemPromptContent = systemPrompt.Content;
                }
            }
            else if (request.BranchedConv != null)
            {
                // Use conv-specific system prompt
                var systemPrompt = await _systemPromptService.GetConvSystemPromptAsync(request.BranchedConv.ConvId);
                if (systemPrompt != null)
                {
                    systemPromptContent = systemPrompt.Content;
                }
            }

            systemPromptContent = systemPromptContent.Replace("{ProjectPath}", _generalSettingsService.CurrentSettings.ProjectPath);

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
                    // Check if all elements are numbers - if so, return as float[]
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
                    // You could add additional logic here if needed
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