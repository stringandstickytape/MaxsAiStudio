using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging;

namespace ModelContextProtocol.TestOAuthServer;

/// <summary>
/// Provides a controllable interface to manage the OAuth test server programmatically.
/// </summary>
public sealed class OAuthServerManager : IDisposable
{
    private readonly Program _oauthServer;
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _serverTask;
    private bool _isRunning;
    private readonly object _lock = new();

    /// <summary>
    /// Gets the base URL of the OAuth server.
    /// </summary>
    public string BaseUrl => "http://localhost:7029";

    /// <summary>
    /// Gets a value indicating whether the server is currently running.
    /// </summary>
    public bool IsRunning
    {
        get
        {
            lock (_lock)
            {
                return _isRunning;
            }
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether an expired token has been issued for testing refresh flows.
    /// </summary>
    public bool HasIssuedExpiredToken
    {
        get => _oauthServer.HasIssuedExpiredToken;
        set => _oauthServer.HasIssuedExpiredToken = value;
    }

    /// <summary>
    /// Gets or sets a value indicating whether a refresh token has been issued.
    /// </summary>
    public bool HasIssuedRefreshToken
    {
        get => _oauthServer.HasIssuedRefreshToken;
        set => _oauthServer.HasIssuedRefreshToken = value;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OAuthServerManager"/> class.
    /// </summary>
    /// <param name="loggerProvider">Optional logger provider for logging.</param>
    /// <param name="kestrelTransport">Optional Kestrel transport for in-memory connections.</param>
    /// <param name="persistenceDataDirectory">Optional directory for persistence data. If null, uses default AppData location.</param>
    public OAuthServerManager(ILoggerProvider? loggerProvider = null, IConnectionListenerFactory? kestrelTransport = null, string? persistenceDataDirectory = null)
    {
        _oauthServer = new Program(loggerProvider, kestrelTransport, persistenceDataDirectory);
    }

    /// <summary>
    /// Starts the OAuth server asynchronously.
    /// </summary>
    /// <param name="args">Optional command line arguments.</param>
    /// <returns>A task that represents the asynchronous start operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the server is already running.</exception>
    public Task StartAsync(string[]? args = null)
    {
        lock (_lock)
        {
            if (_isRunning)
            {
                throw new InvalidOperationException("Server is already running.");
            }

            _cancellationTokenSource = new CancellationTokenSource();
            _serverTask = Task.Run(async () =>
            {
                try
                {
                    await _oauthServer.RunServerAsync(args, _cancellationTokenSource.Token);
                }
                catch (OperationCanceledException)
                {
                    // Expected when stopping the server
                }
                finally
                {
                    lock (_lock)
                    {
                        _isRunning = false;
                    }
                }
            });

            _isRunning = true;
        }

        // Give the server a moment to start up
        return Task.Delay(1000);
    }

    /// <summary>
    /// Stops the OAuth server asynchronously.
    /// </summary>
    /// <param name="timeoutMs">Timeout in milliseconds to wait for graceful shutdown. Default is 5000ms.</param>
    /// <returns>A task that represents the asynchronous stop operation.</returns>
    public async Task StopAsync(int timeoutMs = 5000)
    {
        Task? taskToWait = null;
        CancellationTokenSource? ctsToDispose = null;

        lock (_lock)
        {
            if (!_isRunning || _cancellationTokenSource == null || _serverTask == null)
            {
                return;
            }

            _cancellationTokenSource.Cancel();
            taskToWait = _serverTask;
            ctsToDispose = _cancellationTokenSource;
            _isRunning = false;
        }

        if (taskToWait != null)
        {
            try
            {
                await taskToWait.WaitAsync(TimeSpan.FromMilliseconds(timeoutMs));
            }
            catch (TimeoutException)
            {
                // Server didn't stop gracefully within timeout
            }
        }

        ctsToDispose?.Dispose();

        lock (_lock)
        {
            _serverTask = null;
            _cancellationTokenSource = null;
        }
    }

    /// <summary>
    /// Gets the current server configuration parameters.
    /// </summary>
    /// <returns>A dictionary containing the current server parameters.</returns>
    public Dictionary<string, object> GetParameters()
    {
        return new Dictionary<string, object>
        {
            ["BaseUrl"] = BaseUrl,
            ["Port"] = 7029,
            ["IsRunning"] = IsRunning,
            ["HasIssuedExpiredToken"] = HasIssuedExpiredToken,
            ["HasIssuedRefreshToken"] = HasIssuedRefreshToken,
            ["MetadataEndpoint"] = $"{BaseUrl}/.well-known/oauth-authorization-server",
            ["JwksEndpoint"] = $"{BaseUrl}/.well-known/jwks.json",
            ["AuthorizeEndpoint"] = $"{BaseUrl}/authorize",
            ["TokenEndpoint"] = $"{BaseUrl}/token",
            ["IntrospectionEndpoint"] = $"{BaseUrl}/introspect",
            ["RegistrationEndpoint"] = $"{BaseUrl}/register",
            ["DemoClientId"] = "demo-client",
            ["DemoClientSecret"] = "demo-secret",
            ["TestRefreshClientId"] = "test-refresh-client",
            ["TestRefreshClientSecret"] = "test-refresh-secret"
        };
    }

    /// <summary>
    /// Sets server parameters. Currently supports limited parameters.
    /// </summary>
    /// <param name="parameters">Dictionary of parameters to set.</param>
    /// <exception cref="InvalidOperationException">Thrown when trying to set parameters while server is running.</exception>
    /// <exception cref="ArgumentException">Thrown when an unsupported parameter is provided.</exception>
    public void SetParameters(Dictionary<string, object> parameters)
    {
        lock (_lock)
        {
            if (_isRunning)
            {
                throw new InvalidOperationException("Cannot change parameters while server is running. Stop the server first.");
            }
        }

        foreach (var param in parameters)
        {
            switch (param.Key)
            {
                case "HasIssuedExpiredToken":
                    if (param.Value is bool expiredTokenValue)
                        HasIssuedExpiredToken = expiredTokenValue;
                    else
                        throw new ArgumentException($"Parameter '{param.Key}' must be a boolean value.");
                    break;

                case "HasIssuedRefreshToken":
                    if (param.Value is bool refreshTokenValue)
                        HasIssuedRefreshToken = refreshTokenValue;
                    else
                        throw new ArgumentException($"Parameter '{param.Key}' must be a boolean value.");
                    break;

                case "BaseUrl":
                case "Port":
                case "MetadataEndpoint":
                case "JwksEndpoint":
                case "AuthorizeEndpoint":
                case "TokenEndpoint":
                case "IntrospectionEndpoint":
                case "RegistrationEndpoint":
                case "DemoClientId":
                case "DemoClientSecret":
                case "TestRefreshClientId":
                case "TestRefreshClientSecret":
                    // These are read-only parameters
                    throw new ArgumentException($"Parameter '{param.Key}' is read-only and cannot be modified.");

                default:
                    throw new ArgumentException($"Unsupported parameter: {param.Key}");
            }
        }
    }

    /// <summary>
    /// Restarts the server with new parameters if needed.
    /// </summary>
    /// <param name="args">Optional command line arguments.</param>
    /// <returns>A task that represents the asynchronous restart operation.</returns>
    public async Task RestartAsync(string[]? args = null)
    {
        if (IsRunning)
        {
            await StopAsync();
        }
        await StartAsync(args);
    }

    /// <summary>
    /// Waits for the server to stop running.
    /// </summary>
    /// <returns>A task that completes when the server stops.</returns>
    public async Task WaitForShutdownAsync()
    {
        Task? taskToWait = null;

        lock (_lock)
        {
            taskToWait = _serverTask;
        }

        if (taskToWait != null)
        {
            await taskToWait;
        }
    }

    /// <summary>
    /// Clears all persisted OAuth data.
    /// </summary>
    public void ClearPersistedData()
    {
        _oauthServer.ClearPersistedData();
    }

    /// <summary>
    /// Gets information about the persistent storage.
    /// </summary>
    /// <returns>Dictionary containing storage information.</returns>
    public Dictionary<string, object> GetPersistenceInfo()
    {
        return _oauthServer.GetPersistenceInfo();
    }

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public void Dispose()
    {
        if (IsRunning)
        {
            StopAsync().GetAwaiter().GetResult();
        }

        _cancellationTokenSource?.Dispose();
    }
}