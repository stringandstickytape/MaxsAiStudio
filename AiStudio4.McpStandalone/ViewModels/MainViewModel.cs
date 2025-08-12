using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using AiStudio4.McpStandalone.Models;
using AiStudio4.McpStandalone.Services;
using System.Windows.Input;
using Microsoft.Extensions.Logging;

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

        private void LoadSampleData()
        {
            SelectedServer = new McpServerConfiguration
            {
                Name = _settingsService.GetServerName(),
                Description = _settingsService.GetServerDescription(),
                IsEnabled = true
            };

            AvailableTools.Add(new McpTool 
            { 
                Name = "File Reader", 
                Description = "Reads files from the filesystem",
                Category = "File Operations",
                IsSelected = false
            });
            AvailableTools.Add(new McpTool 
            { 
                Name = "Web Search", 
                Description = "Searches the web for information",
                Category = "Web",
                IsSelected = false
            });
            AvailableTools.Add(new McpTool 
            { 
                Name = "Calculator", 
                Description = "Performs mathematical calculations",
                Category = "Utilities",
                IsSelected = false
            });
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