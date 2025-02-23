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

        public async Task<string> HandleGetAllConversationsRequest(string clientId)
        {
            try
            {
                var conversations = await _conversationStorage.GetAllConversations();

                foreach (var conversation in conversations.Where(c => c.MessageHierarchy.Any()))
                {
                    var tree = _treeBuilder.BuildCachedConversationTree(conversation);
                    
                    await _notificationService.NotifyConversationList(clientId, new ConversationListDto
                    {
                        ConversationId = conversation.ConversationId,
                        Summary = conversation.MessageHierarchy.First().Children[0].UserMessage,
                        LastModified = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                        TreeData = tree
                    });
                }

                return JsonConvert.SerializeObject(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling get all conversations request");
                return JsonConvert.SerializeObject(new { success = false, error = ex.Message });
            }
        }

        public async Task<string> HandleCachedConversationRequest(string clientId, JObject requestObject)
        {
            try
            {
                var conversationId = requestObject["conversationId"].ToString();
                var conversation = await _conversationStorage.LoadConversation(conversationId);

                if (conversation == null)
                {
                    return JsonConvert.SerializeObject(new { success = false, error = "Conversation not found" });
                }

                var tree = _treeBuilder.BuildCachedConversationTree(conversation);
                return JsonConvert.SerializeObject(new
                {
                    success = true,
                    treeData = tree
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling cached conversation request");
                return JsonConvert.SerializeObject(new { success = false, error = ex.Message });
            }
        }
    }
}