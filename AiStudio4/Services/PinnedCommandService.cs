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
        public async Task<List<PinnedCommand>> GetPinnedCommandsAsync(string clientId = null)
        {
            try
            {
                if (!File.Exists(_pinnedCommandsFilePath))
                {
                    _logger.LogDebug("No pinned commands file found");
                    return new List<PinnedCommand>();
                }

                var json = await File.ReadAllTextAsync(_pinnedCommandsFilePath);
                var pinnedCommands = JsonConvert.DeserializeObject<List<PinnedCommand>>(json);

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