# GitHubListIssueCommentsTool
*Retrieves all comments for a specific issue, ordered by creation date.*

## Purpose

This tool allows the AI to fetch all comments from a GitHub issue. It's useful for understanding the full discussion history, tracking progress, reviewing feedback, and getting context before adding new comments or making decisions about an issue.

## Parameters

- `owner` (string, required): Repository owner.
- `repo` (string, required): Repository name.
- `issue_number` (integer, required): The number of the issue.
- `since` (string, optional): Only show comments updated at or after this time (ISO 8601 format: YYYY-MM-DDTHH:MM:SSZ).
- `per_page` (integer, optional): Results per page (max 100). Default: 30.
- `page` (integer, optional): Page number of the results to fetch. Default: 1.

## Example Usage

To get all comments for an issue:
```json
{
  "owner": "microsoft",
  "repo": "vscode",
  "issue_number": 1234
}
```

To get recent comments (since a specific date):
```json
{
  "owner": "myorg",
  "repo": "myproject",
  "issue_number": 567,
  "since": "2024-01-01T00:00:00Z"
}
```

To get the first page of comments with a smaller page size:
```json
{
  "owner": "myorg",
  "repo": "myproject",
  "issue_number": 890,
  "per_page": 10,
  "page": 1
}
```

## Output Format

Returns a formatted list of comments showing:
- Comment author's username
- Comment creation date
- Last update date (if different from creation)
- Full comment content (with Markdown preserved)
- Direct URL to each comment on GitHub
- Pagination information

## Requirements

- A GitHub Personal Access Token (PAT) must be configured in AiStudio4 (`File > Settings > Set GitHub API Key...`).
- The PAT should have appropriate repository access (`repo` scope for private repositories, `public_repo` for public repositories).

## Notes

- Comments are returned in chronological order (oldest first).
- Results are paginated for issues with many comments. Use `page` and `per_page` parameters to navigate.
- The `since` parameter filters by the comment's last update time, not creation time.
- If an issue has no comments, the tool will return an empty list.
- The tool respects GitHub's API rate limits.