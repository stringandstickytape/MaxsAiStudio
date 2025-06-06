# ReadPartialFilesTool

*Read specified line ranges or character ranges from one or multiple files. Each file request must specify either line-based parameters (start_line, line_count) or character-based parameters (start_character, length).*

## Usage

This tool is designed for reading specific portions of files, which is useful when the AI only needs a segment of a large file, helping to manage context window limits. The tool supports two reading modes: line-based and character-based.

**Parameters:**
-   `requests` (array of objects, required): An array where each object specifies a file and either a range of lines or a range of characters to read.
    -   `path` (string, required): The path to the file (relative to Project Path or absolute within it).
    
**For Line-Based Reading:**
    -   `start_line` (integer, minimum: 1): The 1-based line number to start reading from.
    -   `line_count` (integer, minimum: 1, maximum: 500): The number of lines to read from `start_line`. Capped at 500 lines per request to prevent excessive output.
    
**For Character-Based Reading:**
    -   `start_character` (integer, minimum: 0): The 0-based character position to start reading from.
    -   `length` (integer, minimum: 1, maximum: 50000): The number of characters to read from `start_character`. Capped at 50,000 characters per request to prevent excessive output.

**Note:** Each request must specify either line-based parameters (start_line, line_count) OR character-based parameters (start_character, length), but not both.

**Tool Specific Extra Properties (Configurable in Tool Library):**
-   `excludedFileExtensions (CSV)`: Comma-separated list of file extensions to disallow reading.

## Examples

### Line-Based Reading
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

### Character-Based Reading
To read the first 1000 characters from a minified JavaScript file and characters 5000-6000 from a large JSON file:

```json
{
  "requests": [
    {
      "path": "dist/app.min.js",
      "start_character": 0,
      "length": 1000
    },
    {
      "path": "data/large-dataset.json",
      "start_character": 5000,
      "length": 1000
    }
  ]
}
```

## Notes

### Line-Based Reading:
-   The output prefixes each segment with `--- File: {path} (lines {start}-{end}) ---`.
-   If `start_line` is beyond the end of the file, it will indicate that no lines were read from that starting point.
-   If `line_count` requests more lines than available from `start_line`, it will read up to the end of the file.
-   The maximum `line_count` per individual file request is 500. To read more lines, the AI must make multiple requests for different ranges.

### Character-Based Reading:
-   The output prefixes each segment with `--- File: {path} (characters {start}-{end}) ---`.
-   If `start_character` is beyond the end of the file, it will indicate that no characters were read from that starting point.
-   If `length` requests more characters than available from `start_character`, it will read up to the end of the file.
-   The maximum `length` per individual file request is 50,000 characters. To read more characters, the AI must make multiple requests for different ranges.
-   Character positions are 0-based (first character is at position 0).

### General:
-   File path and extension exclusion rules apply to both reading modes.
-   Each request must use either line-based OR character-based parameters, not both.
-   Character-based reading is particularly useful for minified files, large JSON files, or when you need to read a specific byte range.