using AiStudio4.Core.Exceptions;
using AiStudio4.Core.Interfaces;
using AiStudio4.InjectedDependencies;
using AiStudio4.Core.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace AiStudio4.Services
{
    public class FileSystemConvStorage : IConvStorage
    {
        private readonly string _basePath;
        private readonly ILogger<FileSystemConvStorage> _logger;

        public FileSystemConvStorage(ILogger<FileSystemConvStorage> logger)
        {
            _logger = logger;
            _basePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "AiStudio4",
                "convs");
            Directory.CreateDirectory(_basePath);
            _logger.LogInformation("Initialized conv storage at {BasePath}", _basePath);
        }

        public async Task<v4BranchedConv> LoadConv(string convId)
        {
            try
            {
                var path = Path.Combine(_basePath, $"{convId}.json");
                if (!File.Exists(path))
                {
                    _logger.LogInformation("Creating new conv with ID {ConvId}", convId);
                    return new v4BranchedConv(convId);
                }

                var settings = new JsonSerializerSettings { MaxDepth = 10240 };
                var json = await File.ReadAllTextAsync(path);                var conv = JsonConvert.DeserializeObject<v4BranchedConv>(json, settings);
                
                // Handle backward compatibility with old hierarchical structure
                if (conv.Messages == null || !conv.Messages.Any())
                {
                    conv.Messages = new List<v4BranchedConvMessage>();
                }

                // MIGRATION: convert legacy UserMessage -> ContentBlocks
                foreach (var message in conv.Messages)
                {
                    if ((message.ContentBlocks == null || message.ContentBlocks.Count == 0) && !string.IsNullOrEmpty(message.UserMessage))
                    {
                        message.ContentBlocks = new List<ContentBlock>
                        {
                            new ContentBlock { Content = message.UserMessage, ContentType = ContentType.Text }
                        };
                        message.UserMessage = null;
                    }
                }

                _logger.LogDebug("Loaded conv {ConvId}", convId);
                return conv;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading conv {ConvId}", convId);
                throw new ConvStorageException($@"Failed to load conv {convId}", ex);
            }
        }

        public async Task SaveConv(v4BranchedConv conv)
        {
            try
            {
                if (conv == null) throw new ArgumentNullException(nameof(conv));

                var path = Path.Combine(_basePath, $"{conv.ConvId}.json");
                var json = JsonConvert.SerializeObject(conv, new JsonSerializerSettings
                {
                    Formatting = Formatting.Indented,
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                });

                await File.WriteAllTextAsync(path, json);
                _logger.LogDebug("Saved conv {ConvId}", conv.ConvId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving conv {ConvId}", conv?.ConvId);
                throw new ConvStorageException($@"Failed to save conv {conv?.ConvId}", ex);
            }
        }

        public async Task<IEnumerable<v4BranchedConv>> GetAllConvs()
        {
            var convsWithDates = new List<(v4BranchedConv Conv, DateTime FileDate)>();
            foreach (var file in Directory.EnumerateFiles(_basePath, "*.json", SearchOption.TopDirectoryOnly))
            {
                try
                {
                    var settings = new JsonSerializerSettings { MaxDepth = 10240 };
                    var fileInfo = new FileInfo(file);
                    var json = await File.ReadAllTextAsync(file);
                    var conv = JsonConvert.DeserializeObject<v4BranchedConv>(json, settings);

                    if (conv != null)
                    {
                        // Handle backward compatibility with old hierarchical structure
                        if (conv.Messages == null || !conv.Messages.Any())
                        {
                            conv.Messages = new List<v4BranchedConvMessage>();
                        }
                        convsWithDates.Add((conv, fileInfo.LastWriteTime));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error loading conv from {File}", file);
                    // Continue with next file
                }
            }

            // Order by file creation date in descending order (newest first)
            return convsWithDates
                .OrderByDescending(x => x.FileDate)
                .Select(x => x.Conv);
        }

        public async Task<v4BranchedConv> FindConvByMessageId(string messageId)
        {
            try
            {
                var convs = await GetAllConvs();
                return convs.FirstOrDefault(c => ContainsMessage(c, messageId));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding conv by message ID {MessageId}", messageId);
                throw new ConvStorageException($"Failed to find conv containing message {messageId}", ex);
            }
        }

        private bool ContainsMessage(v4BranchedConv conv, string messageId)
        {
            // Check if any message in the flat structure has the given ID
            return conv.Messages.Any(m => m.Id == messageId);
        }

        /// <summary>
        /// Deletes a conversation by its ID
        /// </summary>
        /// <param name="convId">The ID of the conversation to delete</param>
        /// <returns>True if the conversation was successfully deleted, false otherwise</returns>
        public async Task<bool> DeleteConv(string convId)
        {
            try
            {
                if (string.IsNullOrEmpty(convId))
                {
                    throw new ArgumentException("Conversation ID cannot be empty", nameof(convId));
                }

                var path = Path.Combine(_basePath, $"{convId}.json");
                if (!File.Exists(path))
                {
                    _logger.LogWarning("Cannot delete conversation: file not found for ID {ConvId}", convId);
                    return false;
                }

                File.Delete(path);
                _logger.LogInformation("Deleted conversation {ConvId}", convId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting conversation {ConvId}", convId);
                throw new ConvStorageException($"Failed to delete conversation {convId}", ex);
            }
        }

        // Methods removed as they're no longer needed with flat structure

        /// <summary>
        /// Streams all conversations for content matching the search term
        /// </summary>
        /// <param name="searchTerm">The term to search for</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
        /// <returns>Yields search results as they are found</returns>
        public async IAsyncEnumerable<ConversationSearchResult> SearchConversationsStreamingAsync(string searchTerm, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            // Get all conversation file paths without loading content
            var convFiles = Directory.EnumerateFiles(_basePath, "*.json", SearchOption.TopDirectoryOnly).OrderByDescending(file => File.GetLastWriteTimeUtc(file));

            foreach (var filePath in convFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();

                ConversationSearchResult result = null;

                try
                {
                    // Extract conversation ID from filename
                    var convId = Path.GetFileNameWithoutExtension(filePath);
                    var fileInfo = new FileInfo(filePath);

                    // Load conversation individually
                    var conv = await LoadConv(convId);

                    // Search for matches in this conversation
                    var matchingMessageIds = new List<string>();
                    foreach (var message in conv.Messages)
                    {
                        // Skip system messages
                        if (message.Role == v4BranchedConvMessageRole.System)
                            continue;                        // Search inside rich content blocks first
                        bool isMatch = false;
                        if (message.ContentBlocks != null && message.ContentBlocks.Any())
                        {
                            foreach (var block in message.ContentBlocks)
                            {
                                if (block.ContentType == ContentType.Text &&
                                    block.Content != null &&
                                    block.Content.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0)
                                {
                                    isMatch = true;
                                    break;
                                }
                            }
                        }

                        if (isMatch)
                        {
                            matchingMessageIds.Add(message.Id);
                        }
                    }

                    if (matchingMessageIds.Count > 0)
                    {
                        result = new ConversationSearchResult
                        {
                            ConversationId = conv.ConvId,
                            MatchingMessageIds = matchingMessageIds,
                            ConversationSummary = conv.Summary ?? "Untitled Conversation",
                            LastModified = fileInfo.LastWriteTime
                        };
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error searching conversation file {FilePath}", filePath);
                    // Continue with next file instead of failing the entire search
                }

                // Yield outside the try-catch
                if (result != null)
                {
                    yield return result;
                }
            }
        }
        // Legacy batch search method for compatibility (can be removed if not used elsewhere)
        public async Task<List<ConversationSearchResult>> SearchConversationsAsync(string searchTerm, CancellationToken cancellationToken)
        {
            var results = new List<ConversationSearchResult>();
            await foreach (var result in SearchConversationsStreamingAsync(searchTerm, cancellationToken))
            {
                results.Add(result);
            }
            return results;
        }
    }
}