using System.Collections.Concurrent;
using System.Threading.Tasks;
using AiStudio4.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace AiStudio4.Services
{
    /// <summary>
    /// Service for managing user interjections during tool loops
    /// </summary>
    public class InterjectionService : IInterjectionService
    {
        private readonly ILogger<InterjectionService> _logger;
        private readonly ConcurrentDictionary<string, string> _interjections = new();

        public InterjectionService(ILogger<InterjectionService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Stores an interjection for a specific client
        /// </summary>
        public Task StoreInterjectionAsync(string clientId, string interjection)
        {
            _logger.LogInformation("Storing interjection for client {ClientId}", clientId);
            _interjections[clientId] = interjection;
            return Task.CompletedTask;
        }

        /// <summary>
        /// Retrieves and clears an interjection for a specific client
        /// </summary>
        public Task<string> GetAndClearInterjectionAsync(string clientId)
        {
            if (_interjections.TryRemove(clientId, out var interjection))
            {
                _logger.LogInformation("Retrieved and cleared interjection for client {ClientId}", clientId);
                return Task.FromResult(interjection);
            }
            
            _logger.LogInformation("No interjection found for client {ClientId}", clientId);
            return Task.FromResult(string.Empty);
        }

        /// <summary>
        /// Checks if an interjection exists for a specific client
        /// </summary>
        public Task<bool> HasInterjectionAsync(string clientId)
        {
            bool hasInterjection = _interjections.ContainsKey(clientId);
            _logger.LogDebug("Checked for interjection for client {ClientId}: {HasInterjection}", clientId, hasInterjection);
            return Task.FromResult(hasInterjection);
        }
    }
}