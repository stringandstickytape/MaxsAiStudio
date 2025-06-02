# ReadPartialFilesTool

*Read specified line ranges from one or multiple files. Each file request must specify a path, start line (1-based), and line count (max 500).*

## Usage

This tool is designed for reading specific portions of files, which is useful when the AI only needs a segment of a large file, helping to manage context window limits.

**Parameters:**
-   `requests` (array of objects, required): An array where each object specifies a file and a range of lines to read.
    -   `path` (string, required): The path to the file (relative to Project Path or absolute within it).
    -   `start_line` (integer, required, minimum: 1): The 1-based line number to start reading from.
    -   `line_count` (integer, required, minimum: 1, maximum: 500): The number of lines to read from `start_line`. Capped at 500 lines per request to prevent excessive output.

**Tool Specific Extra Properties (Configurable in Tool Library):**
-   `excludedFileExtensions (CSV)`: Comma-separated list of file extensions to disallow reading.

## Examples

To read lines 10-20 from `MyLargeFile.log` and lines 1-5 from `config.ini`:

```json
{
  "requests": [
    {
      "path": "logs/MyLargeFile.log",
      "start_line": 10,
      "line_count": 11
    },
    {
      "path": "config.ini",
      "start_line": 1,
      "line_count": 5
    }
  ]
}
```

## Notes

-   The output prefixes each segment with `--- File: {path} (lines {start}-{end}) ---`.
-   If `start_line` is beyond the end of the file, it will indicate that no lines were read from that starting point.
-   If `line_count` requests more lines than available from `start_line`, it will read up to the end of the file.
-   The maximum `line_count` per individual file request is 500. To read more lines, the AI must make multiple requests for different ranges.
-   File path and extension exclusion rules apply.