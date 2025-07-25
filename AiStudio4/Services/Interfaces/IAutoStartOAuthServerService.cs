namespace AiStudio4.Services.Interfaces;

/// <summary>
/// Service for managing the OAuth server that auto-starts with the application.
/// </summary>
public interface IAutoStartOAuthServerService : IDisposable
{
    /// <summary>
    /// Gets a value indicating whether the OAuth server is running.
    /// </summary>
    bool IsRunning { get; }

    /// <summary>
    /// Starts the OAuth server asynchronously.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task StartAsync();

    /// <summary>
    /// Stops the OAuth server asynchronously.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task StopAsync();

    /// <summary>
    /// Gets the OAuth server manager instance.
    /// </summary>
    ModelContextProtocol.TestOAuthServer.OAuthServerManager? GetOAuthServerManager();
}