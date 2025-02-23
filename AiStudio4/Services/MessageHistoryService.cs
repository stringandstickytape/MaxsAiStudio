using AiStudio4.Core.Interfaces;
using AiStudio4.Core.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Logging;
using AiStudio4.InjectedDependencies;

namespace AiStudio4.Services
{
    public class MessageHistoryService
    {
        private readonly IConversationStorage _conversationStorage;
        private readonly IConversationTreeBuilder _treeBuilder;
        private readonly IWebSocketNotificationService _notificationService;
        private readonly ILogger<MessageHistoryService> _logger;

        public MessageHistoryService(
            IConversationStorage conversationStorage,
            IConversationTreeBuilder treeBuilder,
            IWebSocketNotificationService notificationService,
            ILogger<MessageHistoryService> logger)
        {
            _conversationStorage = conversationStorage;
            _treeBuilder = treeBuilder;
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task<string> HandleConversationMessagesRequest(string clientId, JObject requestObject)
        {
            try
            {
                var messageId = requestObject["messageId"].ToString();
                var conversation = await _conversationStorage.FindConversationByMessageId(messageId);
                
                if (conversation != null)
                {
                    var messageHistory = _treeBuilder.GetMessageHistory(conversation, messageId);
                    var messages = messageHistory.Select(msg => new
                    {
                        id = msg.Id,
                        content = msg.UserMessage,
                        source = msg.Role == v4BranchedConversationMessageRole.User ? "user" : 
                                msg.Role == v4BranchedConversationMessageRole.Assistant ? "ai" : "system",
                        timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                    }).ToList();

                    await _notificationService.NotifyConversationUpdate(clientId, new ConversationUpdateDto
                    {
                        ConversationId = conversation.ConversationId,
                        MessageId = messageId,
                        Content = new
                        {
                            messageType = "loadConversation",
                            content = new
                            {
                                conversationId = conversation.ConversationId,
                                messages = messages
                            }
                        }
                    });

                    return JsonConvert.SerializeObject(new { success = true, messages = messages, conversationId = conversation.ConversationId });
                }
                return JsonConvert.SerializeObject(new { success = false, error = "Message not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling conversation messages request");
                return JsonConvert.SerializeObject(new { success = false, error = ex.Message });
            }
        }
    }
}