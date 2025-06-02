# AzureDevOpsGetWikiPageContentTool

*Retrieves the content of a specific wiki page from Azure DevOps.*

## Usage

This tool allows the AI to fetch the Markdown content of a specific page within an Azure DevOps project wiki. This is useful for understanding documentation, project guidelines, or any information stored in the wiki.

**Parameters:**
-   `organization` (string, required): Your Azure DevOps organization name.
-   `project` (string, required): The project name.
-   `wiki_id` (string, required): The ID or name of the wiki (e.g., "MyProject.wiki").
-   `path` (string, required): The path to the specific wiki page (e.g., "/Setup/Installation-Guide" or "/Home"). If the path is for a folder, it might return an error or the content of a default page within that folder if one exists.
-   `version` (string, optional): The wiki version (e.g., branch name like "wikiMaster"). If not provided, the default version of the wiki is used.

## Examples

To get the content of the wiki page at path "/Developer-Handbook/Code-Styling" from the "MyProject.wiki" in the `develop` version:

```json
{
  "organization": "MyCompany",
  "project": "MyProject",
  "wiki_id": "MyProject.wiki",
  "path": "/Developer-Handbook/Code-Styling",
  "version": "develop"
}
```

## Notes

-   Requires a PAT with "Wiki (Read)" permissions for the specified project.
-   The `path` parameter should start with a `/`.
-   The tool returns the raw content of the wiki page, which is typically Markdown. The output will be wrapped in a Markdown code block for clarity.
-   If the page or wiki is not found, an error will be returned.