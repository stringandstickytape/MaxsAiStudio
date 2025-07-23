# MCP Tool Extra Properties Integration Guide

## Overview
This guide explains how to update existing MCP tools to automatically use extra properties without manual service instantiation. The new pattern eliminates boilerplate code and provides automatic extra properties injection.

## What Was Changed

### 1. Enhanced BaseToolImplementation
The `BaseToolImplementation` class now includes:
- Optional `IBuiltInToolExtraPropertiesService` injection in constructor
- `GetExtraProperties()` method with automatic fallback
- `ExecuteWithExtraProperties()` helper method for MCP tools

### 2. Automatic Extra Properties Flow
- Tools no longer need to manually instantiate `IBuiltInToolExtraPropertiesService`
- Extra properties are automatically fetched using the correct tool name format
- Fallback mechanism works in both DI and non-DI contexts

## Required Changes

### Pattern Identification
Look for MCP tool methods that follow this pattern:

```csharp
[McpServerTool, Description("Tool description")]
public async Task<string> ToolMethodName([Description("JSON parameters")] string parameters = "{}")
{
    try
    {
        // OPTION A: Manual service instantiation (needs updating)
        var service = (IBuiltInToolExtraPropertiesService)Activator.CreateInstance(typeof(BuiltInToolExtraPropertiesService));
        var latestExtraProps = service.GetExtraProperties("toolname");
        var result = await ProcessAsync(parameters, latestExtraProps);
        
        // OPTION B: Empty dictionary (needs updating)
        var result = await ProcessAsync(parameters, new Dictionary<string, string>());
        
        if (!result.WasProcessed)
        {
            return "Tool was not processed successfully.";
        }
        
        return result.ResultMessage ?? "Tool executed successfully with no output.";
    }
    catch (Exception ex)
    {
        return $"Error executing tool: {ex.Message}";
    }
}
```

### Required Replacement
Replace the entire method body with:

```csharp
[McpServerTool, Description("Tool description")]
public async Task<string> ToolMethodName([Description("JSON parameters")] string parameters = "{}")
{
    return await ExecuteWithExtraProperties(parameters);
}
```

## Step-by-Step Instructions

### Step 1: Find All MCP Tool Methods
Search for files containing `[McpServerTool` to identify tools that need updating.

**Search Command:**
```
grep -r "McpServerTool.*Description" --include="*.cs"
```

### Step 2: Update Each Tool Method
For each file found:

1. **Locate the MCP method** (marked with `[McpServerTool, Description(...)]`)
2. **Identify the pattern**:
   - Look for `ProcessAsync(parameters, new Dictionary<string, string>())` 
   - Look for manual `IBuiltInToolExtraPropertiesService` instantiation
   - Look for try/catch blocks with error handling
3. **Replace the entire method body** with: `return await ExecuteWithExtraProperties(parameters);`

### Step 3: Example Transformations

#### Before (Manual Service Instantiation):
```csharp
[McpServerTool, Description("Gets a second opinion from another AI model")]
public async Task<string> SecondAiOpinion([Description("JSON parameters for SecondAiOpinion")] string parameters = "{}")
{
    try
    {
        var service = (IBuiltInToolExtraPropertiesService)Activator.CreateInstance(typeof(BuiltInToolExtraPropertiesService));
        var latestExtraProps = service.GetExtraProperties("secondAiOpinion");
        var result = await ProcessAsync(parameters, latestExtraProps);
        
        if (!result.WasProcessed)
        {
            return "Tool was not processed successfully.";
        }
        
        return result.ResultMessage ?? "Tool executed successfully with no output.";
    }
    catch (Exception ex)
    {
        return $"Error executing tool: {ex.Message}";
    }
}
```

#### After:
```csharp
[McpServerTool, Description("Gets a second opinion from another AI model")]
public async Task<string> SecondAiOpinion([Description("JSON parameters for SecondAiOpinion")] string parameters = "{}")
{
    return await ExecuteWithExtraProperties(parameters);
}
```

