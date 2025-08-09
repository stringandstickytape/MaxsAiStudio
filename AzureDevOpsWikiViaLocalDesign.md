# Azure DevOps Wiki Via Local Git Repository - Design Document

## Overview

This document outlines the design for a new set of tools that enable efficient wiki operations by using local git repositories instead of REST API calls. This approach eliminates the inefficiency of sending complete page content for every update.

## Problem Statement

The current `AzureDevOpsCreateOrUpdateWikiPageTool` requires sending the complete page content via REST API for every update, even for minor changes. This is inefficient for:
- Small edits to large pages
- Batch updates across multiple pages  
- Find-and-replace operations
- Maintaining change history locally

## Solution Architecture

### Core Concept

Clone Azure DevOps wiki repositories locally and perform git operations directly, using the existing PAT for authentication.

### Repository Storage Structure

```
%APPDATA%/AiStudio4/wikis/
└── {organization}/
    └── {project}/
        └── {wiki_name}/  # Using wiki name instead of ID
            ├── .git/
            ├── .order files
            └── wiki pages (.md files)
```

### Authentication

- Retrieve PAT using `_generalSettingsService.GetDecryptedAzureDevOpsPAT()`
- Format for git operations: `https://{PAT}@dev.azure.com/{org}/{project}/_git/{wiki}.wiki`
- Set credentials in git command environment or URL encoding

## Primary Tool: AzureDevOpsCreateOrUpdateWikiPageViaLocalTool

### Purpose
Create or update wiki pages using local git repository operations for efficient partial updates.

### Parameters
```json
{
  "organization": "string",
  "project": "string", 
  "wiki_name": "string",  // Changed from wiki_id
  "path": "string",
  "changes": [  // Aligned with ModifyFileModernTool pattern
    {
      "oldContent": "string",
      "newContent": "string",
      "description": "string"
    }
  ],
  "comment": "string (optional)",
  "auto_pull": true // default true
}
```

### Process Flow

1. **Repository Management**
   - Check if local repo exists at `PathHelper.GetProfileSubPath("wikis", organization, project, wiki_name)`
   - If not exists: Clone repository using ProcessStartInfo (shell out to git)
   - If exists and `auto_pull`: Pull latest changes

2. **Apply Edits**
   - Call ModifyFileModernTool internally with the file path and changes
   - Leverage existing line-based matching and atomic operations
   - All changes must succeed or entire operation is reverted

3. **Commit and Push (Atomic)**
   - Every tool call that modifies the wiki MUST commit and push as part of its operation
   - Use RunGitCommand pattern from GitCommitTool
   - Stage changed file(s)
   - Commit with provided comment or auto-generated message
   - Push to remote
   - If any step fails, revert all changes

4. **Error Handling**
   - **Merge conflicts**: Fail immediately and inform user to resolve manually
   - **Concurrent access**: Not handled - inform user and stop
   - **Authentication failures**: Return clear error about PAT
   - **Network issues**: Fail the operation completely (no partial success)

### Implementation Notes

- Shell out to git using `ProcessStartInfo` (following GitCommitTool pattern)
- Reuse `RunGitCommand` method pattern from `GitCommitTool`
- Call ModifyFileModernTool for file editing operations
- Use FileLockProvider pattern from ModifyFileModernTool for file locking
- PAT retrieved via `_generalSettingsService.GetDecryptedAzureDevOpsPAT()`
- All operations are atomic - either fully succeed or fully fail
- Log all git operations for debugging

## Additional Proposed Tools

### AzureDevOpsSearchWikiViaLocalTool

**Purpose**: Search across all wiki pages using local grep/ripgrep for better performance.

**Parameters**:
```json
{
  "organization": "string",
  "project": "string",
  "wiki_name": "string",  // Changed from wiki_id
  "search_pattern": "string",
  "regex": false,
  "case_sensitive": false
}
```

**Benefits**: 
- Much faster than API-based search
- Support for regex patterns
- Can search across file history

### AzureDevOpsBulkUpdateWikiViaLocalTool

**Purpose**: Perform find-and-replace or bulk operations across multiple wiki pages.

**Parameters**:
```json
{
  "organization": "string",
  "project": "string",
  "wiki_name": "string",  // Changed from wiki_id
  "operations": [
    {
      "path_pattern": "glob pattern",
      "find": "string or regex",
      "replace": "string",
      "preview": true
    }
  ],
  "comment": "string"
}
```

**Note**: All changes are committed and pushed atomically as a single operation.

**Benefits**:
- Batch operations in single commit
- Preview changes before applying
- Atomic updates (all or nothing)

### AzureDevOpsWikiHistoryViaLocalTool

**Purpose**: View change history for wiki pages using git log.

**Parameters**:
```json
{
  "organization": "string",
  "project": "string",
  "wiki_name": "string",  // Changed from wiki_id
  "path": "string",
  "limit": 10
}
```

