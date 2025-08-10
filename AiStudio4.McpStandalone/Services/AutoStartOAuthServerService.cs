using AiStudio4.McpStandalone.Helpers;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.TestOAuthServer;
using System.IO;

namespace AiStudio4.McpStandalone.Services;

/// <summary>
/// Service for managing the OAuth server that auto-starts with the application.
/// </summary>
public class AutoStartOAuthServerService : IAutoStartOAuthServerService
{
    private readonly ILogger<AutoStartOAuthServerService> _logger;
    private OAuthServerManager? _oauthServerManager;
    private bool _disposed = false;

    /// <summary>
    /// Gets a value indicating whether the OAuth server is running.
    /// </summary>
    public bool IsRunning => _oauthServerManager?.IsRunning ?? false;

    /// <summary>
    /// Initializes a new instance of the <see cref="AutoStartOAuthServerService"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public AutoStartOAuthServerService(ILogger<AutoStartOAuthServerService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Starts the OAuth server asynchronously.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task StartAsync()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(AutoStartOAuthServerService));

        if (_oauthServerManager != null && _oauthServerManager.IsRunning)
        {
            _logger.LogInformation("OAuth server is already running");
            return;
        }

        try
        {
            _logger.LogInformation("Starting OAuth server for auto-start...");
            
            // Ensure OAuth data directory exists
            var oauthDataDirectory = PathHelper.GetProfileSubPath("OAuth");
            Directory.CreateDirectory(oauthDataDirectory);
            _logger.LogInformation("OAuth data directory: {Directory}", oauthDataDirectory);
            
            // Create OAuth server manager with persistence directory
            _oauthServerManager = new OAuthServerManager(persistenceDataDirectory: oauthDataDirectory);
            
            await _oauthServerManager.StartAsync();
            
            _logger.LogInformation("OAuth server started successfully for auto-start");
            
            // Register a default client for testing
            using (var initializer = new OAuthServerInitializer(_logger))
            {
                await initializer.RegisterDefaultClientAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start OAuth server for auto-start");
            throw;
        }
    }

    /// <summary>
    /// Stops the OAuth server asynchronously.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task StopAsync()
    {
        if (_oauthServerManager != null && _oauthServerManager.IsRunning)
        {
            try
            {
                _logger.LogInformation("Stopping OAuth server...");
                await _oauthServerManager.StopAsync();
                _logger.LogInformation("OAuth server stopped successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping OAuth server");
            }
        }
    }

    /// <summary>
    /// Gets the OAuth server manager instance.
    /// </summary>
    /// <returns>The OAuth server manager, or null if not initialized.</returns>
    public OAuthServerManager? GetOAuthServerManager()
    {
        return _oauthServerManager;
    }

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            try
            {
                if (_oauthServerManager != null)
                {
                    if (_oauthServerManager.IsRunning)
                    {
                        // Synchronous stop for disposal
                        _oauthServerManager.StopAsync().GetAwaiter().GetResult();
                    }
                    _oauthServerManager.Dispose();
                    _oauthServerManager = null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing OAuth server manager");
            }
            
            _disposed = true;
        }
    }
}