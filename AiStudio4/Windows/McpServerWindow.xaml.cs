using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using AiStudio4.Core.Interfaces;
using AiStudio4.Services;
using AiStudio4.InjectedDependencies;
using System.Collections.Generic;

namespace AiStudio4.Windows
{
    public partial class McpServerWindow : Window
    {
        private readonly IMcpServerService _mcpServerService;
        private readonly IGeneralSettingsService _settingsService;
        private readonly IEnumerable<ITool> _tools;
        private readonly ObservableCollection<ActivityLogEntry> _activityLog = new();
        private readonly ObservableCollection<ToolViewModel> _toolViewModels = new();
        private readonly ObservableCollection<ClientInfo> _connectedClients = new();
        private readonly DispatcherTimer _updateTimer;

        public McpServerWindow(
            IMcpServerService mcpServerService,
            IGeneralSettingsService settingsService,
            IServiceProvider serviceProvider)
        {
            InitializeComponent();
            
            _mcpServerService = mcpServerService;
            _settingsService = settingsService;
            _tools = serviceProvider.GetServices<ITool>();

            InitializeUI();
            LoadSettings();
            
            _updateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _updateTimer.Tick += UpdateTimer_Tick;
            _updateTimer.Start();

            _mcpServerService.StatusChanged += OnServerStatusChanged;
            
            // Set server version
            ServerVersionText.Text = $"v{System.Reflection.Assembly.GetExecutingAssembly().GetName().Version}";
        }

        private void InitializeUI()
        {
            ActivityLog.ItemsSource = _activityLog;
            ToolsGrid.ItemsSource = _toolViewModels;
            ClientsList.ItemsSource = _connectedClients;

            // Load tools
            foreach (var tool in _tools)
            {
                var toolDef = tool.GetToolDefinition();
                _toolViewModels.Add(new ToolViewModel
                {
                    Guid = toolDef.Guid,
                    Name = toolDef.Name,
                    Category = toolDef.Categories?.FirstOrDefault() ?? "Uncategorized",
                    Description = toolDef.Description,
                    IsEnabled = true // Will be loaded from settings
                });
            }

            UpdateUI();
        }

        private void LoadSettings()
        {
            var settings = _settingsService.CurrentSettings;
            var mcpSettings = settings.McpServer;
            
            TransportTypeCombo.SelectedIndex = mcpSettings.DefaultTransportType == McpServerTransportType.Stdio ? 0 : 1;
            PortTextBox.Text = mcpSettings.SsePort.ToString();
            EnableLoggingCheckBox.IsChecked = mcpSettings.EnableLogging;
            AutoStartCheckBox.IsChecked = mcpSettings.AutoStart;
            DefaultPortTextBox.Text = mcpSettings.SsePort.ToString();

            // Load disabled tools
            foreach (var toolVm in _toolViewModels)
            {
                toolVm.IsEnabled = !mcpSettings.DisabledToolGuids.Contains(toolVm.Guid);
            }
        }

