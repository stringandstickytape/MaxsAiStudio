# InfoRequestTool

*A tool which allows you to request more information from the user.*

## Usage

This tool is used by the AI when it needs clarification or additional input from you to proceed with a task. When the AI calls this tool, it typically means the tool loop will pause, and the AI's message (your request for information) will be presented to you. You can then respond in the chat.

**Parameters:**
-   `message` (string, required): The question or request for information that the AI wants to ask you.

## Examples

If the AI needs to know which specific version of a library to install, it might use this tool like so:

```json
{
  "message": "I found multiple versions of 'my-library' (1.2.0, 1.2.5, 1.3.0). Which version should I install?"
}
```

Your response to this message in the chat will then be used by the AI to continue its task (e.g., by calling `NpmInstallTool` with the specified version).

## Notes

-   This tool primarily serves as a communication mechanism from the AI back to you *during a tool loop*.
-   When this tool is called, the tool processing loop usually **pauses** until you provide a response. This is different from the `ThinkAndContinueTool`, which logs a thought and generally continues the loop, or the `StopTool`, which terminates the loop.
-   Your reply in the chat becomes the next input for the AI to continue its processing.