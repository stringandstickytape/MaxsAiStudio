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
        private readonly ILogger<MainViewModel> _logger;
        [ObservableProperty]
        private ObservableCollection<McpServerConfiguration> servers = new();

        [ObservableProperty]
        private McpServerConfiguration? selectedServer;

        [ObservableProperty]
        private ObservableCollection<McpTool> availableTools = new();

        [ObservableProperty]
        private bool isServerRunning;

        [ObservableProperty]
        private string serverStatus = "Stopped";

        [ObservableProperty]
        private bool isOAuthConfigVisible;

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

        public ICommand StartServerCommand { get; }
        public ICommand StopServerCommand { get; }
        public ICommand AddServerCommand { get; }
        public ICommand RemoveServerCommand { get; }
        public ICommand ConfigureOAuthCommand { get; }
        public ICommand SaveConfigurationCommand { get; }

        public MainViewModel(IAutoStartOAuthServerService oauthServerService, ISimpleMcpServerService mcpServerService, ILogger<MainViewModel> logger)
        {
            _oauthServerService = oauthServerService;
            _mcpServerService = mcpServerService;
            _logger = logger;

            StartServerCommand = new RelayCommand(StartServer, CanStartServer);
            StopServerCommand = new RelayCommand(StopServer, CanStopServer);
            AddServerCommand = new RelayCommand(AddServer);
            RemoveServerCommand = new RelayCommand(RemoveServer, CanRemoveServer);
            ConfigureOAuthCommand = new RelayCommand(ConfigureOAuth);
            SaveConfigurationCommand = new RelayCommand(SaveConfiguration);

            LoadSampleData();
            UpdateOAuthServerStatus();
            UpdateMcpServerStatus();
        }

        private void LoadSampleData()
        {
            Servers.Add(new McpServerConfiguration
            {
                Name = "Sample MCP Server",
                Command = "node",
                Arguments = "server.js",
                Description = "A sample MCP server for demonstration",
                IsEnabled = true
            });

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

        private bool CanStartServer() => SelectedServer != null && !IsServerRunning;
        private bool CanStopServer() => IsServerRunning;
        private bool CanRemoveServer() => SelectedServer != null;

        private void StartServer()
        {
            if (SelectedServer == null) return;
            IsServerRunning = true;
            ServerStatus = $"Running: {SelectedServer.Name}";
        }

        private void StopServer()
        {
            IsServerRunning = false;
            ServerStatus = "Stopped";
        }

        private void AddServer()
        {
            var newServer = new McpServerConfiguration
            {
                Name = "New MCP Server",
                Command = "",
                Arguments = "",
                Description = "Configure this new server"
            };
            Servers.Add(newServer);
            SelectedServer = newServer;
        }

        private void RemoveServer()
        {
            if (SelectedServer != null)
            {
                Servers.Remove(SelectedServer);
                SelectedServer = Servers.FirstOrDefault();
            }
        }

        private void ConfigureOAuth()
        {
            IsOAuthConfigVisible = !IsOAuthConfigVisible;
        }

        private void SaveConfiguration()
        {
            ServerStatus = "Configuration saved";
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
                OAuthServerStatus = "OAuth Server: Running on http://localhost:7029";
                _logger.LogInformation("OAuth server is running");
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
                McpServerStatus = "MCP Server: Running on http://localhost:7071";
                _logger.LogInformation("MCP server is running");
            }
            else
            {
                McpServerStatus = "MCP Server: Not Running";
                _logger.LogWarning("MCP server is not running");
            }
        }
    }
}