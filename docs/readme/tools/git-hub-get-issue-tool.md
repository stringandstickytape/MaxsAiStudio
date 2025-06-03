# GitHubGetIssueTool
*Retrieves detailed information for a specific issue by its number.*

## Purpose

This tool allows the AI to fetch comprehensive details about a specific GitHub issue, including its title, description, state, labels, assignees, comments count, and metadata. It's useful for understanding the full context of an issue before taking action or providing detailed analysis.

## Parameters

- `owner` (string, required): Repository owner.
- `repo` (string, required): Repository name.
- `issue_number` (integer, required): The number of the issue to retrieve.

## Example Usage

To get details about issue #1234 in a repository:
```json
{
  "owner": "microsoft",
  "repo": "vscode",
  "issue_number": 1234
}
```

## Output Format

Returns comprehensive issue information including:
- Issue number and title
- Full description/body (with Markdown preserved)
- Current state (open/closed)
- Creator information
- Creation and last update timestamps
- All labels applied to the issue
- All assignees
- Associated milestone (if any)
- Number of comments
- Direct URL to the issue on GitHub

## Requirements

- A GitHub Personal Access Token (PAT) must be configured in AiStudio4 (`File > Settings > Set GitHub API Key...`).
- The PAT should have appropriate repository access (`repo` scope for private repositories, `public_repo` for public repositories).

## Notes

- This tool provides the complete issue context, making it ideal for detailed analysis or before making updates.
- The issue body is returned with original Markdown formatting preserved.
- If the issue doesn't exist, the GitHub API will return a 404 error.