#### Before (Empty Dictionary):
```csharp
[McpServerTool, Description("Searches YouTube for videos")]
public async Task<string> YouTubeSearch([Description("JSON parameters for YouTubeSearch")] string parameters = "{}")
{
    try
    {
        var result = await ProcessAsync(parameters, new Dictionary<string, string>());
        
        if (!result.WasProcessed)
        {
            return "Tool was not processed successfully.";
        }
        
        return result.ResultMessage ?? "Tool executed successfully with no output.";
    }
    catch (Exception ex)
    {
        return $"Error executing YouTubeSearch: {ex.Message}";
    }
}
```

#### After:
```csharp
[McpServerTool, Description("Searches YouTube for videos")]
public async Task<string> YouTubeSearch([Description("JSON parameters for YouTubeSearch")] string parameters = "{}")
{
    return await ExecuteWithExtraProperties(parameters);
}
```

## Tools That Need Updating

Based on the search results, these files contain MCP tools that need updating:

### Core Tools
- [ ] `AiStudio4\Core\Tools\YouTube\YouTubeSearchTool.cs`
- [ ] `AiStudio4\Core\Tools\WindowsSandboxTool.cs`
- [ ] `AiStudio4\Core\Tools\ReadPartialFilesTool.cs`
- [ ] `AiStudio4\Core\Tools\GeminiGoogleSearchTool.cs`
- [ ] `AiStudio4\Core\Tools\ReadFilesTool.cs`
- [ ] `AiStudio4\Core\Tools\LaunchUrlTool.cs`
- [ ] `AiStudio4\Core\Tools\FileRegExSearchTool.cs`
- [ ] `AiStudio4\Core\Tools\ModifyFilesTool.cs`
- [ ] `AiStudio4\Core\Tools\GoogleCustomSearchApiTool.cs`
- [ ] `AiStudio4\Core\Tools\CreateNewFilesTool.cs`
- [ ] `AiStudio4\Core\Tools\ListFilesTool.cs`
- [ ] `AiStudio4\Core\Tools\GitCloneTool.cs`
- [ ] `AiStudio4\Core\Tools\GitCommitTool.cs`
- [ ] `AiStudio4\Core\Tools\GitDiffTool.cs`
- [ ] `AiStudio4\Core\Tools\GitLogTool.cs`
- [ ] `AiStudio4\Core\Tools\GitPushTool.cs`
- [ ] `AiStudio4\Core\Tools\GitStatusTool.cs`
- [ ] `AiStudio4\Core\Tools\PresentResultsAndAwaitUserInputTool.cs`
- [x] `AiStudio4\Core\Tools\SecondAiOpinionTool.cs` ✅ (Already updated)

### Azure DevOps Tools
- [ ] `AiStudio4\Core\Tools\AzureDevOps\AzureDevOpsGetPullRequestByIdTool.cs`
- [ ] `AiStudio4\Core\Tools\AzureDevOps\AzureDevOpsGetPipelineRunsTool.cs`
- [ ] `AiStudio4\Core\Tools\AzureDevOps\AzureDevOpsGetRepositoriesTool.cs`
- [ ] `AiStudio4\Core\Tools\AzureDevOps\AzureDevOpsGetPipelineResourcesTool.cs`
- [ ] `AiStudio4\Core\Tools\AzureDevOps\AzureDevOpsSearchWikiTool.cs`
- [ ] `AiStudio4\Core\Tools\AzureDevOps\AzureDevOpsGetPullRequestThreadsTool.cs`
- [ ] `AiStudio4\Core\Tools\AzureDevOps\AzureDevOpsGetPipelineDefinitionsTool.cs`
- [ ] `AiStudio4\Core\Tools\AzureDevOps\AzureDevOpsQueryWorkItemsTool.cs`
- [ ] `AiStudio4\Core\Tools\AzureDevOps\AzureDevOpsGetCommitDiffsTool.cs`
- [ ] `AiStudio4\Core\Tools\AzureDevOps\AzureDevOpsGetPullRequestsTool.cs`
- [ ] `AiStudio4\Core\Tools\AzureDevOps\AzureDevOpsGetItemContentTool.cs`
- [ ] `AiStudio4\Core\Tools\AzureDevOps\AzureDevOpsGetCommitsTool.cs`
- [ ] `AiStudio4\Core\Tools\AzureDevOps\AzureDevOpsGetPullRequestIterationsTool.cs`
- [ ] `AiStudio4\Core\Tools\AzureDevOps\AzureDevOpsGetWorkItemUpdatesTool.cs`
- [ ] `AiStudio4\Core\Tools\AzureDevOps\AzureDevOpsCreateOrUpdateWikiPageTool.cs`
- [ ] `AiStudio4\Core\Tools\AzureDevOps\AzureDevOpsGetWikiPagesTool.cs`
- [ ] `AiStudio4\Core\Tools\AzureDevOps\AzureDevOpsGetWikiPageContentTool.cs`
- [ ] `AiStudio4\Core\Tools\AzureDevOps\AzureDevOpsGetPullRequestChangesTool.cs`
- [ ] `AiStudio4\Core\Tools\AzureDevOps\AzureDevOpsGetWorkItemCommentsTool.cs`
- [ ] `AiStudio4\Core\Tools\AzureDevOps\AzureDevOpsGetWorkItemsTool.cs`

