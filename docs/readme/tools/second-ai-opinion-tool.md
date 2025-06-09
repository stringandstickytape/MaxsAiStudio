# SecondAiOpinionTool

*Gets a second opinion from another configured AI model on a given prompt.*

## Usage

This tool allows the primary AI to query a completely separate, secondary AI model for a "second opinion." This is useful for breaking out of a loop, analyzing a problem from a different perspective, or leveraging the unique strengths of another model for a specific sub-task.

The secondary model operates in a stateless manner; it has **no access** to the current conversation history or any active tools. Therefore, the `prompt` provided to this tool must contain all necessary context for the secondary AI to understand and respond to the request.

## Parameters

-   `prompt` (string, required): The full and self-contained prompt or question to send to the secondary model.
-   `system_prompt` (string, optional): A temporary system prompt to guide the behavior and persona of the secondary model for this specific query. If not provided, a default "helpful assistant" prompt is used.

## Extra Properties (Configuration)

This tool **requires configuration** in the Tool Library UI before it can be used.

-   `model` (string, required): You **must** specify the GUID of the AI model that should act as the "second opinion" provider.
    1.  Go to the Tool Library (`Ctrl+K` -> "Manage Tools").
    2.  Find and edit the `SecondAiOpinionTool`.
    3.  In the "Extra Properties" section, find the `model` key.
    4.  Select the desired alternate model from the dropdown list. This list is populated from your configured models in Settings.

## Examples

### Example 1: Code Review

The primary AI wants a alternate AI to review a piece of code for potential bugs.

```json
{
  "prompt": "Please review the following C# code for potential logic errors or race conditions:\n\n```csharp\npublic class Counter\n{\n    private int count = 0;\n\n    public void Increment()\n    {\n        count++;\n    }\n\n    public int GetCount()\n    {\n        return count;\n    }\n}\n```",
  "system_prompt": "You are an expert C# code reviewer specializing in concurrency issues."
}
```

### Example 2: Creative Brainstorming

The primary AI is generating marketing copy and wants a more creative AI to provide alternative slogans.

```json
{
  "prompt": "Generate three alternative, catchy slogans for a new brand of eco-friendly coffee. The brand's key values are sustainability, fair trade, and high-quality taste.",
  "system_prompt": "You are a witty and creative marketing expert known for crafting memorable slogans."
}
```

## Notes

-   The primary AI calling this tool will receive the alternate AI's full response as text output. It can then decide how to interpret and present this information to you.
-   This tool is powerful for overcoming the inherent biases or limitations of a single AI model during a complex task.
-   The choice of which model to configure as the alternate "opinion" provider is crucial and depends on the tasks you want to accomplish. For example, you might set a highly creative model for brainstorming or a code-specialized model for reviews.