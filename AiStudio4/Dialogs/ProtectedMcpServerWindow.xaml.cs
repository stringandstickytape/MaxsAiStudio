using AiStudio4.Services;
using Microsoft.Extensions.Logging;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace AiStudio4.Dialogs;

public partial class ProtectedMcpServerWindow : Window
{
    private readonly IProtectedMcpServerService _mcpServerService;
    private readonly ILogger<ProtectedMcpServerWindow> _logger;
    private readonly DispatcherTimer _statusTimer;

    public ProtectedMcpServerWindow(IProtectedMcpServerService mcpServerService, ILogger<ProtectedMcpServerWindow> logger)
    {
        _mcpServerService = mcpServerService;
        _logger = logger;
        
        InitializeComponent();
        
        // Initialize UI with service values (overriding XAML defaults if needed)
        ServerUrlTextBox.Text = _mcpServerService.ServerUrl;
        OAuthServerUrlTextBox.Text = _mcpServerService.OAuthServerUrl;
        ResourceMetadataTextBox.Text = $"{_mcpServerService.ServerUrl}.well-known/oauth-protected-resource";
        
        // Setup status timer to update UI periodically
        _statusTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _statusTimer.Tick += StatusTimer_Tick;
        _statusTimer.Start();
        
        UpdateServerStatus();
        
        // Subscribe to log events (if implemented)
        LogMessage("Protected MCP Server Management Window initialized");
        LogMessage("This server provides weather tools with OAuth JWT authentication:");
        LogMessage("  • GetAlerts - Get weather alerts for a US state (2-letter code)");
        LogMessage("  • GetForecast - Get weather forecast for latitude/longitude coordinates");
        LogMessage("");
        LogMessage("Authentication: Tokens must be validated from the OAuth server");
        LogMessage("Required scope: mcp:tools");
        LogMessage("");
    }

    private void StatusTimer_Tick(object? sender, EventArgs e)
    {
        UpdateServerStatus();
    }

    private void UpdateServerStatus()
    {
        bool isRunning = _mcpServerService.IsServerRunning;
        
        StatusTextBlock.Text = isRunning ? "Running" : "Stopped";
        StatusTextBlock.Foreground = isRunning ? System.Windows.Media.Brushes.Green : System.Windows.Media.Brushes.Red;
        
        StartStopButton.Content = isRunning ? "Stop Server" : "Start Server";
        StartStopButton.IsEnabled = true;
    }

    private async void StartStopButton_Click(object sender, RoutedEventArgs e)
    {
        StartStopButton.IsEnabled = false;
        
        try
        {
            if (_mcpServerService.IsServerRunning)
            {
                LogMessage("Stopping MCP server...");
                await _mcpServerService.StopServerAsync();
                LogMessage("MCP server stopped successfully");
            }
            else
            {
                LogMessage("Starting MCP server...");
                LogMessage($"Server will bind to: {_mcpServerService.ServerUrl}");
                LogMessage($"Using OAuth server: {_mcpServerService.OAuthServerUrl}");
                LogMessage("Configuring JWT Bearer authentication...");
                LogMessage("Registering WeatherTools with MCP server...");
                
                bool success = await _mcpServerService.StartServerAsync();
                if (success)
                {
                    LogMessage("✓ MCP server started successfully");
                    LogMessage($"✓ Server listening on: {_mcpServerService.ServerUrl}");
                    LogMessage($"✓ OAuth validation from: {_mcpServerService.OAuthServerUrl}");
                    LogMessage($"✓ Resource metadata available at: {_mcpServerService.ServerUrl}.well-known/oauth-protected-resource");
                    LogMessage("✓ WeatherTools registered and ready");
                    LogMessage("");
                    LogMessage("Server is ready to accept authenticated MCP requests!");
                }
                else
                {
                    LogMessage("✗ Failed to start MCP server. Check application logs for details.");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during server start/stop operation");
            LogMessage($"Error: {ex.Message}");
        }
        finally
        {
            UpdateServerStatus();
        }
    }

    private void ClearLogsButton_Click(object sender, RoutedEventArgs e)
    {
        LogTextBox.Clear();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void LogMessage(string message)
    {
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        string logEntry = $"[{timestamp}] {message}\n";
        
        Dispatcher.Invoke(() =>
        {
            LogTextBox.AppendText(logEntry);
            LogScrollViewer.ScrollToEnd();
        });
    }

    protected override void OnClosed(EventArgs e)
    {
        _statusTimer?.Stop();
        base.OnClosed(e);
    }
}