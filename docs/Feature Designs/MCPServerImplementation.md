# MCP Server Implementation Design

## Overview

This document outlines the design for adding a Model Context Protocol (MCP) server to AiStudio4 that exposes all existing ITool implementations. The server will support both stdio and SSE (Server-Sent Events) transports and include a WPF management window.

## Architecture Overview

```
┌─────────────────────────────────────────────────────────┐
│                   AiStudio4 Application                   │
├─────────────────────────────────────────────────────────┤
│  ┌─────────────────┐    ┌──────────────────────────┐    │
│  │ Existing ITools │    │    MCP Server Module     │    │
│  │  - 60+ tools    │───►│  ┌──────────────────┐   │    │
│  │  - ITool i/face │    │  │ Tool Adapter     │   │    │
│  └─────────────────┘    │  │ (ITool → MCP)    │   │    │
│                         │  └──────────────────┘   │    │
│                         │  ┌──────────────────┐   │    │
│                         │  │ Transport Layer  │   │    │
│                         │  │ - Stdio          │   │    │
│                         │  │ - SSE (HTTP)     │   │    │
│                         │  └──────────────────┘   │    │
│                         └──────────────────────────┘    │
│  ┌─────────────────────────────────────────────────────┐    │
│  │         MCP Server Management Window (WPF)       │    │
│  └─────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────┘
```

## Core Components

### 1. MCP Server Service Interface

**File**: `AiStudio4/Core/Interfaces/IMcpServerService.cs`

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ModelContextProtocol.Server;

namespace AiStudio4.Core.Interfaces
{
    public interface IMcpServerService
    {
        Task<IMcpServer> StartServerAsync(McpServerTransportType transportType, McpServerConfig config);
        Task StopServerAsync();
        bool IsRunning { get; }
        McpServerTransportType CurrentTransportType { get; }
        event EventHandler<McpServerStatusChangedEventArgs> StatusChanged;
        IReadOnlyList<string> GetConnectedClients();
    }

    public enum McpServerTransportType
    {
        Stdio,
        Sse
    }

    public class McpServerConfig
    {
        public int? HttpPort { get; set; } // For SSE
        public string? StdioCommand { get; set; } // For stdio
        public bool EnableLogging { get; set; }
        public List<string> ExcludedToolGuids { get; set; } = new();
    }

    public class McpServerStatusChangedEventArgs : EventArgs
    {
        public bool IsRunning { get; set; }
        public string? Message { get; set; }
        public McpServerTransportType? TransportType { get; set; }
    }
}
```

### 2. Tool Adapter

**File**: `AiStudio4/Services/Mcp/IToolToMcpAdapter.cs`

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;
using AiStudio4.Core.Interfaces;
using AiStudio4.Core.Models;

namespace AiStudio4.Services.Mcp
{
    public class IToolToMcpAdapter
    {
        private readonly IEnumerable<ITool> _tools;
        private readonly IBuiltInToolExtraPropertiesService _extraPropertiesService;
        private readonly IServiceProvider _serviceProvider;
        private readonly Dictionary<string, ITool> _toolLookup;

        public IToolToMcpAdapter(
            IEnumerable<ITool> tools, 
            IBuiltInToolExtraPropertiesService extraPropertiesService,
            IServiceProvider serviceProvider)
        {
            _tools = tools;
            _extraPropertiesService = extraPropertiesService;
            _serviceProvider = serviceProvider;
            _toolLookup = tools.ToDictionary(t => t.GetToolDefinition().Name, t => t);
        }

        public Tool ConvertToMcpTool(ITool tool)
        {
            var toolDef = tool.GetToolDefinition();
            return new Tool
            {
                Name = toolDef.Name,
                Description = toolDef.Description,
                InputSchema = JsonSerializer.Deserialize<JsonElement>(toolDef.Schema)
            };
        }

        public async Task<CallToolResult> ExecuteTool(
            string toolName, 
            Dictionary<string, object?> arguments, 
            CancellationToken cancellationToken)
        {
            if (!_toolLookup.TryGetValue(toolName, out var tool))
            {
                throw new McpException($"Unknown tool: '{toolName}'");
            }

            try
            {
                // Convert arguments to JSON string as expected by ITool
                var jsonArguments = JsonSerializer.Serialize(arguments);
                
                // Get extra properties for the tool
                var extraProperties = await _extraPropertiesService.GetExtraPropertiesAsync(
                    tool.GetToolDefinition().Guid.ToString());

                // Execute the tool
                var result = await tool.ProcessAsync(jsonArguments, extraProperties);

                // Convert result to MCP format
                var content = new List<ContentBlock>();

                if (!string.IsNullOrEmpty(result.Output))
                {
                    content.Add(new TextContentBlock 
                    { 
                        Type = "text", 
                        Text = result.Output 
                    });
                }

                if (result.Error != null)
                {
                    content.Add(new TextContentBlock 
                    { 
                        Type = "text", 
                        Text = $"Error: {result.Error}" 
                    });
                }

                return new CallToolResult { Content = content };
            }
            catch (Exception ex)
            {
                throw new McpException($"Tool execution failed: {ex.Message}", ex);
            }
        }
    }
}
```

