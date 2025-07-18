








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
            _pinnedCommandsDirectory = PathHelper.GetProfileSubPath("pinnedCommands");

            Directory.CreateDirectory(_pinnedCommandsDirectory);
            _pinnedCommandsFilePath = Path.Combine(_pinnedCommandsDirectory, "pinnedCommands.json");
            _logger.LogInformation("Initialized pinned commands storage at {PinnedCommandsFile}", _pinnedCommandsFilePath);
        }

        /// <inheritdoc />
        public async Task<List<PinnedCommand>> GetPinnedCommandsAsync(string clientId = null)
        {
            try
            {
                var pinnedCommands = new List<PinnedCommand>();

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
                            Section = "settings"
                        },
                        new PinnedCommand
                        {
                            Id = "new-conv",
                            Name = "New Conversation",
                            IconName = "Plus",
                            IconSet = null,
                            Section = "conv"
                        },
                        new PinnedCommand
                        {
                            Id = "select-primary-model-6d21047e-78bd-4adb-a0f7-e3fa6b48ef61",
                            Name = "Sonnet 4 [Primary]",
                            IconName = "Cpu",
                            IconSet = null,
                            Section = "model"
                        },
                        new PinnedCommand
                        {
                            Id = "select-secondary-model-6c21b1dd-2a91-4b5a-b904-a0ee04147ed1",
                            Name = "GPT 4.1 Mini [Secondary]",
                            IconName = "Cpu",
                            IconSet = null,
                            Section = "model"
                        },
                        new PinnedCommand
                        {
                            Id = "select-primary-model-b77ebaae-aa7d-4354-a584-20d33f184f97",
                            Name = "OpenRouter qwen3-235b-a22b [Primary]",
                            IconName = "Cpu",
                            IconSet = null,
                            Section = "model"
                        },
                        new PinnedCommand
                        {
                            Id = "select-primary-model-60c7c581-8fa2-4efd-b393-31c7019ab1aa",
                            Name = "Gemini 2.5 Pro Exp 05 06 [Primary]",
                            IconName = "Cpu",
                            IconSet = null,
                            Section = "model"
                        }
                    };

                    return pinnedCommands;
                }

                var json = await File.ReadAllTextAsync(_pinnedCommandsFilePath);
                pinnedCommands = JsonConvert.DeserializeObject<List<PinnedCommand>>(json);

                _logger.LogDebug("Retrieved {Count} pinned commands", pinnedCommands?.Count ?? 0);

                return pinnedCommands ?? new List<PinnedCommand>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving pinned commands");
                throw;
            }
        }

        /// <inheritdoc />
        public async Task SavePinnedCommandsAsync(string clientId, List<PinnedCommand> pinnedCommands)
        {
            try
            {
                if (pinnedCommands == null)
                {
                    pinnedCommands = new List<PinnedCommand>();
                }

                var json = JsonConvert.SerializeObject(pinnedCommands, Formatting.Indented);
                await File.WriteAllTextAsync(_pinnedCommandsFilePath, json);

                _logger.LogDebug("Saved {Count} pinned commands", pinnedCommands.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving pinned commands");
                throw;
            }
        }
    }
}
