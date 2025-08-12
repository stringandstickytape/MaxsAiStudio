# MCP Shared Tools Library Design

## Overview
This design creates a shared library for tools that can be used by both the main AiStudio4 application and the MCP Standalone server, ensuring tools exist in only one place and avoiding code duplication.

## Goals
1. **Single source of truth** - Each tool implementation exists in exactly one place
2. **Minimal dependencies** - Tools depend only on minimal service interfaces
3. **Flexible service implementations** - Each app provides its own service implementations
4. **No UI during execution** - Tools run headlessly when called via MCP
5. **Configuration UI where needed** - Standalone app has settings UI for credentials

## Architecture

### Project Structure

```
Solution Root/
├── AiStudio4.Tools/              (New shared class library)
│   ├── Interfaces/
│   │   ├── ITool.cs
│   │   ├── IGeneralSettingsService.cs  (minimal interface)
│   │   ├── IStatusMessageService.cs
│   │   └── IBuiltInToolExtraPropertiesService.cs
│   ├── Models/
│   │   ├── Tool.cs
│   │   ├── BuiltinToolResult.cs
│   │   ├── ContentBlock.cs
│   │   └── ToolGuids.cs
│   ├── BaseToolImplementation.cs
│   └── Tools/
│       ├── AzureDevOps/
│       │   ├── AzureDevOpsSearchWikiTool.cs
│       │   └── ... (other Azure DevOps tools)
│       ├── GitHub/
│       │   └── ... (GitHub tools)
│       └── FileOperations/
│           └── ... (File operation tools)
│
├── AiStudio4/                    (Main WPF Application)
│   ├── References: AiStudio4.Tools
│   ├── Services/
│   │   ├── GeneralSettingsService.cs (full implementation with UI)
│   │   ├── StatusMessageService.cs (WebSocket implementation)
│   │   └── BuiltInToolExtraPropertiesService.cs
│   └── ... (rest of main app)
│
└── AiStudio4.McpStandalone/      (MCP Server Application)
    ├── References: AiStudio4.Tools
    ├── Views/
    │   └── SettingsWindow.xaml   (Configuration UI)
    └── Services/
        ├── StandaloneSettingsService.cs (reads from saved config)
        ├── NoOpStatusMessageService.cs  (logs only, no WebSocket)
        └── StandaloneExtraPropertiesService.cs
```

## Component Details

### AiStudio4.Tools Library

#### Minimal Interfaces

```csharp
// IGeneralSettingsService.cs - Minimal interface for tools
public interface IGeneralSettingsService
{
    string GetDecryptedYouTubeApiKey();
    string GetDecryptedAzureDevOpsPAT();
    string GetDecryptedGitHubToken();
    string GetProjectPath();
    // Only methods actually used by tools
}

// IStatusMessageService.cs
public interface IStatusMessageService  
{
    Task SendStatusMessageAsync(string clientId, string message);
}

// ITool.cs
public interface ITool
{
    Tool GetToolDefinition();
    Task<BuiltinToolResult> ProcessAsync(string toolParameters, Dictionary<string, string> extraProperties);
    void UpdateProjectRoot();
}
```

#### BaseToolImplementation
- Simplified version without UI dependencies
- Uses only the minimal interfaces
- Provides common functionality for all tools

### AiStudio4 (Main App) Implementations

```csharp
// Full GeneralSettingsService with UI capabilities
public class GeneralSettingsService : IGeneralSettingsService
{
    // Can show dialogs for missing credentials
    // Has full settings management
    // Integrates with WPF UI
}
```

### AiStudio4.McpStandalone Implementations

```csharp
// Standalone settings service - no UI during execution
public class StandaloneSettingsService : IGeneralSettingsService
{
    private readonly IConfiguration _configuration;
    
    public string GetDecryptedAzureDevOpsPAT()
    {
        // Read from encrypted config file
        // Never prompts user during execution
        return DecryptFromConfig("AzureDevOpsPAT");
    }
}

// No-op status service
public class NoOpStatusMessageService : IStatusMessageService
{
    private readonly ILogger _logger;
    
    public Task SendStatusMessageAsync(string clientId, string message)
    {
        _logger.LogInformation("Tool status: {Message}", message);
        return Task.CompletedTask;
    }
}
```

## Configuration Management

### Standalone App Settings

The standalone app will have a Settings UI for configuration:

```
Settings Window:
├── Credentials Tab
│   ├── Azure DevOps PAT: [****] [Show]
│   ├── GitHub Token: [****] [Show]
│   └── Google API Key: [****] [Show]
├── Paths Tab
│   ├── Working Directory: [C:\Projects\Current]
│   └── Temp Directory: [Default]
└── Tools Tab
    └── Enable/Disable specific tools
```

Settings are:
- Saved to `%APPDATA%\AiStudio4.McpStandalone\settings.json`
- Credentials encrypted using DPAPI
- Loaded at startup
- Never prompt during tool execution

## MCP Server Integration

### Tool Registration

```csharp
public class SimpleMcpServerService
{
    private void RegisterTools(IServiceCollection services)
    {
        // Register service implementations
        services.AddSingleton<IGeneralSettingsService, StandaloneSettingsService>();
        services.AddSingleton<IStatusMessageService, NoOpStatusMessageService>();
        
        // Auto-discover and register tools from AiStudio4.Tools
        var toolTypes = typeof(ITool).Assembly.GetTypes()
            .Where(t => typeof(ITool).IsAssignableFrom(t) && !t.IsAbstract);
            
        foreach (var toolType in toolTypes)
        {
            services.AddTransient(toolType);
        }
    }
}
```

## Migration Path

### Phase 1: Create Shared Library
1. Create AiStudio4.Tools project
2. Define minimal interfaces
3. Copy models and base classes
4. Move one tool (YouTubeSearchTool) as proof of concept - simpler, only needs API key

### Phase 2: Implement Standalone Services
1. Create StandaloneSettingsService
2. Create NoOpStatusMessageService
3. Add Settings UI to standalone app
4. Wire up tool in MCP server

### Phase 3: Verify and Test
1. Test tool works in both apps
2. Verify no code duplication
3. Ensure clean separation of concerns

### Phase 4: Migrate Remaining Tools
1. Move tools in batches by category
2. Update main app to use shared library
3. Test each batch

## Benefits

1. **DRY Principle** - Tools exist in one place only
2. **Clean Architecture** - Clear separation between tools and app-specific code
3. **Maintainability** - Changes to tools automatically available to both apps
4. **Flexibility** - Each app can implement services differently
5. **Testability** - Tools can be unit tested independently

## Considerations

1. **Version Management** - Both apps must reference same version of shared library
2. **Breaking Changes** - Interface changes affect both apps
3. **Dependencies** - Keep shared library dependencies minimal
4. **Build Order** - Shared library must build before dependent projects

## Success Criteria

- [ ] Tools exist in only one location
- [ ] Both apps can use the same tool code
- [ ] No UI prompts during MCP tool execution
- [ ] Settings UI available in standalone app
- [ ] Clean service abstractions
- [ ] No unnecessary dependencies in shared library