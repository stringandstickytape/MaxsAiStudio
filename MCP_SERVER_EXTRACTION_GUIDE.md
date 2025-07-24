# MCP Server Extraction Implementation Guide

## Overview
This guide provides step-by-step instructions for extracting MCP server functionality from AiStudio4 into a standalone WPF application while maximizing code sharing between both applications.

## Project Structure After Implementation

```
MaxsAiStudio/
├── AiStudio4.Core/              # Shared library project
│   ├── Interfaces/
│   ├── Models/
│   ├── Services/
│   ├── Tools/
│   ├── Exceptions/
│   └── Configuration/
├── AiStudio4/                   # Main WPF application
├── AiStudio4.McpManager/        # Standalone MCP Manager WPF app
│   ├── Views/
│   ├── ViewModels/
│   ├── Services/
│   └── App.xaml
└── AiStudio4.sln                # Updated solution file
```

## Phase 1: Create Shared Core Library

### Step 1.1: Create AiStudio4.Core Project
- [ ] Create new Class Library project: `AiStudio4.Core`
- [ ] Target framework: `net9.0-windows`
- [ ] Add to solution file

### Step 1.2: Add NuGet Dependencies to Core
- [ ] Add these packages to AiStudio4.Core.csproj:
```xml
<ItemGroup>
  <PackageReference Include="ModelContextProtocol.Client" Version="*" />
  <PackageReference Include="ModelContextProtocol.Protocol" Version="*" />
  <PackageReference Include="Newtonsoft.Json" Version="*" />
  <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="*" />
  <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="*" />
</ItemGroup>
```

### Step 1.3: Move Core Interfaces
- [ ] Move these files from `AiStudio4/Core/Interfaces/` to `AiStudio4.Core/Interfaces/`:
  - [ ] `IMcpService.cs`
  - [ ] `ITool.cs`
  - [ ] `IToolExecutor.cs`
  - [ ] `IToolService.cs`
  - [ ] `IToolProcessorService.cs`
  - [ ] `IBuiltinToolService.cs`
  - [ ] `IBuiltInToolExtraPropertiesService.cs`
- [ ] Create new `IConfigurationService.cs`

### Step 1.4: Move Core Models
- [ ] Move these files from `AiStudio4/Core/Models/` to `AiStudio4.Core/Models/`:
  - [ ] `McpServerDefinition.cs`
  - [ ] `ToolModels.cs`
  - [ ] `ToolResponse.cs`
  - [ ] `ToolResponseItem.cs`
  - [ ] `ContentBlock.cs`
  - [ ] `ContentType.cs`
  - [ ] `BuiltinToolResult.cs`

### Step 1.5: Move Core Exceptions
- [ ] Move these files from `AiStudio4/Core/Exceptions/` to `AiStudio4.Core/Exceptions/`:
  - [ ] `McpCommunicationException.cs`

### Step 1.6: Move Shared Helpers
- [ ] Create `AiStudio4.Core/Helpers/PathHelper.cs` (extract path-related methods)

## Phase 2: Extract MCP Services

### Step 2.1: Move McpService
- [ ] Move `AiStudio4/Services/McpService.cs` to `AiStudio4.Core/Services/`
- [ ] Update namespace to `AiStudio4.Core.Services`
- [ ] Update all imports

### Step 2.2: Extract Tool Base Classes
- [ ] Move these files to `AiStudio4.Core/Tools/`:
  - [ ] `BaseToolImplementation.cs`
  - [ ] `ToolRequestBuilder.cs`
  - [ ] `ToolGuids.cs`

### Step 2.3: Identify Standalone-Compatible Tools
- [ ] Review each tool in `AiStudio4/Core/Tools/` and categorize:

**Can Move to Core** (no UI dependencies):
- [ ] File system tools (ReadFilesTool, CreateNewFileTool, etc.)
- [ ] Git tools (GitStatusTool, GitCommitTool, etc.)
- [ ] Search tools (FileSearchTool, FileRegExSearchTool)
- [ ] URL tools (RetrieveTextFromUrlTool)
- [ ] Most Azure DevOps tools
- [ ] Most GitHub tools

**Must Stay in AiStudio4** (UI/WPF dependencies):
- [ ] ThinkAndAwaitUserInputTool
- [ ] PresentResultsAndAwaitUserInputTool
- [ ] WindowsSandboxTool
- [ ] Any tools that show dialogs or interact with UI

