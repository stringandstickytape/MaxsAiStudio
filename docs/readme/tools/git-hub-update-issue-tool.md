# GitHubUpdateIssueTool
*Updates an existing issue's title, body, state, labels, assignees, or milestone. Provide only the fields to be changed.*

## Purpose

This tool allows the AI to modify existing GitHub issues. It can update any aspect of an issue including its title, description, state (open/closed), labels, assignees, and milestone. This is useful for issue management, status updates, and maintaining project workflows. All content updated by this tool includes an attribution line indicating it was modified by Max's AI Studio.

## Parameters

- `owner` (string, required): Repository owner.
- `repo` (string, required): Repository name.
- `issue_number` (integer, required): The number of the issue to update.
- `title` (string, optional): New title for the issue.
- `body` (string, optional): New body for the issue (Markdown).
- `state` (string, optional): New state for the issue. Options: "open", "closed".
- `state_reason` (string, optional): Reason for closing the issue (if state is 'closed'). Options: "completed", "not_planned".
- `labels` (array, optional): Array of label names. This will REPLACE all existing labels.
- `assignees` (array, optional): Array of login names. This will REPLACE all existing assignees.
- `milestone` (integer or null, optional): The number of the milestone to associate, or null to remove milestone.

## Example Usage

To close an issue as completed:
```json
{
  "owner": "microsoft",
  "repo": "vscode",
  "issue_number": 1234,
  "state": "closed",
  "state_reason": "completed"
}
```

To update labels and assignees:
```json
{
  "owner": "myorg",
  "repo": "myproject",
  "issue_number": 567,
  "labels": ["bug", "high-priority", "needs-review"],
  "assignees": ["developer1", "reviewer2"]
}
```

To update the issue description:
```json
{
  "owner": "myorg",
  "repo": "myproject",
  "issue_number": 890,
  "body": "## Updated Description\nThis issue has been updated with new information.\n\n## Additional Context\nNew details discovered during investigation."
}
```

To remove milestone and change title:
```json
{
  "owner": "myorg",
  "repo": "myproject",
  "issue_number": 123,
  "title": "Updated: Feature request for dark mode",
  "milestone": null
}
```

## Output Format

Returns confirmation of successful issue update including:
- Updated issue number
- Issue title (current)
- Direct URL to the updated issue on GitHub

## Requirements

- A GitHub Personal Access Token (PAT) must be configured in AiStudio4 (`File > Settings > Set GitHub API Key...`).
- The PAT must have `repo` scope (or `public_repo` for public repositories only).
- Write access to the target repository is required.

## Notes

- All issue bodies updated by this tool will include an attribution line: "*Content updated by GitHubUpdateIssue via Max's AI Studio*"
- Only provide the fields you want to change. Omitted fields will remain unchanged.
- Labels and assignees arrays completely replace existing values - they don't append.
- To remove all labels or assignees, provide an empty array `[]`.
- To remove a milestone, set `milestone` to `null`.
- The `state_reason` parameter is only used when closing an issue (`state: "closed"`).