# AzureDevOpsGetRepositoriesTool

*Retrieves repositories in the specified Azure DevOps project.*

## Usage

Use this tool to list all Git repositories within a given project. You can optionally include hidden repositories and all remote URLs.

**Parameters:**
-   `organization` (string, required): Your Azure DevOps organization name.
-   `project` (string, required): The name of the project for which to list repositories.
-   `include_hidden` (boolean, optional, default: false): Set to `true` to include repositories that are normally hidden in the UI.
-   `include_all_urls` (boolean, optional, default: false): Set to `true` to include all remote URLs associated with each repository (requires `includeLinks=true` in the API call, which this tool handles).

## Examples

To list all repositories, including hidden ones, in `MyProject`:

```json
{
  "organization": "MyCompany",
  "project": "MyProject",
  "include_hidden": true
}
```

## Notes

-   The PAT requires "Code (Read)" permissions for the project.
-   The output includes repository name, ID, default branch, web URL, project name, size, and fork status. If `include_all_urls` is true, additional remote URLs and SSH URLs are also included.