# RecordMistakeTool

*Records AI mistakes for future reference and helps consolidate them into a guide.*

## Usage

When the AI makes an error, especially one related to code modification or understanding the project, it can use this tool to log the mistake. This helps in building a `CommonAiMistakes.md` file in your project root, which can then be incorporated into the AI's system prompt to prevent similar errors in the future.

**Parameters:**
-   `mistake_title` (string, required): A brief, descriptive title for the mistake (e.g., "Incorrectly Modified Loop Condition").
-   `mistake_description` (string, required): A succinct description of what went wrong.
-   `root_cause` (string, required): A concise analysis of why the mistake occurred (e.g., "Misinterpreted the loop's termination criteria").
-   `prevention_strategy` (string, required): How to avoid making this mistake in the future (e.g., "Always double-check loop boundaries and off-by-one errors, especially with `<` vs. `<=`.").

## Examples

If the AI used `ModifyFilesTool` and accidentally removed an important line:

```json
{
  "mistake_title": "Accidental Line Deletion in ModifyFiles",
  "mistake_description": "When modifying a block of code using ModifyFilesTool, an essential line of existing code was omitted from the newContent, effectively deleting it.",
  "root_cause": "The newContent provided to ModifyFilesTool did not fully replicate the surrounding context of the oldContent, leading to an incomplete replacement.",
  "prevention_strategy": "Ensure 'newContent' in ModifyFilesTool includes all necessary surrounding context lines from 'oldContent', only altering the specific lines intended for change. Double-check that the diff between oldContent and newContent accurately reflects *only* the desired modifications."
}
```

## Notes

-   When this tool is called, it appends the formatted mistake to `CommonAiMistakes.md` in your project root. If the file doesn't exist, it's created.
-   After appending, the tool then **uses the secondary AI model** to read the entire `CommonAiMistakes.md` file and generate a *new, consolidated guide to error prevention*. This consolidated version then **replaces** the content of `CommonAiMistakes.md`.
-   The idea is to maintain a single, well-organized list of common pitfalls and prevention strategies that can be fed back to the AI via its system prompt (by including `{CommonAiMistakes}` in the system prompt content).