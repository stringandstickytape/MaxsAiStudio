# MCP Shared Tools Implementation Checklist

## Current Status
Starting implementation of shared tools library for MCP Standalone server.

## Phase 1: Create Shared Library Structure ⏳

### Setup
- [ ] Create new Class Library project `AiStudio4.Tools` (.NET 9.0)
- [ ] Add project to solution
- [ ] Configure project properties and dependencies

### Create Minimal Interfaces
- [ ] Create `Interfaces` folder
- [ ] Create `IGeneralSettingsService.cs` with minimal methods:
  - [ ] `GetDecryptedAzureDevOpsPAT()`
  - [ ] `GetDecryptedGitHubToken()`
  - [ ] `GetProjectPath()`
- [ ] Create `IStatusMessageService.cs`
- [ ] Create `ITool.cs`
- [ ] Create `IBuiltInToolExtraPropertiesService.cs`

### Copy Required Models
- [ ] Create `Models` folder
- [ ] Copy `Tool.cs` from main app
- [ ] Copy `BuiltinToolResult.cs`
- [ ] Copy `ContentBlock.cs` and related enums
- [ ] Copy `ToolGuids.cs`
- [ ] Adjust namespaces to `AiStudio4.Tools.Models`

### Copy Base Implementation
- [ ] Copy `BaseToolImplementation.cs`
- [ ] Remove UI-specific code
- [ ] Update to use minimal interfaces
- [ ] Adjust namespace to `AiStudio4.Tools`

### Add First Tool
- [ ] Create `Tools/YouTube` folder
- [ ] Copy `YouTubeSearchTool.cs`
- [ ] Update namespace to `AiStudio4.Tools.YouTube`
- [ ] Verify it compiles with minimal interfaces

## Phase 2: Update Main App ⏳

### Reference Shared Library
- [ ] Add project reference to `AiStudio4.Tools`
- [ ] Remove duplicate models/interfaces
- [ ] Update using statements

### Implement Adapters
- [ ] Create adapter for existing GeneralSettingsService to implement minimal interface
- [ ] Create adapter for StatusMessageService
- [ ] Update DI registration

### Test
- [ ] Verify YouTubeSearchTool still works in main app
- [ ] Check no functionality lost

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