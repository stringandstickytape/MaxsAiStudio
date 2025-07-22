using AiStudio4.Core.Exceptions;
using System.Collections.Concurrent;
using System.Text;










namespace AiStudio4.Services
{
    public class WebSocketNotificationService : IWebSocketNotificationService
    {
        private readonly WebSocketServer _webSocketServer;
        private readonly ILogger<WebSocketNotificationService> _logger;
        private readonly ConcurrentDictionary<string, CfragBuffer> _activeBuffers;

        public WebSocketNotificationService(
            WebSocketServer webSocketServer,
            ILogger<WebSocketNotificationService> logger)
        {
            _webSocketServer = webSocketServer;
            _logger = logger;
            _activeBuffers = new ConcurrentDictionary<string, CfragBuffer>();
        }

        private class CfragBuffer
        {
            public string MessageId { get; set; }
            public string ClientId { get; set; }
            public StringBuilder Content { get; set; } = new StringBuilder();
            public Timer FlushTimer { get; set; }
            public DateTime LastUpdate { get; set; } = DateTime.UtcNow;
            public readonly object LockObject = new object();

            public void AppendContent(string fragment)
            {
                lock (LockObject)
                {
                    Content.Append(fragment);
                    LastUpdate = DateTime.UtcNow;
                }
            }

            public string GetContentAndClear()
            {
                lock (LockObject)
                {
                    var content = Content.ToString();
                    Content.Clear();
                    return content;
                }
            }

            public void Dispose()
            {
                FlushTimer?.Dispose();
            }
        }        
        
