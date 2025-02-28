using AiStudio4.Services;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AiStudio4.InjectedDependencies
{
    public class ChatManager
    {
        private readonly ConversationService _conversationService;
        private readonly ChatProcessingService _chatProcessingService;
        private readonly MessageHistoryService _messageHistoryService;
        private readonly ILogger<ChatManager> _logger;

        public ChatManager(
            ConversationService conversationService,
            ChatProcessingService chatProcessingService,
            MessageHistoryService messageHistoryService,
            ILogger<ChatManager> logger)
        {
            _conversationService = conversationService;
            _chatProcessingService = chatProcessingService;
            _messageHistoryService = messageHistoryService;
            _logger = logger;
        }

        public async Task<string> HandleGetAllConversationsRequest(string clientId)
        {
            try
            {
                return await _conversationService.HandleGetAllConversationsRequest(clientId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in HandleGetAllConversationsRequest");
                return JsonConvert.SerializeObject(new { success = false, error = ex.Message });
            }
        }

        public async Task<string> HandleChatRequest(string clientId, JObject requestObject)
        {
            try
            {
                return await _chatProcessingService.HandleChatRequest(clientId, requestObject);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in HandleChatRequest");
                return JsonConvert.SerializeObject(new { success = false, error = ex.Message });
            }
        }

        internal async Task<string> HandleConversationMessagesRequest(string clientId, JObject? requestObject)
        {
            try
            {
                return await _messageHistoryService.HandleConversationMessagesRequest(clientId, requestObject);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in HandleConversationMessagesRequest");
                return JsonConvert.SerializeObject(new { success = false, error = ex.Message });
            }
        }

        internal async Task<string> HandleHistoricalConversationTreeRequest(string clientId, JObject? requestObject)
        {
            try
            {
                return await _conversationService.HandleHistoricalConversationTreeRequest(clientId, requestObject);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in HandleHistoricalConversationTreeRequest");
                return JsonConvert.SerializeObject(new { success = false, error = ex.Message });
            }
        }
    }
}