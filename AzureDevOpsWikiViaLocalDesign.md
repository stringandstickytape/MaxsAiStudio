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
        └── {wiki_id}/
            ├── .git/
            ├── .order files
            └── wiki pages (.md files)
```

### Authentication

- Use existing Azure DevOps PAT from `GeneralSettingsService`
- Format for git operations: `https://{PAT}@dev.azure.com/{org}/{project}/_git/{wiki}.wiki`
- Alternative: Username = anything, Password = PAT

## Primary Tool: AzureDevOpsCreateOrUpdateWikiPageViaLocalTool

### Purpose
Create or update wiki pages using local git repository operations for efficient partial updates.

### Parameters
```json
{
  "organization": "string",
  "project": "string", 
  "wiki_id": "string",
  "path": "string",
  "edits": [
    {
      "type": "replace|append|prepend|delete",
      "old_content": "string (for replace)",
      "new_content": "string",
      "line_range": [start, end] // optional, for line-based edits
    }
  ],
  "comment": "string (optional)",
  "auto_pull": true // default true
}
```

### Process Flow

1. **Repository Management**
   - Check if local repo exists at `PathHelper.GetProfileSubPath("wikis", organization, project, wiki_id)`
   - If not exists: Clone repository
   - If exists and `auto_pull`: Pull latest changes

2. **Apply Edits**
   - Load target file from local repo
   - Apply edit operations in sequence
   - Save modified file

3. **Commit and Push**
   - Stage changed file(s)
   - Commit with provided comment or auto-generated message
   - Push to remote

4. **Error Handling**
   - **Merge conflicts**: Show conflict, offer options:
     - Abort and revert local changes
     - Keep local changes uncommitted for manual resolution
     - Force push (with confirmation)
   - **Authentication failures**: Return clear error about PAT
   - **Network issues**: Keep changes locally, inform user

### Implementation Notes

- Reuse `RunGitCommand` pattern from `GitCommitTool`
- Use `ProcessStartInfo` for git operations
- Implement proper file locking for concurrent operations
- Log all git operations for debugging

## Additional Proposed Tools

### AzureDevOpsSearchWikiViaLocalTool

**Purpose**: Search across all wiki pages using local grep/ripgrep for better performance.

**Parameters**:
```json
{
  "organization": "string",
  "project": "string",
  "wiki_id": "string",
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
  "wiki_id": "string",
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
  "wiki_id": "string",
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
  "wiki_id": "string",
  "path": "string",
  "commit_sha": "string (optional, defaults to HEAD~1)"
}
```

## Migration Path

1. Keep existing REST-based tools for backward compatibility
2. Implement ViaLocal tools in parallel
3. UI can offer choice or auto-select based on operation type
4. Simple creates/reads might still use REST
5. Complex edits default to ViaLocal

## Security Considerations

- Local repositories are stored in user's AppData (same security as current app data)
- PAT never stored in git config, only used at runtime
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

## Conclusion

This git-based approach provides significant efficiency improvements for wiki operations while maintaining compatibility with existing REST-based tools. The implementation leverages existing patterns from the codebase (git operations, PAT authentication, PathHelper) to minimize complexity and ensure consistency.