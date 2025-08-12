# Tool Sharing Implementation

## Overview
This document describes the implementation of tool sharing between the main AiStudio4 application and the MCP Standalone server using a shared library approach.

## Architecture

### Shared Library (AiStudio4.Tools)
- **Purpose**: Contains all tool implementations that can be used by both applications
- **Location**: `AiStudio4.Tools` project
- **Key Components**:
  - `ITool` interface - Base interface for all tools
  - `BaseToolImplementation` - Base class with common tool functionality
  - Tool implementations (e.g., `YouTubeSearchTool`)
  - Minimal interfaces for dependencies (`IGeneralSettingsService`, `IStatusMessageService`, etc.)

### Main Application Integration
- **Adapter Pattern**: Uses adapters to bridge between app-specific interfaces and shared library interfaces
- **Key Adapters**:
  - `GeneralSettingsServiceAdapter` - Maps main app's settings service to shared interface
  - `SharedToolAdapter` - Wraps shared tools to work with main app's ITool interface
- **Dynamic Registration**: Assembly scanning automatically discovers and registers shared tools
- **Location**: `AiStudio4\Services\Adapters\`

### MCP Standalone Integration
- **Minimal Services**: Implements lightweight versions of required services
- **Key Services**:
  - `StandaloneSettingsService` - Stores encrypted API keys using DPAPI
  - `NoOpStatusMessageService` - Logs status messages instead of WebSocket
  - `StandaloneExtraPropertiesService` - Simple dictionary-based properties storage
- **Dynamic MCP Registration**: Uses reflection to register shared tools with MCP server
- **Location**: `AiStudio4.McpStandalone\Services\`

## Implementation Details

### Tool Discovery and Registration

#### Main Application
```csharp
// In DependencyInjection.cs
var sharedAssembly = Assembly.Load("AiStudio4.Tools");
var toolTypes = sharedAssembly.GetTypes()
    .Where(t => sharedToolInterfaceType.IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
    .Where(t => t != typeof(SharedToolAdapter)); // Exclude adapter from scan
```

#### MCP Standalone
```csharp
// In SimpleMcpServerService.cs
var mcpBuilder = builder.Services.AddMcpServer()
    .WithTools<HelloWorldTool>();

// Dynamic registration using reflection
foreach (var toolType in sharedToolTypes)
{
    var genericMethod = withToolsMethod.MakeGenericMethod(toolType);
    mcpBuilder = (IMcpServerBuilder)genericMethod.Invoke(mcpBuilder, null);
}
```

### Settings Storage

#### Main Application
- Uses existing `GeneralSettingsService` with full feature set
- Stores in `%APPDATA%\AiStudio\settings.json`

#### MCP Standalone
- Minimal `StandaloneSettingsService` with only essential settings
- Stores in `%APPDATA%\AiStudio4.McpStandalone\settings.json`
- Encrypts sensitive data (API keys, tokens) using DPAPI

### Key Design Decisions

1. **Shared Library Approach**: Single source of truth for tool implementations
2. **Adapter Pattern**: Allows tools to work unchanged in both environments
3. **Dynamic Registration**: No need to manually register each tool
4. **Minimal Dependencies**: Shared library has minimal external dependencies
5. **Interface Segregation**: Shared library defines minimal interfaces for its needs

## Testing Approach

1. **Main App Verification**: Ensure existing functionality preserved
2. **MCP Standalone Testing**: Verify tools exposed via MCP protocol
3. **Claude Code Integration**: Test with `claude mcp add` command

## Benefits

- **No Code Duplication**: Tools maintained in single location
- **Easy Tool Addition**: New tools automatically available in both apps
- **Independent Deployment**: Apps can be updated independently
- **Clean Separation**: Clear boundaries between shared and app-specific code

## Future Enhancements

1. Add more tools to shared library
2. Implement tool configuration UI in MCP Standalone
3. Add tool-specific settings management
4. Support for tool versioning
5. Enhanced error handling and logging