### Step 2.4: Move Standalone Tools
- [ ] For each standalone-compatible tool:
  - [ ] Move to `AiStudio4.Core/Tools/[Category]/`
  - [ ] Update namespace
  - [ ] Remove any UI-specific code
  - [ ] Add logging instead of UI notifications

## Phase 3: Create Service Registration Extensions

### Step 3.1: Create Core Service Extensions
- [ ] Create `AiStudio4.Core/Extensions/ServiceCollectionExtensions.cs`:

```csharp
using Microsoft.Extensions.DependencyInjection;
using AiStudio4.Core.Interfaces;
using AiStudio4.Core.Services;

namespace AiStudio4.Core.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddMcpCore(this IServiceCollection services)
        {
            services.AddSingleton<IMcpService, McpService>();
            return services;
        }

        public static IServiceCollection AddCoreTools(this IServiceCollection services)
        {
            // Register all core tools
            services.AddTransient<ITool, ReadFilesTool>();
            services.AddTransient<ITool, CreateNewFileTool>();
            // ... add all other core tools
            
            return services;
        }
    }
}
```

## Phase 4: Update AiStudio4 Project

### Step 4.1: Update Target Framework
- [ ] Update AiStudio4.csproj target framework to `net9.0-windows`

### Step 4.2: Add Project Reference
- [ ] In AiStudio4.csproj:
```xml
<ItemGroup>
  <ProjectReference Include="..\AiStudio4.Core\AiStudio4.Core.csproj" />
</ItemGroup>
```

### Step 4.3: Update Namespaces
- [ ] Update all files that reference moved classes:
  - [ ] Change `using AiStudio4.Core.Interfaces` to `using AiStudio4.Core.Interfaces`
  - [ ] Update all other namespace references

### Step 4.4: Update Service Registration
- [ ] In `AiStudio4/Core/DependencyInjection.cs`:
```csharp
services.AddMcpCore();
services.AddCoreTools();
// Add UI-specific tools
services.AddTransient<ITool, ThinkAndAwaitUserInputTool>();
// ... other UI tools
```

## Phase 5: Create Standalone MCP Manager WPF Application

### Step 5.1: Create WPF Application
- [ ] Create new WPF Application: `AiStudio4.McpManager`
- [ ] Target framework: `net9.0-windows`
- [ ] Add to solution

### Step 5.2: Add Dependencies
- [ ] In AiStudio4.McpManager.csproj:
```xml
<ItemGroup>
  <ProjectReference Include="..\AiStudio4.Core\AiStudio4.Core.csproj" />
  <PackageReference Include="Microsoft.Extensions.Hosting" Version="*" />
  <PackageReference Include="Microsoft.Extensions.Configuration.Json" Version="*" />
  <PackageReference Include="CommunityToolkit.Mvvm" Version="*" />
  <PackageReference Include="ModernWpfUI" Version="*" />
  <PackageReference Include="Serilog.Extensions.Hosting" Version="*" />
  <PackageReference Include="Serilog.Sinks.Console" Version="*" />
  <PackageReference Include="Serilog.Sinks.File" Version="*" />
</ItemGroup>
```

### Step 5.3: Create App.xaml
- [ ] Create `AiStudio4.McpManager/App.xaml`:

```xml
<Application x:Class="AiStudio4.McpManager.App"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:ui="http://schemas.modernwpf.com/2019">
    <Application.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <ui:ThemeResources />
                <ui:XamlControlsResources />
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </Application.Resources>
</Application>
```

### Step 5.4: Create App.xaml.cs
- [ ] Create `AiStudio4.McpManager/App.xaml.cs`:

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System.Windows;
using AiStudio4.Core.Extensions;
using AiStudio4.McpManager.Views;
using AiStudio4.McpManager.ViewModels;
using AiStudio4.McpManager.Services;

namespace AiStudio4.McpManager
{
    public partial class App : Application
    {
        private readonly IHost _host;

