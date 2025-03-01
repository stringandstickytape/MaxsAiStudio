using AiStudio4.Core.Interfaces;
using AiStudio4.Core.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Logging;
using System.IO;

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
                    treeData = tree,
                    summary = conversation.Summary
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

                // Build tree for each conversation
                var conversationTrees = new List<object>();
                foreach (var conversation in conversations)
                {
                    try
                    {
                        if (conversation.MessageHierarchy?.Count > 0)
                        {
                            var firstMessage = conversation.MessageHierarchy.First().Children?.FirstOrDefault();
                            var summary = conversation.Summary ?? firstMessage?.UserMessage ?? "Untitled Conversation";
                            
                            conversationTrees.Add(new
                            {
                                conversationId = conversation.ConversationId,
                                summary = summary.Length > 150 ? summary.Substring(0, 150) + "..." : summary,
                                lastModified = File.GetLastWriteTimeUtc(Path.Combine(
                                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                                    "AiStudio4",
                                    "conversations",
                                    $"{conversation.ConversationId}.json")).ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                                treeData = _treeBuilder.BuildHistoricalConversationTree(conversation)
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error building tree for conversation {ConversationId}", conversation.ConversationId);
                        // Continue with next conversation
                    }
                }

                return JsonConvert.SerializeObject(new { success = true, conversations = conversationTrees });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling get all historical conversation trees request");
                return JsonConvert.SerializeObject(new { success = false, error = ex.Message });
            }
        }
    }
}