### 3. MCP Server Implementation

**File**: `AiStudio4/Services/McpServerService.cs`

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using AiStudio4.Core.Interfaces;
using AiStudio4.Services.Mcp;

namespace AiStudio4.Services
{
    public class AiStudioMcpServerService : IMcpServerService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AiStudioMcpServerService> _logger;
        private readonly IBuiltInToolExtraPropertiesService _extraPropertiesService;
        private IMcpServer? _currentServer;
        private CancellationTokenSource? _serverCts;
        private McpServerTransportType? _currentTransportType;
        private readonly List<string> _connectedClients = new();

        public bool IsRunning => _currentServer != null && _serverCts != null && !_serverCts.Token.IsCancellationRequested;
        public McpServerTransportType CurrentTransportType => _currentTransportType ?? McpServerTransportType.Stdio;
        public event EventHandler<McpServerStatusChangedEventArgs>? StatusChanged;

        public AiStudioMcpServerService(
            IServiceProvider serviceProvider,
            ILogger<AiStudioMcpServerService> logger,
            IBuiltInToolExtraPropertiesService extraPropertiesService)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _extraPropertiesService = extraPropertiesService;
        }

        public async Task<IMcpServer> StartServerAsync(McpServerTransportType transportType, McpServerConfig config)
        {
            try
            {
                // Stop existing server if running
                await StopServerAsync();

                _logger.LogInformation($"Starting MCP server with {transportType} transport");

                // Create transport based on type
                IMcpServerTransport transport = transportType switch
                {
                    McpServerTransportType.Stdio => new StdioServerTransport("AiStudio4-MCP"),
                    McpServerTransportType.Sse => new SseServerTransport(config.HttpPort ?? 3000),
                    _ => throw new NotSupportedException($"Transport type {transportType} is not supported")
                };

                // Configure server with tools
                var options = CreateServerOptions(config);
                _currentServer = McpServerFactory.Create(transport, options);
                _currentTransportType = transportType;

                // Start server
                _serverCts = new CancellationTokenSource();
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _currentServer.RunAsync(_serverCts.Token);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "MCP server error");
                        OnStatusChanged(false, $"Server error: {ex.Message}");
                    }
                }, _serverCts.Token);

                OnStatusChanged(true, $"Server started on {transportType}");
                return _currentServer;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start MCP server");
                throw;
            }
        }

        public async Task StopServerAsync()
        {
            if (_serverCts != null)
            {
                _logger.LogInformation("Stopping MCP server");
                _serverCts.Cancel();
                _serverCts.Dispose();
                _serverCts = null;
            }

            if (_currentServer != null)
            {
                await _currentServer.DisposeAsync();
                _currentServer = null;
            }

            _connectedClients.Clear();
            OnStatusChanged(false, "Server stopped");
        }

        public IReadOnlyList<string> GetConnectedClients()
        {
            return _connectedClients.AsReadOnly();
        }

        private McpServerOptions CreateServerOptions(McpServerConfig config)
        {
            var tools = _serviceProvider.GetServices<ITool>()
                .Where(t => !config.ExcludedToolGuids.Contains(t.GetToolDefinition().Guid.ToString()))
                .ToList();

            _logger.LogInformation($"Loaded {tools.Count} tools for MCP server");

            var adapter = new IToolToMcpAdapter(tools, _extraPropertiesService, _serviceProvider);

            return new McpServerOptions
            {
                ServerInfo = new Implementation
                {
                    Name = "AiStudio4-MCP-Server",
                    Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0"
                },
                Capabilities = new ServerCapabilities
                {
                    Tools = new ToolsCapability
                    {
                        ListToolsHandler = (request, ct) =>
                        {
                            _logger.LogDebug("Listing tools for MCP client");
                            return ValueTask.FromResult(new ListToolsResult
                            {
                                Tools = tools.Select(adapter.ConvertToMcpTool).ToList()
                            });
                        },

                        CallToolHandler = async (request, ct) =>
                        {
                            if (request.Params?.Name == null)
                                throw new McpException("Tool name is required");

                            _logger.LogInformation($"Executing tool: {request.Params.Name}");

                            var arguments = request.Params.Arguments?
                                .ToDictionary(kvp => kvp.Key, kvp => (object?)kvp.Value);

                            return await adapter.ExecuteTool(
                                request.Params.Name,
                                arguments ?? new(),
                                ct);
                        }
                    }
                }
            };
        }

        private void OnStatusChanged(bool isRunning, string message)
        {
            StatusChanged?.Invoke(this, new McpServerStatusChangedEventArgs
            {
                IsRunning = isRunning,
                Message = message,
                TransportType = _currentTransportType
            });
        }
    }
}
```

### 4. SSE Transport Implementation

**File**: `AiStudio4/Services/Mcp/SseServerTransport.cs`

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace AiStudio4.Services.Mcp
{
    public class SseServerTransport : IMcpServerTransport
    {
        private readonly int _port;
        private HttpListener? _listener;
        private readonly List<SseClient> _clients = new();
        private readonly object _clientsLock = new();

        public SseServerTransport(int port)
        {
            _port = port;
        }

        public async Task RunAsync(IMcpServer server, CancellationToken cancellationToken)
        {
            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://localhost:{_port}/");
            _listener.Start();

            var listenerTask = Task.Run(async () =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        var context = await _listener.GetContextAsync();
                        _ = Task.Run(() => HandleRequest(context, server, cancellationToken), cancellationToken);
                    }
                    catch (HttpListenerException) when (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }
                }
            }, cancellationToken);

            await listenerTask;
            _listener.Stop();
            _listener.Close();
        }

        private async Task HandleRequest(HttpListenerContext context, IMcpServer server, CancellationToken cancellationToken)
        {
            try
            {
                if (context.Request.Url?.AbsolutePath == "/events")
                {
                    await HandleSseClient(context, server, cancellationToken);
                }
                else if (context.Request.Url?.AbsolutePath == "/rpc")
                {
                    await HandleRpcRequest(context, server);
                }
                else
                {
                    context.Response.StatusCode = 404;
                    context.Response.Close();
                }
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = 500;
                var error = Encoding.UTF8.GetBytes(ex.Message);
                await context.Response.OutputStream.WriteAsync(error, 0, error.Length);
                context.Response.Close();
            }
        }

        private async Task HandleSseClient(HttpListenerContext context, IMcpServer server, CancellationToken cancellationToken)
        {
            context.Response.ContentType = "text/event-stream";
            context.Response.Headers.Add("Cache-Control", "no-cache");
            context.Response.Headers.Add("Connection", "keep-alive");

            var client = new SseClient(context.Response);
            lock (_clientsLock)
            {
                _clients.Add(client);
            }

            try
            {
                // Send initial connection event
                await client.SendEventAsync("connected", JsonSerializer.Serialize(new { status = "connected" }));

                // Keep connection alive
                while (!cancellationToken.IsCancellationRequested && client.IsConnected)
                {
                    await Task.Delay(30000, cancellationToken); // 30 second heartbeat
                    await client.SendEventAsync("ping", "{}");
                }
            }
            finally
            {
                lock (_clientsLock)
                {
                    _clients.Remove(client);
                }
                client.Dispose();
            }
        }

        private async Task HandleRpcRequest(HttpListenerContext context, IMcpServer server)
        {
            using var reader = new StreamReader(context.Request.InputStream);
            var requestBody = await reader.ReadToEndAsync();
            var request = JsonSerializer.Deserialize<JsonRpcRequest>(requestBody);

            if (request == null)
            {
                context.Response.StatusCode = 400;
                context.Response.Close();
                return;
            }

            // Process the request through the MCP server
            var response = await server.ProcessRequestAsync(request);

            // Send response
            context.Response.ContentType = "application/json";
            var responseBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(response));
            await context.Response.OutputStream.WriteAsync(responseBytes, 0, responseBytes.Length);
            context.Response.Close();
        }

        private class SseClient : IDisposable
        {
            private readonly HttpListenerResponse _response;
            private readonly StreamWriter _writer;

            public bool IsConnected { get; private set; } = true;

            public SseClient(HttpListenerResponse response)
            {
                _response = response;
                _writer = new StreamWriter(response.OutputStream, Encoding.UTF8);
            }

            public async Task SendEventAsync(string eventType, string data)
            {
                try
                {
                    await _writer.WriteLineAsync($"event: {eventType}");
                    await _writer.WriteLineAsync($"data: {data}");
                    await _writer.WriteLineAsync();
                    await _writer.FlushAsync();
                }
                catch
                {
                    IsConnected = false;
                    throw;
                }
            }

            public void Dispose()
            {
                IsConnected = false;
                _writer?.Dispose();
                _response?.Close();
            }
        }
    }
}
```

