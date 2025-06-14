
using System.Collections.Concurrent;




namespace AiStudio4.InjectedDependencies
{
    public class ClientRequestCancellationService
    {
        private readonly ILogger<ClientRequestCancellationService> _logger;
        private readonly ConcurrentDictionary<string, List<CancellationTokenSource>> _clientTokens = new();

        public ClientRequestCancellationService(ILogger<ClientRequestCancellationService> logger)
        {
            _logger = logger;
        }

        public CancellationToken AddTokenSource(string clientId)
        {
            var tokenSource = new CancellationTokenSource();
            
            _clientTokens.AddOrUpdate(
                clientId,
                new List<CancellationTokenSource> { tokenSource },
                (key, existingList) =>
                {
                    lock (existingList)
                    {
                        existingList.Add(tokenSource);
                        return existingList;
                    }
                });

            _logger.LogDebug("Added new token source for client {ClientId}", clientId);
            return tokenSource.Token;
        }

        public void RemoveTokenSource(string clientId, CancellationToken token)
        {
            if (_clientTokens.TryGetValue(clientId, out var tokenSources))
            {
                lock (tokenSources)
                {
                    var sourceToRemove = tokenSources.FirstOrDefault(ts => ts.Token == token);
                    if (sourceToRemove != null)
                    {
                        tokenSources.Remove(sourceToRemove);
                        sourceToRemove.Dispose();
                        _logger.LogDebug("Removed token source for client {ClientId}", clientId);
                    }

                    if (tokenSources.Count == 0)
                    {
                        _clientTokens.TryRemove(clientId, out _);
                        _logger.LogDebug("Removed client {ClientId} from token dictionary", clientId);
                    }
                }
            }
        }

        public bool CancelAllRequests(string clientId)
        {
            if (_clientTokens.TryGetValue(clientId, out var tokenSources))
            {
                lock (tokenSources)
                {
                    int cancelledCount = 0;
                    foreach (var source in tokenSources.ToList())
                    {
                        try
                        {
                            if (!source.IsCancellationRequested)
                            {
                                source.Cancel();
                                cancelledCount++;
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error cancelling request for client {ClientId}", clientId);
                        }
                    }

                    _logger.LogInformation("Cancelled {Count} requests for client {ClientId}", cancelledCount, clientId);
                    return cancelledCount > 0;
                }
            }

            _logger.LogInformation("No active requests found for client {ClientId}", clientId);
            return false;
        }
    }
}
