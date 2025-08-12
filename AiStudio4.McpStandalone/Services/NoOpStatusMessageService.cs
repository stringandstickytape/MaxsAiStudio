using AiStudio4.Tools.Interfaces;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace AiStudio4.McpStandalone.Services
{
    /// <summary>
    /// Status message service that just logs messages instead of sending via WebSocket
    /// </summary>
    public class NoOpStatusMessageService : IStatusMessageService
    {
        private readonly ILogger<NoOpStatusMessageService> _logger;

        public NoOpStatusMessageService(ILogger<NoOpStatusMessageService> logger)
        {
            _logger = logger;
        }

        public Task SendStatusMessageAsync(string clientId, string message)
        {
            _logger.LogInformation("[Tool Status - Client {ClientId}]: {Message}", clientId, message);
            return Task.CompletedTask;
        }
    }
}