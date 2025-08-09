# MCP Standalone Server Application Design

## Executive Summary
This document outlines the design for extracting MCP server functionality from AiStudio4 into a standalone WPF application while maximizing code reuse through a shared core library. The standalone app will provide MCP server management with its own configuration, security settings, and UI.

## Related Documentation
- **[Implementation Plan](MCP_STANDALONE_IMPLEMENTATION_PLAN.md)** - Detailed 11-phase implementation with 300+ tasks
- **[Preparation Checklist](MCP_IMPLEMENTATION_PREP_CHECKLIST.md)** - Pre-flight checks before starting
- **[Service Dependencies](MCP_SERVICE_DEPENDENCY_MAP.md)** - Detailed service and port mapping
- **[Tool Categorization](MCP_TOOL_CATEGORIZATION.md)** - Classification of all 75 tools
- **[NuGet Dependencies](MCP_NUGET_DEPENDENCIES.md)** - Package requirements for each project

## Architecture Overview

### Solution Structure
```
MaxsAiStudio/
├── AiStudio4.Core/              # Shared library (new)
│   ├── Interfaces/
│   ├── Models/
│   ├── Services/
│   ├── Tools/                   # Tools without UI dependencies
│   └── Configuration/
├── AiStudio4/                   # Main WPF application (existing)
│   └── Tools/                   # UI-dependent tools remain here
├── AiStudio4.McpServer/         # Standalone MCP WPF app (new)
│   ├── Views/
│   ├── ViewModels/
│   ├── Services/
│   ├── Configuration/
│   └── App.xaml
└── SharedClasses/               # Existing shared project
```

## Core Design Decisions

### 1. Tool Management
- **No modifications to existing tools** - tools remain unchanged
- **Configuration-based filtering** - separate lists in config files to mark which tools are available in MCP mode
- **Graceful failure** - tools requiring unavailable services will fail with appropriate error messages
- **Tool categories**:
  - Core tools (file, git, search) → Move to AiStudio4.Core
  - UI-dependent tools (ThinkAndAwait, PresentResults, SecondOpinion) → Stay in AiStudio4
  - External service tools (Azure DevOps, GitHub) → Move to Core with config requirements

### 2. Configuration & Settings
- **Separate configuration stores** - MCP app maintains its own settings
- **Independent security** - Separate encrypted storage for PATs/API keys
- **No settings cross-transfer** - True standalone operation
- **Configuration files**:
  ```
  %APPDATA%/AiStudio4.McpServer/
  ├── settings.json              # General settings
  ├── mcpServers.json           # MCP server definitions
  ├── toolExtraProps.json       # Tool-specific configurations
  ├── enabledTools.json         # List of enabled tool GUIDs
  └── secrets/                  # Encrypted credentials
      ├── azureDevOps.enc
      ├── github.enc
      └── googleApi.enc
  ```

### 3. Service Implementation Strategy

#### Required Services for MCP App:
- **IGeneralSettingsService** - New implementation for MCP app settings
- **IStatusMessageService** - Lightweight WPF status bar implementation
- **IDialogService** - WPF dialogs for confirmations and input
- **INotificationFacade** - Simple WPF toast notifications
- **ILogViewerService** - Lightweight log viewer window
- **IProjectService** - Simple implementation tracking working directory
- **IOAuthServerService** - Reuse existing OAuth server for MCP authentication
- **IProtectedMcpServerService** - Full MCP server hosting capability
- **IBuiltInToolExtraPropertiesService** - Tool configuration management

#### Services NOT Needed:
- **ISecondaryAiService/IAiService** - No AI integration
- **IWebSocketNotificationService** - No websocket support
- **IConversationStorage** - No conversation management
- **IUpdateNotificationService** - No update checks
- **IThemeService** - Basic WPF theming only
- **ITipOfTheDayService** - Not needed

### 4. Security & Authentication
- **OAuth Server Integration** - Include OAuth server for protected MCP endpoints
- **Separate PAT Storage** - Independent encrypted credential storage
- **Configuration UI** - WPF interface for managing:
  - Azure DevOps PAT
  - GitHub API Key
  - Google API Keys
  - Other service credentials

### 5. UI Components

#### Main Window
- **Server List Panel** - List of configured MCP servers with status
- **Server Details Panel** - Configuration and logs for selected server
- **Tool Management Panel** - Quick access to:
  - Enabled tools count/summary
  - Quick enable/disable frequently used tools
  - Tool status indicators (configured, missing credentials, etc.)
- **Status Bar** - Current operation status
- **Menu Bar**:
  - File → Settings, Exit
  - Servers → Add, Remove, Start, Stop
  - Tools → Select Tools, Configure Properties, Manage Credentials
  - View → Logs, OAuth Status, Tool Dashboard
  - Help → About

#### Dialogs
- **Server Configuration Dialog** - Add/edit MCP server settings
- **Tool Configuration Dialog** - Manage tool extra properties
- **Tool Selection Dialog** - Enable/disable tools for MCP with:
  - Tree view or categorized list of all available tools
  - Checkboxes to enable/disable each tool
  - Tool description and requirements display
  - Search/filter functionality
  - Bulk enable/disable by category
  - Warning indicators for tools missing required credentials
- **Working Directory Dialog** - Set and persist working directory
- **Credentials Dialog** - Manage API keys and PATs
- **Log Viewer Window** - View application and server logs
- **Simple Input Dialog** - Generic text input
- **Confirmation Dialog** - Yes/No confirmations

### 6. Shared Core Library (AiStudio4.Core)

#### Interfaces to Move:
- ITool
- IToolExecutor
- IMcpService
- IBuiltinToolService
- IBuiltInToolExtraPropertiesService