        public App()
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.Console()
                .WriteTo.File("logs/mcp-manager-.txt", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            _host = Host.CreateDefaultBuilder()
                .UseSerilog()
                .ConfigureServices((context, services) =>
                {
                    // Add configuration service
                    services.AddSingleton<IConfigurationService, WpfConfigurationService>();
                    
                    // Add core MCP services
                    services.AddMcpCore();
                    services.AddCoreTools();
                    
                    // Add WPF services
                    services.AddSingleton<MainWindow>();
                    services.AddSingleton<MainViewModel>();
                    services.AddTransient<ServerConfigViewModel>();
                    services.AddTransient<ToolConfigViewModel>();
                    
                    // Add application services
                    services.AddSingleton<IDialogService, WpfDialogService>();
                    services.AddSingleton<INavigationService, NavigationService>();
                })
                .Build();
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            await _host.StartAsync();
            
            var mainWindow = _host.Services.GetRequiredService<MainWindow>();
            mainWindow.Show();
            
            base.OnStartup(e);
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            using (_host)
            {
                await _host.StopAsync();
            }
            
            Log.CloseAndFlush();
            base.OnExit(e);
        }
    }
}
```

### Step 5.5: Create Main Window XAML
- [ ] Create `AiStudio4.McpManager/Views/MainWindow.xaml`:

```xml
<mah:MetroWindow x:Class="AiStudio4.McpManager.Views.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:mah="http://metro.mahapps.com/winfx/xaml/controls"
        Title="AiStudio4 MCP Manager" Height="600" Width="900">
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
        </Grid.RowDefinitions>
        
        <!-- Toolbar -->
        <ToolBar Grid.Row="0">
            <Button Command="{Binding AddServerCommand}" Content="Add Server" />
            <Button Command="{Binding RefreshCommand}" Content="Refresh" />
            <Separator />
            <Button Command="{Binding ConfigureToolsCommand}" Content="Configure Tools" />
        </ToolBar>
        
        <!-- Main Content -->
        <Grid Grid.Row="1">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="300"/>
                <ColumnDefinition Width="*"/>
            </Grid.ColumnDefinitions>
            
            <!-- Server List -->
            <GroupBox Grid.Column="0" Header="MCP Servers" Margin="5">
                <ListBox ItemsSource="{Binding Servers}" 
                         SelectedItem="{Binding SelectedServer}">
                    <ListBox.ItemTemplate>
                        <DataTemplate>
                            <Border BorderBrush="LightGray" BorderThickness="0,0,0,1" Padding="5">
                                <StackPanel>
                                    <TextBlock Text="{Binding Name}" FontWeight="Bold"/>
                                    <TextBlock Text="{Binding Command}" Foreground="Gray" FontSize="11"/>
                                    <StackPanel Orientation="Horizontal">
                                        <TextBlock Text="Status: " FontSize="11"/>
                                        <TextBlock Text="{Binding Status}" 
                                                   Foreground="{Binding StatusColor}" 
                                                   FontSize="11"/>
                                    </StackPanel>
                                </StackPanel>
                            </Border>
                        </DataTemplate>
                    </ListBox.ItemTemplate>
                </ListBox>
            </GroupBox>
            
            <!-- Server Details -->
            <GroupBox Grid.Column="1" Header="Server Details" Margin="5">
                <ScrollViewer>
                    <ContentControl Content="{Binding SelectedServerView}"/>
                </ScrollViewer>
            </GroupBox>
        </Grid>
    </Grid>
