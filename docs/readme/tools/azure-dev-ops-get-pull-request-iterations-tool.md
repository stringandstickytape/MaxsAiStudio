# AzureDevOpsGetPullRequestIterationsTool

*Retrieves the iterations (versions) of a specific pull request in Azure DevOps.*

## Usage

Pull requests in Azure DevOps can have multiple iterations as new commits are added or changes are pushed. This tool lists all these iterations, allowing you to see the history of updates to a PR.

**Parameters:**
-   `organization` (string, required): Your Azure DevOps organization name.
-   `project` (string, required): The project name.
-   `repository_id` (string, required): The name or ID of the repository.
-   `pull_request_id` (integer, required): The ID of the pull request for which to retrieve iterations.

## Examples

To get all iterations for Pull Request #123:

```json
{
  "organization": "MyCompany",
  "project": "MyProject",
  "repository_id": "WebAppRepo",
  "pull_request_id": 123
}
```

## Notes

-   The PAT requires "Code (Read)" permissions.
-   Each iteration in the output will have an ID, description, author, creation date, and details about the source and target commits for that iteration.
-   The iteration IDs obtained from this tool can be used with `AzureDevOpsGetPullRequestChangesTool` to see file diffs for specific updates.