#### Models to Move:
- Tool
- ToolResponse
- BuiltinToolResult
- McpServerDefinition
- ContentBlock/ContentType

#### Services to Move:
- McpService (core MCP functionality)
- BaseToolImplementation
- ToolRequestBuilder
- PathSecurityManager

#### Tools to Move (No UI Dependencies):
- File Operations: CreateNewFile, DeleteFile, ReadFiles, RenameFile, ReplaceFile
- Git Tools: GitStatus, GitCommit, GitLog, GitBranch
- Search Tools: FileSearch, FileRegExSearch, DirectoryTree
- Azure DevOps Tools: All (require PAT configuration)
- GitHub Tools: All (require API key configuration)
- Web Tools: RetrieveTextFromUrl, LaunchUrl
- Database Tools: ReadDatabaseSchema
- Utility Tools: FindAndReplace, RecordMistake

#### Tools that Stay in AiStudio4:
- ThinkAndAwaitUserInput (requires conversation context)
- PresentResultsAndAwaitUserInput (requires conversation context)
- SecondAiOpinion (requires AI service)
- WindowsSandbox (complex UI integration)
- StopTool (conversation control)
- ThinkAndContinue (conversation flow)

### 7. MCP Server Integration

The standalone app will include:
- **Protected MCP Server** - Full OAuth-protected MCP server hosting
- **OAuth Server** - Embedded OAuth server for authentication
- **Server Management**:
  - Start/stop MCP servers
  - View server logs
  - Monitor connections
  - Configure authentication

### 8. Working Directory Management
- **Persistent Setting** - Save selected working directory
- **Directory Picker Dialog** - Browse and select directory
- **Quick Switch** - Dropdown with recent directories
- **Project Context** - All file operations relative to working directory

## Implementation Phases

### Phase 1: Core Library Extraction
1. Create AiStudio4.Core project
2. Move interfaces and models
3. Move base tool implementations
4. Move standalone-compatible tools
5. Create service abstractions

### Phase 2: MCP Standalone App Creation
1. Create AiStudio4.McpServer WPF project
2. Implement main window and navigation
3. Implement service implementations
4. Add OAuth server integration
5. Implement MCP server hosting

### Phase 3: Configuration & Security
1. Implement separate settings management
2. Create credential encryption/storage
3. Build configuration UI dialogs
4. Implement tool filtering system

### Phase 4: UI Implementation
1. Create server management UI
2. Build tool selection interface with categorized tree view
3. Build tool configuration interface for extra properties
4. Implement status and notifications
5. Add log viewer
6. Create all required dialogs
7. Implement tool dashboard view

### Phase 5: Integration & Testing
1. Test tool execution in standalone mode
2. Verify OAuth authentication
3. Test credential management
4. Validate working directory operations
5. Ensure proper error handling

## Key Technical Considerations

### Dependency Injection
Both apps will use Microsoft.Extensions.DependencyInjection with:
- Shared registrations from Core library
- App-specific service implementations
- Tool registration based on configuration

### Error Handling
- Tools requiring unavailable services show clear error messages
- Graceful degradation when features unavailable
- Comprehensive logging for debugging

### Performance
- Lazy loading of tools
- Efficient server process management
- Minimal memory footprint

### Security
- Encrypted credential storage using DPAPI
- Secure OAuth token handling
- No plaintext secrets in configuration

## Benefits of This Design

1. **True Standalone Operation** - MCP app works independently
2. **Code Reuse** - Maximum sharing through core library
3. **No Tool Modifications** - Existing tools unchanged
4. **Clean Separation** - Clear boundaries between apps
5. **Maintainability** - Shared code reduces duplication
6. **Flexibility** - Each app can evolve independently

## Migration Path

### For Existing Users:
1. Main AiStudio4 app continues working unchanged
2. Optional installation of MCP standalone
3. No migration of settings required
4. Can run both apps simultaneously

### For New Users:
1. Can choose either or both apps
2. MCP standalone for server management only
3. Full AiStudio4 for complete AI studio experience

## Tool Selection UI Details

### Tool Organization
Tools will be organized in a tree structure by category:
- **File Operations** - Create, Read, Delete, Rename files
- **Git Operations** - Status, Commit, Log, Branch
- **Search Tools** - File search, Regex search, Directory tree
- **Azure DevOps** - Work items, Pull requests, Wiki, Pipelines
- **GitHub** - Issues, Pull requests, Repository operations
- **Web Tools** - URL retrieval, Browser launch
- **Database** - Schema reading
- **Utilities** - Find/Replace, Record mistakes

### Tool Selection Features
- **Visual Indicators**:
  - ✅ Green - Tool enabled and configured
  - ⚠️ Yellow - Tool enabled but missing configuration
  - ❌ Red - Tool disabled
  - 🔒 Lock icon - Requires credentials
- **Bulk Operations**:
  - Select all/none in category
  - Enable all configured tools
  - Disable tools with missing credentials
- **Tool Information Panel**:
  - Tool description
  - Required credentials/configuration
  - Last used timestamp
  - Success/failure statistics
- **Search and Filter**:
  - Search by tool name or description
  - Filter by status (enabled/disabled/configured)
  - Filter by category

## Future Enhancements

- Remote MCP server management
- Tool marketplace integration
- Advanced logging and monitoring
- Performance profiling tools
- Tool testing interface
- Batch tool operations
- Tool usage analytics
- Custom tool categories

## Summary

This design creates a focused, standalone MCP server management application that:
- Shares core functionality with AiStudio4 through a common library
- Maintains its own configuration and security settings
- Provides essential UI for server and tool management
- Operates completely independently from the main application
- Requires no modifications to existing tool implementations

The approach maximizes code reuse while ensuring true standalone operation, making it ideal for users who need MCP server functionality without the full AI studio features.