</mah:MetroWindow>
```

### Step 5.6: Create ViewModels
- [ ] Create `AiStudio4.McpManager/ViewModels/MainViewModel.cs`
- [ ] Create `AiStudio4.McpManager/ViewModels/ServerViewModel.cs`
- [ ] Create `AiStudio4.McpManager/ViewModels/ToolConfigViewModel.cs`
- [ ] Create `AiStudio4.McpManager/ViewModels/ToolPropertyViewModel.cs`

### Step 5.7: Create Tool Configuration Dialog
- [ ] Create `AiStudio4.McpManager/Views/ToolConfigurationDialog.xaml`
- [ ] Create corresponding view model and code-behind

### Step 5.8: Create Tool Extra Properties Dialog  
- [ ] Create `AiStudio4.McpManager/Views/ToolExtraPropertiesDialog.xaml`
- [ ] Create corresponding view model and code-behind

## Phase 6: Configuration and Settings Management

### Step 6.1: Create Configuration Abstraction
- [ ] Create `AiStudio4.Core/Interfaces/IConfigurationService.cs`

### Step 6.2: Move BuiltInToolExtraPropertiesService to Core
- [ ] Move `AiStudio4/InjectedDependencies/BuiltInToolExtraPropertiesService.cs` to `AiStudio4.Core/Services/`
- [ ] Update to use IConfigurationService for paths

### Step 6.3: Create WPF Configuration Service
- [ ] Create `AiStudio4.McpManager/Services/WpfConfigurationService.cs`

### Step 6.4: Create Dialog Service
- [ ] Create `AiStudio4.McpManager/Services/WpfDialogService.cs`
- [ ] Implement IDialogService interface

### Step 6.5: Create Server Details View
- [ ] Create `AiStudio4.McpManager/Views/ServerDetailsView.xaml`
- [ ] Create corresponding view model

### Step 6.6: Create Additional Dialogs
- [ ] Create server configuration dialog
- [ ] Create input dialog for simple text input
- [ ] Create confirmation dialogs

### Step 6.7: Update Main Window Code-Behind
- [ ] Create `AiStudio4.McpManager/Views/MainWindow.xaml.cs`

### Step 6.8: Share Configuration with Main App
- [ ] Ensure both apps use the same configuration directory
- [ ] Verify both apps read/write to the same files:
  - [ ] `builtinToolExtraProps.json`
  - [ ] `mcpServers.json`
  - [ ] Configuration path: `%APPDATA%/AiStudio4/Config/`

## Phase 7: Testing and Validation

### Step 7.1: Test Shared Configuration
- [ ] Ensure both apps read/write to the same mcpServers.json file
- [ ] Test that changes in one app are reflected in the other
- [ ] Verify tool extra properties are shared

### Step 7.2: Test Tool Functionality
- [ ] Verify all moved tools work in both applications
- [ ] Test MCP server connections from both apps
- [ ] Validate tool extra properties configuration

### Step 7.3: Test Standalone Manager
- [ ] Run the MCP Manager standalone
- [ ] Test server management (add, edit, delete, start, stop)
- [ ] Test tool configuration UI
- [ ] Verify all tools are accessible through UI

### Step 7.4: Test UI Integration
- [ ] Test all dialog boxes and forms
- [ ] Verify MVVM patterns work correctly
- [ ] Test data binding and commands

## Phase 8: Build and Deployment

### Step 8.1: Update Build Scripts
- [ ] Modify build_and_release.ps1 to build both projects
- [ ] Create separate release packages for each

### Step 8.2: Create Deployment Package
- [ ] Structure release packages:
```
AiStudio4-Release/
├── AiStudio4.exe           # Main app
├── AiStudio4.Core.dll      # Shared library
└── Other dependencies...

AiStudio4.McpManager-Release/
├── AiStudio4.McpManager.exe # MCP Manager
├── AiStudio4.Core.dll      # Shared library
└── Other dependencies...
```

### Step 8.3: Update Solution Configuration: Ask the user to:
- [ ] Ensure both projects build correctly
- [ ] Test release builds

## Important Considerations

- [ ] **Path Compatibility**: Ensure PathHelper works correctly in both apps
- [ ] **Logging**: Both apps should log to separate files
- [ ] **Tool Discovery**: Implement proper tool registration in both apps
- [ ] **Error Handling**: Ensure UI-specific error handling is isolated
- [ ] **Configuration Sharing**: Use the same config file locations
- [ ] **Version Synchronization**: Keep both apps version-synchronized

## Final Checklist

- [ ] All core interfaces moved to AiStudio4.Core
- [ ] All shared models moved to AiStudio4.Core
- [ ] MCP service extracted to core library
- [ ] Standalone-compatible tools moved to core
- [ ] Service registration extensions created
- [ ] Main app updated to use shared core
- [ ] WPF MCP Manager application created
- [ ] Tool configuration UI implemented
- [ ] Shared configuration working between apps
- [ ] All tests passing
- [ ] Build scripts updated
- [ ] Documentation updated

## Next Steps: do not proceed beyond this line without express permission.

After implementation:
- [ ] Update documentation for both applications
- [ ] Create separate README files
- [ ] Update CI/CD pipelines
- [ ] Plan migration strategy for existing users
- [ ] Create user guides for the new MCP Manager