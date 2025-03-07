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

        public PinnedCommandService(ILogger<PinnedCommandService> logger)
        {
            _logger = logger;
            _pinnedCommandsDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "AiStudio4",
                "PinnedCommands");

            Directory.CreateDirectory(_pinnedCommandsDirectory);
            _logger.LogInformation("Initialized pinned commands storage at {PinnedCommandsDirectory}", _pinnedCommandsDirectory);
        }

        /// <inheritdoc />
        public async Task<List<PinnedCommand>> GetPinnedCommandsAsync(string clientId)
        {
            try
            {
                if (string.IsNullOrEmpty(clientId))
                {
                    throw new ArgumentException("Client ID cannot be empty", nameof(clientId));
                }

                var filePath = GetClientFilePath(clientId);
                if (!File.Exists(filePath))
                {
                    _logger.LogDebug("No pinned commands file found for client {ClientId}", clientId);
                    return new List<PinnedCommand>();
                }

                var json = await File.ReadAllTextAsync(filePath);
                var pinnedCommands = JsonConvert.DeserializeObject<List<PinnedCommand>>(json);

                _logger.LogDebug("Retrieved {Count} pinned commands for client {ClientId}",
                    pinnedCommands?.Count ?? 0, clientId);

                return pinnedCommands ?? new List<PinnedCommand>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving pinned commands for client {ClientId}", clientId);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task SavePinnedCommandsAsync(string clientId, List<PinnedCommand> pinnedCommands)
        {
            try
            {
                if (string.IsNullOrEmpty(clientId))
                {
                    throw new ArgumentException("Client ID cannot be empty", nameof(clientId));
                }

                if (pinnedCommands == null)
                {
                    pinnedCommands = new List<PinnedCommand>();
                }

                var filePath = GetClientFilePath(clientId);
                var json = JsonConvert.SerializeObject(pinnedCommands, Formatting.Indented);
                await File.WriteAllTextAsync(filePath, json);

                _logger.LogDebug("Saved {Count} pinned commands for client {ClientId}",
                    pinnedCommands.Count, clientId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving pinned commands for client {ClientId}", clientId);
                throw;
            }
        }

        /// <summary>
        /// Gets the file path for a client's pinned commands
        /// </summary>
        private string GetClientFilePath(string clientId)
        {
            // Sanitize client ID to make sure it's safe for file system
            var safeClientId = string.Join("_", clientId.Split(Path.GetInvalidFileNameChars()));
            return Path.Combine(_pinnedCommandsDirectory, $"{safeClientId}.json");
        }
    }
}