### 5. WPF Management Window

**File**: `AiStudio4/Windows/McpServerWindow.xaml`

```xml
<Window x:Class="AiStudio4.Windows.McpServerWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
        mc:Ignorable="d"
        Title="MCP Server Management" Height="600" Width="800"
        Background="{DynamicResource BackgroundBrush}"
        Foreground="{DynamicResource PrimaryTextBrush}">
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>
        
        <!-- Control Panel -->
        <Border Grid.Row="0" Background="{DynamicResource SecondaryBackgroundBrush}" 
                BorderBrush="{DynamicResource BorderBrush}" BorderThickness="0,0,0,1">
            <StackPanel Orientation="Horizontal" Margin="10">
                <Label Content="Transport Type:" VerticalAlignment="Center"/>
                <ComboBox x:Name="TransportTypeCombo" Width="100" VerticalAlignment="Center"
                          SelectionChanged="TransportTypeCombo_SelectionChanged">
                    <ComboBoxItem>Stdio</ComboBoxItem>
                    <ComboBoxItem>SSE</ComboBoxItem>
                </ComboBox>
                
                <!-- SSE Options -->
                <StackPanel x:Name="SseOptions" Orientation="Horizontal" 
                            Visibility="Collapsed" Margin="10,0,0,0">
                    <Label Content="Port:" VerticalAlignment="Center"/>
                    <TextBox x:Name="PortTextBox" Width="60" Text="3000" 
                             VerticalAlignment="Center" Margin="5,0"/>
                </StackPanel>
                
                <Button x:Name="StartStopButton" Content="Start Server" 
                        Click="StartStopButton_Click" Margin="10,0"
                        Padding="10,5" VerticalAlignment="Center"/>
                
                <TextBlock x:Name="StatusText" VerticalAlignment="Center" 
                           Foreground="{DynamicResource SecondaryTextBrush}"/>
            </StackPanel>
        </Border>
        
        <!-- Main Content -->
        <TabControl Grid.Row="1" Background="Transparent">
            <TabItem Header="Server Status">
                <Grid>
                    <Grid.RowDefinitions>
                        <RowDefinition Height="Auto"/>
                        <RowDefinition Height="*"/>
                    </Grid.RowDefinitions>
                    
                    <GroupBox Header="Server Information" Grid.Row="0" Margin="5">
                        <StackPanel Margin="10">
                            <TextBlock x:Name="ServerInfoText" Margin="0,5"/>
                            <TextBlock x:Name="ConnectionInfoText" Margin="0,5"/>
                            <Border Background="{DynamicResource CodeBlockBackgroundBrush}" 
                                    BorderBrush="{DynamicResource BorderBrush}" 
                                    BorderThickness="1" CornerRadius="4" 
                                    Padding="10" Margin="0,10">
                                <StackPanel>
                                    <TextBlock Text="Connection Instructions:" 
                                               FontWeight="Bold" Margin="0,0,0,5"/>
                                    <TextBlock x:Name="ConnectionInstructionsText" 
                                               FontFamily="Consolas" TextWrapping="Wrap"/>
                                </StackPanel>
                            </Border>
                        </StackPanel>
                    </GroupBox>
                    
                    <GroupBox Header="Activity Log" Grid.Row="1" Margin="5">
                        <ListBox x:Name="ActivityLog" 
                                 ScrollViewer.HorizontalScrollBarVisibility="Disabled"
                                 Background="{DynamicResource CodeBlockBackgroundBrush}">
                            <ListBox.ItemTemplate>
                                <DataTemplate>
                                    <Border BorderBrush="{DynamicResource BorderBrush}" 
                                            BorderThickness="0,0,0,1" Padding="5">
                                        <StackPanel>
                                            <TextBlock Text="{Binding Timestamp}" 
                                                       Foreground="{DynamicResource SecondaryTextBrush}" 
                                                       FontSize="10"/>
                                            <TextBlock Text="{Binding Message}" TextWrapping="Wrap"/>
                                        </StackPanel>
                                    </Border>
                                </DataTemplate>
                            </ListBox.ItemTemplate>
                        </ListBox>
                    </GroupBox>
                </Grid>
            </TabItem>
            
            <TabItem Header="Available Tools">
                <Grid>
                    <Grid.RowDefinitions>
                        <RowDefinition Height="Auto"/>
                        <RowDefinition Height="*"/>
                    </Grid.RowDefinitions>
                    
                    <StackPanel Grid.Row="0" Margin="5">
                        <TextBlock Text="{Binding ElementName=ToolsGrid, Path=Items.Count, 
                                          StringFormat='Total Tools: {0}'}" 
                                   Margin="5"/>
                        <CheckBox x:Name="ShowDisabledToolsCheckBox" 
                                  Content="Show disabled tools" 
                                  Margin="5" 
                                  Checked="ShowDisabledToolsCheckBox_Changed"
                                  Unchecked="ShowDisabledToolsCheckBox_Changed"/>
                    </StackPanel>
                    
                    <DataGrid x:Name="ToolsGrid" Grid.Row="1" AutoGenerateColumns="False" 
                              CanUserAddRows="False" Margin="5">
                        <DataGrid.Columns>
                            <DataGridCheckBoxColumn Header="Enabled" 
                                                    Binding="{Binding IsEnabled}"
                                                    Width="60"/>
                            <DataGridTextColumn Header="Name" 
                                                Binding="{Binding Name}" 
                                                Width="200" IsReadOnly="True"/>
                            <DataGridTextColumn Header="Category" 
                                                Binding="{Binding Category}" 
                                                Width="150" IsReadOnly="True"/>
                            <DataGridTextColumn Header="Description" 
                                                Binding="{Binding Description}" 
                                                Width="*" IsReadOnly="True"/>
                        </DataGrid.Columns>
                    </DataGrid>
                </Grid>
            </TabItem>
            
            <TabItem Header="Connected Clients">
                <Grid>
                    <Grid.RowDefinitions>
                        <RowDefinition Height="Auto"/>
                        <RowDefinition Height="*"/>
                    </Grid.RowDefinitions>
                    
                    <TextBlock Grid.Row="0" Margin="10" 
                               Text="{Binding ElementName=ClientsList, Path=Items.Count, 
                                      StringFormat='Connected Clients: {0}'}"/>
                    
                    <ListBox x:Name="ClientsList" Grid.Row="1" Margin="5">
                        <ListBox.ItemTemplate>
                            <DataTemplate>
                                <Border BorderBrush="{DynamicResource BorderBrush}" 
                                        BorderThickness="1" CornerRadius="4" 
                                        Padding="10" Margin="5">
                                    <StackPanel>
                                        <TextBlock Text="{Binding ClientId}" FontWeight="Bold"/>
                                        <TextBlock Text="{Binding ConnectedAt, 
                                                          StringFormat='Connected: {0}'}" 
                                                   Foreground="{DynamicResource SecondaryTextBrush}"/>
                                        <TextBlock Text="{Binding LastActivity, 
                                                          StringFormat='Last Activity: {0}'}" 
                                                   Foreground="{DynamicResource SecondaryTextBrush}"/>
                                    </StackPanel>
                                </Border>
                            </DataTemplate>
                        </ListBox.ItemTemplate>
                    </ListBox>
                </Grid>
            </TabItem>
            
            <TabItem Header="Configuration">
                <ScrollViewer VerticalScrollBarVisibility="Auto">
                    <StackPanel Margin="10">
                        <GroupBox Header="General Settings" Margin="0,5">
                            <StackPanel Margin="10">
                                <CheckBox x:Name="EnableLoggingCheckBox" 
                                          Content="Enable detailed logging" Margin="0,5"/>
                                <CheckBox x:Name="AutoStartCheckBox" 
                                          Content="Auto-start server on application launch" Margin="0,5"/>
                            </StackPanel>
                        </GroupBox>
                        
                        <GroupBox Header="Stdio Configuration" Margin="0,10">
                            <StackPanel Margin="10">
                                <TextBlock Text="When using stdio transport, clients connect via command line" 
                                           TextWrapping="Wrap" Margin="0,5"/>
                                <TextBlock Text="Example:" FontWeight="Bold" Margin="0,5"/>
                                <Border Background="{DynamicResource CodeBlockBackgroundBrush}" 
                                        BorderBrush="{DynamicResource BorderBrush}" 
                                        BorderThickness="1" CornerRadius="4" Padding="10">
                                    <TextBlock FontFamily="Consolas" 
                                               Text="npx @modelcontextprotocol/inspector aistudio4-mcp"/>
                                </Border>
                            </StackPanel>
                        </GroupBox>
                        
                        <GroupBox Header="SSE Configuration" Margin="0,10">
                            <StackPanel Margin="10">
                                <Grid>
                                    <Grid.ColumnDefinitions>
                                        <ColumnDefinition Width="Auto"/>
                                        <ColumnDefinition Width="*"/>
                                    </Grid.ColumnDefinitions>
                                    <Grid.RowDefinitions>
                                        <RowDefinition/>
                                        <RowDefinition/>
                                    </Grid.RowDefinitions>
                                    
                                    <Label Grid.Row="0" Grid.Column="0" Content="Base URL:"/>
                                    <TextBox Grid.Row="0" Grid.Column="1" x:Name="BaseUrlTextBox" 
                                             IsReadOnly="True" Margin="5"/>
                                    
                                    <Label Grid.Row="1" Grid.Column="0" Content="Default Port:"/>
                                    <TextBox Grid.Row="1" Grid.Column="1" x:Name="DefaultPortTextBox" 
                                             Text="3000" Margin="5"/>
                                </Grid>
                                
                                <TextBlock Text="SSE Endpoints:" FontWeight="Bold" Margin="0,10,0,5"/>
                                <Border Background="{DynamicResource CodeBlockBackgroundBrush}" 
                                        BorderBrush="{DynamicResource BorderBrush}" 
                                        BorderThickness="1" CornerRadius="4" Padding="10">
                                    <TextBlock FontFamily="Consolas">
                                        <Run Text="/events - Server-Sent Events stream"/>
                                        <LineBreak/>
                                        <Run Text="/rpc - JSON-RPC endpoint"/>
                                    </TextBlock>
                                </Border>
                            </StackPanel>
                        </GroupBox>
                        
                        <Button Content="Save Configuration" Click="SaveConfiguration_Click" 
                                HorizontalAlignment="Right" Margin="0,10" Padding="10,5"/>
                    </StackPanel>
                </ScrollViewer>
            </TabItem>
        </TabControl>
        
        <!-- Status Bar -->
        <StatusBar Grid.Row="2" Background="{DynamicResource SecondaryBackgroundBrush}">
            <StatusBarItem>
                <TextBlock x:Name="StatusBarText" Text="Ready"/>
            </StatusBarItem>
            <Separator/>
            <StatusBarItem>
                <TextBlock x:Name="ServerVersionText"/>
            </StatusBarItem>
        </StatusBar>
    </Grid>
</Window>
```

