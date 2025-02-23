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
                    // Get all messages in the conversation to preserve full hierarchy
                    var allMessages = new List<v4BranchedConversationMessage>();
                    CollectAllMessages(conversation.MessageHierarchy, allMessages);

                    var messages = allMessages.Select(msg =>
                    {
                        // Find parent by checking message relationships in full hierarchy
                        var parentMessage = allMessages.FirstOrDefault(m => m.Children.Any(c => c.Id == msg.Id));
                        return new
                        {
                            id = msg.Id,
                            content = msg.UserMessage,
                            source = msg.Role == v4BranchedConversationMessageRole.User ? "user" : 
                                    msg.Role == v4BranchedConversationMessageRole.Assistant ? "ai" : "system",
                            parentId = parentMessage?.Id,
                            timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                        };
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

        private void CollectAllMessages(IEnumerable<v4BranchedConversationMessage> messages, List<v4BranchedConversationMessage> allMessages)
        {
            foreach (var message in messages)
            {
                allMessages.Add(message);
                if (message.Children != null && message.Children.Any())
                {
                    CollectAllMessages(message.Children, allMessages);
                }
            }
        }

    }
}