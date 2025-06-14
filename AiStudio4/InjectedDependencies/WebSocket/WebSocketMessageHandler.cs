using System.Net.WebSockets;

using AiStudio4.InjectedDependencies.WebSocketManagement;




using System.Collections.Concurrent;
using System.Threading;

namespace AiStudio4.InjectedDependencies.WebSocket
{
    public class WebSocketMessageHandler
    {
        private readonly WebSocketConnectionManager _connectionManager;
        private readonly ILogger<WebSocketMessageHandler> _logger;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly IInterjectionService _interjectionService;
        private readonly IConvStorage _convStorage;
        private readonly ConcurrentDictionary<string, CancellationTokenSource> _activeSearches = new();

        public WebSocketMessageHandler(
            WebSocketConnectionManager connectionManager,
            ILogger<WebSocketMessageHandler> logger,
            IInterjectionService interjectionService,
            IConvStorage convStorage)
        {
            _connectionManager = connectionManager;
            _logger = logger;
            _interjectionService = interjectionService;
            _convStorage = convStorage;
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public async Task SendToClientAsync(string clientId, string message)
        {
            try
            {
                var socket = _connectionManager.GetSocket(clientId);
                if (socket?.State == WebSocketState.Open)
                {
                    var buffer = Encoding.UTF8.GetBytes(message);
                    await socket.SendAsync(
                        new ArraySegment<byte>(buffer),
                        WebSocketMessageType.Text,
                        true,
                        _cancellationTokenSource.Token);
                    _logger.LogTrace("Sent message to client {ClientId}", clientId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message to client {ClientId}", clientId);
                _connectionManager.RemoveConnection(clientId);
            }
        }

        public async Task SendToAllClientsAsync(string message)
        {
            var buffer = Encoding.UTF8.GetBytes(message);
            var arraySegment = new ArraySegment<byte>(buffer);

            foreach (var client in _connectionManager.GetAllConnections())
            {
                try
                {
                    if (client.Value.State == WebSocketState.Open)
                    {
                        await client.Value.SendAsync(
                            arraySegment,
                            WebSocketMessageType.Text,
                            true,
                            _cancellationTokenSource.Token);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending message to client {ClientId}", client.Key);
                    _connectionManager.RemoveConnection(client.Key);
                }
            }
        }

        public async Task HandleIncomingMessages(string clientId, System.Net.WebSockets.WebSocket webSocket)
        {
            var buffer = new byte[1024 * 4];

            try
            {
                while (webSocket.State == WebSocketState.Open)
                {
                    var result = await webSocket.ReceiveAsync(
                        new ArraySegment<byte>(buffer),
                        _cancellationTokenSource.Token);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await webSocket.CloseAsync(
                            WebSocketCloseStatus.NormalClosure,
                            "Connection closed by client",
                            _cancellationTokenSource.Token);
                        break;
                    }

                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    _logger.LogTrace("Received message from client {ClientId}: {Message}", clientId, message);
                    
                    await HandleClientMessageAsync(clientId, message);
                }
            }
            catch (WebSocketException ex)
            {
                _logger.LogError(ex, "WebSocket error for client {ClientId}", clientId);
            }
            finally
            {
                _connectionManager.RemoveConnection(clientId);
            }
        }

        private async Task HandleClientMessageAsync(string clientId, string message)
        {
            try
            {
                var messageObj = JsonConvert.DeserializeObject<dynamic>(message);
                string messageType = messageObj.messageType;

                switch (messageType)
                {
                    case "ping":
                        await HandlePingAsync(clientId);
                        break;
                    case "chat":
                        await HandleChatAsync(clientId, messageObj);
                        break;
                    case "interject":
                        await HandleInterjectionAsync(clientId, messageObj);
                        break;
                    case "cancelRequest":
                        await HandleCancelRequestAsync(clientId, messageObj);
                        break;
                    case "loadConv":
                        await HandleLoadConvAsync(clientId, messageObj);
                        break;
                    case "createConv":
                        await HandleCreateConvAsync(clientId, messageObj);
                        break;
                    case "deleteConv":
                        await HandleDeleteConvAsync(clientId, messageObj);
                        break;
                    case "renameConv":
                        await HandleRenameConvAsync(clientId, messageObj);
                        break;
                    case "loadConvList":
                        await HandleLoadConvListAsync(clientId);
                        break;
                    case "searchConversations":
                        await HandleSearchConversationsAsync(clientId, messageObj);
                        break;
                    case "cancelSearch":
                        await HandleCancelSearchAsync(clientId, messageObj);
                        break;
                    default:
                        _logger.LogWarning("Unknown message type: {MessageType}", messageType);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling client message: {Message}", ex.Message);
            }
        }
        
        private async Task HandleInterjectionAsync(string clientId, dynamic messageObj)
        {
            try
            {
                string interjection = messageObj.content.message;
                _logger.LogInformation("Received interjection from client {ClientId}: {Interjection}", clientId, interjection);
                
                await _interjectionService.StoreInterjectionAsync(clientId, interjection);
                
                // Send acknowledgment back to client
                await SendToClientAsync(clientId, JsonConvert.SerializeObject(new
                {
                    messageType = "interjectionAck",
                    content = new { success = true }
                }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling interjection from client {ClientId}", clientId);
                
                // Send error back to client
                await SendToClientAsync(clientId, JsonConvert.SerializeObject(new
                {
                    messageType = "interjectionAck",
                    content = new { success = false, error = ex.Message }
                }));
            }
        }
        
        // Implement other handler methods here
        private Task HandlePingAsync(string clientId)
        {
            // Implementation details
            return Task.CompletedTask;
        }
        
        private Task HandleChatAsync(string clientId, dynamic messageObj)
        {
            // Implementation details
            return Task.CompletedTask;
        }
        
        private Task HandleCancelRequestAsync(string clientId, dynamic messageObj)
        {
            // Implementation details
            return Task.CompletedTask;
        }
        
        private Task HandleLoadConvAsync(string clientId, dynamic messageObj)
        {
            // Implementation details
            return Task.CompletedTask;
        }
        
        private Task HandleCreateConvAsync(string clientId, dynamic messageObj)
        {
            // Implementation details
            return Task.CompletedTask;
        }
        
        private Task HandleDeleteConvAsync(string clientId, dynamic messageObj)
        {
            // Implementation details
            return Task.CompletedTask;
        }
        
        private Task HandleRenameConvAsync(string clientId, dynamic messageObj)
        {
            // Implementation details
            return Task.CompletedTask;
        }
        
        private Task HandleLoadConvListAsync(string clientId)
        {
            // Implementation details
            return Task.CompletedTask;
        }

        private async Task HandleSearchConversationsAsync(string clientId, dynamic messageObj)
        {
            try
            {
                string searchTerm = messageObj.content.searchTerm;
                string searchId = messageObj.content.searchId;
                
                // Cancel any existing search for this client
                if (_activeSearches.TryRemove(clientId, out var existingCts))
                {
                    existingCts.Cancel();
                    existingCts.Dispose();
                }
                
                // Create new cancellation token source
                var cts = new CancellationTokenSource();
                _activeSearches[clientId] = cts;
                
                try
                {
                    // Notify client that search has started
                    await SendToClientAsync(clientId, JsonConvert.SerializeObject(new
                    {
                        messageType = "searchStarted",
                        content = new { searchId }
                    }));

                    // Stream results as they are found
                    var results = new List<object>();
                    await foreach (var result in _convStorage.SearchConversationsStreamingAsync(searchTerm, cts.Token))
                    {
                        var resultObj = new
                        {
                            conversationId = result.ConversationId,
                            matchingMessageIds = result.MatchingMessageIds,
                            summary = result.ConversationSummary,
                            lastModified = result.LastModified
                        };
                        results.Add(resultObj);
                        // Send each result as it is found
                        await SendToClientAsync(clientId, JsonConvert.SerializeObject(new
                        {
                            messageType = "searchResultPartial",
                            content = new
                            {
                                searchId,
                                result = resultObj
                            }
                        }));
                    }

                    // Send completion message
                    await SendToClientAsync(clientId, JsonConvert.SerializeObject(new
                    {
                        messageType = "searchResultsComplete",
                        content = new
                        {
                            searchId
                        }
                    }));
                }
                catch (OperationCanceledException)
                {
                    // Search was cancelled, notify client
                    await SendToClientAsync(clientId, JsonConvert.SerializeObject(new
                    {
                        messageType = "searchCancelled",
                        content = new { searchId }
                    }));
                }
                finally
                {
                    // Remove the cancellation token source
                    _activeSearches.TryRemove(clientId, out _);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling search request from client {ClientId}", clientId);
                
                // Send error to client
                await SendToClientAsync(clientId, JsonConvert.SerializeObject(new
                {
                    messageType = "searchError",
                    content = new { error = ex.Message }
                }));
            }
        }

        private async Task HandleCancelSearchAsync(string clientId, dynamic messageObj)
        {
            try
            {
                string searchId = messageObj.content.searchId;
                
                // Cancel the search if it exists
                if (_activeSearches.TryRemove(clientId, out var cts))
                {
                    cts.Cancel();
                    cts.Dispose();
                    
                    // Notify client that search was cancelled
                    await SendToClientAsync(clientId, JsonConvert.SerializeObject(new
                    {
                        messageType = "searchCancelled",
                        content = new { searchId }
                    }));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling cancel search request from client {ClientId}", clientId);
            }
        }
    }
}