### Vite/NPM Tools
- [ ] `AiStudio4\Core\Tools\Vite\StartViteDevServerTool.cs`
- [ ] `AiStudio4\Core\Tools\Vite\OpenBrowserTool.cs`
- [ ] `AiStudio4\Core\Tools\Vite\NpmRunScriptTool.cs`
- [ ] `AiStudio4\Core\Tools\Vite\NpmInstallTool.cs`

### GitHub Tools
- [ ] `AiStudio4\Core\Tools\GitHub\GitHubSearchRepositoriesTool.cs`
- [ ] `AiStudio4\Core\Tools\GitHub\GitHubGetRepositoryInfoTool.cs`
- [ ] `AiStudio4\Core\Tools\GitHub\GitHubSearchIssuesTool.cs`
- [ ] `AiStudio4\Core\Tools\GitHub\GitHubGetFileContentTool.cs`
- [ ] `AiStudio4\Core\Tools\GitHub\GitHubGetPullRequestsTool.cs`
- [ ] `AiStudio4\Core\Tools\GitHub\GitHubGetCommitsTool.cs`
- [ ] `AiStudio4\Core\Tools\GitHub\GitHubGetIssuesTool.cs`

### Protected MCP Server Tools (may not need updating)
- [ ] `ProtectedMCPServer\Tools\WeatherTools.cs` (GetAlerts, GetForecast)
- [ ] `AiStudio4\Services\ProtectedMcpServer\WeatherTools.cs` (GetAlerts, GetForecast)

**Total Tools to Update: ~50+ tools across multiple categories**

## Verification Steps

After making changes:

1. **Compilation Check**: Ensure all modified files compile without errors
2. **Method Signature**: Verify the method signature remains unchanged (only the body changes)
3. **Attribute Preservation**: Ensure `[McpServerTool, Description(...)]` attributes are preserved
4. **Parameter Names**: Ensure parameter names and descriptions are preserved

## Benefits of This Change

1. **Automatic Extra Properties**: Tools automatically get the latest extra properties
2. **Reduced Boilerplate**: From ~15 lines to 1 line per MCP method
3. **Consistent Error Handling**: Centralized in the base class
4. **Maintainability**: Single point of change for MCP execution logic
5. **Future Proof**: New tools automatically benefit from the pattern

## Important Notes

- **Only change MCP tool methods**: Don't modify the main `ProcessAsync` method
- **Preserve attributes**: Keep all `[McpServerTool, Description(...)]` attributes exactly as they are
- **Keep parameter names**: Don't change parameter names or descriptions
- **Test thoroughly**: Verify tools still work with their configured extra properties

## Troubleshooting

If a tool doesn't work after the change:
1. Check that the tool inherits from `BaseToolImplementation`
2. Verify the tool's `ProcessAsync` method handles extra properties correctly
3. Ensure the tool name in extra properties storage matches the expected format (first letter lowercase)
4. Check that required extra properties are configured in the tool's settings

## Technical Details

The `ExecuteWithExtraProperties` method:
1. Calls `GetExtraProperties()` to fetch the latest extra properties for the tool
2. Passes them to the tool's `ProcessAsync` method
3. Handles success/failure logic consistently
4. Provides centralized error handling
5. Returns the appropriate string response for MCP

This eliminates the need for manual service instantiation and provides a clean, consistent pattern for all MCP tools.