**File**: `AiStudio4/Windows/McpServerWindow.xaml.cs`

```csharp
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
                    IsEnabled = true // Load from settings
                });
            }

            UpdateUI();
        }

        private void LoadSettings()
        {
            var settings = _settingsService.GetMcpServerSettings();
            
            TransportTypeCombo.SelectedIndex = settings.DefaultTransportType == McpServerTransportType.Stdio ? 0 : 1;
            PortTextBox.Text = settings.SsePort.ToString();
            EnableLoggingCheckBox.IsChecked = settings.EnableLogging;
            AutoStartCheckBox.IsChecked = settings.AutoStart;
            DefaultPortTextBox.Text = settings.SsePort.ToString();

            // Load disabled tools
            foreach (var toolVm in _toolViewModels)
            {
                toolVm.IsEnabled = !settings.DisabledToolGuids.Contains(toolVm.Guid.ToString());
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
                    var transportType = TransportTypeCombo.SelectedIndex == 0 
                        ? McpServerTransportType.Stdio 
                        : McpServerTransportType.Sse;

                    var config = new McpServerConfig
                    {
                        HttpPort = transportType == McpServerTransportType.Sse 
                            ? int.Parse(PortTextBox.Text) 
                            : null,
                        EnableLogging = EnableLoggingCheckBox.IsChecked ?? false,
                        ExcludedToolGuids = _toolViewModels
                            .Where(t => !t.IsEnabled)
                            .Select(t => t.Guid.ToString())
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
                // Update client list...
            }
        }

        private void SaveConfiguration_Click(object sender, RoutedEventArgs e)
        {
            var settings = _settingsService.GetMcpServerSettings();
            
            settings.DefaultTransportType = TransportTypeCombo.SelectedIndex == 0 
                ? McpServerTransportType.Stdio 
                : McpServerTransportType.Sse;
            settings.SsePort = int.Parse(DefaultPortTextBox.Text);
            settings.EnableLogging = EnableLoggingCheckBox.IsChecked ?? false;
            settings.AutoStart = AutoStartCheckBox.IsChecked ?? false;
            settings.DisabledToolGuids = _toolViewModels
                .Where(t => !t.IsEnabled)
                .Select(t => t.Guid.ToString())
                .ToList();

            _settingsService.SaveMcpServerSettings(settings);
            
            MessageBox.Show("Configuration saved successfully", "Success", 
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ShowDisabledToolsCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            // Filter tools based on checkbox
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

            public Guid Guid { get; set; }
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
```

