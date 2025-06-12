using AiStudio4.Core.Exceptions;
using AiStudio4.Core.Interfaces;
using AiStudio4.Core.Models;
using AiStudio4.InjectedDependencies;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        }        public async Task NotifyConvUpdate(string clientId, ConvUpdateDto update)
        {
            try
            {
                {
                    Debug.WriteLine($"{update.ConvId} {update.Source}: {update.ParentId} -> {update.MessageId}");
                }

                if (string.IsNullOrEmpty(clientId)) throw new ArgumentNullException(nameof(clientId));
                if (update == null) throw new ArgumentNullException(nameof(update));                // Determine update type
                bool hasContentBlocks = update.ContentBlocks != null && update.ContentBlocks.Count > 0;
                bool hasPlainContent = update.Content is string;

                if (hasContentBlocks)
                {
                    var message = new
                    {
                        messageType = "conv",
                        content = new
                        {
                            convId = update.ConvId,
                            id = update.MessageId,                            contentBlocks = update.ContentBlocks,
                            content = string.Join("\n\n", update.ContentBlocks.Select(cb => cb.Content)),
                            source = update.Source ?? "ai",
                            parentId = update.ParentId,
                            timestamp = update.Timestamp,
                            tokenUsage = update.TokenUsage,
                            costInfo = update.CostInfo,
                            cumulativeCost = update.CumulativeCost,
                            attachments = update.Attachments,
                            durationMs = update.DurationMs,
                            temperature = update.Temperature
                        }
                    };
                    await _webSocketServer.SendToAllClientsAsync(JsonConvert.SerializeObject(message));
                }
                else if (hasPlainContent)
                {
                    var message = new
                    {
                        messageType = "conv",
                        content = new
                        {
                            convId = update.ConvId,
                            id = update.MessageId,
                            content = update.Content,
                            source = update.Source ?? "ai",
                            parentId = update.ParentId,
                            timestamp = update.Timestamp,
                            tokenUsage = update.TokenUsage,
                            costInfo = update.CostInfo,
                            cumulativeCost = update.CumulativeCost,
                            attachments = update.Attachments,
                            durationMs = update.DurationMs,
                            temperature = update.Temperature
                        }
                    };
                    await _webSocketServer.SendToAllClientsAsync(JsonConvert.SerializeObject(message));
                }
                else
                {
                    // Fallback: send raw content object to specific client
                    await _webSocketServer.SendToClientAsync(clientId, JsonConvert.SerializeObject(update.Content));
                }

                _logger.LogDebug("Sent conv update to client {ClientId}", clientId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send conv update to client {ClientId}", clientId);
                throw new WebSocketNotificationException("Failed to send conv update", ex);
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
                    messageId = update.MessageId, // Include messageId
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

        public async Task NotifyConvList(ConvListDto convs)
        {
            try
            {
                if (convs == null) throw new ArgumentNullException(nameof(convs));

                // Format matches client expectations for WebSocket messages
                var message = new
                {
                    messageType = "historicalConvTree",
                    content = new
                    {
                        convGuid = convs.ConvId,
                        summary = convs.Summary,
                        fileName = $"conv_{convs.ConvId}.json",
                        lastModified = convs.LastModified
                    }
                };

                await _webSocketServer.SendToAllClientsAsync(JsonConvert.SerializeObject(message));
                _logger.LogDebug("Sent conv list to clients");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send conv list to clients");
                throw new WebSocketNotificationException("Failed to send conv list", ex);
            }
        }
        public async Task NotifyTranscription(string transcriptionText)
        {
            try
            {
                if (string.IsNullOrEmpty(transcriptionText)) throw new ArgumentNullException(nameof(transcriptionText));

                var message = new
                {
                    messageType = "transcription",
                    content = new
                    {
                        text = transcriptionText,
                        action = "appendToUserPrompt"
                    }
                };

                await _webSocketServer.SendToAllClientsAsync(JsonConvert.SerializeObject(message));
                _logger.LogDebug("Sent transcription update to all clients");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send transcription update to all clients");
                throw new WebSocketNotificationException("Failed to send transcription update", ex);
            }
        }

        public async Task NotifyStatusMessage(string clientId, string message)
        {
            try
            {
                if (string.IsNullOrEmpty(clientId)) throw new ArgumentNullException(nameof(clientId));

                var messageObj = new
                {
                    messageType = "status",
                    content = new
                    {
                        message = message
                    }
                };

                await _webSocketServer.SendToClientAsync(clientId, JsonConvert.SerializeObject(messageObj));
                _logger.LogDebug("Sent status message to client {ClientId}: {Message}", clientId, message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send status message to client {ClientId}", clientId);
                throw new WebSocketNotificationException("Failed to send status message", ex);
            }
        }
        
        public async Task NotifyFileSystemChanges(IReadOnlyList<string> directories, IReadOnlyList<string> files)
        {
            try
            {
                if (directories == null) throw new ArgumentNullException(nameof(directories));
                if (files == null) throw new ArgumentNullException(nameof(files));

                var message = new
                {
                    messageType = "fileSystem",
                    content = new
                    {
                        directories = directories,
                        files = files
                    }
                };

                await _webSocketServer.SendToAllClientsAsync(JsonConvert.SerializeObject(message));
                _logger.LogDebug("Sent file system update to all clients");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send file system update to clients");
                throw new WebSocketNotificationException("Failed to send file system update", ex);
            }
        }
    }
}