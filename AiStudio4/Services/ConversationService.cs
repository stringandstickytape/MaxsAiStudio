using AiStudio4.Core.Interfaces;
using AiStudio4.Core.Models;
using AiStudio4.InjectedDependencies;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AiStudio4.Services
{
    public class ConversationService
    {
        private readonly IConversationStorage _conversationStorage;
        private readonly IWebSocketNotificationService _notificationService;
        private readonly ILogger<ConversationService> _logger;

        public ConversationService(
            IConversationStorage conversationStorage,
            IWebSocketNotificationService notificationService,
            ILogger<ConversationService> logger)
        {
            _conversationStorage = conversationStorage;
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

                // Get all messages in a flat structure
                var allMessages = conversation.GetAllMessages();

                // Convert to the format expected by the client
                var messagesForClient = allMessages.Select(msg => new {
                    id = msg.Id,
                    text = msg.UserMessage?.Length > 20
                        ? msg.UserMessage.Substring(0, 20) + "..."
                        : msg.UserMessage ?? "[Empty Message]",
                    parentId = msg.ParentId,
                    source = msg.Role == v4BranchedConversationMessageRole.User ? "user" :
                            msg.Role == v4BranchedConversationMessageRole.Assistant ? "ai" : "system"
                }).ToList();

                return JsonConvert.SerializeObject(new
                {
                    success = true,
                    conversationId = conversation.ConversationId,
                    summary = conversation.Summary ?? "Untitled Conversation",
                    treeData = messagesForClient  // Flat structure with parentId references
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling historical conversation tree request");
                return JsonConvert.SerializeObject(new { success = false, error = ex.Message });
            }
        }

        public async Task<string> HandleGetAllHistoricalConversationTreesRequest(string clientId)
        {
            try
            {
                // Get all conversations from storage
                var conversations = await (_conversationStorage as FileSystemConversationStorage)?.GetAllConversations();

                if (conversations == null || !conversations.Any())
                {
                    return JsonConvert.SerializeObject(new { success = true, conversations = new List<object>() });
                }

                // Build conversation metadata for each conversation
                var conversationList = new List<object>();
                foreach (var conversation in conversations)
                {
                    try
                    {
                        if (conversation.MessageHierarchy?.Count > 0)
                        {
                            // Find the first non-system message to use as summary if needed
                            var allMessages = conversation.GetAllMessages();
                            var firstUserMessage = allMessages
                                .Where(m => m.Role != v4BranchedConversationMessageRole.System)
                                .OrderBy(m => m.Id)
                                .FirstOrDefault();

                            var summary = conversation.Summary ??
                                (firstUserMessage?.UserMessage ?? "Untitled Conversation");

                            // For each conversation, create an entry with just the metadata
                            // No need to include full messages here
                            conversationList.Add(new
                            {
                                conversationId = conversation.ConversationId,
                                convGuid = conversation.ConversationId,
                                summary = summary.Length > 150 ? summary.Substring(0, 150) + "..." : summary,
                                fileName = $"conv_{conversation.ConversationId}.json",
                                lastModified = File.GetLastWriteTimeUtc(Path.Combine(
                                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                                    "AiStudio4",
                                    "conversations",
                                    $"{conversation.ConversationId}.json")).ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error building metadata for conversation {ConversationId}", conversation.ConversationId);
                        // Continue with next conversation
                    }
                }

                return JsonConvert.SerializeObject(new { success = true, conversations = conversationList });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling get all historical conversation trees request");
                return JsonConvert.SerializeObject(new { success = false, error = ex.Message });
            }
        }
    }
}