using AiStudio4.Core.Interfaces;
using AiStudio4.Core.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace AiStudio4.Services
{
    public class PinnedCommandService : IPinnedCommandService
    {
        private readonly ILogger<PinnedCommandService> _logger;
        private readonly string _pinnedCommandsDirectory;
        private readonly string _pinnedCommandsFilePath;

        public PinnedCommandService(ILogger<PinnedCommandService> logger)
        {
            _logger = logger;
            _pinnedCommandsDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "AiStudio4",
                "PinnedCommands");

            Directory.CreateDirectory(_pinnedCommandsDirectory);
            _pinnedCommandsFilePath = Path.Combine(_pinnedCommandsDirectory, "pinnedCommands.json");
            _logger.LogInformation("Initialized pinned commands storage at {PinnedCommandsFile}", _pinnedCommandsFilePath);
        }

        /// <inheritdoc />
        public async Task<(List<PinnedCommand> commands, List<string> categoryOrder)> GetPinnedCommandsAsync(string clientId = null)
        {
            try
            {
                var pinnedCommands = new List<PinnedCommand>();
                var categoryOrder = new List<string>();

                if (!File.Exists(_pinnedCommandsFilePath))
                {
                    _logger.LogDebug("No pinned commands file found");

                    pinnedCommands = new List<PinnedCommand>
                    {
                        new PinnedCommand
                        {
                            Id = "open-models-dialog",
                            Name = "Edit Models",
                            IconName = "Book",
                            IconSet = null,
                            Section = "settings",
                            Position = 0
                        },
                        new PinnedCommand
                        {
                            Id = "new-conv",
                            Name = "New Conversation",
                            IconName = "Plus",
                            IconSet = null,
                            Section = "conv",
                            Position = 0
                        },
                        new PinnedCommand
                        {
                            Id = "select-primary-model-6d21047e-78bd-4adb-a0f7-e3fa6b48ef61",
                            Name = "Sonnet 4 [Primary]",
                            IconName = "Cpu",
                            IconSet = null,
                            Section = "model",
                            Position = 0
                        },
                        new PinnedCommand
                        {
                            Id = "select-secondary-model-6c21b1dd-2a91-4b5a-b904-a0ee04147ed1",
                            Name = "GPT 4.1 Mini [Secondary]",
                            IconName = "Cpu",
                            IconSet = null,
                            Section = "model",
                            Position = 1
                        },
                        new PinnedCommand
                        {
                            Id = "select-primary-model-b77ebaae-aa7d-4354-a584-20d33f184f97",
                            Name = "OpenRouter qwen3-235b-a22b [Primary]",
                            IconName = "Cpu",
                            IconSet = null,
                            Section = "model",
                            Position = 2
                        },
                        new PinnedCommand
                        {
                            Id = "select-primary-model-60c7c581-8fa2-4efd-b393-31c7019ab1aa",
                            Name = "Gemini 2.5 Pro Exp 05 06 [Primary]",
                            IconName = "Cpu",
                            IconSet = null,
                            Section = "model",
                            Position = 3
                        }
                    };

                    // Default category order
                    categoryOrder = new List<string> { "conv", "model", "settings" };

                    return (pinnedCommands, categoryOrder);
                }

                var json = await File.ReadAllTextAsync(_pinnedCommandsFilePath);
                var data = JsonConvert.DeserializeObject<SaveData>(json);

                if (data != null)
                {
                    pinnedCommands = data.PinnedCommands ?? new List<PinnedCommand>();
                    categoryOrder = data.CategoryOrder ?? new List<string>();
                }
                else
                {
                    // Fallback for old format
                    pinnedCommands = JsonConvert.DeserializeObject<List<PinnedCommand>>(json) ?? new List<PinnedCommand>();
                    categoryOrder = new List<string>();
                }

                // Migrate old data that doesn't have positions
                MigrateOldData(pinnedCommands, categoryOrder);

                _logger.LogDebug("Retrieved {Count} pinned commands with {CategoryCount} categories", pinnedCommands?.Count ?? 0, categoryOrder?.Count ?? 0);

                return (pinnedCommands, categoryOrder);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving pinned commands");
                throw;
            }
        }

        /// <inheritdoc />
        public async Task SavePinnedCommandsAsync(string clientId, List<PinnedCommand> pinnedCommands, List<string> categoryOrder)
        {
            try
            {
                if (pinnedCommands == null)
                {
                    pinnedCommands = new List<PinnedCommand>();
                }

                if (categoryOrder == null)
                {
                    categoryOrder = new List<string>();
                }

                var data = new SaveData
                {
                    PinnedCommands = pinnedCommands,
                    CategoryOrder = categoryOrder
                };

                var json = JsonConvert.SerializeObject(data, Formatting.Indented);
                await File.WriteAllTextAsync(_pinnedCommandsFilePath, json);

                _logger.LogDebug("Saved {Count} pinned commands with {CategoryCount} categories", pinnedCommands.Count, categoryOrder.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving pinned commands");
                throw;
            }
        }

        private void MigrateOldData(List<PinnedCommand> pinnedCommands, List<string> categoryOrder)
        {
            // Assign positions to commands that don't have them
            var commandsByCategory = pinnedCommands.GroupBy(c => c.Section).ToList();
            
            foreach (var categoryGroup in commandsByCategory)
            {
                var commands = categoryGroup.OrderBy(c => c.Position).ToList();
                for (int i = 0; i < commands.Count; i++)
                {
                    commands[i].Position = i;
                }

                // Add category to order if not present
                if (!string.IsNullOrEmpty(categoryGroup.Key) && !categoryOrder.Contains(categoryGroup.Key))
                {
                    categoryOrder.Add(categoryGroup.Key);
                }
            }
        }

        private class SaveData
        {
            public List<PinnedCommand> PinnedCommands { get; set; } = new List<PinnedCommand>();
            public List<string> CategoryOrder { get; set; } = new List<string>();
        }
    }
}