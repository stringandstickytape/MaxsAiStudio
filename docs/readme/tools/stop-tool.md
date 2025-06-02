# StopTool

*A tool which allows the AI to indicate that all outstanding tasks are completed, or it cannot proceed any further.*

## Usage

This tool is used by the AI to signal the end of its current multi-step process or tool loop. It indicates that it has either completed the requested task or has reached a point where it cannot continue with the current set of tools or information.

**Parameters:**
-   `param` (string, optional): A final message or summary from the AI regarding the completion or stoppage of the task. This message will be included in the AI's final response to you.

## Examples

If the AI has successfully refactored a file and has no further actions:

```json
{
  "param": "File refactoring complete. All changes have been applied and validated."
}
```

If the AI determines it cannot fulfill a request after several attempts:

```json
{
  "param": "I am unable to find the specific information requested after multiple search attempts. You may need to provide more specific keywords or a different data source."
}
```

## Notes

-   When the `StopTool` is called, the tool processing loop in AiStudio4 will terminate for the current user prompt.
-   The AI's response to you will typically include the content of the `param` field (if provided), followed by any accumulated text from its `ThinkTool` calls or other non-actionable tool outputs during the loop.
-   This tool is crucial for preventing infinite loops and for the AI to gracefully conclude its operations.