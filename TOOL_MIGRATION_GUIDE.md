# Tool Migration Guide: Moving Tools to Shared Library

This guide explains how to migrate tools from the main AiStudio4 project to the shared AiStudio4.Tools library, using the Azure DevOps Search Wiki tool as an example.

## Overview

The goal is to move tools to `AiStudio4.Tools` so they can be used by both:
1. The main AiStudio4 application
2. The standalone MCP server (AiStudio4.McpStandalone)

## Quick Summary: What Actually Needs to be Done

For the Azure DevOps Search Wiki tool migration, here's what you need to do:

### ✅ Already Done (Infrastructure):
- Settings service methods for PAT (`GetDecryptedAzureDevOpsPAT`, `SetAzureDevOpsPAT`)
- UI for entering Azure DevOps PAT in settings
- Base classes and interfaces in place

### ⚠️ To Do (Actual Migration):
1. **MOVE the file**: `AiStudio4\Core\Tools\AzureDevOps\AzureDevOpsSearchWikiTool.cs` → `AiStudio4.Tools\Tools\AzureDevOps\AzureDevOpsSearchWikiTool.cs` (delete from original location)
2. **Move the GUID**: Find `AZURE_DEV_OPS_SEARCH_WIKI_TOOL_GUID` in the main project's ToolGuids and add it to `AiStudio4.Tools\Models\ToolGuids.cs`
3. **Update namespace**: Change `AiStudio4.Core.Tools.AzureDevOps` to `AiStudio4.Tools.AzureDevOps`
4. **Update imports**: Change any `using AiStudio4.Core.*` to `using AiStudio4.Tools.*`
5. **Register in server**: Add 3 lines to `SimpleMcpServerService.cs` (see step 6 below)
6. **Test**: Verify it works in both apps

That's it! Most infrastructure is already in place.

## Migration Steps

### 1. Move Tool Implementation

**From:** `AiStudio4\Core\Tools\AzureDevOps\AzureDevOpsSearchWikiTool.cs`  
**To:** `AiStudio4.Tools\Tools\AzureDevOps\AzureDevOpsSearchWikiTool.cs`

**Important:** DELETE the original file after moving - don't leave a copy in the main project!

### 2. Update Namespace and Base Class

Change:
```csharp
namespace AiStudio4.Core.Tools.AzureDevOps
{
    public class AzureDevOpsSearchWikiTool : BaseToolImplementation
```

To:
```csharp
namespace AiStudio4.Tools.AzureDevOps
{
    public class AzureDevOpsSearchWikiTool : BaseToolImplementation
```

The `BaseToolImplementation` class already exists in `AiStudio4.Tools\BaseToolImplementation.cs`.

### 3. Update Service Dependencies

The tool uses these services that need to be available in both projects:

#### Required Services:
- `ILogger<T>` - Standard .NET logging (✓ Already available)
- `IGeneralSettingsService` - For retrieving API keys/PATs
- `IStatusMessageService` - For sending status updates
- `IBuiltInToolExtraPropertiesService` - For extra properties (optional)

#### API Key/PAT Requirements:

The Azure DevOps tool needs:
- **Azure DevOps Personal Access Token (PAT)**
  - Retrieved via: `_generalSettingsService.GetDecryptedAzureDevOpsPAT()`
  - Needs to be added to `StandaloneSettingsService`

### 4. Verify Settings Service Methods

✅ **Already Implemented** in `AiStudio4.McpStandalone\Services\StandaloneSettingsService.cs`:
- `GetDecryptedAzureDevOpsPAT()` - Line 49
- `SetAzureDevOpsPAT(string pat)` - Line 103
- `EncryptedAzureDevOpsPAT` property in Settings class

No changes needed here!

### 5. Verify UI for Azure DevOps PAT in Settings

✅ **Already Implemented** in `AiStudio4.McpStandalone\Views\SettingsWindow.xaml`:

