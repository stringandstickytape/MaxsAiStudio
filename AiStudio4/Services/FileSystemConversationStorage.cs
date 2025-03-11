using AiStudio4.Core.Exceptions;
using AiStudio4.Core.Interfaces;
using AiStudio4.InjectedDependencies;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
                var json = await File.ReadAllTextAsync(path);
                var conv = JsonConvert.DeserializeObject<v4BranchedConv>(json, settings);

                // Rebuild relationships to ensure Children collections are populated correctly
                RebuildRelationships(conv);

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
            foreach (var file in Directory.GetFiles(_basePath, "*.json"))
            {
                try
                {
                    var settings = new JsonSerializerSettings { MaxDepth = 10240 };
                    var fileInfo = new FileInfo(file);
                    var json = await File.ReadAllTextAsync(file);
                    var conv = JsonConvert.DeserializeObject<v4BranchedConv>(json, settings);

                    if (conv != null)
                    {
                        // Rebuild relationships to ensure Children collections are populated correctly
                        RebuildRelationships(conv);
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
            // Flatten the message hierarchy and check for the message ID
            return GetAllMessages(conv.MessageHierarchy)
                .Any(m => m.Id == messageId);
        }

        private List<v4BranchedConvMessage> GetAllMessages(List<v4BranchedConvMessage> messages)
        {
            var result = new List<v4BranchedConvMessage>();
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

        private void RebuildRelationships(v4BranchedConv conv)
        {
            // Get all messages in a flat list
            var allMessages = GetAllMessages(conv.MessageHierarchy);

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