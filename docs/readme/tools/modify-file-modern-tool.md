# Modify File Modern Tool

Path: AiStudio4/Core/Tools/ModifyFileModernTool.cs

Summary
The Modify File Modern tool performs atomic, line-based modifications to a single file. It applies one or more change blocks in sequence and reverts the file if any change fails. It preserves original encoding/BOM and newline style and always returns continueProcessing=true so callers can recover or proceed.

Key capabilities
- Pure line-based matching and replacement (no partial-line edits)
- Multiple changes in a single call, applied sequentially
- Optional whitespace-tolerant matching (TrimEnd)
- Control for multiple matches: apply first, fail, or apply all occurrences
- Automatic revert on any failure (with original encoding/BOM preserved)
- Concurrency guard per file
- Diagnostics for no-match cases using Levenshtein proximity for the first line

Tool name
ModifyFileModern

Input schema
{
  "name": "ModifyFileModern",
  "description": "Modifies a single file atomically using an array of line-based changes. If any change fails, the file is reverted and a detailed error is returned with continueProcessing=true.",
  "input_schema": {
    "type": "object",
    "properties": {
      "path": { "type": "string", "description": "Absolute path to the file to modify (must be inside the project root)" },
      "whitespaceTolerant": { "type": "boolean", "description": "If true, match compares lines using TrimEnd() (ignore trailing whitespace). Default: true.", "default": true },
      "strictMultipleMatches": { "type": "boolean", "description": "If true, fail (and revert) when oldContent matches more than once. If false, apply to first and warn. Default: false.", "default": false },
      "applyAllOccurrences": { "type": "boolean", "description": "If true, replaces all occurrences of oldContent blocks; otherwise only the first match is replaced.", "default": false },
      "changes": {
        "type": "array",
        "description": "Array of line-based changes to apply sequentially to this single file.",
        "items": {
          "type": "object",
          "properties": {
            "oldContent": { "type": "string", "description": "Original content to match on whole-line boundaries only. Should include the full lines to be replaced, plus at least three lines of context either side." },
            "newContent": { "type": "string", "description": "Replacement content written on whole-line boundaries only." },
            "description": { "type": "string", "description": "Human-readable explanation of this change." }
          },
          "required": ["oldContent", "newContent", "description"]
        }
      }
    },
    "required": ["path", "changes"]
  }
}

Behavior and guarantees
- Atomicity: If any change fails, the file is reverted to its original content.
- Encoding/BOM: Original encoding and BOM are detected and preserved for both success and revert paths.
- Newlines: Original newline style (LF/CRLF) and trailing-final-newline are preserved.
- Concurrency: Per-file lock prevents interleaved modifications.
- Continue processing: The tool always sets continueProcessing=true in its result metadata.

Best practices for callers
- Provide 3–5 lines of context before and after the target lines in oldContent to uniquely identify the block.
- Avoid single-line oldContent where possible. The tool will emit warningOldContentTooSmall when oldContent < 3 lines.
- Use whitespaceTolerant=true when trailing whitespace might differ.
- If multiple identical blocks may exist, set strictMultipleMatches=true to fail fast, or applyAllOccurrences=true to replace all.

Example request
{
  "path": "C:/path/to/file.cs",
  "whitespaceTolerant": true,
  "strictMultipleMatches": false,
  "applyAllOccurrences": false,
  "changes": [
    {
      "oldContent": "// before line A\nvar x = 1;\n// after line A",
      "newContent": "// before line A\nvar x = 2;\n// after line A",
      "description": "Bump x from 1 to 2"
    }
  ]
}

Example success response (excerpt)
{
  "summary": {
    "path": "C:/path/to/file.cs",
    "changeCount": 1,
    "success": true,
    "whitespaceTolerant": true,
    "strictMultipleMatches": false,
    "applyAllOccurrences": false,
    "preservedTrailingNewline": true,
    "message": "All line-based changes applied successfully."
  },
  "changes": [
    {
      "index": 1,
      "description": "Bump x from 1 to 2",
      "matchedCount": 1,
      "appliedAtIndex": 120,
      "replacedLineCount": 3
    }
  ]
}

Example failure response (excerpt)
{
  "success": false,
  "continueProcessing": true,
  "error": "Change 1 failed: oldContent is empty.",
  "errorDetails": {
    "message": "Change 1 failed: oldContent is empty.",
    "changeIndex": 1
  }
}

Notes
- Matching and replacement are performed on whole lines only; partial line edits are not supported.
- The tool provides closest single-line match diagnostics if oldContent cannot be found.