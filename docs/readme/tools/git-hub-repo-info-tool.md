# GitHubRepoInfoTool

*Retrieves basic metadata about a GitHub repository using the `/repos/{owner}/{repo}` endpoint.*

## Usage

This tool provides a summary of a GitHub repository, including its description, owner, language, stars, forks, and important URLs.

**Parameters:**
-   `owner` (string, required): The GitHub username or organization that owns the repository.
-   `repo` (string, required): The name of the repository.

## Examples

To get information about the `facebook/react` repository:

```json
{
  "owner": "facebook",
  "repo": "react"
}
```

## Notes

-   A GitHub PAT configured in AiStudio4 (`File > Settings > Set GitHub API Key...`) is used for authentication. Basic info for public repos might be accessible without a token, but rate limits are stricter.
-   The output is a formatted summary including:
    -   Full Name, Description, URL, Default Branch
    -   Creation, Last Updated, Last Pushed dates
    -   Statistics: Stars, Watchers, Forks, Open Issues, Size (KB)
    -   Details: Primary Language, License, Visibility, Fork status, and whether features like Wiki, Issues, Projects, Downloads, Pages, Discussions are enabled.