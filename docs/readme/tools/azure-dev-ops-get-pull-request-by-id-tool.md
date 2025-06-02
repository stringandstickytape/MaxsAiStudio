# AzureDevOpsGetPullRequestByIdTool

*Retrieves detailed information about a specific pull request in an Azure DevOps repository.*

## Usage

Use this tool to get all details for a single pull request, including its status, description, creator, reviewers, and optionally associated commits and work items.

**Parameters:**
-   `organization` (string, required): Your Azure DevOps organization name.
-   `project` (string, required): The project name.
-   `repository_id` (string, required): The name or ID of the repository where the PR exists.
-   `pull_request_id` (integer, required): The numeric ID of the pull request.
-   `include_commits` (boolean, optional, default: false): Set to `true` to include the list of commits associated with the pull request.
-   `include_work_item_refs` (boolean, optional, default: false): Set to `true` to include references to linked work items.

## Examples

To get details for Pull Request #123, including its linked work items:

```json
{
  "organization": "MyCompany",
  "project": "MyProject",
  "repository_id": "WebAppRepo",
  "pull_request_id": 123,
  "include_work_item_refs": true
}
```

## Notes

-   Requires a PAT with "Code (Read)" and potentially "Work Items (Read)" if `include_work_item_refs` is true.
-   The output is a formatted summary of the pull request.