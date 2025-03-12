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
        private readonly IConvStorage _convStorage;
        private readonly IWebSocketNotificationService _notificationService;
        private readonly ILogger<MessageHistoryService> _logger;

        public MessageHistoryService(
            IConvStorage convStorage,
            IWebSocketNotificationService notificationService,
            ILogger<MessageHistoryService> logger)
        {
            _convStorage = convStorage;
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task<string> HandleConvMessagesRequest(string clientId, JObject requestObject)
        {
            try
            {
                var messageId = requestObject["messageId"].ToString();
                var conv = await _convStorage.FindConvByMessageId(messageId);

                if (conv != null)
                {
                    // Get all messages from the flat structure
                    var allMessages = conv.Messages;

                    // Map to the format expected by the client
                    var messages = allMessages.Select(msg =>
                    {
                        return new
                        {
                            id = msg.Id,
                            content = msg.UserMessage,
                            source = msg.Role == v4BranchedConvMessageRole.User ? "user" :
                                    msg.Role == v4BranchedConvMessageRole.Assistant ? "ai" : "system",
                            parentId = msg.ParentId,
                            timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                            costInfo = msg.CostInfo
                        };
                    }).ToList();

                    // Sort messages by parentId relationships to ensure proper loading order
                    var sortedMessages = SortMessagesByRelationship(messages.Cast<dynamic>().ToList());

                    await _notificationService.NotifyConvUpdate(clientId, new Core.Models.ConvUpdateDto
                    {
                        ConvId = conv.ConvId,
                        MessageId = messageId,
                        Content = new
                        {
                            messageType = "loadConv",
                            content = new
                            {
                                convId = conv.ConvId,
                                messages = sortedMessages
                            }
                        }
                    });

                    return JsonConvert.SerializeObject(new { success = true, messages = sortedMessages, convId = conv.ConvId });
                }
                return JsonConvert.SerializeObject(new { success = false, error = "Message not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling conv messages request");
                return JsonConvert.SerializeObject(new { success = false, error = ex.Message });
            }
        }

        // No longer needed with flat message structure

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