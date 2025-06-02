# ReadFilesTool

*Read the contents of one or multiple files.*

## Usage

This tool allows the AI to read the full content of one or more specified files from your project.

**Parameters:**
-   `paths` (string or array of strings, required): A single file path or an array of file paths to read. Paths should be relative to the Project Path or absolute paths within the Project Path.

**Tool Specific Extra Properties (Configurable in Tool Library):**
-   `excludedFileExtensions (CSV)`: A comma-separated list of file extensions to disallow reading (e.g., `.exe,.dll`). If a requested file has an excluded extension, its content will not be returned.

## Examples

To read a single file `main.py`:

```json
{
  "paths": "src/main.py"
}
```

To read multiple files `config.yaml` and `src/utils/helpers.js`:

```json
{
  "paths": ["config.yaml", "src/utils/helpers.js"]
}
```

## Notes

-   The tool will return the content of each successfully read file, prefixed by its path (e.g., `--- File: src/main.py ---`).
-   If a file is not found or cannot be accessed, an error message for that specific file will be included in the output, but the tool will attempt to read other requested files.
-   File paths are subject to project root security restrictions.
-   This tool is for reading entire files. For partial reads, use `ReadPartialFilesTool`.