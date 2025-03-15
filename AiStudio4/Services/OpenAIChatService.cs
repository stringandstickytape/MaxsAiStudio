using Microsoft.Extensions.Logging;
using AiStudio4.InjectedDependencies;
using AiStudio4.Core.Exceptions;
using AiStudio4.Core.Models;
using AiStudio4.Core.Interfaces;
using SharedClasses.Providers;
using AiStudio4.AiServices;
using AiStudio4.DataModels;
using AiStudio4.Convs;

namespace AiStudio4.Services
{
    public class OpenAIChatService : IChatService
    {
        private readonly ILogger<OpenAIChatService> _logger;
        private readonly SettingsManager _settingsManager;
        private readonly IToolService _toolService;
        private readonly ISystemPromptService _systemPromptService;

        public event EventHandler<string> StreamingTextReceived;
        public event EventHandler<string> StreamingComplete;

        public OpenAIChatService(ILogger<OpenAIChatService> logger, SettingsManager settingsManager, IToolService toolService, ISystemPromptService systemPromptService)
        {
            _logger = logger;
            _settingsManager = settingsManager;
            _toolService = toolService;
            _systemPromptService = systemPromptService;
        }

        public async Task<SimpleChatResponse> ProcessSimpleChatRequest(string chatMessage)
        {
            var startTime = DateTime.UtcNow;
            try
            {
                _logger.LogInformation("Processing simple chat request");
                
                // Get the secondary model
                var secondaryModelName = _settingsManager.DefaultSettings?.SecondaryModel;
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
                var model = _settingsManager.CurrentSettings.ModelList.FirstOrDefault(x => x.ModelName == secondaryModelName);
                if (model == null)
                {
                    return new SimpleChatResponse
                    {
                        Success = false,
                        Error = $"Secondary model '{secondaryModelName}' not found",
                        ProcessingTime = DateTime.UtcNow - startTime
                    };
                }

                var service = ServiceProvider.GetProviderForGuid(_settingsManager.CurrentSettings.ServiceProviders, model.ProviderGuid);
                var aiService = AiServiceResolver.GetAiService(service.ServiceName, _toolService);

                // Create a simple chat request
                var systemPrompt = await _systemPromptService.GetDefaultSystemPromptAsync();
                var conv = new LinearConv(DateTime.Now)
                {
                    systemprompt = systemPrompt?.Content ?? "You are a helpful assistant.",
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
                    ApiSettings = _settingsManager.CurrentSettings.ToApiSettings(),
                    MustNotUseEmbedding = true,
                    UseStreaming = false
                };
                
                var response = await aiService.FetchResponse(requestOptions);
                
                return new SimpleChatResponse
                {
                    Success = response.Success,
                    Response = response.ResponseText,
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
                _logger.LogInformation("Processing chat request for conv {ConvId}", request.ConvId);

                var model = _settingsManager.CurrentSettings.ModelList.First(x => x.ModelName == request.Model);
                var service = ServiceProvider.GetProviderForGuid(_settingsManager.CurrentSettings.ServiceProviders, model.ProviderGuid);
                var aiService = AiServiceResolver.GetAiService(service.ServiceName, _toolService);


                // Wire up streaming events
                aiService.StreamingTextReceived += (sender, text) =>
                {
                    _logger.LogTrace("Received streaming text fragment");
                    StreamingTextReceived?.Invoke(this, text);
                };
                aiService.StreamingComplete += (sender, text) =>
                {
                    _logger.LogDebug("Streaming complete");
                    StreamingComplete?.Invoke(this, text);
                };

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
                else if (!string.IsNullOrEmpty(request.ConvId))
                {
                    // Use conv-specific system prompt
                    var systemPrompt = await _systemPromptService.GetConvSystemPromptAsync(request.ConvId);
                    if (systemPrompt != null)
                    {
                        systemPromptContent = systemPrompt.Content;
                    }
                }
                
                var conv = new LinearConv(DateTime.Now)
                {
                    systemprompt = systemPromptContent,
                    messages = new List<LinearConvMessage>()
                };

                // Get tools if specified
                List<string> toolNames = null;
                if (request.ToolIds != null && request.ToolIds.Any())
                {
                    toolNames = new List<string>();
                    foreach (var toolId in request.ToolIds)
                    {
                        var tool = await _toolService.GetToolByIdAsync(toolId);
                        if (tool != null)
                        {
                            toolNames.Add(tool.Name);
                        }
                    }
                }


                // Add all messages from history first
                foreach (var historyItem in request.MessageHistory.Where(x => x.Role != "system"))
                {
                    conv.messages.Add(new LinearConvMessage
                    {
                        role = historyItem.Role,
                        content = historyItem.Content
                    });
                }

                var requestOptions = new AiRequestOptions
                {
                    ServiceProvider = service,
                    Model = model,
                    Conv = conv,
                    CancellationToken = new CancellationToken(false),
                    ApiSettings = _settingsManager.CurrentSettings.ToApiSettings(),
                    MustNotUseEmbedding = true,
                    ToolIds = request.ToolIds ?? new List<string>(),
                    UseStreaming = true,
                    
                    // No need to set CustomSystemPrompt as we've already set it in the conv object
                };
                
                var response = await aiService.FetchResponse(requestOptions);
                
                // Calculate cost from the model directly
                if (response.TokenUsage != null)
                {
                    response.CostInfo = new AiStudio4.Core.Models.TokenCost(
                        response.TokenUsage,
                        model
                    );
                }

                _logger.LogInformation("Successfully processed chat request");

                var responseText = response.ResponseText;

                if(response.ChosenTool != null)
                {
                    var tool = await _toolService.GetToolByNameAsync(response.ChosenTool);

                    if(tool != null)
                    {
                        responseText = $"{new string('`', 3)}{tool.Filetype}\n{responseText}\n{new string('`', 3)}";
                    }
                }
                return new ChatResponse
                {
                    Success = true,
                    ResponseText = responseText,
                    CostInfo = response.CostInfo
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing chat request");
                throw new ChatProcessingException("Failed to process chat request", ex);
            }
        }
    }
}