# ReplaceFileTool

*Replaces an existing file with new content.*

## Usage

This tool is used when the AI needs to completely overwrite the content of an existing file. It's different from `ModifyFilesTool` which makes targeted changes within a file; `ReplaceFileTool` replaces the entire file content.

**Parameters:**
-   `path` (string, required): The absolute path of the file whose content is to be replaced. Must be within the Project Path.
-   `content` (string, required): The new content that will completely replace the existing file's content.
-   `description` (string, required): A brief explanation of why this file's content is being replaced.

## Examples

To replace the content of `README.md` with new information:

```json
{
  "path": "C:\\Users\\YourUser\\Projects\\MyWebApp\\README.md",
  "content": "# New Project Title\n\nThis is the updated README content.\n",
  "description": "Update README with new project title and information."
}
```

Using a relative path:

```json
{
  "path": "README.md",
  "content": "# New Project Title\n\nThis is the updated README content.\n",
  "description": "Update README with new project title and information."
}
```

## Notes

-   The tool will fail if the file specified by `path` does not exist. If you need to create a new file, use `CreateNewFileTool`.
-   The entire existing content of the file will be overwritten by the new `content` provided.
-   Ensure the provided `content` is complete and correct, as the original content will be lost.