        public async Task NotifyConvUpdate(string clientId, ConvUpdateDto update)
        {
            try
            {
                {
                    _logger.LogInformation("🔔 SENDING TO FRONTEND - Conv: {ConvId}, MessageId: {MessageId}, ParentId: {ParentId}, Source: {Source}, ContentBlocks: {BlockCount}", 
                        update.ConvId, update.MessageId, update.ParentId, update.Source, update.ContentBlocks?.Count ?? 0);
                    
                    // Log content block types for debugging
                    if (update.ContentBlocks != null && update.ContentBlocks.Any())
                    {
                        var blockTypes = string.Join(", ", update.ContentBlocks.Select(b => $"{b.ContentType}({b.Content?.Length ?? 0} chars)"));
                        _logger.LogInformation("🔔 Content Block Types: {BlockTypes}", blockTypes);
                    }
                }

                if (string.IsNullOrEmpty(clientId)) throw new ArgumentNullException(nameof(clientId));
                if (update == null) throw new ArgumentNullException(nameof(update));                // Determine update type
                bool hasContentBlocks = update.ContentBlocks != null && update.ContentBlocks.Count > 0;
                bool hasPlainContent = update.ContentBlocks.Any(x => x.ContentType == 0);

                if (hasContentBlocks)
                {
                    var message = new
                    {
                        messageType = "conv",
                        content = new
                        {
                            convId = update.ConvId,
                            id = update.MessageId,
                            contentBlocks = update.ContentBlocks,
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
                            contentBlocks = update.ContentBlocks,
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
                    _logger.LogDebug("Conversation update error for {ClientId}", clientId);
                }

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

                if (update.MessageType == "cfrag")
                {
                    await BufferCfragment(clientId, update.MessageId, update.Content);
                }
                else if (update.MessageType == "endstream")
                {
                    await FlushBuffer(update.MessageId, sendEndstream: true);
                }
                else
                {
                    // Send other message types immediately (toolcalls, toolresult, etc.)
                    await SendMessageDirectly(clientId, update);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send streaming update to client {ClientId}", clientId);
                throw new WebSocketNotificationException("Failed to send streaming update", ex);
            }
        }

        private async Task BufferCfragment(string clientId, string messageId, string content)
        {
            var buffer = _activeBuffers.AddOrUpdate(messageId,
                // Add new buffer
                key => CreateNewBuffer(clientId, messageId, content),
                // Update existing buffer
                (key, existingBuffer) => 
                {
                    existingBuffer.AppendContent(content);
                    // Don't reset timer - let it flush after exactly 100ms
                    return existingBuffer;
                });

            // If this was a new buffer, the timer was set in CreateNewBuffer
            // If existing buffer, just append content - timer keeps running
        }

        private CfragBuffer CreateNewBuffer(string clientId, string messageId, string initialContent)
        {
            var buffer = new CfragBuffer
            {
                MessageId = messageId,
                ClientId = clientId
            };
            
            buffer.AppendContent(initialContent);
            buffer.FlushTimer = new Timer(async _ => await FlushBufferCallback(messageId), null, TimeSpan.FromMilliseconds(100), Timeout.InfiniteTimeSpan);
            
            return buffer;
        }


        private async Task FlushBufferCallback(string messageId)
        {
            try
            {
                await FlushBuffer(messageId, sendEndstream: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in flush buffer callback for message {MessageId}", messageId);
            }
        }

        private async Task FlushBuffer(string messageId, bool sendEndstream)
        {
            if (_activeBuffers.TryRemove(messageId, out var buffer))
            {
                try
                {
                    var content = buffer.GetContentAndClear();
                    
                    // Send buffered content if any
                    if (!string.IsNullOrEmpty(content))
                    {
                        var cfragMessage = new
                        {
                            messageType = "cfrag",
                            messageId = messageId,
                            content = content
                        };
                        
                        await _webSocketServer.SendToClientAsync(buffer.ClientId, JsonConvert.SerializeObject(cfragMessage));
                    }
                    
                    // Send endstream if requested
                    if (sendEndstream)
                    {
                        var endstreamMessage = new
                        {
                            messageType = "endstream",
                            messageId = messageId,
                            content = ""
                        };
                        
                        await _webSocketServer.SendToClientAsync(buffer.ClientId, JsonConvert.SerializeObject(endstreamMessage));
                    }
                    else
                    {
                        // If not ending stream, create a new buffer for continued streaming
                        // This handles the case where timer fired during active streaming
                        CreateNewBufferForContinuation(buffer.ClientId, messageId);
                    }
                }
                finally
                {
                    buffer.Dispose();
                }
            }
        }

        private void CreateNewBufferForContinuation(string clientId, string messageId)
        {
            var newBuffer = new CfragBuffer
            {
                MessageId = messageId,
                ClientId = clientId
            };
            
            newBuffer.FlushTimer = new Timer(async _ => await FlushBufferCallback(messageId), null, TimeSpan.FromMilliseconds(100), Timeout.InfiniteTimeSpan);
            
            // Only add if no buffer exists (race condition protection)
            _activeBuffers.TryAdd(messageId, newBuffer);
        }

        private async Task SendMessageDirectly(string clientId, StreamingUpdateDto update)
        {
            var message = new
            {
                messageType = update.MessageType,
                messageId = update.MessageId,
                content = update.Content
            };

            await _webSocketServer.SendToClientAsync(clientId, JsonConvert.SerializeObject(message));
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
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send status message to client {ClientId}", clientId);
                throw new WebSocketNotificationException("Failed to send status message", ex);
            }
        }

        public async Task NotifyConvPlaceholderUpdate(string clientId, v4BranchedConv conv, v4BranchedConvMessage placeholderMessage)
        {
            await NotifyConvUpdate(clientId, new ConvUpdateDto
            {
                ConvId = conv.ConvId,
                MessageId = placeholderMessage.Id,
                ContentBlocks = placeholderMessage.ContentBlocks,
                ParentId = placeholderMessage.ParentId,
                Timestamp = new DateTimeOffset(placeholderMessage.Timestamp).ToUnixTimeMilliseconds(),
                Source = "assistant"
            });
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

        public void Dispose()
        {
            // Clean up any remaining buffers
            foreach (var buffer in _activeBuffers.Values)
            {
                buffer.Dispose();
            }
            _activeBuffers.Clear();
        }
    }
}
