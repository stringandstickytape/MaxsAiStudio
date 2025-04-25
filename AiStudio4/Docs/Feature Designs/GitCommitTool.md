// Docs/Feature Designs/GitCommitTool.md

# Feature Design: GitCommit Tool

## Overview
The `GitCommit` tool provides a safe, auditable way for the AI to commit selected files to the project's git repository. It is strictly limited to the commit operation (no add, push, pull, or other git commands). The AI must specify both the commit message and the exact list of files to include in the commit. All file paths are validated to ensure they are within the project root, and no operation is permitted outside this boundary.

## Rationale
- **Safety:** Prevents accidental or malicious commits outside the project root.
- **Transparency:** Requires explicit file list and commit message from the AI.
- **Consistency:** Follows the established tool schema and validation patterns (see `CodeDiffTool`).
- **Auditability:** All actions are logged, and the result is reported to the user.

## Tool Schema
```json
{
  "name": "GitCommit",
  "description": "Commits a specified set of files to the git repository with a provided commit message. Only files within the project root may be committed. No other git operations are permitted.",
  "input_schema": {
    "type": "object",
    "properties": {
      "commit": {
        "type": "object",
        "description": "The commit operation parameters.",
        "properties": {
          "message": {
            "type": "string",
            "description": "The commit message to use. Must be non-empty."
          },
          "files": {
            "type": "array",
            "description": "An array of absolute file paths to commit. Each must be within the project root.",
            "items": {
              "type": "string"
            },
            "minItems": 1
          }
        },
        "required": ["message", "files"]
      }
    },
    "required": ["commit"]
  }
}
```

## Safety & Validation
- **Project Root Enforcement:** All file paths are normalized and checked to ensure they are strictly within the project root. Any violation aborts the operation.
- **File Existence:** Each file must exist at the time of commit; missing files cause validation failure.
- **No Wildcards:** Only explicitly listed files are committed. No directory or wildcard expansion is allowed.
- **Commit Message:** Must be a non-empty string.
- **No Other Git Commands:** Only `git commit` is run (with `git add` for the specified files if needed). No push, pull, or branch operations.
- **Logging:** All actions and validation steps are logged for auditability.
- **Status Feedback:** The tool reports progress and errors via status messages, following the pattern in `BaseToolImplementation`.

## Processing Logic
1. **Parse and Validate Input:**
    - Ensure `commit.message` is non-empty.
    - Ensure `commit.files` is a non-empty array of strings.
    - For each file:
        - Normalize path and check it is within project root.
        - Check file exists.
2. **Prepare Commit:**
    - Stage only the specified files using `git add -- <file>` for each.
    - Run `git commit -m <message> -- <file1> <file2> ...`.
    - Capture stdout/stderr for reporting.
3. **Report Result:**
    - Return a JSON summary including success/failure, committed files, and any error messages.
    - Send status updates at each major step.

## Example Input
```json
{
  "commit": {
    "message": "Refactor data access layer for clarity and testability.",
    "files": [
      "C:\\Users\\maxhe\\source\\repos\\CloneTest\\MaxsAiTool\\AiStudio4\\Core\\Data\\Repository.cs",
      "C:\\Users\\maxhe\\source\\repos\\CloneTest\\MaxsAiTool\\AiStudio4\\Core\\Data\\UnitOfWork.cs"
    ]
  }
}
```

## Expected Outcomes
- Only the listed files are staged and committed.
- If any file is outside the project root or missing, the operation fails with a clear error.
- The commit message is exactly as provided.
- The user receives status updates and a final summary of the operation.

## Extension & Future Work
- Optionally, support for amending the previous commit (with explicit flag).
- Optionally, support for dry-run mode to preview the commit.
- Optionally, allow the AI to request a diff summary before committing.

---
*This design follows the conventions and safety practices established in the existing tool implementations, especially `CodeDiffTool`.*