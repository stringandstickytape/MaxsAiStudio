using AiStudio4.Core.Exceptions;
using AiStudio4.Core.Models;
using AiStudio4.Core.Interfaces;
using AiStudio4.InjectedDependencies;
using Newtonsoft.Json;
using System.IO;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

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
                var conversation = JsonConvert.DeserializeObject<v4BranchedConversation>(json,settings);
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
            foreach (var file in Directory.GetFiles(_basePath, "conv_*.json"))
            {
                try
                {
                    var settings = new JsonSerializerSettings { MaxDepth = 10240 };
                    var fileInfo = new FileInfo(file);
                    var json = await File.ReadAllTextAsync(file);
                    var conversation = JsonConvert.DeserializeObject<v4BranchedConversation>(json, settings);
                    if (conversation != null)
                        conversationsWithDates.Add((conversation, fileInfo.LastWriteTime));
                }
                catch (Exception ex)
                {
                    // Log error and continue
                }
            }

            // Order by file creation date in descending order (newest first)
            return conversationsWithDates
                .OrderByDescending(x => x.FileDate)
                .Select(x => x.Conversation);
        }

        public async Task<v4BranchedConversation> FindConversationByMessageId(string messageId)
        {
            var conversations = await GetAllConversations();
            return conversations.FirstOrDefault(c => ContainsMessage(c, messageId));
        }

        private bool ContainsMessage(v4BranchedConversation conversation, string messageId)
        {
            bool SearchMessage(v4BranchedConversationMessage message)
            {
                if (message.Id == messageId) return true;
                return message.Children.Any(SearchMessage);
            }

            return conversation.MessageHierarchy.Any(SearchMessage);
        }
    }
}