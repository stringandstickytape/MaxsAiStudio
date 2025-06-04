# GitHubListPullRequestsTool

*Lists pull requests for a GitHub repository. Requires GitHub Personal Access Token with repo permissions.*

## Purpose
This tool enables you to retrieve a list of pull requests from a specified GitHub repository, with optional filtering by state, branch, or pagination. Useful for workflow automation, dashboards, or reviewing PR status directly from AiStudio4.

## Parameters
- `owner` (string, required): Repository owner (username or organization)
- `repo` (string, required): Repository name
- `state` (string, optional): State of the PR: `open`, `closed`, or `all` (default: `open`)
- `head` (string, optional): Filter by head user or branch name (e.g., `feature-branch`)
- `base` (string, optional): Filter by base branch name (e.g., `main`)
- `per_page` (integer, optional): Number of results per page (max 100)
- `page` (integer, optional): Page number of the results to fetch

## Example Usage
List all open pull requests targeting the `main` branch in the `octocat/Hello-World` repository:

```
{
  "owner": "octocat",
  "repo": "Hello-World",
  "base": "main",
  "state": "open"
}
```

## Output
On success, returns a JSON object containing a list of pull requests:

```
{
  "success": true,
  "count": 2,
  "pullRequests": [
    {
      "number": 42,
      "title": "Add new feature",
      "state": "open",
      "head": "feature-branch",
      "base_branch": "main",
      "url": "https://github.com/octocat/Hello-World/pull/42",
      "user": "octocat",
      "created_at": "2024-06-01T12:00:00Z",
      "updated_at": "2024-06-04T17:00:00Z"
    },
    {
      "number": 43,
      "title": "Fix bug",
      "state": "open",
      "head": "bugfix-branch",
      "base_branch": "main",
      "url": "https://github.com/octocat/Hello-World/pull/43",
      "user": "octocat",
      "created_at": "2024-06-02T13:00:00Z",
      "updated_at": "2024-06-04T17:10:00Z"
    }
  ]
}
```

On failure, returns an error message with details from the GitHub API.

## Notes
- Requires a valid GitHub Personal Access Token with repo permissions configured in AiStudio4.
- Pagination is supported via `per_page` and `page` parameters.
- You must have read access to the repository.