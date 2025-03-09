using AiStudio4.Core.Exceptions;
using AiStudio4.Core.Interfaces;
using AiStudio4.Core.Models;
using AiStudio4.InjectedDependencies;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

namespace AiStudio4.Services
{
    public class WebSocketNotificationService : IWebSocketNotificationService
    {
        private readonly WebSocketServer _webSocketServer;
        private readonly ILogger<WebSocketNotificationService> _logger;

        public WebSocketNotificationService(
            WebSocketServer webSocketServer,
            ILogger<WebSocketNotificationService> logger)
        {
            _webSocketServer = webSocketServer;
            _logger = logger;
        }

        public async Task NotifyConversationUpdate(string clientId, ConversationUpdateDto update)
        {
            try
            {
                if (string.IsNullOrEmpty(clientId)) throw new ArgumentNullException(nameof(clientId));
                if (update == null) throw new ArgumentNullException(nameof(update));

                // If Content is a regular conversation update
                if (update.Content is string)
                {
                    var message = new
                    {
                        messageType = "conversation",
                        content = new
                        {
                            id = update.MessageId,
                            content = update.Content,
                            source = update.Source ?? "ai", // Use provided source or default to "ai"
                            parentId = update.ParentId,
                            timestamp = update.Timestamp,
                            // Note: We're no longer sending children arrays to the client
                        }
                    };
                    await _webSocketServer.SendToClientAsync(clientId, JsonConvert.SerializeObject(message));
                }
                // If Content is a conversation load message
                else
                {
                    await _webSocketServer.SendToClientAsync(clientId, JsonConvert.SerializeObject(update.Content));
                }

                _logger.LogDebug("Sent conversation update to client {ClientId}", clientId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send conversation update to client {ClientId}", clientId);
                throw new WebSocketNotificationException("Failed to send conversation update", ex);
            }
        }

        public async Task NotifyStreamingUpdate(string clientId, StreamingUpdateDto update)
        {
            try
            {
                if (string.IsNullOrEmpty(clientId)) throw new ArgumentNullException(nameof(clientId));
                if (update == null) throw new ArgumentNullException(nameof(update));

                var message = new
                {
                    messageType = update.MessageType,
                    content = update.Content
                };

                await _webSocketServer.SendToClientAsync(clientId, JsonConvert.SerializeObject(message));
                _logger.LogTrace("Sent streaming update to client {ClientId}", clientId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send streaming update to client {ClientId}", clientId);
                throw new WebSocketNotificationException("Failed to send streaming update", ex);
            }
        }

        public async Task NotifyConversationList(string clientId, ConversationListDto conversations)
        {
            try
            {
                if (string.IsNullOrEmpty(clientId)) throw new ArgumentNullException(nameof(clientId));
                if (conversations == null) throw new ArgumentNullException(nameof(conversations));

                // Format matches client expectations for WebSocket messages
                var message = new
                {
                    messageType = "historicalConversationTree",
                    content = new
                    {
                        convGuid = conversations.ConversationId,
                        summary = conversations.Summary,
                        fileName = $"conv_{conversations.ConversationId}.json",
                        lastModified = conversations.LastModified
                    }
                };

                await _webSocketServer.SendToClientAsync(clientId, JsonConvert.SerializeObject(message));
                _logger.LogDebug("Sent conversation list to client {ClientId}", clientId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send conversation list to client {ClientId}", clientId);
                throw new WebSocketNotificationException("Failed to send conversation list", ex);
            }
        }
    }
}