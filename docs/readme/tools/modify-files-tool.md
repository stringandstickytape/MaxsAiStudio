# ModifyFilesTool

*Modifies content within one or more existing files.*

## Usage

This is a powerful tool that allows the AI to make targeted changes to existing files. It takes an array of modifications, where each modification targets a specific file and contains an array of changes to apply to that file. Each change specifies the approximate line number, the exact `oldContent` to find and replace, and the `newContent` to insert.

**Parameters:**
-   `modifications` (array of objects, required): An array where each object represents modifications to a single file.
    -   `path` (string, required): The absolute path to the file to modify. Must be within the Project Path.
    -   `changes` (array of objects, required): An array of changes to make within this file.
        -   `lineNumber` (integer, required): The approximate 1-based line number where `oldContent` is expected. This helps the tool (and a secondary AI, if programmatic replacement fails) locate the correct segment.
        -   `oldContent` (string, required): The exact block of text (can be multi-line) to be replaced. **Crucially, this should include 3-5 lines of context *before* and *after* the specific lines being changed to ensure uniqueness and correct placement.**
        -   `newContent` (string, required): The text that will replace `oldContent`. This should also include the same 3-5 lines of context as `oldContent` if the intention is to replace a block with modifications within that context. If only a single line within the `oldContent` block is changing, the `newContent` must still represent the entire original block but with that one line altered.
        -   `description` (string, required): A human-readable explanation of this specific change.

## Examples

To modify a function in `utils.js` and add a comment in `styles.css`:

```json
{
  "modifications": [
    {
      "path": "C:\\Users\\YourUser\\Projects\\MyWebApp\\src\\utils.js",
      "changes": [
        {
          "lineNumber": 42,
          "oldContent": "function oldFunction(param) {\n  // old logic\n  return param * 2;\n}",
          "newContent": "function newFunction(param) {\n  // new improved logic\n  console.log('Processing:', param);\n  return param + 10;\n}",
          "description": "Refactor oldFunction to newFunction with updated logic and logging."
        }
      ]
    },
    {
      "path": "C:\\Users\\YourUser\\Projects\\MyWebApp\\src\\styles.css",
      "changes": [
        {
          "lineNumber": 15,
          "oldContent": ".button {\n  padding: 10px;\n}",
          "newContent": ".button {\n  /* Updated padding */\n  padding: 12px;\n}",
          "description": "Increase button padding and add a comment."
        }
      ]
    }
  ]
}
```

## Notes

-   **Context is Key:** The `oldContent` and `newContent` must include sufficient surrounding context (3-5 lines before and after the actual change) to ensure the modification is applied correctly. If `oldContent` is too short or not unique, the change might be applied incorrectly or fail.
-   The tool first attempts a programmatic, direct string replacement. If this fails (e.g., `oldContent` not found, or multiple occurrences), it may fall back to a secondary AI model to attempt the modification based on the provided `lineNumber` and `description` as hints. Details of failed programmatic merges are saved to `%APPDATA%/AiStudio4/DebugLogs/MergeFailures/`.
-   Always ensure paths are correct and within the project root.
-   Line numbers are approximate guides; the primary matching is done via `oldContent`.