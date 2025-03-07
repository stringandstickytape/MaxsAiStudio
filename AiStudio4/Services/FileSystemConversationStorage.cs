using AiStudio4.Core.Exceptions;
using AiStudio4.Core.Interfaces;
using AiStudio4.InjectedDependencies;
using AiTool3.Conversations;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AiStudio4.Services
{
    public class FileSystemConversationStorage : IConversationStorage
    {
        private readonly string _basePath;
        private readonly ILogger<FileSystemConversationStorage> _logger;

        public FileSystemConversationStorage(ILogger<FileSystemConversationStorage> logger)
        {
            _logger = logger;
            _basePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "AiStudio4",
                "conversations");
            Directory.CreateDirectory(_basePath);
            _logger.LogInformation("Initialized conversation storage at {BasePath}", _basePath);
        }

        public async Task<v4BranchedConversation> LoadConversation(string conversationId)
        {
            try
            {
                var path = Path.Combine(_basePath, $"{conversationId}.json");
                if (!File.Exists(path))
                {
                    _logger.LogInformation("Creating new conversation with ID {ConversationId}", conversationId);
                    return new v4BranchedConversation(conversationId);
                }

                var settings = new JsonSerializerSettings { MaxDepth = 10240 };
                var json = await File.ReadAllTextAsync(path);
                var conversation = JsonConvert.DeserializeObject<v4BranchedConversation>(json, settings);

                // Rebuild relationships to ensure Children collections are populated correctly
                RebuildRelationships(conversation);

                _logger.LogDebug("Loaded conversation {ConversationId}", conversationId);
                return conversation;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading conversation {ConversationId}", conversationId);
                throw new ConversationStorageException($@"Failed to load conversation {conversationId}", ex);
            }
        }

        public async Task SaveConversation(v4BranchedConversation conversation)
        {
            try
            {
                if (conversation == null) throw new ArgumentNullException(nameof(conversation));

                var path = Path.Combine(_basePath, $"{conversation.ConversationId}.json");
                var json = JsonConvert.SerializeObject(conversation, new JsonSerializerSettings
                {
                    Formatting = Formatting.Indented,
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                });

                await File.WriteAllTextAsync(path, json);
                _logger.LogDebug("Saved conversation {ConversationId}", conversation.ConversationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving conversation {ConversationId}", conversation?.ConversationId);
                throw new ConversationStorageException($@"Failed to save conversation {conversation?.ConversationId}", ex);
            }
        }

        public async Task<IEnumerable<v4BranchedConversation>> GetAllConversations()
        {
            var conversationsWithDates = new List<(v4BranchedConversation Conversation, DateTime FileDate)>();
            foreach (var file in Directory.GetFiles(_basePath, "*.json"))
            {
                try
                {
                    var settings = new JsonSerializerSettings { MaxDepth = 10240 };
                    var fileInfo = new FileInfo(file);
                    var json = await File.ReadAllTextAsync(file);
                    var conversation = JsonConvert.DeserializeObject<v4BranchedConversation>(json, settings);

                    if (conversation != null)
                    {
                        // Rebuild relationships to ensure Children collections are populated correctly
                        RebuildRelationships(conversation);
                        conversationsWithDates.Add((conversation, fileInfo.LastWriteTime));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error loading conversation from {File}", file);
                    // Continue with next file
                }
            }

            // Order by file creation date in descending order (newest first)
            return conversationsWithDates
                .OrderByDescending(x => x.FileDate)
                .Select(x => x.Conversation);
        }

        public async Task<v4BranchedConversation> FindConversationByMessageId(string messageId)
        {
            try
            {
                var conversations = await GetAllConversations();
                return conversations.FirstOrDefault(c => ContainsMessage(c, messageId));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding conversation by message ID {MessageId}", messageId);
                throw new ConversationStorageException($"Failed to find conversation containing message {messageId}", ex);
            }
        }

        private bool ContainsMessage(v4BranchedConversation conversation, string messageId)
        {
            // Flatten the message hierarchy and check for the message ID
            return GetAllMessages(conversation.MessageHierarchy)
                .Any(m => m.Id == messageId);
        }

        private List<v4BranchedConversationMessage> GetAllMessages(List<v4BranchedConversationMessage> messages)
        {
            var result = new List<v4BranchedConversationMessage>();
            foreach (var message in messages)
            {
                result.Add(message);
                if (message.Children.Any())
                {
                    result.AddRange(GetAllMessages(message.Children));
                }
            }
            return result;
        }

        private void RebuildRelationships(v4BranchedConversation conversation)
        {
            // Get all messages in a flat list
            var allMessages = GetAllMessages(conversation.MessageHierarchy);

            // Clear all Children collections
            foreach (var message in allMessages)
            {
                message.Children.Clear();
            }

            // Rebuild Children collections based on ParentId
            foreach (var message in allMessages.Where(m => !string.IsNullOrEmpty(m.ParentId)))
            {
                var parent = allMessages.FirstOrDefault(m => m.Id == message.ParentId);
                if (parent != null)
                {
                    parent.Children.Add(message);
                }
            }
        }
    }
}