# MCP Standalone Implementation Plan - Phased Approach

## Overview
This implementation plan is designed to maintain a working AiStudio4 application at every phase while gradually extracting functionality into a shared core library and building the standalone MCP application.

## Phase 1: Preparation and Planning (No Code Changes)
**Goal**: Set up for safe refactoring without breaking existing app
**Test**: Existing app continues to work unchanged

### 1.1 Project Setup
- [ ] Create feature branch `feature/mcp-standalone`
- [ ] Document current project structure
- [ ] Create backup of current solution
- [ ] Review and document all tool dependencies
- [ ] Identify all services used by each tool
- [ ] Document current DI registrations

### 1.2 Tool Inventory and Classification
- [ ] Create comprehensive tool list with dependencies
- [ ] Classify tools as "Core" (can move) vs "App-specific" (must stay)
- [ ] Document service dependencies for each tool
- [ ] Identify shared models and interfaces
- [ ] List all NuGet packages needed for core library

### 1.3 Testing Baseline
- [ ] Run all existing tests in AiStudio4
- [ ] Document current test coverage
- [ ] Create manual test checklist for main app features
- [ ] Test all tools currently work
- [ ] Document any existing issues

**Validation Checkpoint**: 
- [ ] Main app builds and runs
- [ ] All tools functional
- [ ] All tests pass

---

## Phase 2: Create Core Library Project (No Breaking Changes)
**Goal**: Create shared library project without modifying existing code
**Test**: Existing app unchanged and fully functional

### 2.1 Create Core Project
- [ ] Create new project `AiStudio4.Core` (Class Library, .NET 9.0-windows)
- [ ] Add project to solution
- [ ] Configure project properties (version, assembly info)
- [ ] Create folder structure matching planned organization
- [ ] Add SharedClasses project reference to Core

### 2.2 Add Core Dependencies
- [ ] Add NuGet packages to Core project:
  - [ ] ModelContextProtocol.Client
  - [ ] ModelContextProtocol.Protocol
  - [ ] ModelContextProtocol.Server
  - [ ] Newtonsoft.Json
  - [ ] Microsoft.Extensions.Logging.Abstractions
  - [ ] Microsoft.Extensions.DependencyInjection.Abstractions
  - [ ] Microsoft.Extensions.Configuration.Abstractions
  - [ ] System.Text.Json

### 2.3 Create Core Interfaces (Copies)
- [ ] Create `AiStudio4.Core.Interfaces` namespace
- [ ] Copy (not move) ITool interface to Core
- [ ] Copy IToolExecutor interface to Core
- [ ] Copy IMcpService interface to Core
- [ ] Copy IBuiltinToolService interface to Core
- [ ] Copy IBuiltInToolExtraPropertiesService interface to Core
- [ ] Create new IConfigurationService interface in Core
- [ ] Create new IDialogService interface in Core
- [ ] Create new IStatusMessageService interface in Core

### 2.4 Create Core Models (Copies)
- [ ] Create `AiStudio4.Core.Models` namespace
- [ ] Copy Tool model to Core
- [ ] Copy ToolResponse model to Core
- [ ] Copy BuiltinToolResult model to Core
- [ ] Copy McpServerDefinition model to Core
- [ ] Copy ContentBlock model to Core
- [ ] Copy ContentType enum to Core
- [ ] Copy ToolResponseItem model to Core

**Validation Checkpoint**:
- [ ] Core project builds successfully
- [ ] Main app still builds (using its own copies)
- [ ] Main app runs unchanged
- [ ] All tools still functional

---

## Phase 3: Create Abstraction Layer in Core
**Goal**: Build service abstractions without affecting main app
**Test**: Main app continues using its own implementations

### 3.1 Create Service Abstractions
- [ ] Create abstract BaseToolImplementation in Core
- [ ] Create IGeneralSettingsService interface in Core
- [ ] Create IProjectService interface in Core
- [ ] Create INotificationService interface in Core
- [ ] Create ILogService interface in Core
- [ ] Ensure all interfaces in Core namespace

