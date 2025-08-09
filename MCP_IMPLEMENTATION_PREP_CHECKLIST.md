# MCP Implementation Preparation Checklist

## Current State Analysis

### Tool Inventory
- **Total Tools with MCP Attributes**: 76 tools
- **Tools Already MCP-Ready**: All tools have McpServerToolType attribute
- **Tool Categories Found**:
  - Azure DevOps: 18 tools
  - GitHub: 13 tools  
  - Git: 4 tools
  - File Operations: 10 tools
  - Vite/NPM: 9 tools
  - Search: 4 tools
  - Web/URL: 3 tools
  - Utility: 15 tools

### Critical Dependencies to Document

#### Services Used by Tools
- [ ] Document which tools use ISecondaryAiService (SecondAiOpinionTool)
- [ ] Document which tools use conversation context (ThinkAndAwait, PresentResults, Stop)
- [ ] Document which tools require UI dialogs
- [ ] Document which tools need credentials (Azure DevOps, GitHub, Google)

## Pre-Implementation Checklist

### 1. Backup and Version Control
- [ ] Create full backup of current solution
- [ ] Ensure all current changes are committed
- [ ] Create new feature branch `feature/mcp-standalone`
- [ ] Tag current version as `pre-mcp-extraction`
- [ ] Document current git hash for rollback

### 2. Test Current Functionality
- [ ] Build main app in Debug mode - verify success
- [ ] Build main app in Release mode - verify success
- [ ] Run main app and test:
  - [ ] Basic chat functionality
  - [ ] File operations (create, read, modify)
  - [ ] Git operations
  - [ ] Azure DevOps tools (if configured)
  - [ ] GitHub tools (if configured)
  - [ ] MCP server start/stop
  - [ ] Tool extra properties configuration

### 3. Document Current Configuration
- [ ] Location of settings files:
  ```
  Current: %APPDATA%/AiStudio4/
  - settings.json
  - builtinToolExtraProps.json
  - mcpServers.json
  ```
- [ ] Document current DI registration order
- [ ] List all external dependencies (NuGet packages)
- [ ] Note any custom build steps

### 4. Prepare Development Environment
- [ ] Ensure .NET 9.0 SDK installed
- [ ] Verify Visual Studio 2022 or VS Code with C# extensions
- [ ] Install git command line tools
- [ ] Set up test Azure DevOps PAT (if testing those tools)
- [ ] Set up test GitHub token (if testing those tools)

### 5. Create Tool Classification List

#### Tools Safe to Move to Core (No UI/AI Dependencies)
```
File Operations:
- CreateNewFileTool
- DeleteFileTool  
- ReadFilesTool
- ReadPartialFilesTool
- RenameFileTool
- ReplaceFileTool
- ModifyFilesTool
- ModifyFileModernTool
- FindAndReplaceTool

Git Operations:
- GitStatusTool
- GitCommitTool
- GitLogTool
- GitBranchTool

Search:
- FileSearchTool
- FileRegExSearchTool
- DirectoryTreeTool

Azure DevOps (All 18):
- All Azure DevOps tools (require PAT)

GitHub (All 13):
- All GitHub tools (require API key)

Web/URL:
- RetrieveTextFromUrlTool
- LaunchUrlTool
- GoogleCustomSearchApiTool
- GeminiGoogleSearchTool
- RunDuckDuckGoSearchTool

Database:
- ReadDatabaseSchemaTool

Vite/NPM (All 9):
- All Vite/NPM tools

Utility:
- RecordMistakeTool
- YouTubeSearchTool
- SentryTool
```

#### Tools That Must Stay in Main App
```
UI/Conversation Dependent:
- ThinkAndAwaitUserInputTool (stops processing)
- PresentResultsAndAwaitUserInputTool (stops processing)
- ThinkAndContinueTool (conversation flow)
- StopTool (conversation control)
- SecondAiOpinionTool (needs AI service)
- WindowsSandboxTool (complex UI)
- ModifyFilesUsingMorphTool (may need special handling)
```

### 6. Risk Assessment

#### High Risk Areas
- [ ] OAuth server extraction - complex authentication flow
- [ ] Credential management - security critical
- [ ] Service dependencies - many interconnected services
- [ ] Tool discovery mechanism - reflection-based

#### Mitigation Strategies
- [ ] Test OAuth server in isolation first
- [ ] Create credential encryption tests
- [ ] Map all service dependencies before moving
- [ ] Create tool discovery unit tests

### 7. Testing Strategy

#### Unit Tests Needed
- [ ] Core library tool tests
- [ ] Service abstraction tests
- [ ] Configuration service tests
- [ ] Credential encryption tests

#### Integration Tests Needed
- [ ] MCP server communication tests
- [ ] OAuth flow tests
- [ ] Tool execution via MCP tests
- [ ] Cross-app configuration tests

#### Manual Test Scenarios
- [ ] Tool execution in both apps
- [ ] Credential management workflow
- [ ] Server start/stop/restart
- [ ] Configuration changes persistence
- [ ] Error handling and recovery

### 8. Documentation Preparation

#### Documentation to Create/Update
- [ ] Architecture diagram showing shared core
- [ ] Service dependency graph
- [ ] Tool compatibility matrix
- [ ] Configuration file specifications
- [ ] API documentation for core library
- [ ] Migration guide for users
- [ ] Developer setup guide

### 9. Build and Deployment Preparation

#### Build System Changes Needed
- [ ] Update solution file structure
- [ ] Modify build scripts for multiple projects
- [ ] Create package manifests for both apps
- [ ] Set up build order dependencies
- [ ] Configure output paths

#### Deployment Artifacts
- [ ] Main app executable + dependencies
- [ ] Standalone MCP app executable + dependencies  
- [ ] Shared core library DLL
- [ ] Configuration file templates
- [ ] Documentation package

### 10. Communication Plan

#### Stakeholder Updates
- [ ] Document breaking changes (if any)
- [ ] Create feature announcement
- [ ] Prepare FAQ for users
- [ ] Plan rollout strategy

## Decision Points to Resolve

1. **Naming Convention**
   - [ ] Confirm `AiStudio4.Core` for shared library
   - [ ] Confirm `AiStudio4.McpServer` for standalone app
   - [ ] Decide on namespace strategy

2. **Configuration Strategy**
   - [ ] Confirm separate config folders
   - [ ] Decide on config migration approach
   - [ ] Plan for config version management

3. **Tool Registration**
   - [ ] Confirm attribute-based discovery
   - [ ] Decide on tool filtering mechanism
   - [ ] Plan for tool versioning

4. **Error Handling**
   - [ ] Define error codes/categories
   - [ ] Decide on logging levels
   - [ ] Plan for error recovery

## Final Pre-Flight Checks

Before starting Phase 1:
- [ ] All current work committed and pushed
- [ ] Feature branch created
- [ ] Main app tests passing
- [ ] Development environment ready
- [ ] Documentation templates created
- [ ] Team/stakeholders notified
- [ ] Rollback plan confirmed
- [ ] Success metrics defined

## Go/No-Go Decision

### Required for GO:
- [ ] All preparation items complete
- [ ] Risk mitigation strategies in place
- [ ] Testing strategy defined
- [ ] Rollback plan tested
- [ ] Documentation ready

### Reasons for NO-GO:
- [ ] Uncommitted changes in main branch
- [ ] Failing tests in current build
- [ ] Missing critical documentation
- [ ] Unresolved decision points
- [ ] High-risk dependencies not mitigated

## Notes
- Keep this checklist updated throughout implementation
- Check off items as completed
- Document any issues or changes to plan
- Review before each phase transition