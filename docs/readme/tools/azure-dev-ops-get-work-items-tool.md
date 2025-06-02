# AzureDevOpsGetWorkItemsTool

*Retrieves detailed information about specific work items by their IDs from Azure DevOps.*

## Usage

Use this tool to fetch comprehensive details for one or more work items when you know their IDs. This is more direct than using a query if you have specific IDs.

**Parameters:**
-   `organization` (string, required): Your Azure DevOps organization name.
-   `project` (string, required): The project name.
-   `ids` (array of integers, required): A list of work item IDs to retrieve (e.g., `[123, 456, 789]`).
-   `fields` (array of strings, optional): A list of specific field names (e.g., "System.Title", "Microsoft.VSTS.Scheduling.StoryPoints") to return. If omitted, a default set of common fields is returned.
-   `as_of` (string, optional): A date (ISO 8601 format) to view the work items as they were at that point in time.
-   `expand` (string, optional, default: "none"): Specifies the level of detail for relations. Can be "relations" (includes links to other work items), "fields" (includes all fields), or "all" (includes both relations and all fields), or "none".

## Examples

To get details for work items #101 and #102, expanding relations:

```json
{
  "organization": "MyCompany",
  "project": "MyProject",
  "ids": [101, 102],
  "expand": "relations"
}
```

To get only the Title and State for work item #205:

```json
{
  "organization": "MyCompany",
  "project": "MyProject",
  "ids": [205],
  "fields": ["System.Title", "System.State"]
}
```

## Notes

-   The PAT requires "Work Items (Read)" permissions.
-   The output provides a formatted summary for each requested work item, including its ID, type, title, state, assigned user, creation date, and other requested fields or relations.