### 6. Menu Integration

**File**: Update to `AiStudio4/MainWindow.xaml`

Add to the existing menu structure:

```xml
<MenuItem Header="Tools">
    <!-- Existing items... -->
    <Separator/>
    <MenuItem Header="MCP Server" Click="McpServer_Click">
        <MenuItem.Icon>
            <Path Data="M12,2A10,10 0 0,0 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2M12,4A8,8 0 0,1 20,12A8,8 0 0,1 12,20A8,8 0 0,1 4,12A8,8 0 0,1 12,4M12,6A6,6 0 0,0 6,12A6,6 0 0,0 12,18A6,6 0 0,0 18,12A6,6 0 0,0 12,6M12,8A4,4 0 0,1 16,12A4,4 0 0,1 12,16A4,4 0 0,1 8,12A4,4 0 0,1 12,8M12,10A2,2 0 0,0 10,12A2,2 0 0,0 12,14A2,2 0 0,0 14,12A2,2 0 0,0 12,10Z" 
                  Fill="{DynamicResource PrimaryTextBrush}" Width="16" Height="16"/>
        </MenuItem.Icon>
    </MenuItem>
</MenuItem>
```

**File**: Update to `AiStudio4/MainWindow.xaml.cs`

Add the click handler:

```csharp
private void McpServer_Click(object sender, RoutedEventArgs e)
{
    var mcpWindow = _serviceProvider.GetRequiredService<McpServerWindow>();
    mcpWindow.Owner = this;
    mcpWindow.ShowDialog();
}
```

