using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using AiStudio4.McpStandalone.Models;
using AiStudio4.McpStandalone.Services;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Collections.Generic;

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

        public MainViewModel(IAutoStartOAuthServerService oauthServerService, ISimpleMcpServerService mcpServerService, StandaloneSettingsService settingsService, ILogger<MainViewModel> logger)
        {
            _oauthServerService = oauthServerService;
            _mcpServerService = mcpServerService;
            _settingsService = settingsService;
            _logger = logger;

            LoadSampleData();
            UpdateOAuthServerStatus();
            UpdateMcpServerStatus();
            UpdateClaudeInstallCommand();
        }

        [ObservableProperty]
        private bool needsRestart = false;
        
        [ObservableProperty]
        private string restartMessage = string.Empty;
        
        private void LoadSampleData()
        {
            SelectedServer = new McpServerConfiguration
            {
                Name = _settingsService.GetServerName(),
                Description = _settingsService.GetServerDescription(),
                IsEnabled = true
            };

            // Load the real YouTube Search Tool
            var enabledTools = _settingsService.GetEnabledTools();
            var youtubeSearchTool = new McpTool 
            { 
                Name = "YouTube Search", 
                Description = "Search YouTube for videos, channels, and playlists",
                Category = "Web APIs",
                IsSelected = enabledTools.Contains("YouTubeSearchTool"),
                ToolId = "YouTubeSearchTool"
            };
            
            // Subscribe to property changes to persist selection and update server
            youtubeSearchTool.PropertyChanged += (sender, e) => 
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
            
            AvailableTools.Add(youtubeSearchTool);
        }
        
        [ObservableProperty]
        private bool isRestarting = false;
        
        [RelayCommand]
        private async Task RestartServer()
        {
            try
            {
                IsRestarting = true;
                RestartMessage = "Restarting server...";
                
                await _mcpServerService.RestartServerAsync();
                
                NeedsRestart = false;
                RestartMessage = string.Empty;
                IsRestarting = false;
                UpdateMcpServerStatus();
                _logger.LogInformation("MCP server restarted successfully");
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
    }
}