using Microsoft.Extensions.Logging;
using AiStudio4.InjectedDependencies;
using AiStudio4.Core.Exceptions;
using AiStudio4.Core.Models;
using AiStudio4.Core.Interfaces;
using SharedClasses.Providers;
using AiStudio4.AiServices;
using AiStudio4.DataModels;
using AiStudio4.Conversations;

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

        public async Task<ChatResponse> ProcessChatRequest(ChatRequest request)
        {
            try
            {
                _logger.LogInformation("Processing chat request for conversation {ConversationId}", request.ConversationId);

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
                else if (!string.IsNullOrEmpty(request.ConversationId))
                {
                    // Use conversation-specific system prompt
                    var systemPrompt = await _systemPromptService.GetConversationSystemPromptAsync(request.ConversationId);
                    if (systemPrompt != null)
                    {
                        systemPromptContent = systemPrompt.Content;
                    }
                }
                
                var conversation = new LinearConversation(DateTime.Now)
                {
                    systemprompt = systemPromptContent,
                    messages = new List<LinearConversationMessage>()
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
                    conversation.messages.Add(new LinearConversationMessage
                    {
                        role = historyItem.Role,
                        content = historyItem.Content
                    });
                }

                var requestOptions = new AiRequestOptions
                {
                    ServiceProvider = service,
                    Model = model,
                    Conversation = conversation,
                    CancellationToken = new CancellationToken(false),
                    ApiSettings = _settingsManager.CurrentSettings.ToApiSettings(),
                    MustNotUseEmbedding = true,
                    ToolIds = request.ToolIds ?? new List<string>(),
                    UseStreaming = true
                    // No need to set CustomSystemPrompt as we've already set it in the conversation object
                };
                
                var response = await aiService.FetchResponse(requestOptions);

                _logger.LogInformation("Successfully processed chat request");

                return new ChatResponse
                {
                    Success = true,
                    ResponseText = response.ResponseText
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