### 7. Settings Integration

**File**: Update to `AiStudio4/InjectedDependencies/GeneralSettings.cs`

Add the MCP server settings:

```csharp
public class McpServerSettings
{
    public McpServerTransportType DefaultTransportType { get; set; } = McpServerTransportType.Stdio;
    public int SsePort { get; set; } = 3000;
    public bool EnableLogging { get; set; } = true;
    public bool AutoStart { get; set; } = false;
    public List<string> DisabledToolGuids { get; set; } = new();
}

// Add to GeneralSettings class:
public McpServerSettings McpServer { get; set; } = new();
```

### 8. Dependency Injection Setup

**File**: Update to `AiStudio4/Extensions/ServiceCollectionExtensions.cs`

Add the following registrations:

```csharp
// MCP Server Services
services.AddSingleton<IMcpServerService, AiStudioMcpServerService>();
services.AddTransient<McpServerWindow>();
services.AddTransient<IToolToMcpAdapter>();
```

### 9. Auto-start Implementation

**File**: Update to `AiStudio4/InjectedDependencies/StartupService.cs`

Add auto-start logic:

```csharp
public async Task InitializeAsync()
{
    // Existing initialization...

    // Auto-start MCP server if configured
    var settings = _settingsService.GetMcpServerSettings();
    if (settings.AutoStart)
    {
        try
        {
            var mcpService = _serviceProvider.GetRequiredService<IMcpServerService>();
            await mcpService.StartServerAsync(settings.DefaultTransportType, new McpServerConfig
            {
                HttpPort = settings.SsePort,
                EnableLogging = settings.EnableLogging,
                ExcludedToolGuids = settings.DisabledToolGuids
            });
            
            _logger.LogInformation("MCP server auto-started successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to auto-start MCP server");
        }
    }
}
```