### 3.2 Copy Tool Base Classes
- [ ] Copy BaseToolImplementation to Core (new namespace)
- [ ] Copy ToolRequestBuilder to Core
- [ ] Copy ToolGuids to Core
- [ ] Adjust namespaces to Core
- [ ] Remove UI-specific dependencies from copies

### 3.3 Create Core Service Implementations
- [ ] Create CoreMcpService (copy of McpService, Core namespace)
- [ ] Create CoreToolService (basic implementation)
- [ ] Create CoreConfigurationService (file-based)
- [ ] Create PathSecurityManager in Core
- [ ] Ensure all use Core interfaces

### 3.4 Create Extension Methods
- [ ] Create ServiceCollectionExtensions in Core
- [ ] Add AddCoreServices() method
- [ ] Add AddCoreTools() method (empty for now)
- [ ] Add AddCoreMcp() method

**Validation Checkpoint**:
- [ ] Core project builds with abstractions
- [ ] Main app unaffected (still using original code)
- [ ] All main app functionality works
- [ ] No namespace conflicts

---

## Phase 4: Copy Tools to Core (Non-Breaking)
**Goal**: Duplicate tools in Core library without removing from main app
**Test**: Main app uses original tools, Core has copies ready

### 4.1 File Operation Tools
- [ ] Copy CreateNewFileTool to Core/Tools/FileOperations/
- [ ] Copy DeleteFileTool to Core/Tools/FileOperations/
- [ ] Copy ReadFilesTool to Core/Tools/FileOperations/
- [ ] Copy ReadPartialFilesTool to Core/Tools/FileOperations/
- [ ] Copy RenameFileTool to Core/Tools/FileOperations/
- [ ] Copy ReplaceFileTool to Core/Tools/FileOperations/
- [ ] Update namespaces to Core
- [ ] Update to use Core interfaces

### 4.2 Git Tools
- [ ] Copy GitStatusTool to Core/Tools/Git/
- [ ] Copy GitCommitTool to Core/Tools/Git/
- [ ] Copy GitLogTool to Core/Tools/Git/
- [ ] Copy GitBranchTool to Core/Tools/Git/
- [ ] Update namespaces and interfaces

### 4.3 Search Tools
- [ ] Copy FileSearchTool to Core/Tools/Search/
- [ ] Copy FileRegExSearchTool to Core/Tools/Search/
- [ ] Copy DirectoryTreeTool to Core/Tools/Search/
- [ ] Copy FindAndReplaceTool to Core/Tools/Search/
- [ ] Update namespaces and interfaces

### 4.4 Azure DevOps Tools
- [ ] Copy all Azure DevOps tools to Core/Tools/AzureDevOps/
- [ ] Update namespaces
- [ ] Ensure credential handling abstracted

### 4.5 GitHub Tools
- [ ] Copy all GitHub tools to Core/Tools/GitHub/
- [ ] Update namespaces
- [ ] Ensure API key handling abstracted

### 4.6 Other Tools
- [ ] Copy RetrieveTextFromUrlTool to Core/Tools/Web/
- [ ] Copy LaunchUrlTool to Core/Tools/Web/
- [ ] Copy GoogleCustomSearchApiTool to Core/Tools/Web/
- [ ] Copy ReadDatabaseSchemaTool to Core/Tools/Database/
- [ ] Copy RecordMistakeTool to Core/Tools/Utility/

**Validation Checkpoint**:
- [ ] Core project builds with all copied tools
- [ ] Main app still using original tools
- [ ] Main app fully functional
- [ ] Core tools compile but not yet used

---

## Phase 5: Create MCP Standalone App Structure
**Goal**: Build standalone app skeleton without integration
**Test**: New app runs independently, main app unchanged

