# AzureDevOpsGetWorkItemUpdatesTool

*Retrieves the update history for a specific work item in Azure DevOps.*

## Usage

This tool provides a log of all changes made to a work item, including field updates, relation changes, and comments added in each revision.

**Parameters:**
-   `organization` (string, required): Your Azure DevOps organization name.
-   `project` (string, required): The project name.
-   `id` (integer, required): The ID of the work item for which to retrieve updates.
-   `top` (integer, optional, default: 100): Maximum number of updates to return.
-   `skip` (integer, optional, default: 0): Number of updates to skip (for pagination).

## Examples

To get the latest 10 updates for Work Item #789:

```json
{
  "organization": "MyCompany",
  "project": "MyProject",
  "id": 789,
  "top": 10
}
```

## Notes

-   The PAT needs "Work Items (Read)" permissions.
-   Each update in the output includes an update ID, who made the revision, the date of revision, and details about what fields were changed (old and new values), any relations added/removed, and comments made during that update.