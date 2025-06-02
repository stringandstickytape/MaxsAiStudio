# DirectoryTreeTool

*Get a recursive tree view of files and directories with customizable depth and filtering. Returns a structured view of the directory tree with files and subdirectories. Directories are marked with trailing slashes. The output is formatted as an indented list for readability. By default, common development directories like .git, node_modules, and venv are noted but not traversed unless explicitly requested. Only works within allowed directories.*

## Usage

This tool provides a text-based representation of a directory's structure, similar to the `tree` command in some operating systems. It helps the AI understand the layout of your project.

**Parameters:**
-   `path` (string, required): The path to the directory to view (relative to your project root, or an absolute path within the project root).
-   `depth` (integer, optional, default: 3): The maximum depth to traverse. `0` means unlimited depth.

**Tool Specific Extra Properties (Configurable in Tool Library):**
-   `excludedFileExtensions (CSV)`: A comma-separated list of file extensions to exclude (e.g., `.obj,.pdb`).
-   `excludedDirectories (CSV)`: A comma-separated list of directory names to exclude from traversal (e.g., `bin,obj,node_modules`).

## Examples

To get a directory tree of the `src/components` folder, up to a depth of 2:

```json
{
  "path": "src/components",
  "depth": 2
}
```

To view the entire project root (use with caution for large projects, depth 0 can be very verbose):

```json
{
  "path": "/", 
  "depth": 0
}
```

## Notes

-   The output is a plain text, indented list. Directories are typically marked with a trailing `/`.
-   The default exclusion list for directories (like `.git`, `node_modules`, `bin`, `obj`) helps keep the output concise and relevant for development tasks. You can customize these exclusions via the tool's `ExtraProperties` in the Tool Library UI.
-   If the specified path does not exist, the tool will return an error message, potentially suggesting alternatives if it finds similarly named directories within the project.