### 5.1 Create WPF Project
- [ ] Create AiStudio4.McpServer WPF project (.NET 9.0-windows)
- [ ] Add to solution
- [ ] Add reference to AiStudio4.Core
- [ ] Configure project properties
- [ ] Set up app icon and manifest

### 5.2 Add Dependencies
- [ ] Add NuGet packages:
  - [ ] Microsoft.Extensions.Hosting
  - [ ] Microsoft.Extensions.Configuration.Json
  - [ ] Microsoft.Extensions.DependencyInjection
  - [ ] CommunityToolkit.Mvvm
  - [ ] ModernWpfUI or MahApps.Metro
  - [ ] Serilog.Extensions.Hosting
  - [ ] Serilog.Sinks.Console
  - [ ] Serilog.Sinks.File
  - [ ] Microsoft.AspNetCore.Authentication.JwtBearer
  - [ ] ModelContextProtocol.AspNetCore.Authentication

### 5.3 Create Basic App Structure
- [ ] Create App.xaml with ModernWpf resources
- [ ] Create App.xaml.cs with DI container setup
- [ ] Create Views folder
- [ ] Create ViewModels folder
- [ ] Create Services folder
- [ ] Create Configuration folder

### 5.4 Implement Core Services
- [ ] Create McpConfigurationService
- [ ] Create McpGeneralSettingsService
- [ ] Create McpStatusMessageService
- [ ] Create McpDialogService
- [ ] Create McpNotificationService
- [ ] Create McpLogService
- [ ] Create McpProjectService

### 5.5 Create Main Window
- [ ] Create MainWindow.xaml
- [ ] Create MainWindowViewModel
- [ ] Add basic menu structure
- [ ] Add status bar
- [ ] Add placeholder panels
- [ ] Wire up DI for MainWindow

**Validation Checkpoint**:
- [ ] Standalone app launches (empty window)
- [ ] DI container initializes
- [ ] Logging works
- [ ] Main app still fully functional

---

## Phase 6: Implement OAuth and MCP Server in Standalone
**Goal**: Add server functionality to standalone app
**Test**: Standalone can host MCP server, main app unchanged

### 6.1 Copy OAuth Server Components
- [ ] Copy OAuth server code to Core or McpServer project
- [ ] Copy AuthorizationServerMetadata
- [ ] Copy TokenResponse models
- [ ] Copy OAuth middleware setup
- [ ] Create IOAuthServerService interface

### 6.2 Implement MCP Server Hosting
- [ ] Copy ProtectedMcpServerService to standalone
- [ ] Adapt for standalone DI container
- [ ] Create server management service
- [ ] Implement start/stop functionality
- [ ] Add server status monitoring

### 6.3 Create Server Management UI
- [ ] Create ServerListView and ViewModel
- [ ] Create ServerDetailsView and ViewModel
- [ ] Implement server start/stop buttons
- [ ] Add server status indicators
- [ ] Create add/edit server dialog

### 6.4 Wire Up MCP Server
- [ ] Register Core tools in DI
- [ ] Configure MCP server endpoints
- [ ] Set up OAuth authentication
- [ ] Test server starts and accepts connections
- [ ] Verify tools are exposed via MCP

**Validation Checkpoint**:
- [ ] Standalone app can start MCP server
- [ ] OAuth server runs
- [ ] Server accepts MCP connections
- [ ] Tools visible via MCP protocol
- [ ] Main app unaffected

---

## Phase 7: Implement Tool Selection UI
**Goal**: Add tool management interface to standalone
**Test**: Can configure tools in standalone, main app unchanged

### 7.1 Create Tool Models
- [ ] Create ToolViewModel
- [ ] Create ToolCategoryViewModel
- [ ] Create ToolConfigurationModel
- [ ] Create EnabledToolsConfiguration

