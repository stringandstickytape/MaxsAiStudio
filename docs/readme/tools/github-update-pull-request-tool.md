# GitHubUpdatePullRequestTool

*Updates an existing pull request in a GitHub repository. Requires GitHub Personal Access Token with repo permissions.*

## Purpose
This tool allows you to update the properties of an existing pull request on GitHub, such as its title, body, state (open/closed), base branch, and whether maintainers can modify it. This is useful for workflow automation, correcting mistakes, or managing PRs directly from AiStudio4.

## Parameters
- `owner` (string, required): Repository owner (username or organization)
- `repo` (string, required): Repository name
- `pull_number` (integer, required): The number of the pull request to update
- `title` (string, optional): The new title for the pull request
- `body` (string, optional): The new contents of the pull request (Markdown)
- `state` (string, optional): State of the PR: `open` or `closed`
- `base` (string, optional): The branch you want the changes pulled into
- `maintainer_can_modify` (boolean, optional): Whether maintainers can modify the PR

## Example Usage
Update the title and body of pull request #42 in the `octocat/Hello-World` repository:

```
{
  "owner": "octocat",
  "repo": "Hello-World",
  "pull_number": 42,
  "title": "New PR Title",
  "body": "Updated PR description."
}
```

## Output
On success, returns a JSON object with details about the updated pull request:

```
{
  "success": true,
  "pullRequest": {
    "number": 42,
    "url": "https://github.com/octocat/Hello-World/pull/42",
    "title": "New PR Title",
    "state": "open",
    "base": "main",
    "updated_at": "2024-06-04T17:00:00Z"
  },
  "message": "✅ Pull request #42 updated successfully!"
}
```

On failure, returns an error message with details from the GitHub API.

## Notes
- Requires a valid GitHub Personal Access Token with repo permissions configured in AiStudio4.
- Only the fields you provide will be updated; others remain unchanged.
- You must have write access to the repository and the pull request.