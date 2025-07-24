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

    public ProtectedMcpServerWindow(IProtectedMcpServerService mcpServerService, IGeneralSettingsService settingsService, IServiceProvider serviceProvider, ILogger<ProtectedMcpServerWindow> logger)
    {
        _mcpServerService = mcpServerService;
        _settingsService = settingsService;
        _serviceProvider = serviceProvider;
        _logger = logger;
        _availableTools = new ObservableCollection<ToolDisplayModel>();
        
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

    protected override void OnClosed(EventArgs e)
    {
        _statusTimer?.Stop();
        base.OnClosed(e);
    }
}