using AiTool3.AiServices;
using AiTool3.Conversations;
using AiTool3.DataModels;
using Microsoft.Extensions.Logging;
using AiStudio4.InjectedDependencies;
using AiStudio4.Core.Exceptions;
using AiStudio4.Core.Models;
using AiStudio4.Core.Interfaces;
using SharedClasses.Providers;

namespace AiStudio4.Services
{
    public class OpenAIChatService : IChatService
    {
        private readonly ILogger<OpenAIChatService> _logger;
        private readonly SettingsManager _settingsManager;

        public event EventHandler<string> StreamingTextReceived;
        public event EventHandler<string> StreamingComplete;

        public OpenAIChatService(ILogger<OpenAIChatService> logger, SettingsManager settingsManager)
        {
            _logger = logger;
            _settingsManager = settingsManager;
        }

        public async Task<ChatResponse> ProcessChatRequest(ChatRequest request)
        {
            try
            {
                _logger.LogInformation("Processing chat request for conversation {ConversationId}", request.ConversationId);

                var model = _settingsManager.CurrentSettings.ModelList.First(x => x.ModelName == request.Model);
                var service = ServiceProvider.GetProviderForGuid(_settingsManager.CurrentSettings.ServiceProviders, model.ProviderGuid);
                var aiService = AiServiceResolver.GetAiService(service.ServiceName, null);

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

                // Add all messages from history first
                foreach (var historyItem in request.MessageHistory.Where(x => x.Role != "system"))
                {
                    conversation.messages.Add(new LinearConversationMessage
                    {
                        role = historyItem.Role,
                        content = historyItem.Content
                    });
                }

                var response = await aiService.FetchResponse(
                    service,
                    model,
                    conversation,
                    null,
                    null,
                    new CancellationToken(false),
                    _settingsManager.CurrentSettings.ToApiSettings(),
                    mustNotUseEmbedding: true,
                    toolNames: null,
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