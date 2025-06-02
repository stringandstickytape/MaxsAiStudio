# GitHubListContentsTool

*Lists files and directories within a specified path in a GitHub repository using the `/repos/{owner}/{repo}/contents/{path}` endpoint.*

## Usage

This tool helps the AI explore the structure of a GitHub repository by listing the contents (files and subdirectories) of a given path.

**Parameters:**
-   `owner` (string, required): The username or organization owner of the repository.
-   `repo` (string, required): The name of the repository.
-   `path` (string, optional): The path within the repository to list contents from. If omitted, the root of the repository is listed.
-   `ref` (string, optional): The name of the commit, branch, or tag. Defaults to the repository's default branch if not specified.

## Examples

To list the contents of the `src/utils` directory in the `develop` branch of `my-library` owned by `janedoe`:

```json
{
  "owner": "janedoe",
  "repo": "my-library",
  "path": "src/utils",
  "ref": "develop"
}
```

To list the root directory of the default branch:

```json
{
  "owner": "janedoe",
  "repo": "my-library"
}
```

## Notes

-   Requires a GitHub PAT configured in AiStudio4 with appropriate repository access (`repo` or `public_repo` scope).
-   The output includes a list of items, each with its name, type (file or dir), size (for files), path, SHA, and HTML URL.
-   Directories are typically indicated with a folder icon (📁) and files with a document icon (📄) in the formatted output.