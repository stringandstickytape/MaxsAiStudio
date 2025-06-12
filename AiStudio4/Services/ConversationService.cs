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
    public class ConvService
    {
        private readonly IConvStorage _convStorage;
        private readonly IWebSocketNotificationService _notificationService;
        private readonly ILogger<ConvService> _logger;

        public ConvService(
            IConvStorage convStorage,
            IWebSocketNotificationService notificationService,
            ILogger<ConvService> logger)
        {
            _convStorage = convStorage;
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task<string> HandleHistoricalConvTreeRequest(string clientId, JObject requestObject)
        {
            try
            {
                var convId = requestObject["convId"].ToString();
                var conv = await _convStorage.LoadConv(convId);

                if (conv == null)
                {
                    return JsonConvert.SerializeObject(new { success = false, error = "Conv not found" });
                }

                // Get all messages in a flat structure
                var allMessages = conv.GetAllMessages();

                // Convert to the format expected by the client
                var messagesForClient = allMessages.Select(msg => new {
                    id = msg.Id,
                    text = msg.ContentBlocks != null && msg.ContentBlocks.Any() 
                        ? string.Join("\n\n", msg.ContentBlocks.Select(cb => cb.Content))
                        : msg.UserMessage ?? "[Empty Message]",
                    contentBlocks = msg.ContentBlocks ?? 
                        (string.IsNullOrEmpty(msg.UserMessage) ? new List<ContentBlock>() : 
                         new List<ContentBlock> { new ContentBlock { Content = msg.UserMessage, ContentType = ContentType.Text } }),
                    parentId = msg.ParentId,
                    source = msg.Role == v4BranchedConvMessageRole.User ? "user" :
                            msg.Role == v4BranchedConvMessageRole.Assistant ? "ai" : "system",
                    costInfo = msg.CostInfo,
                    attachments = msg.Attachments,
                    timestamp = new DateTimeOffset(msg.Timestamp).ToUnixTimeMilliseconds(),
                    durationMs = msg.DurationMs,
                    cumulativeCost = msg.CumulativeCost,
                    temperature = msg.Temperature
                }).ToList();

                return JsonConvert.SerializeObject(new
                {
                    success = true,
                    convId = conv.ConvId,
                    summary = conv.Summary ?? "Untitled Conv",
                    flatMessageStructure = messagesForClient 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling historical conv tree request");
                return JsonConvert.SerializeObject(new { success = false, error = ex.Message });
            }
        }

        public async Task<string> HandleDeleteMessageWithDescendantsRequest(string clientId, JObject requestObject)
        {
            try
            {
                var convId = requestObject["convId"].ToString();
                var messageId = requestObject["messageId"].ToString();
                
                var conv = await _convStorage.LoadConv(convId);
                
                if (conv == null)
                {
                    return JsonConvert.SerializeObject(new { success = false, error = "Conversation not found" });
                }
                
                // Get all messages to find descendants
                var allMessages = conv.GetAllMessages().ToList();
                var toDelete = new HashSet<string>();
                
                // Helper function to recursively find all descendants
                void FindDescendants(string id)
                {
                    toDelete.Add(id);
                    var children = allMessages.Where(m => m.ParentId == id).ToList();
                    foreach (var child in children)
                    {
                        FindDescendants(child.Id);
                    }
                }
                
                // Start the recursive search
                FindDescendants(messageId);
                
                // Remove all messages marked for deletion
                var messagesToKeep = allMessages.Where(m => !toDelete.Contains(m.Id)).ToList();
                
                // Update the conversation (this depends on your data structure)
                conv.Messages = messagesToKeep;
                
                // Save the updated conversation
                await _convStorage.SaveConv(conv);
                
                // Notify client of success
                await _notificationService.NotifyConvUpdate(clientId, new ConvUpdateDto
                {
                    ConvId = conv.ConvId,
                    MessageId = messageId,
                    Content = new { type = "messageDeleted", messageId, descendantCount = toDelete.Count - 1 }
                });
                
                return JsonConvert.SerializeObject(new { success = true, deletedCount = toDelete.Count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling delete message with descendants request");
                return JsonConvert.SerializeObject(new { success = false, error = ex.Message });
            }
        }

        public async Task<string> HandleDeleteConvRequest(string clientId, JObject requestObject)
        {
            try
            {
                var convId = requestObject["convId"]?.ToString();
                if (string.IsNullOrEmpty(convId))
                {
                    return JsonConvert.SerializeObject(new { success = false, error = "Conversation ID cannot be empty" });
                }
                
                var success = await _convStorage.DeleteConv(convId);
                
                if (success)
                {
                    // Notify client of success via WebSocket
                    await _notificationService.NotifyConvUpdate(clientId, new ConvUpdateDto
                    {
                        ConvId = convId,
                        MessageId = null,
                        Content = new { type = "conversationDeleted", convId }
                    });
                    
                    return JsonConvert.SerializeObject(new { success = true });
                }
                else
                {
                    return JsonConvert.SerializeObject(new { success = false, error = "Failed to delete conversation" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling delete conversation request");
                return JsonConvert.SerializeObject(new { success = false, error = ex.Message });
            }
        }

        public async Task<string> HandleGetAllHistoricalConvTreesRequest(string clientId)
        {
            try
            {
                // Get all convs from storage
                var convs = await (_convStorage as FileSystemConvStorage)?.GetAllConvs();

                if (convs == null || !convs.Any())
                {
                    return JsonConvert.SerializeObject(new { success = true, convs = new List<object>() });
                }

                // Build conv metadata for each conv
                var convList = new List<object>();
                foreach (var conv in convs)
                {
                    try
                    {
                        if (conv.Messages?.Count > 0)
                        {
                            // Find the first non-system message to use as summary if needed
                            var firstUserMessage = conv.Messages
                                .Where(m => m.Role != v4BranchedConvMessageRole.System)
                                .OrderBy(m => m.Id)
                                .FirstOrDefault();

                            var summary = conv.Summary ??
                                (firstUserMessage?.UserMessage ?? "Untitled Conv");

                            // For each conv, create an entry with just the metadata
                            // No need to include full messages here
                            convList.Add(new
                            {
                                convId = conv.ConvId,
                                convGuid = conv.ConvId,
                                summary = summary.Length > 150 ? summary.Substring(0, 150) + "..." : summary,
                                fileName = $"conv_{conv.ConvId}.json",
                                lastModified = File.GetLastWriteTimeUtc(Path.Combine(
                                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                                    "AiStudio4",
                                    "convs",
                                    $"{conv.ConvId}.json")).ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error building metadata for conv {ConvId}", conv.ConvId);
                        // Continue with next conv
                    }
                }

                return JsonConvert.SerializeObject(new { success = true, convs = convList });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling get all historical conv trees request");
                return JsonConvert.SerializeObject(new { success = false, error = ex.Message });
            }
        }
    }
}