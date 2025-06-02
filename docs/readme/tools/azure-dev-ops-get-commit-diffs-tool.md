# AzureDevOpsGetCommitDiffsTool

*Retrieves the file changes associated with a specific commit in an Azure DevOps repository.*

## Usage

This tool allows you to inspect the differences introduced by a particular commit. You need to provide the organization, project, repository, and the specific commit ID.

**Parameters:**
-   `organization` (string, required): The name of your Azure DevOps organization (e.g., "MyCompany").
-   `project` (string, required): The name of the project within the organization (e.g., "MyProject").
-   `repository_id` (string, required): The name or ID of the repository (e.g., "WebAppRepo" or a GUID).
-   `commit_id` (string, required): The full SHA hash of the commit you want to examine.
-   `top` (integer, optional, default: 100): The maximum number of changes to return.
-   `skip` (integer, optional, default: 0): The number of changes to skip, for pagination.
-   `base_version` (string, optional): A commit ID to compare against. If not provided, the diff is typically against the parent of `commit_id`.

## Examples

To get the diffs for commit `abc123def456` in the `WebAppRepo` of `MyProject` under `MyCompany`:

```json
{
  "organization": "MyCompany",
  "project": "MyProject",
  "repository_id": "WebAppRepo",
  "commit_id": "abc123def456b00b1e5f00d00d1e5f00d"
}
```

## Notes

-   Ensure the Personal Access Token (PAT) configured in AiStudio4 has the necessary permissions (e.g., "Code (Read)") for the specified repository.
-   The output will be a formatted text detailing the changes (added, modified, deleted files).