```xml
<!-- Azure DevOps PAT -->
<ui:CardExpander>
    <ui:CardExpander.Header>
        <Grid>
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="Auto"/>
                <ColumnDefinition Width="*"/>
            </Grid.ColumnDefinitions>
            <ui:SymbolIcon Symbol="Globe24" Margin="0,0,12,0"/>
            <StackPanel Grid.Column="1">
                <TextBlock Text="Azure DevOps" FontWeight="SemiBold"/>
                <TextBlock Text="Configure Azure DevOps Personal Access Token" 
                          FontSize="12" 
                          Foreground="{DynamicResource TextFillColorSecondaryBrush}"/>
            </StackPanel>
        </Grid>
    </ui:CardExpander.Header>
    <ui:PasswordBox Name="AzureDevOpsPATBox" 
                   PlaceholderText="Enter Azure DevOps PAT"
                   Margin="0,8,0,0"/>
</ui:CardExpander>
```

### 6. Register the Tool in SimpleMcpServerService

In `AiStudio4.McpStandalone\Services\SimpleMcpServerService.cs`, add registration:

```csharp
// Only register tools that are enabled
var enabledTools = _settingsService.GetEnabledTools();
if (enabledTools.Contains("YouTubeSearchTool"))
{
    mcpBuilder = mcpBuilder.WithTools<AiStudio4.Tools.YouTube.YouTubeSearchTool>();
    _logger.LogInformation("Registered YouTubeSearchTool");
}
if (enabledTools.Contains("AzureDevOpsSearchWikiTool"))
{
    mcpBuilder = mcpBuilder.WithTools<AiStudio4.Tools.AzureDevOps.AzureDevOpsSearchWikiTool>();
    _logger.LogInformation("Registered AzureDevOpsSearchWikiTool");
}
```

### 7. Move Tool GUID

The tool GUID needs to be moved from the main project:

**From:** Find in main project (likely in `AiStudio4\Core\ToolGuids.cs` or similar)
```csharp
public const string AZURE_DEV_OPS_SEARCH_WIKI_TOOL_GUID = "...some-guid-value...";
```

**To:** Add to `AiStudio4.Tools\Models\ToolGuids.cs`:
```csharp
public static class ToolGuids
{
    // YouTube Tools
    public const string YOUTUBE_SEARCH_TOOL_GUID = "d1e2f3a4-b5c6-7890-1234-567890abcdef10";
    
    // Azure DevOps Tools
    public const string AZURE_DEV_OPS_SEARCH_WIKI_TOOL_GUID = "...copy-from-original...";
}
```

### 8. Update Main Project References

In the main AiStudio4 project:
1. Remove the original tool file from `Core\Tools\AzureDevOps\`
2. Ensure the project references `AiStudio4.Tools`
3. Update any direct references to use the new namespace

## Testing Checklist

After migration, test that the tool works in both environments:

### Main Application:
- [ ] Tool appears in the tool list
- [ ] Tool can be enabled/disabled
- [ ] Tool executes successfully with valid PAT
- [ ] Tool handles missing PAT gracefully

### Standalone MCP Server:
- [ ] Tool appears in Available Tools after app restart
- [ ] Tool can be toggled on/off
- [ ] Server restart works when tool selection changes
- [ ] Tool is available in Claude after reconnecting
- [ ] Tool executes successfully with valid PAT
- [ ] Tool handles missing PAT gracefully

## Summary of Required Changes

1. **File Location**: Move from `AiStudio4\Core\Tools\` to `AiStudio4.Tools\Tools\`
2. **Namespace**: Update to `AiStudio4.Tools.<ToolCategory>`
3. **Settings Service**: Add `GetDecryptedAzureDevOpsPAT()` and `SetAzureDevOpsPAT()` methods
4. **UI**: Ensure Settings window has PAT input field
5. **Registration**: Add tool registration in `SimpleMcpServerService`
6. **Tool Discovery**: Tool will be automatically discovered by the UI

## Notes

- The UI dynamically discovers tools, so no changes needed there
- Tool registration in the server is still manual (by design)
- Each tool that needs API keys/PATs requires corresponding methods in `StandaloneSettingsService`
- The `BaseToolImplementation` class handles most of the boilerplate
- Status messages work automatically through the `IStatusMessageService`

## Common Issues and Solutions

**Issue**: Tool doesn't appear in UI  
**Solution**: Ensure the tool has the `[McpServerToolType]` attribute on the class

**Issue**: Tool appears but doesn't work  
**Solution**: Check that the PAT/API key is configured in settings

**Issue**: Compilation errors after migration  
**Solution**: Check namespaces and ensure all required services are registered

**Issue**: Tool works in main app but not standalone  
**Solution**: Verify the tool is registered in `SimpleMcpServerService` and PAT methods are implemented