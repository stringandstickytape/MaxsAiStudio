# RenameFileTool

*Renames an existing file to a new path.*

## Usage

This tool allows the AI to rename or move a file within the project. It requires the current (old) path and the desired new path.

**Parameters:**
-   `path` (string, required): The current absolute path of the file to be renamed. Must be within the Project Path.
-   `newPath` (string, required): The new absolute path for the file. Must also be within the Project Path.
-   `description` (string, required): A brief explanation of why this file is being renamed.

## Examples

To rename `old_module.js` to `new_module.ts` in the `src/lib` directory:

```json
{
  "path": "C:\\Users\\YourUser\\Projects\\MyWebApp\\src\\lib\\old_module.js",
  "newPath": "C:\\Users\\YourUser\\Projects\\MyWebApp\\src\\lib\\new_module.ts",
  "description": "Rename module to reflect TypeScript migration and new functionality."
}
```

Using relative paths:

```json
{
  "path": "src/lib/old_module.js",
  "newPath": "src/lib/new_module.ts",
  "description": "Rename module to reflect TypeScript migration and new functionality."
}
```

## Notes

-   Both `path` and `newPath` must be within the allowed project root.
-   The tool will fail if the source file (`path`) does not exist.
-   The tool will fail if a file or directory already exists at `newPath`.
-   Any necessary parent directories for `newPath` will be created if they don't exist.