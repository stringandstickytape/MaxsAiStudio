using AiStudio4.Services;
using AiStudio4.InjectedDependencies;
using AiStudio4.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Collections.ObjectModel;
using System.Reflection;
using ModelContextProtocol.Server;
using ModelContextProtocol.TestOAuthServer;
using ModelContextProtocol;

namespace AiStudio4.Dialogs;

// Model for tool display in the UI
public class ToolDisplayModel
{
    public string Guid { get; set; }
    public string Name { get; set; }
    public bool IsEnabled { get; set; }
}

public partial class ProtectedMcpServerWindow : Window
{
    private readonly IProtectedMcpServerService _mcpServerService;
    private readonly IGeneralSettingsService _settingsService;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ProtectedMcpServerWindow> _logger;
    private readonly DispatcherTimer _statusTimer;
    private readonly ObservableCollection<ToolDisplayModel> _availableTools;
    private readonly OAuthServerManager _oauthServerManager;

    public ProtectedMcpServerWindow(IProtectedMcpServerService mcpServerService, IGeneralSettingsService settingsService, IServiceProvider serviceProvider, ILogger<ProtectedMcpServerWindow> logger)
    {
        _mcpServerService = mcpServerService;
        _settingsService = settingsService;
        _serviceProvider = serviceProvider;
        _logger = logger;
        _availableTools = new ObservableCollection<ToolDisplayModel>();
        _oauthServerManager = new OAuthServerManager();
        
        InitializeComponent();
        
        // Initialize tool selection UI
        ToolsItemsControl.ItemsSource = _availableTools;
        LoadAvailableTools();
        
        // Initialize UI with service values (overriding XAML defaults if needed)
        ServerUrlTextBox.Text = _mcpServerService.ServerUrl;
        OAuthServerUrlTextBox.Text = _mcpServerService.OAuthServerUrl;
        ResourceMetadataTextBox.Text = $"{_mcpServerService.ServerUrl}.well-known/oauth-protected-resource";
        ClaudeCodeInstallTextBox.Text = $"claude mcp add --transport http AiStudio4 {_mcpServerService.ServerUrl}";
        
        // Setup status timer to update UI periodically
        _statusTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _statusTimer.Tick += StatusTimer_Tick;
        _statusTimer.Start();
        
        UpdateServerStatus();
        UpdateOAuthServerStatus();
        UpdateOAuthParametersDisplay();
        
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
        UpdateOAuthServerStatus();
    }

    private void UpdateServerStatus()
    {
        bool mcpRunning = _mcpServerService.IsServerRunning;
        bool oauthRunning = _oauthServerManager.IsRunning;
        
        StatusTextBlock.Text = mcpRunning ? "Running" : "Stopped";
        StatusTextBlock.Foreground = mcpRunning ? System.Windows.Media.Brushes.Green : System.Windows.Media.Brushes.Red;
        
        OAuthStatusTextBlock.Text = oauthRunning ? "Running" : "Stopped";
        OAuthStatusTextBlock.Foreground = oauthRunning ? System.Windows.Media.Brushes.Green : System.Windows.Media.Brushes.Red;
        
        // Button text based on the state - if either is running, show "Stop Servers"
        if (mcpRunning || oauthRunning)
        {
            StartStopButton.Content = "Stop Servers";
        }
        else
        {
            StartStopButton.Content = "Start Servers";
        }
        
        StartStopButton.IsEnabled = true;
    }

