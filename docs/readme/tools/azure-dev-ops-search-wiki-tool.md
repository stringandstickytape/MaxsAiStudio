# AzureDevOpsSearchWikiTool

*Searches for content across Azure DevOps wiki pages using the Azure DevOps Search API.*

## Usage

This tool allows you to search for keywords and phrases across all the pages in your Azure DevOps wiki.

**Parameters:**
- `organization` (string, required): The name of your Azure DevOps organization.
- `project` (string, required): The name of the project within the organization.
- `search_text` (string, required): The text to search for.
- `top` (integer, optional, default: 25): The maximum number of results to return.
- `skip` (integer, optional, default: 0): The number of results to skip, for pagination.
- `include_facets` (boolean, optional, default: false): Include facet information (breakdown by project/wiki) in results.
- `project_filters` (array of strings, optional): Array of project names to filter results to specific projects.
- `sort_by` (string, optional, default: "relevance"): Field to sort results by. Can be "relevance", "filename", "lastmodifieddate".
- `sort_order` (string, optional, default: "desc"): Sort order for results. Can be "asc", "desc".

## Examples

To search for "deployment process" in the `MyProject` wiki:

```json
{
  "organization": "MyCompany",
  "project": "MyProject",
  "search_text": "deployment process"
}
```

## Notes

- Requires a PAT with "Wiki (Read)" permissions.
- The output is a formatted list of search results, including the page title, path, and snippets of the matching text.