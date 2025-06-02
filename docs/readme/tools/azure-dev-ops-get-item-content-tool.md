# AzureDevOpsGetItemContentTool

*Retrieves the content of a specific file from an Azure DevOps repository.*

## Usage

This tool fetches and returns the content of a file at a specific version (branch, commit, or tag) in your repository.

**Parameters:**
-   `organization` (string, required): Your Azure DevOps organization name.
-   `project` (string, required): The project name.
-   `repository_id` (string, required): The name or ID of the repository.
-   `path` (string, required): The full path to the file within the repository (e.g., "/src/app/main.cs").
-   `version_type` (string, optional, default: "branch"): The type of version to retrieve from. Can be "branch", "commit", or "tag".
-   `version` (string, optional, default: "main" or repository's default branch): The specific branch name, commit SHA, or tag name.

## Examples

To get the content of `package.json` from the `develop` branch:

```json
{
  "organization": "MyCompany",
  "project": "MyProject",
  "repository_id": "WebAppRepo",
  "path": "/package.json",
  "version_type": "branch",
  "version": "develop"
}
```

## Notes

-   The PAT needs "Code (Read)" permissions.
-   Binary files will be indicated as such; their content won't be displayed directly but can be inferred from the context.
-   The tool will attempt to provide a language hint for syntax highlighting based on the file extension.