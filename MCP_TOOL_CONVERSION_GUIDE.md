# MCP Tool Conversion Guide

This guide explains how to convert existing ITool implementations to be compatible with Model Context Protocol (MCP) server registration.

## Overview

Each ITool class needs minimal modifications to work as an MCP tool:
1. Add `[McpServerToolType]` attribute to the class
2. Add a single MCP bridge method that calls the existing `ProcessAsync` method
3. Register the tool with the MCP server

## Required Using Statements

Add these to the top of each tool file:
```csharp
using ModelContextProtocol;
using ModelContextProtocol.Server;
using System.ComponentModel;
```

## Conversion Template

For each ITool class, follow this pattern:

### Before:
```csharp
public class YourTool : BaseToolImplementation
{
    // existing constructor and methods...
    
    public override async Task<BuiltinToolResult> ProcessAsync(string toolParameters, Dictionary<string, string> extraProperties)
    {
        // existing implementation...
    }
}
```

### After:
```csharp
[McpServerToolType]  // <- ADD THIS
public class YourTool : BaseToolImplementation
{
    // existing constructor and methods...
    
    public override async Task<BuiltinToolResult> ProcessAsync(string toolParameters, Dictionary<string, string> extraProperties)
    {
        // existing implementation...
    }
    
    // ADD THIS METHOD:
    [McpServerTool, Description("TOOL_DESCRIPTION_HERE")]
    public async Task<string> TOOL_METHOD_NAME([Description("JSON parameters for TOOL_NAME")] string parameters = "{}")
    {
        try
        {
            var result = await ProcessAsync(parameters, new Dictionary<string, string>());
            
            if (!result.WasProcessed)
            {
                return $"Tool was not processed successfully.";
            }
            
            return result.ResultMessage ?? "Tool executed successfully with no output.";
        }
        catch (Exception ex)
        {
            return $"Error executing tool: {ex.Message}";
        }
    }
}
```

## Tool-Specific Values

For each tool, you need to determine:

1. **TOOL_DESCRIPTION_HERE**: Use the `Description` from `GetToolDefinition()`
2. **TOOL_METHOD_NAME**: Use the `Name` from `GetToolDefinition()`, sanitized (no spaces, dashes, or underscores)
3. **TOOL_NAME**: Use the exact `Name` from `GetToolDefinition()` for the parameter description

## Conversion Checklist

