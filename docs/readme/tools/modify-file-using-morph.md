# ModifyFileUsingMorph

*Use this tool to propose an edit to an existing file.*

## Usage

This tool is designed to be used by the AI to propose changes to a file using a special diff-like syntax. It is particularly useful when the AI needs to make a small change to a large file, as it avoids the need to send the entire file content back and forth.

**Parameters:**
- `target_file` (string, required): The full path of the file to be modified.
- `update_snippet` (string, required): The code edits in the special format, using `// ... existing` to denote unchanged code.

## Examples

To change a single line in a file:

```json
{
  "target_file": "C:\Users\YourUser\Projects\MyWebApp\src\app.js",
  "update_snippet": "// ... existing\nconst oldVariable = 1;\n// ... existing"
}
```

## Notes

- This tool relies on a secondary AI model to apply the changes. The `update_snippet` should be clear and unambiguous.
- The tool will fail if the `target_file` does not exist.
