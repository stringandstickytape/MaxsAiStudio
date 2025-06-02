# AzureDevOpsGetPullRequestsTool

*Retrieves pull requests matching specified criteria from an Azure DevOps repository.*

## Usage

This tool allows you to search for pull requests within a repository using filters like status, creator, reviewer, or source/target branches.

**Parameters:**
-   `organization` (string, required): Your Azure DevOps organization name.
-   `project` (string, required): The project name.
-   `repository_id` (string, required): The name or ID of the repository.
-   `status` (string, optional, default: "active"): Filter by PR status. Valid values: "active", "abandoned", "completed", "all".
-   `creator_id` (string, optional): Filter by the ID (GUID) of the user who created the PR.
-   `reviewer_id` (string, optional): Filter by the ID (GUID) of a user who is a reviewer on the PR.
-   `source_reference_name` (string, optional): Filter by the source branch name (e.g., "refs/heads/feature/new-login").
-   `target_reference_name` (string, optional): Filter by the target branch name (e.g., "refs/heads/main").
-   `top` (integer, optional, default: 100): Maximum number of pull requests to return.
-   `skip` (integer, optional, default: 0): Number of pull requests to skip for pagination.

## Examples

To get all completed pull requests created by user `user-guid-123` targeting the `main` branch:

```json
{
  "organization": "MyCompany",
  "project": "MyProject",
  "repository_id": "WebAppRepo",
  "status": "completed",
  "creator_id": "user-guid-123",
  "target_reference_name": "refs/heads/main"
}
```

## Notes

-   The PAT requires "Code (Read)" permissions.
-   User IDs for `creator_id` and `reviewer_id` are typically GUIDs. You might need to find these IDs through other means (e.g., Azure DevOps UI or other API calls) if not known.
-   Branch names must be fully qualified (e.g., `refs/heads/mybranch`).