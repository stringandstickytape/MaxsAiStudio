# FileSearchTool

*Searches for files containing specific terms within a directory tree.*

## Usage

This tool searches the content of files for one or more text strings. It's useful for finding specific keywords, variable names, or phrases in your project.

**Parameters:**
-   `path` (string, required): The directory path to start searching from (relative to the project root).
-   `search_terms` (array of strings, required): An array of text strings to search for. A file is considered a match if its content contains *any* of the provided search terms.
-   `depth` (integer, optional, default: 0): The maximum depth to search recursively. `0` for unlimited depth.

**Tool Specific Extra Properties (Configurable in Tool Library):**
-   `excludedFileExtensions (CSV)`: Comma-separated list of file extensions to ignore (e.g., `.log,.tmp`).
-   `excludedFilePrefixes (CSV)`: Comma-separated list of filename prefixes to ignore (e.g., `temp_,~$`).

## Examples

To find all files under the `src/services` directory that contain either the word "Authentication" or "Authorization":

```json
{
  "path": "src/services",
  "search_terms": ["Authentication", "Authorization"],
  "depth": 0
}
```

## Notes

-   Searches are typically case-insensitive.
-   The output lists each matching file, followed by the line number and the content of up to 10 matching lines (truncated if very long) from that file.
-   Like other file system tools, it respects `.gitignore` and the tool's `ExtraProperties` for exclusions.