# GitBranch Tool

*Manages git branches including creating, switching, listing, and deleting branches within the project repository.*

## Overview

This tool provides comprehensive branch management capabilities for your git repository. It allows the AI to create new branches, switch between existing branches, list all branches, delete branches, and get information about the current branch.

## Parameters

- `operation` (string, required): The branch operation to perform. Must be one of: `create`, `switch`, `list`, `delete`, `current`.
- `branch_name` (string, required for create/switch/delete): Name of the branch to operate on.
- `create_from` (string, optional): Reference to create branch from (defaults to current HEAD). Only used with `create` operation.
- `force` (boolean, optional, default: false): Force the operation (use with caution, primarily for delete operations).
- `include_remote` (boolean, optional, default: false): Include remote branches in list operation.

## Examples

### Create a new branch
```json
{
  "operation": "create",
  "branch_name": "feature/new-authentication",
  "create_from": "main"
}
```

### Switch to an existing branch
```json
{
  "operation": "switch",
  "branch_name": "develop"
}
```

### List all branches (including remote)
```json
{
  "operation": "list",
  "include_remote": true
}
```

### Delete a branch
```json
{
  "operation": "delete",
  "branch_name": "feature/completed-feature",
  "force": false
}
```

### Get current branch
```json
{
  "operation": "current"
}
```

## Output

The tool returns a JSON object containing:
- `overallSuccess`: Boolean indicating if the operation succeeded
- `operation`: The operation that was performed
- `branchName`: The branch name that was operated on (if applicable)
- `operationResult`: Detailed results of the operation including:
  - For `list`: Array of branch objects with name, current status, and remote status
  - For `current`: The name of the current branch
  - For other operations: Success status and descriptive messages
- `errors`: Array of any errors encountered
- `summary`: Human-readable summary of the operation

## Important Notes

- **Repository Required**: This tool requires the project path to be a valid git repository.
- **Branch Creation**: When creating a branch, the tool automatically switches to the new branch.
- **Force Delete**: Use the `force` parameter with caution when deleting branches, as it can delete branches with unmerged changes.
- **Remote Branches**: When listing with `include_remote: true`, you'll see both local and remote tracking branches.
- **Current Branch**: The `current` operation is useful for understanding the repository state before performing other operations.

## Common Use Cases

1. **Feature Development**: Create a new feature branch from main/develop
2. **Branch Switching**: Switch between different branches during development
3. **Repository Exploration**: List all branches to understand the repository structure
4. **Cleanup**: Delete completed feature branches
5. **Status Check**: Get the current branch name for context

## Error Handling

The tool provides detailed error messages for common scenarios:
- Attempting to create a branch that already exists
- Trying to switch to a non-existent branch
- Attempting to delete the current branch
- Git repository not found or not initialized
- Permission issues with git operations

This tool integrates seamlessly with other git tools like `GitCommitTool`, `GitStatusTool`, and `GitLogTool` to provide a complete git workflow experience.