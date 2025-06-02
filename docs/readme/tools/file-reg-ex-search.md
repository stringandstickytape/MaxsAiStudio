# FileRegExSearch

*Searches for files containing lines matching any of the provided regular expressions within a directory tree.*

## Usage

This tool allows for powerful pattern-based searches within your project files. It's more flexible than simple string matching, enabling complex queries like finding specific function signatures, comments, or data structures.

**Parameters:**
-   `path` (string, required): The directory path to start searching from (relative to the project root).
-   `search_regexes` (array of strings, required): An array of regular expressions. A file is considered a match if any of its lines match *any* of the provided regex patterns.
-   `depth` (integer, optional, default: 0): The maximum depth to search recursively. `0` for unlimited depth.

**Tool Specific Extra Properties (Configurable in Tool Library):**
-   `excludedFileExtensions (CSV)`: Comma-separated list of file extensions to ignore (e.g., `.log,.tmp`).
-   `excludedFilePrefixes (CSV)`: Comma-separated list of filename prefixes to ignore (e.g., `temp_,~$`).

## Examples

To find all files under the `src` directory that contain either a `// TODO:` comment or a function definition like `function exampleFunc(...)`:

```json
{
  "path": "src",
  "search_regexes": [
    "//\\s*TODO:",
    "function\\s+[A-Za-z_][A-Za-z0-9_]*\\s*\\("
  ],
  "depth": 0
}
```

## Notes

-   Regular expressions are typically case-insensitive by default with this tool, but always check the specific AI model's capabilities or the tool's implementation if case sensitivity is critical.
-   Complex or poorly written regexes can be slow. Keep them as specific as possible.
-   The output will list each matching file, followed by the line number and the content of up to 10 matching lines (truncated if very long) within that file.
-   File and directory exclusion rules (like `.gitignore` and the tool's `ExtraProperties`) are respected.