### 7.2 Build Tool Selection Dialog
- [ ] Create ToolSelectionDialog.xaml
- [ ] Create ToolSelectionViewModel
- [ ] Implement tree view with categories
- [ ] Add checkboxes for enable/disable
- [ ] Add search functionality
- [ ] Add category bulk operations

### 7.3 Implement Tool Configuration
- [ ] Create ToolConfigurationDialog.xaml
- [ ] Create ToolConfigurationViewModel
- [ ] Build property grid for extra properties
- [ ] Add save/load configuration
- [ ] Implement validation

### 7.4 Create Tool Dashboard
- [ ] Create ToolDashboardView
- [ ] Show enabled tools summary
- [ ] Display configuration status
- [ ] Add quick enable/disable toggles
- [ ] Show missing credentials warnings

### 7.5 Persist Tool Settings
- [ ] Create enabledTools.json structure
- [ ] Create toolExtraProps.json structure
- [ ] Implement save/load methods
- [ ] Add configuration watching
- [ ] Handle configuration errors

**Validation Checkpoint**:
- [ ] Tool selection UI functional
- [ ] Tool configurations persist
- [ ] Selected tools available in MCP server
- [ ] Main app still uses own tool config

---

## Phase 8: Implement Credentials Management
**Goal**: Add secure credential storage to standalone
**Test**: Standalone manages own credentials, main app credentials separate

### 8.1 Create Credential Models
- [ ] Create CredentialType enum
- [ ] Create CredentialEntry model
- [ ] Create EncryptedCredentialStore
- [ ] Implement DPAPI encryption

### 8.2 Build Credentials UI
- [ ] Create CredentialsDialog.xaml
- [ ] Create CredentialsViewModel
- [ ] Add fields for each credential type
- [ ] Implement show/hide password
- [ ] Add test connection buttons

### 8.3 Integrate with Tools
- [ ] Update Azure DevOps tools to use credentials
- [ ] Update GitHub tools to use credentials
- [ ] Update Google API tools to use credentials
- [ ] Add credential validation
- [ ] Show warnings for missing credentials

### 8.4 Secure Storage
- [ ] Create encrypted credentials folder
- [ ] Implement secure save/load
- [ ] Add credential migration support
- [ ] Handle credential errors gracefully
- [ ] Add credential backup/restore

**Validation Checkpoint**:
- [ ] Credentials UI works
- [ ] Credentials stored securely
- [ ] Tools can access credentials
- [ ] Main app credentials unaffected

---

## Phase 9: Complete Standalone Features
**Goal**: Finish all standalone app features
**Test**: Standalone fully functional, main app unchanged

### 9.1 Working Directory Management
- [ ] Create WorkingDirectoryDialog
- [ ] Create WorkingDirectoryService
- [ ] Add recent directories list
- [ ] Implement directory validation
- [ ] Persist selected directory

### 9.2 Logging and Monitoring
- [ ] Create LogViewerWindow
- [ ] Create LogViewerViewModel
- [ ] Implement log filtering
- [ ] Add log export functionality
- [ ] Create real-time log updates

### 9.3 Status and Notifications
- [ ] Implement status bar updates
- [ ] Create toast notification system
- [ ] Add notification history
- [ ] Implement notification settings
- [ ] Create notification sound options

### 9.4 Settings and Preferences
- [ ] Create SettingsDialog
- [ ] Create SettingsViewModel
- [ ] Add general settings tab
- [ ] Add appearance settings
- [ ] Add advanced settings

### 9.5 Help and Documentation
- [ ] Create AboutDialog
- [ ] Add help menu items
- [ ] Create quick start guide
- [ ] Add tooltips throughout UI
- [ ] Include troubleshooting guide

**Validation Checkpoint**:
- [ ] All standalone features working
- [ ] Settings persist correctly
- [ ] Logging functional
- [ ] Notifications working
- [ ] Main app still independent

---

## Phase 10: Refactor Main App to Use Core
**Goal**: Update main app to use shared core library
**Test**: Main app works identically using core library