## Usage Examples

### Stdio Transport

```bash
# Using MCP Inspector
npx @modelcontextprotocol/inspector aistudio4-mcp

# Using custom client
echo '{"jsonrpc":"2.0","method":"tools/list","id":1}' | aistudio4-mcp
```

### SSE Transport

```bash
# List available tools
curl http://localhost:3000/rpc -X POST -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","method":"tools/list","id":1}'

# Call a tool
curl http://localhost:3000/rpc -X POST -H "Content-Type: application/json" \
  -d '{
    "jsonrpc":"2.0",
    "method":"tools/call",
    "params":{
      "name":"ReadFilesTool",
      "arguments":{"files":["README.md"]}
    },
    "id":2
  }'

# Connect to SSE stream
curl http://localhost:3000/events -H "Accept: text/event-stream"
```

## Testing Strategy

1. **Unit Tests**
   - Test ITool to MCP adapter conversions
   - Test server configuration options
   - Test transport implementations

2. **Integration Tests**
   - Test actual tool execution through MCP
   - Test client connections and disconnections
   - Test error handling and recovery

3. **Manual Testing**
   - Use MCP Inspector for stdio testing
   - Use curl/Postman for SSE testing
   - Test with real MCP clients

## Security Considerations

1. **Transport Security**
   - SSE server binds only to localhost by default
   - No authentication implemented (relies on local-only access)
   - Consider adding API key authentication for production use

2. **Tool Security**
   - Existing tool security (PathSecurityManager) is maintained
   - Extra properties service handles sensitive data
   - Tool execution permissions follow existing patterns

3. **Client Isolation**
   - Each client connection is isolated
   - No cross-client communication
   - Resource limits should be considered for production

## Future Enhancements

1. **Additional Transports**
   - WebSocket support
   - Named pipes for Windows
   - Unix domain sockets for Linux/Mac

2. **Advanced Features**
   - Tool usage analytics
   - Client authentication/authorization
   - Rate limiting and quotas
   - Tool result caching

3. **Integration Improvements**
   - Export/import tool configurations
   - Tool testing interface in management window
   - Real-time tool execution monitoring

## Conclusion

This design provides a comprehensive MCP server implementation that:
- Leverages all existing ITool implementations without modification
- Supports multiple transport types (stdio and SSE)
- Includes a full-featured management interface
- Integrates seamlessly with the existing application
- Maintains security and stability
- Provides extensibility for future enhancements

The implementation requires minimal changes to existing code while adding significant new functionality that allows AiStudio4 to act as an MCP server for any compatible client.