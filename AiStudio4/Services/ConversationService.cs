using AiStudio4.Core.Interfaces;
using AiStudio4.Core.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Logging;

namespace AiStudio4.Services
{
    public class ConversationService
    {
        private readonly IConversationStorage _conversationStorage;
        private readonly IConversationTreeBuilder _treeBuilder;
        private readonly IWebSocketNotificationService _notificationService;
        private readonly ILogger<ConversationService> _logger;

        public ConversationService(
            IConversationStorage conversationStorage,
            IConversationTreeBuilder treeBuilder,
            IWebSocketNotificationService notificationService,
            ILogger<ConversationService> logger)
        {
            _conversationStorage = conversationStorage;
            _treeBuilder = treeBuilder;
            _notificationService = notificationService;
            _logger = logger;
        }



        public async Task<string> HandleHistoricalConversationTreeRequest(string clientId, JObject requestObject)
        {
            try
            {
                var conversationId = requestObject["conversationId"].ToString();
                var conversation = await _conversationStorage.LoadConversation(conversationId);

                if (conversation == null)
                {
                    return JsonConvert.SerializeObject(new { success = false, error = "Conversation not found" });
                }

                var tree = _treeBuilder.BuildHistoricalConversationTree(conversation);
                return JsonConvert.SerializeObject(new
                {
                    success = true,
                    treeData = tree
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling historical conversation tree request");
                return JsonConvert.SerializeObject(new { success = false, error = ex.Message });
            }
        }
    }
}