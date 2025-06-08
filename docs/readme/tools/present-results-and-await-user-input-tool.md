# PresentResultsAndAwaitUserInputTool

*Use the tool to present results, findings, or completed work to the user and then explicitly await their input. This tool is for delivering final outputs, summaries, or conclusions and then pausing to wait for user feedback, approval, or next instructions. This tool's operation will stop AI processing to wait for the user.*

## Usage

This tool allows the AI to present completed work, analysis results, or findings to the user and then pause its operation to wait for explicit input or feedback from the user. The content of the `results` parameter is typically the final output or summary that the AI wants to deliver before stopping execution.

**Parameters:**
-   `results` (string, required): The results, findings, or completed work to present to the user before pausing.

## Examples

After completing a code analysis, the AI might use `PresentResultsAndAwaitUserInputTool`:

```json
{
  "results": "I have completed the analysis of your authentication module. Here are my findings:\n\n1. Security Issues Found: 2 critical, 3 medium priority\n2. Performance Bottlenecks: 1 in the token validation process\n3. Code Quality: Overall good, but 5 methods could be refactored\n\nDetailed Report:\n- Critical Issue #1: Password hashing uses deprecated MD5\n- Critical Issue #2: JWT tokens never expire\n\nRecommended next steps:\n1. Upgrade to bcrypt for password hashing\n2. Implement token expiration (suggest 24 hours)\n3. Add rate limiting to login endpoints\n\nWould you like me to proceed with implementing these fixes, or do you have questions about any of these findings?"
}
```

After completing a file organization task, the AI might use it to summarize what was done:

```json
{
  "results": "File organization complete! Here's what I accomplished:\n\n✅ Moved 23 files to appropriate directories\n✅ Created 4 new folders: /components, /utils, /types, /tests\n✅ Updated 12 import statements to reflect new paths\n✅ Removed 3 duplicate files\n\nAll tests are still passing and the build is successful. The project structure is now much cleaner and follows standard conventions. What would you like me to work on next?"
}
```

## Notes

-   The `PresentResultsAndAwaitUserInputTool` **always** pauses the AI's execution and waits for the user to respond. This is its primary purpose.
-   This tool is ideal for delivering completed work, analysis results, summaries, or any final output that requires user review or approval before proceeding.
-   Unlike `ThinkAndContinueTool` (which continues processing) or `ThinkAndAwaitUserInputTool` (which is for planning/reasoning), this tool is specifically for presenting finished results.
-   The user's subsequent input will then guide the AI's next actions or provide feedback on the presented results.