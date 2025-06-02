# DeleteFileTool

*Deletes an existing file.*

## Usage

This tool allows the AI to delete a specific file from your project. The AI must provide the full path to the file.

**Parameters:**
-   `path` (string, required): The absolute path of the file to be deleted. This path must be within the currently set Project Path in AiStudio4.
-   `description` (string, required): A brief explanation of why this file is being deleted.

## Examples

To delete a temporary file named `temp_output.txt` from the project's `data` subfolder:

```json
{
  "path": "C:\\Users\\YourUser\\Projects\\MyWebApp\\data\\temp_output.txt",
  "description": "Remove obsolete temporary output file."
}
```

Or, using a relative path:

```json
{
  "path": "data/temp_output.txt",
  "description": "Remove obsolete temporary output file."
}
```

## Notes

-   This operation is permanent and cannot be undone easily (unless the file is under version control and hasn't been committed/pushed).
-   The tool will fail if the file specified by `path` does not exist.
-   The `description` parameter is important for understanding the AI's reasoning for the deletion.