    private async void StartStopButton_Click(object sender, RoutedEventArgs e)
    {
        StartStopButton.IsEnabled = false;
        
        try
        {
            bool mcpRunning = _mcpServerService.IsServerRunning;
            bool oauthRunning = _oauthServerManager.IsRunning;
            
            if (mcpRunning || oauthRunning)
            {
                // Stop servers in reverse order: MCP first, then OAuth
                if (mcpRunning)
                {
                    LogMessage("Stopping MCP server...");
                    await _mcpServerService.StopServerAsync();
                    LogMessage("✓ MCP server stopped successfully");
                }
                
                if (oauthRunning)
                {
                    LogMessage("Stopping OAuth server...");
                    await _oauthServerManager.StopAsync();
                    LogMessage("✓ OAuth server stopped successfully");
                }
                
                LogMessage("");
                LogMessage("All servers stopped.");
            }
            else
            {
                // Start servers in order: OAuth first, then MCP
                LogMessage("Starting OAuth server...");
                LogMessage($"OAuth server will bind to: {_oauthServerManager.BaseUrl}");
                LogMessage("Configuring OAuth endpoints...");
                LogMessage("Setting up demo clients...");
                
                await _oauthServerManager.StartAsync();
                LogMessage("✓ OAuth server started successfully");
                LogMessage($"✓ OAuth server listening on: {_oauthServerManager.BaseUrl}");
                LogMessage("✓ Authorization endpoint: /authorize");
                LogMessage("✓ Token endpoint: /token");
                LogMessage("✓ Metadata endpoint: /.well-known/oauth-authorization-server");
                LogMessage("✓ JWKS endpoint: /.well-known/jwks.json");
                LogMessage("✓ Demo client configured (ID: demo-client)");
                LogMessage("");
                
                // Wait a moment for OAuth server to be fully ready
                await Task.Delay(2000);
                
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
                    LogMessage("🚀 Both servers are running and ready to accept authenticated MCP requests!");
                }
                else
                {
                    LogMessage("✗ Failed to start MCP server. Check application logs for details.");
                    LogMessage("Stopping OAuth server due to MCP server failure...");
                    await _oauthServerManager.StopAsync();
                    LogMessage("✓ OAuth server stopped");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during server start/stop operation");
            LogMessage($"Error: {ex.Message}");
            
            // Attempt to stop both servers on error
            try
            {
                if (_mcpServerService.IsServerRunning)
                {
                    await _mcpServerService.StopServerAsync();
                    LogMessage("✓ MCP server stopped after error");
                }
                if (_oauthServerManager.IsRunning)
                {
                    await _oauthServerManager.StopAsync();
                    LogMessage("✓ OAuth server stopped after error");
                }
            }
            catch (Exception cleanupEx)
            {
                _logger.LogError(cleanupEx, "Error during cleanup after failure");
                LogMessage($"Cleanup error: {cleanupEx.Message}");
            }
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

    private void LoadAvailableTools()
    {
        _availableTools.Clear();
        
        // Find all ITool implementations with MCP attributes
        var toolTypes = typeof(ITool).Assembly.GetTypes()
            .Where(type => type.IsClass && 
                          !type.IsAbstract &&
                          typeof(ITool).IsAssignableFrom(type) &&
                          type.GetCustomAttribute<McpServerToolTypeAttribute>() != null)
            .ToList();
        
        foreach (var toolType in toolTypes)
        {
            try
            {
                // Use dependency injection to create the tool instance
                var toolInstance = _serviceProvider.GetService(toolType) as ITool;
                if (toolInstance != null)
                {
                    var toolDefinition = toolInstance.GetToolDefinition();
                    if (toolDefinition != null)
                    {
                        var toolModel = new ToolDisplayModel
                        {
                            Guid = toolDefinition.Guid,
                            Name = toolDefinition.Name,
                            IsEnabled = _settingsService.IsMcpToolEnabled(toolDefinition.Guid)
                        };
                        _availableTools.Add(toolModel);
                    }
                }
                else
                {
                    _logger.LogWarning("Could not resolve tool instance for {ToolType}", toolType.Name);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load tool definition for {ToolType}", toolType.Name);
            }
        }
    }
    
    private void ToolCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (sender is CheckBox checkBox && checkBox.Tag is string toolGuid)
        {
            bool isEnabled = checkBox.IsChecked ?? false;
            _settingsService.UpdateMcpToolEnabled(toolGuid, isEnabled);
            
            // Find and update the model
            var toolModel = _availableTools.FirstOrDefault(t => t.Guid == toolGuid);
            if (toolModel != null)
            {
                toolModel.IsEnabled = isEnabled;
            }
            
            LogMessage($"Tool {checkBox.Content} {(isEnabled ? "enabled" : "disabled")} for MCP server");
        }
    }

    private void UpdateOAuthServerStatus()
    {
        // Update URL display
        OAuthServerUrlDisplayTextBox.Text = _oauthServerManager.BaseUrl;
    }

    private void UpdateOAuthParametersDisplay()
    {
        HasIssuedExpiredTokenCheckBox.IsChecked = _oauthServerManager.HasIssuedExpiredToken;
        HasIssuedRefreshTokenCheckBox.IsChecked = _oauthServerManager.HasIssuedRefreshToken;
    }


    private void OAuthParameter_Changed(object sender, RoutedEventArgs e)
    {
        if (_oauthServerManager == null) return;

        try
        {
            var parameters = new Dictionary<string, object>();
            
            if (sender == HasIssuedExpiredTokenCheckBox)
            {
                parameters["HasIssuedExpiredToken"] = HasIssuedExpiredTokenCheckBox.IsChecked ?? false;
            }
            else if (sender == HasIssuedRefreshTokenCheckBox)
            {
                parameters["HasIssuedRefreshToken"] = HasIssuedRefreshTokenCheckBox.IsChecked ?? false;
            }

            _oauthServerManager.SetParameters(parameters);
            
            string paramName = sender == HasIssuedExpiredTokenCheckBox ? "HasIssuedExpiredToken" : "HasIssuedRefreshToken";
            bool isChecked = sender == HasIssuedExpiredTokenCheckBox ? 
                (HasIssuedExpiredTokenCheckBox.IsChecked ?? false) : 
                (HasIssuedRefreshTokenCheckBox.IsChecked ?? false);
            
            LogMessage($"OAuth parameter {paramName} set to: {isChecked}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting OAuth parameters");
            LogMessage($"Error setting OAuth parameters: {ex.Message}");
            
            // Revert the checkbox state
            UpdateOAuthParametersDisplay();
        }
    }

    private void ResetOAuthParametersButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var parameters = new Dictionary<string, object>
            {
                ["HasIssuedExpiredToken"] = false,
                ["HasIssuedRefreshToken"] = false
            };

            _oauthServerManager.SetParameters(parameters);
            UpdateOAuthParametersDisplay();
            
            LogMessage("OAuth parameters reset to default values");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting OAuth parameters");
            LogMessage($"Error resetting OAuth parameters: {ex.Message}");
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        _statusTimer?.Stop();
        _oauthServerManager?.Dispose();
        base.OnClosed(e);
    }
}