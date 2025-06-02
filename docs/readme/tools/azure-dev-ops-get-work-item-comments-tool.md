# AzureDevOpsGetWorkItemCommentsTool

*Retrieves comments associated with a specific work item in Azure DevOps.*

## Usage

This tool fetches the discussion history (comments) for a given work item ID. It's useful for understanding the context and decisions made regarding a task, bug, or feature.

**Parameters:**
-   `organization` (string, required): Your Azure DevOps organization name.
-   `project` (string, required): The project name.
-   `id` (integer, required): The numeric ID of the work item.
-   `top` (integer, optional, default: 100): Maximum number of comments to return.
-   `skip` (integer, optional, default: 0): Number of comments to skip for pagination.

## Examples

To get all comments for Work Item #456:

```json
{
  "organization": "MyCompany",
  "project": "MyProject",
  "id": 456,
  "top": 200 
}
```

## Notes

-   The PAT needs "Work Items (Read)" permissions.
-   The output will list each comment with its ID, author, creation/modification dates, and the comment text itself. Deleted comments will be indicated.