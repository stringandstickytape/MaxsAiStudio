# GitHubListIssuesTool
*Retrieves a list of issues for the specified repository. Supports filtering by state, labels, assignee, milestone, etc.*

## Purpose

This tool allows the AI to fetch a list of issues from a GitHub repository with comprehensive filtering options. It's useful for project management, bug tracking, and understanding the current state of a repository's issues.

## Parameters

- `owner` (string, required): Repository owner (username or organization).
- `repo` (string, required): Repository name.
- `milestone` (string, optional): Milestone number or '*' for any, 'none' for no milestone.
- `state` (string, optional): Issue state. Options: "open", "closed", "all". Default: "open".
- `assignee` (string, optional): Login of the assignee or '*' for any, 'none' for no assignee.
- `creator` (string, optional): Login of the issue creator.
- `mentioned` (string, optional): Login of a user mentioned in an issue.
- `labels` (string, optional): Comma-separated list of label names (e.g., "bug,enhancement").
- `sort` (string, optional): What to sort results by. Options: "created", "updated", "comments". Default: "created".
- `direction` (string, optional): The direction of the sort. Options: "asc", "desc". Default: "desc".
- `since` (string, optional): Only show issues updated at or after this time (ISO 8601 format: YYYY-MM-DDTHH:MM:SSZ).
- `per_page` (integer, optional): Results per page (max 100). Default: 30.
- `page` (integer, optional): Page number of the results to fetch. Default: 1.

## Example Usage

To list all open issues in a repository:
```json
{
  "owner": "microsoft",
  "repo": "vscode"
}
```

To find all closed bugs assigned to a specific user:
```json
{
  "owner": "microsoft",
  "repo": "vscode",
  "state": "closed",
  "labels": "bug",
  "assignee": "username"
}
```

To get the most recently updated issues:
```json
{
  "owner": "microsoft",
  "repo": "vscode",
  "sort": "updated",
  "direction": "desc",
  "per_page": 10
}
```

## Output Format

Returns a formatted list showing:
- Issue number and title
- Current state (open/closed)
- Labels applied to the issue
- Primary assignee (if any)
- Direct URL to the issue on GitHub
- Pagination information

## Requirements

- A GitHub Personal Access Token (PAT) must be configured in AiStudio4 (`File > Settings > Set GitHub API Key...`).
- The PAT should have appropriate repository access (`repo` scope for private repositories, `public_repo` for public repositories).

## Notes

- Results are paginated. Use the `page` and `per_page` parameters to navigate through large result sets.
- The tool respects GitHub's API rate limits.
- For repositories with many issues, consider using specific filters to narrow down results.