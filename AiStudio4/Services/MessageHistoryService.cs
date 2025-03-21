using AiStudio4.Core.Interfaces;
using AiStudio4.InjectedDependencies;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AiStudio4.Services
{
    public class MessageHistoryService
    {
        private readonly IConversationStorage _conversationStorage;
        private readonly IWebSocketNotificationService _notificationService;
        private readonly ILogger<MessageHistoryService> _logger;

        public MessageHistoryService(
            IConversationStorage conversationStorage,
            IWebSocketNotificationService notificationService,
            ILogger<MessageHistoryService> logger)
        {
            _conversationStorage = conversationStorage;
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
                    // Get all messages in the conversation for a flat structure
                    var allMessages = new List<v4BranchedConversationMessage>();
                    CollectAllMessages(conversation.MessageHierarchy, allMessages);

                    // Map to the format expected by the client
                    var messages = allMessages.Select(msg =>
                    {
                        return new
                        {
                            id = msg.Id,
                            content = msg.UserMessage,
                            source = msg.Role == v4BranchedConversationMessageRole.User ? "user" :
                                    msg.Role == v4BranchedConversationMessageRole.Assistant ? "ai" : "system",
                            parentId = msg.ParentId,
                            timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                            tokenUsage = msg.TokenUsage
                        };
                    }).ToList();

                    // Sort messages by parentId relationships to ensure proper loading order
                    var sortedMessages = SortMessagesByRelationship(messages.Cast<dynamic>().ToList());

                    await _notificationService.NotifyConversationUpdate(clientId, new Core.Models.ConversationUpdateDto
                    {
                        ConversationId = conversation.ConversationId,
                        MessageId = messageId,
                        Content = new
                        {
                            messageType = "loadConversation",
                            content = new
                            {
                                conversationId = conversation.ConversationId,
                                messages = sortedMessages
                            }
                        }
                    });

                    return JsonConvert.SerializeObject(new { success = true, messages = sortedMessages, conversationId = conversation.ConversationId });
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

        private List<dynamic> SortMessagesByRelationship(List<dynamic> messages)
        {
            // Create a map of parent->children
            var parentChildMap = new Dictionary<string, List<dynamic>>();

            // Find root messages (no parentId)
            var rootMessages = new List<dynamic>();

            foreach (var msg in messages)
            {
                string parentId = msg.parentId;

                if (string.IsNullOrEmpty(parentId))
                {
                    rootMessages.Add(msg);
                }
                else
                {
                    if (!parentChildMap.ContainsKey(parentId))
                    {
                        parentChildMap[parentId] = new List<dynamic>();
                    }
                    parentChildMap[parentId].Add(msg);
                }
            }

            // Sort by timestamp within each parent group
            foreach (var key in parentChildMap.Keys.ToList())
            {
                var childrenList = parentChildMap[key];
                childrenList.Sort((a, b) => ((long)a.timestamp).CompareTo((long)b.timestamp));
            }

            // Function to collect messages in the correct order
            List<dynamic> CollectMessages(List<dynamic> parents)
            {
                var result = new List<dynamic>();

                foreach (var parent in parents)
                {
                    result.Add(parent);

                    if (parentChildMap.TryGetValue((string)parent.id, out var childrenList))
                    {
                        result.AddRange(CollectMessages(childrenList));
                    }
                }

                return result;
            }

            // Sort root messages by timestamp
            rootMessages.Sort((a, b) => ((long)a.timestamp).CompareTo((long)b.timestamp));

            // Return messages in tree traversal order
            return CollectMessages(rootMessages);
        }
    }
}