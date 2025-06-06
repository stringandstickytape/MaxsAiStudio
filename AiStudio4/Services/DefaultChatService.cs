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

                const int MAX_ITERATIONS = 50; 


                int currentIteration = 0;
                bool continueLoop = true;
                AiResponse response = null; 
                AiStudio4.Core.Models.TokenCost accumulatedCostInfo = null;
                List<Attachment> finalAttachments = new List<Attachment>(); 


                StringBuilder collatedResponse = new StringBuilder();


                
                string previousToolRequested = null; 
                while (continueLoop && currentIteration < MAX_ITERATIONS)
                {
                    if(currentIteration != 0)
                        await _statusMessageService.SendStatusMessageAsync(request.ClientId, $"Looping...");
                        

                    
                    if (request.CancellationToken.IsCancellationRequested)
                    {
                        _logger.LogInformation("Cancellation requested during tool loop. Exiting loop.");
                        break;
                    }

                    collatedResponse = new StringBuilder();
                    currentIteration++;

                    
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

                    _logger.LogInformation("Processing chat request - Iteration {Iteration}", currentIteration);
                    
                    var requestOptions = new AiRequestOptions
                    {
                        ServiceProvider = service,
                        Model = model,
                        Conv = linearConversation, 
                        CancellationToken = request.CancellationToken,
                        ApiSettings = _generalSettingsService.CurrentSettings.ToApiSettings(),
                        TopP = _generalSettingsService.CurrentSettings.ToApiSettings().TopP, // Added TopP
                        MustNotUseEmbedding = true,
                        ToolIds = request.ToolIds ?? new List<string>(), 
                        OnStreamingUpdate = request.OnStreamingUpdate,
                        OnStreamingComplete = request.OnStreamingComplete
                    };

                    await _statusMessageService.SendStatusMessageAsync(request.ClientId, $"Sending request...");

                    
                    if (request.ToolIds.Any() || (await _mcpService.GetAllServerDefinitionsAsync()).Any(x => x.IsEnabled))
                    {
                        var stopTool = (await _toolService.GetToolByToolNameAsync("PresentResultsAndAwaitUserInput")).Guid;
                        if (!request.ToolIds.Contains(stopTool))
                            request.ToolIds.Add(stopTool);

                        stopTool = (await _toolService.GetToolByToolNameAsync("Stop")).Guid;
                        if (!request.ToolIds.Contains(stopTool))
                            request.ToolIds.Add(stopTool);
                    }

                    response = await aiService.FetchResponse(requestOptions);

                    await _statusMessageService.SendStatusMessageAsync(request.ClientId, $"Response received...");

                    if (response.Attachments != null && response.Attachments.Any())
                    {
                        finalAttachments.AddRange(response.Attachments); 
                    }

                    var newAssistantMessageId = $"msg_{Guid.NewGuid()}";

                    var toolResult = await _toolProcessorService.ProcessToolsAsync(response, linearConversation, collatedResponse, request.CancellationToken, request.ClientId);

                    bool duplicateDetection = false;

                    
                    if (previousToolRequested != null && toolResult.RequestedToolsSummary != null && toolResult.RequestedToolsSummary.Trim() == previousToolRequested.Trim())
                    {
                        _logger.LogError("Detected identical consecutive tool requests: {ToolRequested}. Aborting tool loop as AI is stuck.", toolResult.RequestedToolsSummary);
                        await _statusMessageService.SendStatusMessageAsync(request.ClientId, $"Error: AI requested the same tool(s) twice in a row with identical parameters. Tool loop aborted.");
                        duplicateDetection = true;
                    }
                    previousToolRequested = toolResult.RequestedToolsSummary;
                    

                    continueLoop = toolResult.ShouldContinueToolLoop;

                    if (toolResult.Attachments?.Count > 0)
                    {
                        if (finalAttachments == null) finalAttachments = new List<Attachment>();
                        finalAttachments.AddRange(toolResult.Attachments);
                    }

                    continueLoop = continueLoop && currentIteration < MAX_ITERATIONS && !duplicateDetection;

                    var costInfo = new TokenCost(response.TokenUsage, model);

                    
                    if (continueLoop)
                    {
                        
                        var interjectionService = _serviceProvider.GetService<IInterjectionService>();
                        if (interjectionService != null && await interjectionService.HasInterjectionAsync(request.ClientId))
                        {
                            string interjection = await interjectionService.GetAndClearInterjectionAsync(request.ClientId);
                            if (!string.IsNullOrEmpty(interjection))
                            {
                                
                                collatedResponse.Insert(0, $"User interjection: {interjection}\n\n");

                                
                                await _statusMessageService.SendStatusMessageAsync(request.ClientId, "Your interjection has been added to the conversation.");
                            }
                        }

                        var msg = request.BranchedConv.AddNewMessage(role: v4BranchedConvMessageRole.Assistant, newMessageId: newAssistantMessageId,
                            userMessage: response.ResponseText, parentMessageId: request.MessageId,
                            attachments: response.Attachments, costInfo: new TokenCost(response.TokenUsage, model));
                        msg.Temperature = requestOptions.ApiSettings.Temperature;

                        await _notificationService.NotifyConvUpdate(request.ClientId, new ConvUpdateDto
                        {
                            ConvId = request.BranchedConv.ConvId,
                            MessageId = newAssistantMessageId,
                            Content = toolResult.RequestedToolsSummary,
                            ParentId = request.MessageId,
                            Timestamp = new DateTimeOffset(DateTime.Now).ToUnixTimeMilliseconds(),
                            Source = "assistant",
                            Attachments = response.Attachments,
                            DurationMs = 0,
                            CostInfo = costInfo,
                            CumulativeCost = msg.CumulativeCost,
                            TokenUsage = response.TokenUsage,
                            Temperature = msg.Temperature
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
                            duplicateDetectionText = $"AI requested the same tool(s) twice in a row with identical parameters: {toolResult.RequestedToolsSummary}. Tool loop aborted.\n\n";
                        }

                        string userMessage = $"{duplicateDetectionText}{response.ResponseText}\n{toolResult.AggregatedToolOutput}";

                        v4BranchedConvMessage msg = request.BranchedConv.AddNewMessage(role: v4BranchedConvMessageRole.Assistant, newMessageId: newAssistantMessageId,
                            userMessage: userMessage, parentMessageId: request.MessageId,
                            attachments: response.Attachments, costInfo: new TokenCost(response.TokenUsage, model));
                        msg.Temperature = requestOptions.ApiSettings.Temperature;

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
                            TokenUsage = response.TokenUsage,
                            Temperature = msg.Temperature
                        });

                   }

                } 

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
