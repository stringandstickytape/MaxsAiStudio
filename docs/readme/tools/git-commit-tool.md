# GitCommitTool

*Commits a specified set of files to the git repository with a provided commit message and pushes changes by default. Only files within the project root may be committed.*

## Usage

This tool allows the AI to create a new commit in your project's local Git repository. It's designed for committing changes that the AI might have made using tools like `ModifyFilesTool`, `CreateNewFileTool`, etc. The AI must specify the files to be included in the commit and a commit message.

**Parameters:**
-   `commit` (object, required): An object containing the commit details.
    -   `message` (string, required): The commit message. Must not be empty.
    -   `files` (array of strings, required, minItems: 1): An array of absolute file paths to be included in the commit. Each path must be within the current Project Path.
    -   `push` (boolean, optional): Whether to push changes after committing. Defaults to true.

## Examples

To commit two files, `src/app.js` and `src/components/Header.js`, with the message "Add new feature and update header":

```json
{
  "commit": {
    "message": "Add new feature and update header",
    "files": [
      "C:\\Users\\YourUser\\Projects\\MyWebApp\\src\\app.js",
      "C:\\Users\\YourUser\\Projects\\MyWebApp\\src\\components\\Header.js"
    ],
    "push": true
  }
}
```

Or using relative paths (which will be resolved against the Project Path):

```json
{
  "commit": {
    "message": "Fix typo in README",
    "files": ["README.md"]
  }
}
```

To commit without pushing to remote:

```json
{
  "commit": {
    "message": "Local changes only",
    "files": ["src/temp.js"],
    "push": false
  }
}
```

## Notes

-   **Safety:** This tool will **only** operate on files within the currently set Project Path. It will internally use `git add` for the specified files before committing.
-   **Push by Default:** This tool **pushes changes by default** after creating the commit. Set `push: false` in the commit object to only create a local commit without pushing.
-   **File Status:** The files specified should reflect their intended state for the commit (e.g., modified, newly created, or staged for deletion if that's the intent, though deleting files typically happens via `DeleteFileTool` first and then those changes are committed).
-   The tool will fail if Git is not installed or not found in the system's PATH, or if the Project Path is not a valid Git repository.
-   The output will confirm the success or failure of the commit operation, including any messages from Git.

