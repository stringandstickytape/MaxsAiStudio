// AiStudio4/Services/StatusMessageService.cs
using AiStudio4.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace AiStudio4.Services
{
    /// <summary>
    /// Implementation of the Status Message Service which provides a centralized mechanism
    /// for sending status messages throughout the application
    /// </summary>
    public class StatusMessageService : IStatusMessageService
    {
        private readonly IWebSocketNotificationService _webSocketNotificationService;
        private readonly ILogger<StatusMessageService> _logger;

        public StatusMessageService(
            IWebSocketNotificationService webSocketNotificationService,
            ILogger<StatusMessageService> logger)
        {
            _webSocketNotificationService = webSocketNotificationService ?? throw new ArgumentNullException(nameof(webSocketNotificationService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Sends a status message to a specific client
        /// </summary>
        /// <param name="clientId">The ID of the client to send the message to</param>
        /// <param name="message">The status message to send</param>
        public async Task SendStatusMessageAsync(string clientId, string message)
        {
            try
            {
                if (string.IsNullOrEmpty(clientId))
                {
                    _logger.LogWarning("Cannot send status message: clientId is null or empty");
                    return;
                }

                await _webSocketNotificationService.NotifyStatusMessage(clientId, message);
                _logger.LogDebug("Sent status message to client {ClientId}: {Message}", clientId, message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send status message to client {ClientId}", clientId);
            }
        }

        /// <summary>
        /// Clears the status message for a specific client
        /// </summary>
        /// <param name="clientId">The ID of the client to clear the message for</param>
        public async Task ClearStatusMessageAsync(string clientId)
        {
            await SendStatusMessageAsync(clientId, "");
        }
    }
}