### 10.1 Add Core Reference
- [ ] Add project reference to AiStudio4.Core
- [ ] Update using statements
- [ ] Resolve namespace conflicts
- [ ] Update project dependencies

### 10.2 Update Interfaces
- [ ] Alias or remove duplicate interfaces
- [ ] Update tools to use Core interfaces
- [ ] Update services to use Core interfaces
- [ ] Fix compilation errors

### 10.3 Update Tool Registrations
- [ ] Update DI to use Core tools where applicable
- [ ] Keep UI-specific tools in main app
- [ ] Update tool discovery logic
- [ ] Test all tools still work

### 10.4 Remove Duplicates
- [ ] Remove tool copies now in Core
- [ ] Remove duplicate models
- [ ] Remove duplicate interfaces
- [ ] Clean up unused code

### 10.5 Integration Testing
- [ ] Test all main app features
- [ ] Verify all tools work
- [ ] Check performance
- [ ] Validate no regressions

**Validation Checkpoint**:
- [ ] Main app uses Core library
- [ ] All features still work
- [ ] No duplicate code
- [ ] Both apps share core functionality

---

## Phase 11: Final Testing and Polish
**Goal**: Ensure both apps are production-ready
**Test**: Both apps fully functional and polished

### 11.1 Comprehensive Testing
- [ ] Test all tools in both apps
- [ ] Verify credential handling
- [ ] Test error scenarios
- [ ] Check memory usage
- [ ] Validate performance

### 11.2 UI Polish
- [ ] Review all dialogs for consistency
- [ ] Check keyboard shortcuts
- [ ] Verify tab order
- [ ] Test accessibility features
- [ ] Review error messages

### 11.3 Documentation
- [ ] Update main app README
- [ ] Create standalone app README
- [ ] Document configuration files
- [ ] Create migration guide
- [ ] Update tool documentation

### 11.4 Build and Packaging
- [ ] Update build scripts
- [ ] Create release packages
- [ ] Test installation process
- [ ] Verify file associations
- [ ] Check uninstall process

### 11.5 Final Validation
- [ ] Run full test suite
- [ ] User acceptance testing
- [ ] Performance benchmarks
- [ ] Security review
- [ ] Code review

**Final Checkpoint**:
- [ ] Both apps production-ready
- [ ] All tests passing
- [ ] Documentation complete
- [ ] Ready for release

---

## Rollback Plan

At any phase, if issues arise:

1. **Immediate Rollback**:
   - [ ] Revert to last known good commit
   - [ ] Verify main app functionality restored
   - [ ] Document issue encountered

2. **Partial Rollback**:
   - [ ] Keep completed phases
   - [ ] Revert only problematic changes
   - [ ] Re-test from last good phase

3. **Recovery Steps**:
   - [ ] Restore from backup if needed
   - [ ] Rebuild from stable branch
   - [ ] Re-apply successful changes only

## Success Metrics

- [ ] Main app remains functional throughout
- [ ] No loss of existing features
- [ ] Standalone app fully operational
- [ ] Code sharing achieved (>60% shared)
- [ ] Both apps independently deployable
- [ ] Performance unchanged or improved
- [ ] All tests passing
- [ ] User documentation complete

## Risk Mitigation

- **Daily Backups**: Commit working state daily
- **Feature Flags**: Use flags to toggle new code
- **Parallel Development**: Keep both code paths until ready
- **Incremental Migration**: Move one tool at a time
- **Continuous Testing**: Test after each change
- **Rollback Ready**: Always have rollback plan

## Notes for Next Session

When starting implementation:
1. Begin with Phase 1 (no code changes)
2. Create feature branch immediately
3. Test main app works before any changes
4. Proceed phase by phase
5. Validate at each checkpoint
6. Only move to next phase when current is stable

This plan ensures the main AiStudio4 app continues working throughout the entire implementation process.