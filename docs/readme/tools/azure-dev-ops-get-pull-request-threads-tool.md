# AzureDevOpsGetPullRequestThreadsTool

*Retrieves comment threads associated with a specific pull request in Azure DevOps.*

## Usage

This tool allows you to fetch the discussion threads (including comments and replies) related to a pull request. This is useful for understanding the review process and decisions made.

**Parameters:**
-   `organization` (string, required): Your Azure DevOps organization name.
-   `project` (string, required): The project name.
-   `repository_id` (string, required): The name or ID of the repository.
-   `pull_request_id` (integer, required): The ID of the pull request.
-   `top` (integer, optional, default: 100): The maximum number of threads to return.
-   `skip` (integer, optional, default: 0): The number of threads to skip (for pagination).

## Examples

To get the first 50 comment threads for Pull Request #123:

```json
{
  "organization": "MyCompany",
  "project": "MyProject",
  "repository_id": "WebAppRepo",
  "pull_request_id": 123,
  "top": 50
}
```

## Notes

-   The PAT requires "Code (Read)" permissions.
-   The output will include details for each thread, such as its status (e.g., active, closed), the file path it's associated with (if it's a code comment), line numbers, and the individual comments within the thread, including author and content.