        private async void StartStopButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_mcpServerService.IsRunning)
                {
                    await _mcpServerService.StopServerAsync();
                }
                else
                {
                    var transportType = McpServerTransportType.Stdio; // Only Stdio supported for now
                    if (TransportTypeCombo.SelectedIndex == 1)
                    {
                        MessageBox.Show("SSE transport is not yet implemented. Using Stdio instead.", "Information", 
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }

                    var config = new McpServerConfig
                    {
                        HttpPort = transportType == McpServerTransportType.Sse 
                            ? int.Parse(PortTextBox.Text) 
                            : null,
                        EnableLogging = EnableLoggingCheckBox.IsChecked ?? false,
                        ExcludedToolGuids = _toolViewModels
                            .Where(t => !t.IsEnabled)
                            .Select(t => t.Guid)
                            .ToList()
                    };

                    await _mcpServerService.StartServerAsync(transportType, config);
                }

                UpdateUI();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "MCP Server Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateUI()
        {
            var isRunning = _mcpServerService.IsRunning;
            
            StartStopButton.Content = isRunning ? "Stop Server" : "Start Server";
            TransportTypeCombo.IsEnabled = !isRunning;
            PortTextBox.IsEnabled = !isRunning;
            
            if (isRunning)
            {
                StatusText.Text = $"Running ({_mcpServerService.CurrentTransportType})";
                ServerInfoText.Text = $"Server Status: Running\nTransport: {_mcpServerService.CurrentTransportType}";
                
                if (_mcpServerService.CurrentTransportType == McpServerTransportType.Stdio)
                {
                    ConnectionInstructionsText.Text = "npx @modelcontextprotocol/inspector aistudio4-mcp";
                }
                else
                {
                    var port = int.Parse(PortTextBox.Text);
                    ConnectionInstructionsText.Text = $"Base URL: http://localhost:{port}\n" +
                                                     $"Events: http://localhost:{port}/events\n" +
                                                     $"RPC: http://localhost:{port}/rpc";
                    BaseUrlTextBox.Text = $"http://localhost:{port}";
                }
            }
            else
            {
                StatusText.Text = "Stopped";
                ServerInfoText.Text = "Server Status: Stopped";
                ConnectionInstructionsText.Text = "Start the server to see connection instructions";
                BaseUrlTextBox.Text = "";
            }

            ConnectionInfoText.Text = $"Connected Clients: {_connectedClients.Count}";
            ClientCountText.Text = $"Connected Clients: {_connectedClients.Count}";
            ToolCountText.Text = $"Total Tools: {_toolViewModels.Count}";
        }

        private void TransportTypeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SseOptions != null)
            {
                SseOptions.Visibility = TransportTypeCombo.SelectedIndex == 1 
                    ? Visibility.Visible 
                    : Visibility.Collapsed;
            }
        }

        private void OnServerStatusChanged(object sender, McpServerStatusChangedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                AddActivityLog(e.Message ?? "Status changed");
                UpdateUI();
            });
        }

        private void AddActivityLog(string message)
        {
            _activityLog.Insert(0, new ActivityLogEntry
            {
                Timestamp = DateTime.Now.ToString("HH:mm:ss"),
                Message = message
            });

            // Keep only last 100 entries
            while (_activityLog.Count > 100)
            {
                _activityLog.RemoveAt(_activityLog.Count - 1);
            }
        }

        private void UpdateTimer_Tick(object sender, EventArgs e)
        {
            if (_mcpServerService.IsRunning)
            {
                // Update connected clients
                var clients = _mcpServerService.GetConnectedClients();
                // For now, just update the count - in a real implementation you'd update the actual list
                if (clients.Count != _connectedClients.Count)
                {
                    Dispatcher.Invoke(() =>
                    {
                        _connectedClients.Clear();
                        foreach (var clientId in clients)
                        {
                            _connectedClients.Add(new ClientInfo
                            {
                                ClientId = clientId,
                                ConnectedAt = DateTime.Now,
                                LastActivity = DateTime.Now
                            });
                        }
                        UpdateUI();
                    });
                }
            }
        }

        private void SaveConfiguration_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var settings = _settingsService.CurrentSettings;
                
                settings.McpServer.DefaultTransportType = McpServerTransportType.Stdio; // Only Stdio supported for now
                settings.McpServer.SsePort = int.Parse(DefaultPortTextBox.Text);
                settings.McpServer.EnableLogging = EnableLoggingCheckBox.IsChecked ?? false;
                settings.McpServer.AutoStart = AutoStartCheckBox.IsChecked ?? false;
                settings.McpServer.DisabledToolGuids = _toolViewModels
                    .Where(t => !t.IsEnabled)
                    .Select(t => t.Guid)
                    .ToList();

                _settingsService.SaveSettings();
                
                MessageBox.Show("Configuration saved successfully", "Success", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving configuration: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowDisabledToolsCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            // Filter tools based on checkbox - could implement this later
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _updateTimer.Stop();
            _mcpServerService.StatusChanged -= OnServerStatusChanged;
        }

        // View Models
        private class ActivityLogEntry
        {
            public string Timestamp { get; set; } = "";
            public string Message { get; set; } = "";
        }

        private class ToolViewModel : INotifyPropertyChanged
        {
            private bool _isEnabled;

            public string Guid { get; set; } = "";
            public string Name { get; set; } = "";
            public string Category { get; set; } = "";
            public string Description { get; set; } = "";

            public bool IsEnabled
            {
                get => _isEnabled;
                set
                {
                    _isEnabled = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsEnabled)));
                }
            }

            public event PropertyChangedEventHandler? PropertyChanged;
        }

        private class ClientInfo
        {
            public string ClientId { get; set; } = "";
            public DateTime ConnectedAt { get; set; }
            public DateTime LastActivity { get; set; }
        }
    }
}