For each tool file in `AiStudio4\Core\Tools\`:

### Step 1: Preparation
- [ ] Open the tool's `.cs` file
- [ ] Note the tool's name from `GetToolDefinition().Name`
- [ ] Note the tool's description from `GetToolDefinition().Description`
- [ ] Add required using statements at the top

### Step 2: Class Modification
- [ ] Add `[McpServerToolType]` attribute above the class declaration
- [ ] Verify the class inherits from `BaseToolImplementation`

### Step 3: Method Addition
- [ ] Add the MCP bridge method using the template above
- [ ] Replace `TOOL_DESCRIPTION_HERE` with the actual description
- [ ] Replace `TOOL_METHOD_NAME` with sanitized tool name
- [ ] Replace `TOOL_NAME` in parameter description with actual tool name

### Step 4: Validation
- [ ] Ensure method signature is exactly: `public async Task<string> MethodName([Description("...")] string parameters = "{}")`
- [ ] Verify the method calls `ProcessAsync(parameters, new Dictionary<string, string>())`
- [ ] Check error handling follows the template
- [ ] Compile to verify no syntax errors

### Step 5: Registration
- [ ] **Automatic!** Tools with `[McpServerToolType]` are automatically discovered and registered

## Complete Tool List

**Main Tools** (located in `AiStudio4\Core\Tools\`):
- [X] `CreateNewFileTool.cs` ✅ **COMPLETED**
- [X] `DeleteFileTool.cs` ✅ **COMPLETED**
- [X] `DirectoryTreeTool.cs` ✅ **COMPLETED**
- [X] `FileRegExSearch.cs` ✅ **COMPLETED**
- [X] `FileSearchTool.cs` ✅ **COMPLETED**
- [X] `FindAndReplaceTool.cs` ✅ **COMPLETED**
- [ ] `GeminiGoogleSearchTool.cs`
- [ ] `GoogleCustomSearchApiTool.cs`
- [ ] `LaunchUrlTool.cs`
- [ ] `ModifyFilesTool.cs`
- [ ] `ModifyFilesUsingMorph.cs`
- [ ] `PresentResultsAndAwaitUserInputTool.cs`
- [ ] `ReadDatabaseSchemaTool.cs`
- [x] `ReadFilesTool.cs` ✅ **COMPLETED**
- [ ] `ReadPartialFilesTool.cs`
- [ ] `RecordMistakeTool.cs`
- [ ] `RenameFileTool.cs`
- [ ] `ReplaceFileTool.cs`
- [ ] `RetrieveTextFromUrlTool.cs`
- [ ] `RunDuckDuckGoSearchTool.cs`
- [ ] `SecondAiOpinionTool.cs`
- [ ] `StopTool.cs`
- [ ] `ThinkAndAwaitUserInputTool.cs`
- [ ] `ThinkAndContinueTool.cs`
- [ ] `WindowsSandboxTool.cs`

**Azure DevOps Tools** (located in `AiStudio4\Core\Tools\AzureDevOps\`):
- [ ] `AzureDevOpsCreateOrUpdateWikiPageTool.cs`
- [ ] `AzureDevOpsGetCommitDiffsTool.cs`
- [ ] `AzureDevOpsGetCommitsTool.cs`
- [ ] `AzureDevOpsGetItemContentTool.cs`
- [ ] `AzureDevOpsGetPipelineDefinitionsTool.cs`
- [ ] `AzureDevOpsGetPipelineResourcesTool.cs`
- [ ] `AzureDevOpsGetPipelineRunsTool.cs`
- [ ] `AzureDevOpsGetPullRequestByIdTool.cs`
- [ ] `AzureDevOpsGetPullRequestChangesTool.cs`
- [ ] `AzureDevOpsGetPullRequestIterationsTool.cs`
- [ ] `AzureDevOpsGetPullRequestsTool.cs`
- [ ] `AzureDevOpsGetPullRequestThreadsTool.cs`
- [ ] `AzureDevOpsGetRepositoriesTool.cs`
- [ ] `AzureDevOpsGetWikiPageContentTool.cs`
- [ ] `AzureDevOpsGetWikiPagesTool.cs`
- [ ] `AzureDevOpsGetWorkItemCommentsTool.cs`
- [ ] `AzureDevOpsGetWorkItemsTool.cs`
- [ ] `AzureDevOpsGetWorkItemUpdatesTool.cs`
- [ ] `AzureDevOpsQueryWorkItemsTool.cs`
- [ ] `AzureDevOpsSearchWikiTool.cs`

**Git Tools** (located in `AiStudio4\Core\Tools\Git\`):
- [ ] `GitBranchTool.cs`
- [ ] `GitCommitTool.cs`
- [ ] `GitLogTool.cs`
- [ ] `GitStatusTool.cs`

**GitHub Tools** (located in `AiStudio4\Core\Tools\GitHub\`):
- [ ] `GitHubCreateIssueCommentTool.cs`
- [ ] `GitHubCreateIssueTool.cs`
- [ ] `GitHubCreatePullRequestTool.cs`
- [ ] `GitHubGetContentTool.cs`
- [ ] `GitHubGetIssueTool.cs`
- [ ] `GitHubListContentsTool.cs`
- [ ] `GitHubListIssueCommentsTool.cs`
- [ ] `GitHubListIssuesTool.cs`
- [ ] `GitHubListPullRequestsTool.cs`
- [ ] `GitHubRepoInfoTool.cs`
- [ ] `GitHubSearchCodeTool.cs`
- [ ] `GitHubUpdateIssueTool.cs`
- [ ] `GitHubUpdatePullRequestTool.cs`

**Sentry Tools** (located in `AiStudio4\Core\Tools\Sentry\`):
- [ ] `SentryTool.cs`

**Vite Tools** (located in `AiStudio4\Core\Tools\Vite\`):
- [ ] `CheckNodeVersionTool.cs`
- [ ] `GetViteProjectInfoTool.cs`
- [ ] `InstallVitePluginTool.cs`
- [ ] `ModifyViteConfigTool.cs`
- [ ] `NpmCreateViteTool.cs`
- [ ] `NpmInstallTool.cs`
- [ ] `NpmRunScriptTool.cs`
- [ ] `OpenBrowserTool.cs`
- [ ] `StartViteDevServerTool.cs`

**YouTube Tools** (located in `AiStudio4\Core\Tools\YouTube\`):
- [ ] `YouTubeSearchTool.cs`

**Total: ~70+ tools to convert (7 completed, 63+ remaining)**

## MCP Server Registration 

**Fully automatic!** The MCP server automatically discovers and registers all ITool classes that have the `[McpServerToolType]` attribute using reflection.

The server will log during startup:
- `Found X ITool classes with MCP attributes` - showing how many tools were discovered
- `Will register MCP tool: ToolName` - for each tool found
- `MCP Registration chain: ...` - showing the equivalent manual registration code
- `Successfully registered MCP tool: ToolName` - for each successfully registered tool

If a tool doesn't appear, check that:
1. The class has `[McpServerToolType]` attribute
2. The class implements `ITool`  
3. The class is not abstract
4. No compilation errors exist
5. The MCP bridge method has the correct signature

**How it works:**
1. Scans `typeof(ITool).Assembly` for classes with `[McpServerToolType]`
2. Uses reflection to call `WithTools<T>()` for each discovered tool
3. Automatically registers them with the MCP server

## Example Conversion

### ReadFilesTool.cs

**Tool Definition Values:**
- Name: "ReadFiles"
- Description: "Read the contents of one or multiple files."

**Required Changes:**
```csharp
[McpServerToolType]
public class ReadFilesTool : BaseToolImplementation
{
    // ... existing code ...

    [McpServerTool, Description("Read the contents of one or multiple files.")]
    public async Task<string> ReadFiles([Description("JSON parameters for ReadFiles")] string parameters = "{}")
    {
        try
        {
            var result = await ProcessAsync(parameters, new Dictionary<string, string>());
            
            if (!result.WasProcessed)
            {
                return $"Tool was not processed successfully.";
            }
            
            return result.ResultMessage ?? "Tool executed successfully with no output.";
        }
        catch (Exception ex)
        {
            return $"Error executing tool: {ex.Message}";
        }
    }
}
```

## Testing

After conversion:
1. Build the project to check for compilation errors
2. Start the MCP server
3. Connect with an MCP client
4. Verify the tool appears in the tools list
5. Test tool execution with sample parameters

## Notes

- The existing `ProcessAsync` method remains unchanged
- The existing `GetToolDefinition` method remains unchanged
- Only add the MCP bridge method and class attribute
- Each tool becomes individually callable via MCP
- Tool names in MCP will match the `Name` from `GetToolDefinition()`
- Parameter schemas are preserved from the existing tool definitions