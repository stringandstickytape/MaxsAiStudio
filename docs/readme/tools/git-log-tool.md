# GitLogTool
*Retrieves git commit history between two references without showing diffs.*

## Description
This tool allows the AI to retrieve git commit history between two references (tags, branches, commits) without showing diffs. Commit messages are automatically truncated to prevent excessive output. It's useful for understanding what changed between versions or getting an overview of recent development activity.

## Parameters
- `from_ref` (string, required): Starting git reference (tag, branch, or commit hash). Use format like 'v0.93', 'main', or commit hash.
- `to_ref` (string, optional): Ending git reference (tag, branch, or commit hash). Defaults to 'HEAD' if not specified.
- `limit` (integer, optional): Maximum number of commits to return. Defaults to 100 to prevent excessive output. Maximum allowed is 500.
- `format` (string, optional): Output format. Options are:
  - `oneline`: Shows hash and message only
  - `short`: Adds author and date (default)
  - `full`: Includes all metadata
- `reverse` (boolean, optional): Show commits in chronological order (oldest first) instead of reverse chronological order. Defaults to false.

## Usage Examples

### Get commits since a specific tag
```json
{
  "from_ref": "v0.93"
}
```

### Get commits between two specific versions
```json
{
  "from_ref": "v0.92",
  "to_ref": "v0.93",
  "limit": 50
}
```

### Get recent commits with full details
```json
{
  "from_ref": "HEAD~10",
  "format": "full",
  "limit": 10
}
```

### Get commits in chronological order
```json
{
  "from_ref": "v1.0.0",
  "reverse": true,
  "limit": 20
}
```

## Notes
- The tool operates within the current project path's git repository
- Commit messages are automatically truncated to prevent excessive output
- The tool respects the git repository structure and will fail if the project path is not a git repository
- This tool is particularly useful for release notes, understanding changes between versions, or getting development activity overviews
- The tool continues processing only on success to prevent error cascading