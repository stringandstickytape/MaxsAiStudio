namespace ModelContextProtocol.TestOAuthServer;

/// <summary>
/// Console host entry point for backward compatibility.
/// </summary>
public static class ConsoleHost
{
    /// <summary>
    /// Entry point for the console application.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task Main(string[] args)
    {
        using var manager = new OAuthServerManager();
        
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            manager.StopAsync().GetAwaiter().GetResult();
        };

        await manager.StartAsync(args);
        await manager.WaitForShutdownAsync();
    }
}