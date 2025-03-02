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

        public event EventHandler<string> StreamingTextReceived;
        public event EventHandler<string> StreamingComplete;

        public OpenAIChatService(ILogger<OpenAIChatService> logger, SettingsManager settingsManager, IToolService toolService)
        {
            _logger = logger;
            _settingsManager = settingsManager;
            _toolService = toolService;
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

                // here, we should take request.messagehistory and use it to build the conversation, then add the new user message too.
                var conversation = new LinearConversation(DateTime.Now)
                {
                    systemprompt = "You are a helpful chatbot.",
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

                // Pass the tool IDs to FetchResponse to ensure tools are available
                var response = await aiService.FetchResponse(
                    service,
                    model,
                    conversation,
                    null,
                    null,
                    new CancellationToken(false),
                    _settingsManager.CurrentSettings.ToApiSettings(),
                    mustNotUseEmbedding: true,
                    toolIds: request.ToolIds ?? new(), // Use the original tool IDs from the request
                    useStreaming: true);

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