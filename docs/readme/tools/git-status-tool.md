# GitStatus Tool

*Shows working directory status, current branch, and repository state information including ahead/behind remote status.*

## Overview

This tool provides comprehensive repository state information, helping the AI understand the current state of your git repository. It shows which files have been modified, the current branch, and how the local branch compares to its remote tracking branch.

## Parameters

- `include_branches` (boolean, optional, default: false): Include list of all branches in the output.
- `include_remote_status` (boolean, optional, default: true): Include ahead/behind status compared to remote tracking branch.
- `porcelain` (boolean, optional, default: false): Use porcelain format for machine-readable output.

## Examples

### Basic status check
```json
{
  "include_remote_status": true
}
```

### Comprehensive status with branch list
```json
{
  "include_branches": true,
  "include_remote_status": true
}
```

### Machine-readable format
```json
{
  "porcelain": true,
  "include_remote_status": false
}
```

## Output

The tool returns a JSON object containing:
- `overallSuccess`: Boolean indicating if the operation succeeded
- `statusInfo`: Detailed repository status including:
  - `currentBranch`: Name of the current branch
  - `workingDirectory`: Array of changed files with their status
  - `hasChanges`: Boolean indicating if there are uncommitted changes
  - `fileCount`: Number of files with changes
  - `remoteTracking`: Information about remote tracking status (if enabled)
    - `upstream`: Name of the upstream branch
    - `ahead`: Number of commits ahead of remote
    - `behind`: Number of commits behind remote
    - `upToDate`: Boolean indicating if local and remote are in sync
  - `branches`: Array of all branches (if requested)
- `options`: The options used for this status check
- `errors`: Array of any errors encountered
- `summary`: Human-readable summary of the repository status

## File Status Codes

The working directory status uses git's standard status codes:
- **Index Status** (first character):
  - `A`: Added to index
  - `M`: Modified in index
  - `D`: Deleted from index
  - `R`: Renamed in index
  - `C`: Copied in index
  - `U`: Unmerged in index

- **Working Tree Status** (second character):
  - `M`: Modified in working tree
  - `D`: Deleted in working tree
  - `?`: Untracked
  - `!`: Ignored
  - `U`: Unmerged in working tree

## Important Notes

- **Repository Required**: This tool requires the project path to be a valid git repository.
- **Remote Tracking**: Remote status is only available if the current branch has an upstream branch configured.
- **Performance**: Including branch list may take longer in repositories with many branches.
- **Porcelain Format**: The porcelain format provides stable, machine-readable output that won't change between git versions.

## Common Use Cases

1. **Pre-Commit Check**: Verify what changes will be committed before running GitCommitTool
2. **Repository Overview**: Get a quick snapshot of the repository state
3. **Sync Status**: Check if local branch is up to date with remote
4. **Branch Context**: Understand which branch you're working on and what other branches exist
5. **Change Detection**: Identify which files have been modified for selective commits

## Integration with Other Tools

This tool works seamlessly with other git tools:
- **GitCommitTool**: Use GitStatus to see what files are available to commit
- **GitBranchTool**: Check current branch before switching or creating new branches
- **GitLogTool**: Understand commit history in context of current repository state
- **GitHubCreatePullRequestTool**: Verify branch state before creating pull requests

## Example Output

```json
{
  "overallSuccess": true,
  "statusInfo": {
    "currentBranch": "feature/new-authentication",
    "workingDirectory": [
      {
        "file": "src/auth/login.js",
        "indexStatus": "M",
        "workTreeStatus": " ",
        "statusDescription": "Modified in index"
      },
      {
        "file": "tests/auth.test.js",
        "indexStatus": "?",
        "workTreeStatus": "?",
        "statusDescription": "Untracked"
      }
    ],
    "hasChanges": true,
    "fileCount": 2,
    "remoteTracking": {
      "upstream": "origin/feature/new-authentication",
      "ahead": 2,
      "behind": 0,
      "upToDate": false
    }
  },
  "summary": "Repository status retrieved successfully\\nCurrent branch: feature/new-authentication\\nWorking directory has 2 changed files"
}
```

This tool provides essential repository awareness for AI-driven development workflows.