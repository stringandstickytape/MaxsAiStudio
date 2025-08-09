# Tool Categorization for MCP Standalone

## Summary Statistics
- **Total Tools**: 75 (all with BaseToolImplementation)
- **Can Move to Standalone**: 69 tools
- **Must Stay in Main App**: 6 tools

## Tools Safe for Standalone (69 tools)

### File Operations (10 tools)
```
✅ CreateNewFileTool
✅ DeleteFileTool
✅ ReadFilesTool
✅ ReadPartialFilesTool
✅ RenameFileTool
✅ ReplaceFileTool
✅ ModifyFilesTool
✅ ModifyFileModernTool
✅ ModifyFilesUsingMorphTool (may need special handling but can work)
✅ FindAndReplaceTool
```

### Git Operations (4 tools)
```
✅ GitStatusTool
✅ GitCommitTool
✅ GitLogTool
✅ GitBranchTool
```

### Search Operations (4 tools)
```
✅ FileSearchTool
✅ FileRegExSearchTool
✅ DirectoryTreeTool
✅ RecordMistakeTool (utility/search hybrid)
```

### Azure DevOps (18 tools) - Require PAT
```
✅ AzureDevOpsCreateOrUpdateWikiPageTool
✅ AzureDevOpsCreateOrUpdateWikiPageViaLocalTool
✅ AzureDevOpsGetCommitDiffsTool
✅ AzureDevOpsGetCommitsTool
✅ AzureDevOpsGetItemContentTool
✅ AzureDevOpsGetPipelineDefinitionsTool
✅ AzureDevOpsGetPipelineResourcesTool
✅ AzureDevOpsGetPipelineRunsTool
✅ AzureDevOpsGetPullRequestByIdTool
✅ AzureDevOpsGetPullRequestChangesTool
✅ AzureDevOpsGetPullRequestIterationsTool
✅ AzureDevOpsGetPullRequestThreadsTool
✅ AzureDevOpsGetPullRequestsTool
✅ AzureDevOpsGetRepositoriesTool
✅ AzureDevOpsGetWikiPageContentTool
✅ AzureDevOpsGetWikiPagesTool
✅ AzureDevOpsGetWorkItemCommentsTool
✅ AzureDevOpsGetWorkItemsTool
✅ AzureDevOpsGetWorkItemUpdatesTool
✅ AzureDevOpsQueryWorkItemsTool
✅ AzureDevOpsSearchWikiTool
```

### GitHub (13 tools) - Require API Key
```
✅ GitHubCreateIssueCommentTool
✅ GitHubCreateIssueTool
✅ GitHubCreatePullRequestTool
✅ GitHubGetContentTool
✅ GitHubGetIssueTool
✅ GitHubListContentsTool
✅ GitHubListIssueCommentsTool
✅ GitHubListIssuesTool
✅ GitHubListPullRequestsTool
✅ GitHubRepoInfoTool
✅ GitHubSearchCodeTool
✅ GitHubUpdateIssueTool
✅ GitHubUpdatePullRequestTool
```

### Vite/NPM (9 tools)
```
✅ CheckNodeVersionTool
✅ GetViteProjectInfoTool
✅ InstallVitePluginTool
✅ ModifyViteConfigTool
✅ NpmCreateViteTool
✅ NpmInstallTool
✅ NpmRunScriptTool
✅ OpenBrowserTool
✅ StartViteDevServerTool
```

### Web/URL (5 tools)
```
✅ RetrieveTextFromUrlTool
✅ LaunchUrlTool
✅ GoogleCustomSearchApiTool (requires API key)
✅ GeminiGoogleSearchTool (requires API key)
✅ RunDuckDuckGoSearchTool
```

### Database (1 tool)
```
✅ ReadDatabaseSchemaTool
```

### Other Utilities (3 tools)
```
✅ YouTubeSearchTool (requires API key)
✅ SentryTool
✅ RecordMistakeTool
```

## Tools That Must Stay in Main App (6 tools)

### Conversation Control (4 tools)
```
❌ ThinkAndAwaitUserInputTool - Stops processing, needs conversation context
❌ PresentResultsAndAwaitUserInputTool - Stops processing, needs conversation context  
❌ ThinkAndContinueTool - Controls conversation flow
❌ StopTool - Stops conversation processing
```

### AI Integration (1 tool)
```
❌ SecondAiOpinionTool - Requires AI service integration
```

### Complex UI (1 tool)
```
❌ WindowsSandboxTool - Complex Windows integration
```

## Credential Requirements

### Tools Requiring Credentials
- **Azure DevOps PAT**: 18 tools
- **GitHub API Key**: 13 tools
- **Google API Key**: 2 tools (GoogleCustomSearchApiTool, GeminiGoogleSearchTool)
- **YouTube API Key**: 1 tool (YouTubeSearchTool)

### Tools with No Special Requirements
- File operations: 10 tools
- Git operations: 4 tools
- Search operations: 4 tools
- Vite/NPM: 9 tools
- Basic utilities: 3 tools
- **Total**: 30 tools work without any credentials

## Implementation Priority

### Phase 1 - Core Functionality (30 tools)
Start with tools that have no external dependencies:
- File operations
- Git operations
- Search operations
- Basic utilities

### Phase 2 - NPM/Build Tools (9 tools)
Add development workflow tools:
- Vite/NPM tools
- Build automation

### Phase 3 - External Services (30 tools)
Add tools requiring credentials:
- Azure DevOps integration
- GitHub integration
- Web search tools

## Configuration Recommendations

### Default Enabled Tools
Enable by default in standalone:
- All file operations
- All Git operations  
- All search operations
- NPM/Vite tools

### Require User Configuration
Disable by default, enable after credential setup:
- Azure DevOps tools
- GitHub tools
- Google search tools
- YouTube search

### Never Available in Standalone
These tools should not appear in tool selection:
- ThinkAndAwaitUserInputTool
- PresentResultsAndAwaitUserInputTool
- ThinkAndContinueTool
- StopTool
- SecondAiOpinionTool
- WindowsSandboxTool

## Tool Registration Strategy

### In Core Library
```csharp
// Register all 69 standalone-compatible tools
services.AddTransient<ITool, CreateNewFileTool>();
services.AddTransient<ITool, GitStatusTool>();
// ... etc for all 69 tools
```

### In Main App Only
```csharp
// Register the 6 UI/conversation tools
services.AddTransient<ITool, ThinkAndAwaitUserInputTool>();
services.AddTransient<ITool, SecondAiOpinionTool>();
// ... etc for 6 tools
```

### Tool Filtering
```csharp
// Standalone app filters based on enabled list
var enabledGuids = config.EnabledToolGuids;
var availableTools = allTools.Where(t => enabledGuids.Contains(t.GetToolDefinition().Guid));
```