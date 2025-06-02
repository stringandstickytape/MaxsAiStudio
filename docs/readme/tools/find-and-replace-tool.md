# FindAndReplaceTool

*Finds and replaces text in files within a directory tree.*

## Usage

This tool allows the AI to perform find and replace operations across multiple files in your project. It can be used for refactoring, correcting common typos, or updating boilerplate text.

**Parameters:**
-   `path` (string, required): The directory path to start searching from (relative to the project root).
-   `replacements` (array of objects, required): An array where each object specifies a find/replace pair.
    -   `search` (string, required): The text to search for.
    -   `replace` (string, required): The text to replace the `search` text with.
    -   `case_sensitive` (boolean, optional, default: false): Whether the search should be case-sensitive.
-   `depth` (integer, optional, default: 0): The maximum depth to search recursively. `0` for unlimited depth.
-   `include_filtered` (boolean, optional, default: false): If `true`, includes files and directories normally filtered by `.gitignore`.
-   `preview_only` (boolean, optional, default: true): If `true`, the tool will only report what changes *would* be made, without actually modifying files. **It is highly recommended to run with `preview_only: true` first.**

**Tool Specific Extra Properties (Configurable in Tool Library):**
-   `excludedFileExtensions (CSV)`: Comma-separated list of file extensions to ignore.
-   `excludedFilePrefixes (CSV)`: Comma-separated list of filename prefixes to ignore.

## Examples

To preview replacing all occurrences of "oldApiEndpoint" with "newApiEndpoint" (case-insensitive) in files under the `src` directory:

```json
{
  "path": "src",
  "replacements": [
    {
      "search": "oldApiEndpoint",
      "replace": "newApiEndpoint",
      "case_sensitive": false
    }
  ],
  "preview_only": true
}
```

To actually perform the replacement after previewing:

```json
{
  "path": "src",
  "replacements": [
    {
      "search": "oldApiEndpoint",
      "replace": "newApiEndpoint"
    }
  ],
  "preview_only": false
}
```

## Notes

-   **Use `preview_only: true` extensively before making actual changes, especially with broad search terms.**
-   The tool outputs a summary of changes, including the number of files affected and total replacements made (or that would be made in preview mode).
-   For each affected file, it will list the lines that were (or would be) modified, showing the original and new content for up to 10 changes per file.
-   Be cautious with case-insensitive replacements, as they can have unintended consequences.
-   Ensure your project is under version control before running with `preview_only: false`.