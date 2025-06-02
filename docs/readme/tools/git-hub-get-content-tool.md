# GitHubGetContentTool

*Retrieves the content of a specific file from a GitHub repository using the `/repos/{owner}/{repo}/contents/{path}` endpoint.*

## Usage

This tool allows the AI to read the content of a file from a public or private (if PAT has access) GitHub repository. It's useful for fetching configuration files, source code, or documentation directly from GitHub.

**Parameters:**
-   `owner` (string, required): The username of the repository owner or the organization name.
-   `repo` (string, required): The name of the repository.
-   `path` (string, required): The path to the file within the repository (e.g., "src/app.js", "README.md").
-   `ref` (string, optional): The name of the commit, branch, or tag. If not provided, the repository's default branch is used.

## Examples

To get the content of `package.json` from the `main` branch of the `my-awesome-app` repository owned by `octocat`:

```json
{
  "owner": "octocat",
  "repo": "my-awesome-app",
  "path": "package.json",
  "ref": "main"
}
```

## Notes

-   A GitHub Personal Access Token (PAT) with `repo` scope (or at least `public_repo` for public repositories) must be configured in AiStudio4 (`File > Settings > Set GitHub API Key...`).
-   The tool returns the file content decoded from Base64, along with metadata like file name, path, size, SHA, and HTML URL.
-   For very large files, the content might be truncated or the API might return an error. The GitHub API has size limits for file content retrieval.
-   If the path points to a directory, the tool will return an error; use `GitHubListContentsTool` for directories.