**Benefits**:
- Full git history available
- Can see diffs between versions
- Blame functionality

### AzureDevOpsRevertWikiPageViaLocalTool

**Purpose**: Revert a wiki page to a previous version.

**Parameters**:
```json
{
  "organization": "string",
  "project": "string",
  "wiki_name": "string",  // Changed from wiki_id
  "path": "string",
  "commit_sha": "string (optional, defaults to HEAD~1)"
}
```

**Note**: Revert operation commits and pushes changes atomically.

## Migration Path

1. Keep existing REST-based tools for backward compatibility
2. Implement ViaLocal tools in parallel
3. UI can offer choice or auto-select based on operation type
4. Simple creates/reads might still use REST
5. Complex edits default to ViaLocal

## Security Considerations

- Local repositories are stored in user's AppData (same security as current app data)
- PAT retrieved at runtime via `_generalSettingsService.GetDecryptedAzureDevOpsPAT()`
- PAT never stored in git config, only passed via command line or environment
- No additional security implications beyond current REST approach
- Option to clear local cache through UI if needed

## Performance Benefits

| Operation | REST API | Via Local |
|-----------|----------|-----------|
| Small edit to large page | Send full content | Send only diff |
| Bulk find-replace | N API calls | 1 git push |
| Search across wiki | Limited API search | Full text/regex search |
| View history | Multiple API calls | Instant git log |

## Testing Considerations

- Unit tests for git operations
- Integration tests with test Azure DevOps instance
- Error simulation (network failures, conflicts)
- Performance benchmarks vs REST approach

## Future Enhancements

1. **Offline Mode**: Full offline editing with sync when connected
2. **Diff Viewer**: Built-in UI for reviewing changes before push
3. **Branch Support**: Work on branches, create PRs for wiki changes
4. **Templates**: Local template system for new pages
5. **Auto-sync**: Background sync of frequently used wikis
6. **Cached Search Index**: Local search index for instant searches

## Implementation Priority

1. `AzureDevOpsCreateOrUpdateWikiPageViaLocalTool` - Core functionality
2. `AzureDevOpsSearchWikiViaLocalTool` - High value for users
3. `AzureDevOpsBulkUpdateWikiViaLocalTool` - Power user feature
4. `AzureDevOpsWikiHistoryViaLocalTool` - Nice to have
5. `AzureDevOpsRevertWikiPageViaLocalTool` - Nice to have

## Key Design Decisions

1. **Shell Out to Git**: Use ProcessStartInfo to execute git commands (trusted, proven approach)
2. **Wiki Name vs ID**: Use wiki name for folder structure (matches Azure DevOps URLs)
3. **Atomic Operations**: Every modification commits and pushes - no partial states
4. **Reuse Existing Tools**: Leverage ModifyFileModernTool for edits, GitCommitTool patterns for git
5. **No Concurrent Access Handling**: Fail fast and inform user of conflicts
6. **PAT Security**: Retrieve at runtime, never persist in git config

## Conclusion

This git-based approach provides significant efficiency improvements for wiki operations while maintaining compatibility with existing REST-based tools. The implementation leverages existing patterns from the codebase (ModifyFileModernTool for edits, GitCommitTool for git operations, PAT authentication from GeneralSettingsService) to minimize complexity and ensure consistency.

## Detailed Implementation Task List

### Core Infrastructure
- [ ] Create base infrastructure for local wiki repository management
- [ ] Implement repository cloning functionality with PAT authentication
- [ ] Add proper file locking mechanism for concurrent operations
- [ ] Implement comprehensive logging for all git operations

### Primary Tool Implementation
- [ ] Create AzureDevOpsCreateOrUpdateWikiPageViaLocalTool class structure
- [ ] Implement edit operations (replace, append, prepend, delete) for wiki pages
- [ ] Add git commit and push functionality with error handling
- [ ] Implement merge conflict detection and resolution options

### Additional Tools
- [ ] Create AzureDevOpsSearchWikiViaLocalTool with grep/ripgrep integration
- [ ] Implement AzureDevOpsBulkUpdateWikiViaLocalTool for batch operations
- [ ] Add preview functionality for bulk updates
- [ ] Create AzureDevOpsWikiHistoryViaLocalTool using git log
- [ ] Implement AzureDevOpsRevertWikiPageViaLocalTool for version rollback

### Testing
- [ ] Create unit tests for git operations and edit functions
- [ ] Add integration tests with test Azure DevOps instance
- [ ] Implement error simulation tests (network failures, conflicts)
- [ ] Create performance benchmarks comparing REST vs Local approaches

### UI and Documentation
- [ ] Add UI option to choose between REST and ViaLocal tools
- [ ] Implement local cache clearing functionality in UI
- [ ] Create documentation for new ViaLocal tools usage