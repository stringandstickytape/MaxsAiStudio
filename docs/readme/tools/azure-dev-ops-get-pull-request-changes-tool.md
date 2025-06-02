# AzureDevOpsGetPullRequestChangesTool

*Retrieves the file changes associated with a specific pull request iteration in Azure DevOps.*

## Usage

This tool lists all files that were added, modified, deleted, or renamed in a pull request, or a specific iteration of it. This is useful for understanding the scope of changes in a PR.

**Parameters:**
-   `organization` (string, required): Your Azure DevOps organization name.
-   `project` (string, required): The project name.
-   `repository_id` (string, required): The name or ID of the repository.
-   `pull_request_id` (integer, required): The ID of the pull request.
-   `iteration_id` (integer, required): The ID of the specific iteration within the pull request to get changes for. Iterations represent updates to a PR (e.g., after new commits are pushed).
-   `top` (integer, optional, default: 100): Maximum number of changes to return.
-   `skip` (integer, optional, default: 0): Number of changes to skip (for pagination).
-   `compare_to` (integer, optional): The ID of another iteration to compare against. If provided, changes are shown between `compare_to` and `iteration_id`.

## Examples

To get the changes for iteration 2 of Pull Request #123:

```json
{
  "organization": "MyCompany",
  "project": "MyProject",
  "repository_id": "WebAppRepo",
  "pull_request_id": 123,
  "iteration_id": 2
}
```

## Notes

-   The PAT requires "Code (Read)" permissions.
-   Iteration IDs can be found using the `AzureDevOpsGetPullRequestIterationsTool`.
-   Output includes a summary of adds, edits, deletes, and renames, followed by a list of changed files.