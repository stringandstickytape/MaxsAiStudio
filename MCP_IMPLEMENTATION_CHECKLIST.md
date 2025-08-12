# MCP Shared Tools Implementation Checklist

## Current Status
Starting implementation of shared tools library for MCP Standalone server.

## Phase 1: Create Shared Library Structure ✅

### Setup
- [x] Create new Class Library project `AiStudio4.Tools` (.NET 9.0)
- [x] Add project to solution
- [x] Configure project properties and dependencies (fixed package version to 9.0.5)

### Create Minimal Interfaces
- [x] Create `Interfaces` folder
- [x] Create `IGeneralSettingsService.cs` with minimal methods:
  - [x] `GetDecryptedYouTubeApiKey()`
  - [x] `GetDecryptedAzureDevOpsPAT()`
  - [x] `GetDecryptedGitHubToken()` (mapped to GetDecryptedGitHubApiKey)
  - [x] `GetProjectPath()`
- [x] Create `IStatusMessageService.cs`
- [x] Create `ITool.cs`
- [x] Create `IBuiltInToolExtraPropertiesService.cs`

### Copy Required Models
- [x] Create `Models` folder
- [x] Copy `Tool.cs` from main app
- [x] Copy `BuiltinToolResult.cs` with Attachment class
- [x] Copy `ToolGuids.cs`
- [x] Adjust namespaces to `AiStudio4.Tools.Models`

### Copy Base Implementation
- [x] Copy `BaseToolImplementation.cs`
- [x] Remove UI-specific code
- [x] Update to use minimal interfaces
- [x] Adjust namespace to `AiStudio4.Tools`

### Add First Tool
- [x] Create `Tools/YouTube` folder
- [x] Copy `YouTubeSearchTool.cs`
- [x] Update namespace to `AiStudio4.Tools.YouTube`
- [x] Verify it compiles with minimal interfaces

## Phase 2: Update Main App ✅

### Reference Shared Library
- [x] Add project reference to `AiStudio4.Tools`
- [x] Remove duplicate YouTubeSearchTool from main app
- [x] Update using statements

### Implement Adapters
- [x] Create `GeneralSettingsServiceAdapter` (maps GetDecryptedGitHubApiKey to GetDecryptedGitHubToken)
- [x] Create `StatusMessageServiceAdapter`
- [x] Create `BuiltInToolExtraPropertiesServiceAdapter`
- [x] Create `SharedToolAdapter` to wrap shared tools for main app's ITool interface
- [x] Update DI registration with dynamic assembly scanning
- [x] Exclude SharedToolAdapter from main app tool scan

### Test
- [x] Verify YouTubeSearchTool still works in main app
- [x] Check no functionality lost

## Phase 3: Implement Standalone Services ⏳

### Settings Service
- [ ] Create `StandaloneSettingsService.cs`
- [ ] Implement `IGeneralSettingsService`
- [ ] Add configuration file support
- [ ] Add DPAPI encryption for credentials

### Status Service  
- [ ] Create `NoOpStatusMessageService.cs`
- [ ] Implement `IStatusMessageService`
- [ ] Log messages instead of WebSocket

### Extra Properties Service
- [ ] Create `StandaloneExtraPropertiesService.cs`
- [ ] Implement basic property storage

## Phase 4: Add Settings UI to Standalone ⏳

### Create Settings Window
- [ ] Create `Views/SettingsWindow.xaml`
- [ ] Create `ViewModels/SettingsViewModel.cs`
- [ ] Add tabs for:
  - [ ] Credentials (YouTube API key, Azure DevOps PAT, GitHub token)
  - [ ] Paths (Working directory)
  - [ ] Tools (Enable/disable)

### Wire Up Settings
- [ ] Add menu item to open settings
- [ ] Save settings to AppData
- [ ] Load settings on startup
- [ ] Encrypt credentials with DPAPI

## Phase 5: Integrate Tool with MCP Server ⏳

### Update MCP Server Service
- [ ] Reference `AiStudio4.Tools` library
- [ ] Register service implementations in DI
- [ ] Auto-discover tools from shared library
- [ ] Replace HelloWorldTool with real tools

### Update Tool Wrapper
- [ ] Create MCP tool wrapper for shared tools
- [ ] Handle tool registration
- [ ] Map tool schemas to MCP format

### Test End-to-End
- [ ] Start OAuth server
- [ ] Start MCP server
- [ ] Connect with Claude Code
- [ ] Test YouTubeSearchTool works
- [ ] Verify API key is read from settings

## Phase 6: Add More Tools 📋

### File Operation Tools
- [ ] Move CreateNewFileTool
- [ ] Move DeleteFileTool
- [ ] Move ReadFilesTool
- [ ] Move RenameFileTool
- [ ] Test all work in both apps

### Git Tools
- [ ] Move GitStatusTool
- [ ] Move GitCommitTool
- [ ] Move GitLogTool
- [ ] Move GitBranchTool

### GitHub Tools (if time permits)
- [ ] Move GitHub tools that don't require UI
- [ ] Update credential handling

## Phase 7: Polish and Documentation 📋

### Error Handling
- [ ] Add graceful handling for missing credentials
- [ ] Improve error messages
- [ ] Add logging throughout

### Documentation
- [ ] Update README with setup instructions
- [ ] Document settings format
- [ ] Create user guide for standalone app

### Testing
- [ ] Test all tools in standalone
- [ ] Verify main app still works
- [ ] Check for any regressions

## Success Metrics

- ✅ Tools exist in shared library only
- ✅ Both apps use same tool code
- ✅ No code duplication
- ✅ Settings UI works in standalone
- ✅ Tools run headlessly via MCP
- ✅ Clean separation of concerns

## Notes

- Start with YouTubeSearchTool as proof of concept (simpler, only needs API key)
- Keep shared library dependencies minimal
- Test frequently to catch issues early
- Document any deviations from design