# AzureDevOpsGetCommitsTool

*Retrieves commits matching specified criteria from an Azure DevOps repository.*

## Usage

Use this tool to fetch a list of commits based on various filters like date range, author, or affected path.

**Parameters:**
-   `organization` (string, required): Your Azure DevOps organization name.
-   `project` (string, required): The project name.
-   `repository_id` (string, required): The name or ID of the repository.
-   `from_date` (string, optional): Start date for filtering commits (ISO 8601 format, e.g., "2023-01-01T00:00:00Z").
-   `to_date` (string, optional): End date for filtering commits (ISO 8601 format).
-   `author` (string, optional): Filter commits by author's display name or email.
-   `item_path` (string, optional): Filter commits that affect a specific file or folder path within the repository.
-   `top` (integer, optional, default: 100): Maximum number of commits to return.
-   `skip` (integer, optional, default: 0): Number of commits to skip for pagination.

## Examples

To get the last 10 commits by "john.doe@example.com" in `WebAppRepo`:

```json
{
  "organization": "MyCompany",
  "project": "MyProject",
  "repository_id": "WebAppRepo",
  "author": "john.doe@example.com",
  "top": 10
}
```

## Notes

-   The PAT must have "Code (Read)" permissions.
-   Date formats should be precise (ISO 8601) for accurate filtering.
-   The output is a formatted list of commits including their ID, author, date, and message.