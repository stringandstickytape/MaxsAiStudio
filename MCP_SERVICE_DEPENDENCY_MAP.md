# MCP Service Dependency Map

## Current Architecture Analysis

### Port Configuration
- **Main App (Current)**:
  - MCP Server: `http://localhost:7071/`
  - OAuth Server: `http://localhost:7029`
  
- **Standalone App (Planned)**:
  - MCP Server: `http://localhost:8071/` (default, configurable)
  - OAuth Server: `http://localhost:8029` (default, configurable)
  - No other ports needed (no WebSocket server, no file server)

### OAuth Server Implementation
- **Current**: Uses `OAuthServerManager` from `ModelContextProtocol.TestOAuthServer`
- **Persistence**: Stores data in `%APPDATA%/AiStudio4/OAuth/`
- **Reusable**: Yes, can be used identically in standalone app
- **Configuration**: Just needs different ports and data directory

### MCP Server Dependencies

#### Core Services Required
```csharp
// From ProtectedMcpServerService
IBuiltinToolService       // Tool discovery and management
IGeneralSettingsService    // Settings and configuration
ILogger                    // Logging
```

#### Authentication Chain
```
Client → OAuth Server (8029) → Issues JWT Token
Client → MCP Server (8071) → Validates JWT Token → Executes Tools
```

## Tool Service Dependencies

### Analyzed Tool Dependencies

#### Minimal Dependencies (Most Tools)
These tools only need basic services:
- `ILogger<T>` - Logging
- `IGeneralSettingsService` - Project path, settings
- `IStatusMessageService` - Status updates (can be lightweight)

**Tools**: All file operations, Git tools, search tools

#### Credential Dependencies
These tools need credential access:
- Azure DevOps tools → Need PAT from settings
- GitHub tools → Need API key from settings  
- Google search tools → Need API key from settings

**Implementation**: Settings service provides encrypted credentials

#### Special Dependencies
- `SecondAiOpinionTool` → Needs `ISecondaryAiService` (EXCLUDE from standalone)
- `ThinkAndAwaitUserInputTool` → Needs conversation context (EXCLUDE)
- `PresentResultsAndAwaitUserInputTool` → Needs conversation context (EXCLUDE)
- `StopTool` → Controls conversation flow (EXCLUDE)
- `WindowsSandboxTool` → Complex UI integration (EXCLUDE)

## Service Implementation Requirements

### Must Implement in Standalone

#### 1. IGeneralSettingsService
```csharp
public interface IGeneralSettingsService {
    GeneralSettings CurrentSettings { get; }
    Task SaveSettings();
    // Provides: ProjectPath, Credentials, Temperature, etc.
}
```

#### 2. IStatusMessageService  
```csharp
public interface IStatusMessageService {
    Task SendStatusMessageAsync(string clientId, string message);
    // Lightweight WPF implementation - just update status bar
}
```

#### 3. IBuiltinToolService
```csharp
public interface IBuiltinToolService {
    List<ITool> GetTools();
    ITool GetToolByName(string name);
    // Tool discovery and management
}
```

#### 4. IBuiltInToolExtraPropertiesService
```csharp
public interface IBuiltInToolExtraPropertiesService {
    Dictionary<string, string> GetExtraProperties(string toolName);
    void SaveExtraProperties(string toolName, Dictionary<string, string> props);
}
```

### Can Stub/Simplify

#### 1. IDialogService
- Simple WPF dialogs for confirmations
- Input dialogs for user text
- No complex multi-step wizards

#### 2. INotificationFacade  
- Simple toast notifications
- No need for complex notification center

#### 3. ILogViewerService
- Basic log viewing window
- Filter by level
- No need for complex analysis

## Configuration File Structure

### Standalone App Configuration
```
%APPDATA%/AiStudio4.McpServer/
├── settings.json              # General settings, ports, paths
├── credentials.json           # Encrypted PATs and API keys
├── enabledTools.json          # List of enabled tool GUIDs
├── toolExtraProps.json        # Tool-specific configurations
├── mcpServers.json            # MCP server configurations (if managing others)
└── OAuth/                     # OAuth server persistence
    └── [OAuth data files]
```

### Settings.json Structure
```json
{
  "mcpServerPort": 8071,
  "oauthServerPort": 8029,
  "workingDirectory": "C:\\Projects",
  "logLevel": "Information",
  "autoStartOAuth": true,
  "theme": "Dark"
}
```

### Credentials.json Structure (Encrypted)
```json
{
  "azureDevOpsPAT": "[encrypted]",
  "githubApiKey": "[encrypted]",
  "googleApiKey": "[encrypted]",
  "googleCustomSearchEngineId": "[encrypted]"
}
```

### EnabledTools.json Structure
```json
{
  "enabledToolGuids": [
    "a1b2c3d4-e5f6-7890-1234-567890abcd01",
    "b2c3d4e5-f6a7-8901-2345-67890abcdef04"
    // ... other enabled tool GUIDs
  ]
}
```

## DI Registration Order

### Core Services (Register First)
1. Configuration/Settings services
2. Logging
3. OAuth server service
4. Credential management

### Tool Services (Register Second)  
1. Tool discovery service
2. Tool extra properties service
3. Individual tool registrations

### MCP Services (Register Last)
1. MCP server service
2. Authentication middleware
3. Server hosting

## Implementation Notes

### Reusable Components
- `OAuthServerManager` - Use as-is, just different ports
- `BaseToolImplementation` - Use as-is
- `ToolGuids` - Use as-is for tool identification
- Tool implementations - Most can be used unchanged

### Components Needing Adaptation
- `ProtectedMcpServerService` - Adapt for different DI container
- `GeneralSettingsService` - Simpler version for standalone
- `BuiltinToolService` - Filter tools based on enabled list

### New Components Needed
- WPF StatusMessageService
- WPF NotificationService  
- Standalone configuration service
- Credential encryption service
- Tool selection/filtering logic

## Testing Considerations

### Integration Points to Test
1. OAuth token generation and validation
2. Tool discovery and registration
3. Credential encryption/decryption
4. MCP protocol communication
5. Tool execution via MCP

### Isolated Testing Strategy
1. Test OAuth server independently first
2. Test tool execution without MCP
3. Test MCP server with mock tools
4. Full integration test with real tools