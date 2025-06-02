# AzureDevOpsGetWikiPagesTool

*Retrieves a list of wiki pages from an Azure DevOps wiki, potentially with their hierarchy and content.*

## Usage

This tool helps explore the structure of an Azure DevOps wiki. It can list pages from a specific path, recursively fetch subpages, and optionally include the content of each page.

**Parameters:**
-   `organization` (string, required): Your Azure DevOps organization name.
-   `project` (string, required): The project name.
-   `wiki_id` (string, required): The ID or name of the wiki (e.g., "MyProject.wiki").
-   `path` (string, optional, default: "/"): The path to a specific wiki page or directory to start listing from. Use "/" for the root.
-   `recursion_level` (string, optional, default: "none"): Controls how deep to retrieve pages.
    -   `none`: Gets only the page(s) at the specified `path`.
    -   `oneLevel`: Gets the page(s) at `path` and their direct children.
    -   `full`: Gets all descendants from `path`.
-   `version` (string, optional): The wiki version (e.g., branch name like "wikiMaster"). Defaults to the wiki's default version.
-   `include_content` (boolean, optional, default: false): Set to `true` to include the Markdown content of each page in the response. Be cautious, as this can lead to very large outputs for wikis with many pages.

## Examples

To list all pages under "/User-Guides" and their direct subpages, without content:

```json
{
  "organization": "MyCompany",
  "project": "MyProject",
  "wiki_id": "MyProject.wiki",
  "path": "/User-Guides",
  "recursion_level": "oneLevel",
  "include_content": false
}
```

## Notes

-   Requires a PAT with "Wiki (Read)" permissions.
-   When `include_content: true`, the output can be very large. Use with specific paths or limited recursion if you only need an overview.
-   The output is a formatted list showing the page path, order, and (if requested) its content. Parent pages (folders) are indicated.