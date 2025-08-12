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
    private readonly ILoggerFactory _loggerFactory;
    private readonly StandaloneSettingsService _settingsService;
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
    public AutoStartOAuthServerService(ILogger<AutoStartOAuthServerService> logger, ILoggerFactory loggerFactory, StandaloneSettingsService settingsService)
    {
        _logger = logger;
        _loggerFactory = loggerFactory;
        _settingsService = settingsService;
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
            
            // Debug: Check if any existing OAuth files exist
            if (Directory.Exists(oauthDataDirectory))
            {
                var files = Directory.GetFiles(oauthDataDirectory, "*.json");
                _logger.LogInformation("Found {Count} existing OAuth files in directory:", files.Length);
                foreach (var file in files)
                {
                    var fileInfo = new FileInfo(file);
                    _logger.LogInformation("  - {FileName} (Size: {Size} bytes, Modified: {Modified})", 
                        fileInfo.Name, fileInfo.Length, fileInfo.LastWriteTime);
                }
            }
            
            // Create OAuth server manager with persistence directory and configured ports
            var oauthPort = _settingsService.GetOAuthServerPort();
            var mcpPort = _settingsService.GetMcpServerPort();
            _oauthServerManager = new OAuthServerManager(
                persistenceDataDirectory: oauthDataDirectory,
                port: oauthPort,
                mcpPort: mcpPort);
            
            await _oauthServerManager.StartAsync();
            
            _logger.LogInformation("OAuth server started on port {Port} with MCP port {McpPort}", oauthPort, mcpPort);
            
            _logger.LogInformation("OAuth server started successfully for auto-start");
            
            // Debug: Check OAuth server state after startup
            _logger.LogInformation("OAuth server running status: {IsRunning}", _oauthServerManager.IsRunning);
            _logger.LogInformation("OAuth persistence directory being used: {Directory}", oauthDataDirectory);
            
            // Ensure we have a registered OAuth client
            var clientManager = new OAuthClientManager(_loggerFactory.CreateLogger<OAuthClientManager>(), _settingsService);
            var registered = await clientManager.EnsureClientRegisteredAsync();
            
            if (registered)
            {
                _logger.LogInformation("OAuth client ready - ID: {ClientId}", clientManager.ClientId);
            }
            else
            {
                _logger.LogWarning("Failed to ensure OAuth client registration");
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
                
                // Debug: Check OAuth directory contents before stopping
                var oauthDataDirectory = PathHelper.GetProfileSubPath("OAuth");
                if (Directory.Exists(oauthDataDirectory))
                {
                    var files = Directory.GetFiles(oauthDataDirectory, "*.json");
                    _logger.LogInformation("OAuth files before shutdown ({Count} files):", files.Length);
                    foreach (var file in files)
                    {
                        var fileInfo = new FileInfo(file);
                        _logger.LogInformation("  - {FileName} (Size: {Size} bytes)", fileInfo.Name, fileInfo.Length);
                    }
                }
                
                await _oauthServerManager.StopAsync();
                _logger.LogInformation("OAuth server stopped successfully");
                
                // Debug: Check OAuth directory contents after stopping
                if (Directory.Exists(oauthDataDirectory))
                {
                    var files = Directory.GetFiles(oauthDataDirectory, "*.json");
                    _logger.LogInformation("OAuth files after shutdown ({Count} files):", files.Length);
                    foreach (var file in files)
                    {
                        var fileInfo = new FileInfo(file);
                        _logger.LogInformation("  - {FileName} (Size: {Size} bytes)", fileInfo.Name, fileInfo.Length);
                    }
                }
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