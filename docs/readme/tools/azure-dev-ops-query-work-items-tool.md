# AzureDevOpsQueryWorkItemsTool

*Executes a WIQL (Work Item Query Language) query to find work items matching specific criteria.*

## Usage

This tool allows the AI to run complex searches for work items using WIQL. It first executes the WIQL query to get a list of matching work item IDs, and then fetches the details for those IDs.

**Parameters:**
-   `organization` (string, required): Your Azure DevOps organization name.
-   `project` (string, required): The project name.
-   `query` (string, required): The WIQL query text (e.g., "SELECT [System.Id], [System.Title], [System.State] FROM WorkItems WHERE [System.WorkItemType] = 'Bug' AND [System.State] = 'Active' ORDER BY [Microsoft.VSTS.Common.Priority] asc, [System.CreatedDate] desc").
-   `top` (integer, optional, default: 100): Maximum number of work items whose details are fetched from the query results.
-   `skip` (integer, optional, default: 0): Number of work item IDs from the query result to skip before fetching details (for pagination of detailed results).
-   `time_precision` (boolean, optional, default: false): If `true`, includes time precision for date fields in the detailed work item output.

## Examples

To find the top 5 active Product Backlog Items assigned to "user@example.com":

```json
{
  "organization": "MyCompany",
  "project": "MyScrumProject",
  "query": "SELECT [System.Id] FROM WorkItems WHERE [System.WorkItemType] = 'Product Backlog Item' AND [System.AssignedTo] = 'user@example.com' AND [System.State] = 'Active'",
  "top": 5
}
```

## Notes

-   The PAT requires "Work Items (Read)" permissions.
-   The tool performs two API calls: one for the WIQL query (returns IDs), and one to get details for the (paginated) IDs.
-   WIQL syntax can be complex. Refer to Azure DevOps documentation for WIQL details.
-   The output includes a summary of total matches from the WIQL query and then detailed information for the fetched work items (up to `top` results after `skip`).