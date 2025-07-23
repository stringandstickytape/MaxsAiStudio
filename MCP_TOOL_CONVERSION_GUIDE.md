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
- [X] `GeminiGoogleSearchTool.cs` ✅ **COMPLETED**
- [X] `GoogleCustomSearchApiTool.cs` ✅ **COMPLETED**
- [X] `LaunchUrlTool.cs` ✅ **COMPLETED**
- [X] `ModifyFilesTool.cs` ✅ **COMPLETED**
- [X] `ModifyFilesUsingMorph.cs` ✅ **COMPLETED**
- [X] `PresentResultsAndAwaitUserInputTool.cs` ✅ **COMPLETED**
- [X] `ReadDatabaseSchemaTool.cs` ✅ **COMPLETED**
- [x] `ReadFilesTool.cs` ✅ **COMPLETED**
- [x] `ReadPartialFilesTool.cs` ✅ **COMPLETED**
- [x] `RecordMistakeTool.cs` ✅ **COMPLETED**
- [x] `RenameFileTool.cs` ✅ **COMPLETED**
- [x] `ReplaceFileTool.cs` ✅ **COMPLETED**
- [x] `RetrieveTextFromUrlTool.cs` ✅ **COMPLETED**
- [x] `RunDuckDuckGoSearchTool.cs` ✅ **COMPLETED**
- [x] `SecondAiOpinionTool.cs` ✅ **COMPLETED**
- [x] `StopTool.cs` ✅ **COMPLETED**
- [x] `ThinkAndAwaitUserInputTool.cs` ✅ **COMPLETED**
- [x] `ThinkAndContinueTool.cs` ✅ **COMPLETED**
- [x] `WindowsSandboxTool.cs` ✅ **COMPLETED**

**Azure DevOps Tools** (located in `AiStudio4\Core\Tools\AzureDevOps\`):
- [X] `AzureDevOpsCreateOrUpdateWikiPageTool.cs` ✅ **COMPLETED**
- [X] `AzureDevOpsGetCommitDiffsTool.cs` ✅ **COMPLETED**
- [X] `AzureDevOpsGetCommitsTool.cs` ✅ **COMPLETED**
- [X] `AzureDevOpsGetItemContentTool.cs` ✅ **COMPLETED**
- [X] `AzureDevOpsGetPipelineDefinitionsTool.cs` ✅ **COMPLETED**
- [X] `AzureDevOpsGetPipelineResourcesTool.cs` ✅ **COMPLETED**
- [X] `AzureDevOpsGetPipelineRunsTool.cs` ✅ **COMPLETED**
- [X] `AzureDevOpsGetPullRequestByIdTool.cs` ✅ **COMPLETED**
- [X] `AzureDevOpsGetPullRequestChangesTool.cs` ✅ **COMPLETED**
- [X] `AzureDevOpsGetPullRequestIterationsTool.cs` ✅ **COMPLETED**
- [X] `AzureDevOpsGetPullRequestsTool.cs` ✅ **COMPLETED**
- [X] `AzureDevOpsGetPullRequestThreadsTool.cs` ✅ **COMPLETED**
- [X] `AzureDevOpsGetRepositoriesTool.cs` ✅ **COMPLETED**
- [X] `AzureDevOpsGetWikiPageContentTool.cs` ✅ **COMPLETED**
- [X] `AzureDevOpsGetWikiPagesTool.cs` ✅ **COMPLETED**
- [X] `AzureDevOpsGetWorkItemCommentsTool.cs` ✅ **COMPLETED**
- [X] `AzureDevOpsGetWorkItemsTool.cs` ✅ **COMPLETED**
- [X] `AzureDevOpsGetWorkItemUpdatesTool.cs` ✅ **COMPLETED**
- [X] `AzureDevOpsQueryWorkItemsTool.cs` ✅ **COMPLETED**
- [X] `AzureDevOpsSearchWikiTool.cs` ✅ **COMPLETED**

**Git Tools** (located in `AiStudio4\Core\Tools\Git\`):
- [X] `GitBranchTool.cs` ✅ **COMPLETED**
- [X] `GitCommitTool.cs` ✅ **COMPLETED**
- [X] `GitLogTool.cs` ✅ **COMPLETED**
- [X] `GitStatusTool.cs` ✅ **COMPLETED**

**GitHub Tools** (located in `AiStudio4\Core\Tools\GitHub\`):
- [X] `GitHubCreateIssueCommentTool.cs` ✅ **COMPLETED**
- [X] `GitHubCreateIssueTool.cs` ✅ **COMPLETED**
- [X] `GitHubCreatePullRequestTool.cs` ✅ **COMPLETED**
- [X] `GitHubGetContentTool.cs` ✅ **COMPLETED**
- [X] `GitHubGetIssueTool.cs` ✅ **COMPLETED**
- [X] `GitHubListContentsTool.cs` ✅ **COMPLETED**
- [X] `GitHubListIssueCommentsTool.cs` ✅ **COMPLETED**
- [X] `GitHubListIssuesTool.cs` ✅ **COMPLETED**
- [X] `GitHubListPullRequestsTool.cs` ✅ **COMPLETED**
- [X] `GitHubRepoInfoTool.cs` ✅ **COMPLETED**
- [X] `GitHubSearchCodeTool.cs` ✅ **COMPLETED**
- [X] `GitHubUpdateIssueTool.cs` ✅ **COMPLETED**
- [X] `GitHubUpdatePullRequestTool.cs` ✅ **COMPLETED**

**Sentry Tools** (located in `AiStudio4\Core\Tools\Sentry\`):
- [X] `SentryTool.cs` ✅ **COMPLETED**

**Vite Tools** (located in `AiStudio4\Core\Tools\Vite\`):
- [X] `CheckNodeVersionTool.cs` ✅ **COMPLETED**
- [X] `GetViteProjectInfoTool.cs` ✅ **COMPLETED**
- [X] `InstallVitePluginTool.cs` ✅ **COMPLETED**
- [X] `ModifyViteConfigTool.cs` ✅ **COMPLETED**
- [X] `NpmCreateViteTool.cs` ✅ **COMPLETED**
- [X] `NpmInstallTool.cs` ✅ **COMPLETED**
- [X] `NpmRunScriptTool.cs` ✅ **COMPLETED**
- [X] `OpenBrowserTool.cs` ✅ **COMPLETED**
- [X] `StartViteDevServerTool.cs` ✅ **COMPLETED**

**YouTube Tools** (located in `AiStudio4\Core\Tools\YouTube\`):
- [X] `YouTubeSearchTool.cs` ✅ **COMPLETED**

**Total: 70+ tools converted - ALL TOOLS COMPLETED! 🎉**

## 🎉 Conversion Complete!

**All ITool implementations have been successfully converted to support MCP (Model Context Protocol)!**

### Summary of Completed Work:
- **Main Tools**: 25 tools converted ✅
- **Azure DevOps Tools**: 19 tools converted ✅  
- **Git Tools**: 4 tools converted ✅
- **GitHub Tools**: 13 tools converted ✅
- **Sentry Tools**: 1 tool converted ✅
- **Vite Tools**: 9 tools converted ✅
- **YouTube Tools**: 1 tool converted ✅

### Total: 72 tools successfully converted to MCP compatibility

Each tool now supports both:
1. **Legacy interface** - Original `ProcessAsync` method for backward compatibility
2. **MCP interface** - New MCP bridge method for Model Context Protocol integration

All tools are automatically discovered and registered with the MCP server using the `[McpServerToolType]` attribute.

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