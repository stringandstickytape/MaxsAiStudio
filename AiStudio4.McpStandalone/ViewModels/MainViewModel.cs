using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using AiStudio4.McpStandalone.Models;
using AiStudio4.McpStandalone.Services;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System;
using Wpf.Ui.Controls;

namespace AiStudio4.McpStandalone.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly IAutoStartOAuthServerService _oauthServerService;
        private readonly ISimpleMcpServerService _mcpServerService;
        private readonly StandaloneSettingsService _settingsService;
        private readonly ILogger<MainViewModel> _logger;
        [ObservableProperty]
        private McpServerConfiguration selectedServer = new();

        [ObservableProperty]
        private ObservableCollection<McpTool> availableTools = new();

        [ObservableProperty]
        private string searchText = string.Empty;

        [ObservableProperty]
        private bool isOAuthServerRunning;

        [ObservableProperty]
        private string oAuthServerStatus = "OAuth Server: Checking...";

        [ObservableProperty]
        private bool isMcpServerRunning;

        [ObservableProperty]
        private string mcpServerStatus = "MCP Server: Checking...";

        [ObservableProperty]
        private string claudeInstallCommand = "claude mcp add --transport http McpStandalone http://localhost:7071/";
        
        [ObservableProperty]
        private ObservableCollection<NavigationViewItem> navigationItems = new();
        
        [ObservableProperty]
        private ObservableCollection<NavigationViewItem> footerNavigationItems = new();
        
        [ObservableProperty]
        private int mcpServerPort;
        
        [ObservableProperty]
        private int oAuthServerPort;

        public MainViewModel(IAutoStartOAuthServerService oauthServerService, ISimpleMcpServerService mcpServerService, StandaloneSettingsService settingsService, ILogger<MainViewModel> logger)
        {
            _oauthServerService = oauthServerService;
            _mcpServerService = mcpServerService;
            _settingsService = settingsService;
            _logger = logger;

            LoadAvailableTools();
            UpdateOAuthServerStatus();
            UpdateMcpServerStatus();
            UpdateClaudeInstallCommand();
            McpServerPort = _settingsService.GetMcpServerPort();
            OAuthServerPort = _settingsService.GetOAuthServerPort();
        }

        [ObservableProperty]
        private bool needsRestart = false;
        
        [ObservableProperty]
        private string restartMessage = string.Empty;
        
        private void LoadAvailableTools()
        {
            SelectedServer = new McpServerConfiguration
            {
                Name = _settingsService.GetServerName(),
                Description = _settingsService.GetServerDescription(),
                IsEnabled = true
            };

            // Discover all available MCP tools from the Tools assembly
            var enabledTools = _settingsService.GetEnabledTools();
            var toolsAssembly = typeof(AiStudio4.Tools.Interfaces.ITool).Assembly;
            
            var toolTypes = toolsAssembly.GetTypes()
                .Where(t => typeof(AiStudio4.Tools.Interfaces.ITool).IsAssignableFrom(t) 
                    && !t.IsInterface 
                    && !t.IsAbstract
                    && t.GetCustomAttribute<ModelContextProtocol.Server.McpServerToolTypeAttribute>() != null)
                .ToList();
            
            foreach (var toolType in toolTypes)
            {
                try
                {
                    // Create an instance to get the tool definition
                    var toolInstance = Activator.CreateInstance(toolType, 
                        new object?[] { null, _settingsService, null }) as AiStudio4.Tools.Interfaces.ITool;
                    
                    if (toolInstance != null)
                    {
                        var toolDef = toolInstance.GetToolDefinition();
                        if (toolDef != null)
                        {
                            var mcpTool = new McpTool
                            {
                                Name = toolDef.Name,
                                Description = toolDef.Description ?? string.Empty,
                                Category = toolDef.Categories?.FirstOrDefault() ?? "Tools",
                                IsSelected = enabledTools.Contains(toolType.Name),
                                ToolId = toolType.Name
                            };
                            
                            // Subscribe to property changes to persist selection and update server
                            mcpTool.PropertyChanged += (sender, e) =>
                            {
                                if (e.PropertyName == nameof(McpTool.IsSelected) && sender is McpTool tool)
                                {
                                    SaveToolSelection();
                                    // Update the tool state in the running server
                                    _mcpServerService.UpdateToolState(tool.ToolId, tool.IsSelected);
                                    _logger.LogInformation("Updated tool state: {ToolId} = {IsSelected}", tool.ToolId, tool.IsSelected);
                                    
                                    // Show restart needed message
                                    NeedsRestart = true;
                                    RestartMessage = "Tool selection changed. Restart the MCP server to apply changes.";
                                }
                            };
                            
                            AvailableTools.Add(mcpTool);
                            _logger.LogInformation("Discovered tool: {ToolName} ({ToolId})", mcpTool.Name, mcpTool.ToolId);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to load tool {ToolType}", toolType.Name);
                }
            }
            
            _logger.LogInformation("Loaded {Count} available tools", AvailableTools.Count);
        }
        
        [ObservableProperty]
        private bool isRestarting = false;
        
        [ObservableProperty]
        private bool showReconnectInfo = false;
        
        [ObservableProperty]
        private string reconnectInfoMessage = string.Empty;
        
        [RelayCommand]
        private async Task RestartServer()
        {
            try
            {
                IsRestarting = true;
                RestartMessage = "Restarting server...";
                ShowReconnectInfo = false;
                
                await _mcpServerService.RestartServerAsync();
                
                NeedsRestart = false;
                RestartMessage = string.Empty;
                IsRestarting = false;
                UpdateMcpServerStatus();
                
                // Show reconnect info
                ShowReconnectInfo = true;
                ReconnectInfoMessage = "Server restarted. In Claude, use: /mcp → McpStandalone → Reconnect";
                
                _logger.LogInformation("MCP server restarted successfully");
                
                // Auto-hide the message after 10 seconds
                _ = Task.Run(async () =>
                {
                    await Task.Delay(10000);
                    ShowReconnectInfo = false;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to restart MCP server");
                RestartMessage = "Failed to restart server: " + ex.Message;
                IsRestarting = false;
            }
        }

        private void SaveToolSelection()
        {
            var enabledTools = AvailableTools
                .Where(t => t.IsSelected && !string.IsNullOrEmpty(t.ToolId))
                .Select(t => t.ToolId)
                .ToList();
            
            _settingsService.SetEnabledTools(enabledTools);
            _logger.LogInformation("Saved tool selection: {Tools}", string.Join(", ", enabledTools));
        }

        partial void OnSearchTextChanged(string value)
        {
            FilterTools(value);
        }

        private void FilterTools(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                foreach (var tool in AvailableTools)
                {
                    tool.IsSelected = tool.IsSelected;
                }
            }
        }

        private void UpdateOAuthServerStatus()
        {
            IsOAuthServerRunning = _oauthServerService.IsRunning;
            if (IsOAuthServerRunning)
            {
                var port = _settingsService.GetOAuthServerPort();
                OAuthServerStatus = $"OAuth Server: Running on http://localhost:{port}";
                _logger.LogInformation("OAuth server is running on port {Port}", port);
            }
            else
            {
                OAuthServerStatus = "OAuth Server: Not Running";
                _logger.LogWarning("OAuth server is not running");
            }
        }

        private void UpdateMcpServerStatus()
        {
            IsMcpServerRunning = _mcpServerService.IsServerRunning;
            if (IsMcpServerRunning)
            {
                var port = _settingsService.GetMcpServerPort();
                McpServerStatus = $"MCP Server: Running on http://localhost:{port}";
                _logger.LogInformation("MCP server is running on port {Port}", port);
            }
            else
            {
                McpServerStatus = "MCP Server: Not Running";
                _logger.LogWarning("MCP server is not running");
            }
        }

        private void UpdateClaudeInstallCommand()
        {
            var port = _settingsService.GetMcpServerPort();
            ClaudeInstallCommand = $"claude mcp add --transport http McpStandalone http://localhost:{port}/";
        }
        
        public void InitializeNavigation()
        {
            NavigationItems = new ObservableCollection<NavigationViewItem>
            {
                new NavigationViewItem
                {
                    Content = "Server",
                    Icon = new SymbolIcon { Symbol = SymbolRegular.Server24 },
                    Tag = "Server"
                },
                new NavigationViewItem
                {
                    Content = "Settings",
                    Icon = new SymbolIcon { Symbol = SymbolRegular.Settings24 },
                    Tag = "Settings"
                },
                new NavigationViewItem
                {
                    Content = "Legacy",
                    Icon = new SymbolIcon { Symbol = SymbolRegular.History24 },
                    Tag = "Legacy"
                }
            };
            
            FooterNavigationItems = new ObservableCollection<NavigationViewItem>();
        }
    }
}