# GeminiGoogleSearchTool

*Performs a Google Search using a configured Gemini model that has built-in search capabilities. Returns a summary of the search results and key information.*

## Usage

This tool leverages Google's Gemini models that have built-in Google Search capabilities to perform web searches. Unlike the GoogleCustomSearchApiTool which requires a Custom Search Engine ID, this tool uses Gemini's native search integration to provide search results with intelligent summarization.

**Parameters:**
-   `query` (string, required): The search query to be executed by the Gemini model.
-   `custom_instructions` (string, optional): Specific instructions for the Gemini model on how to process or summarize the search results. If not provided, Gemini will provide a general summary and list of results.

**Extra Properties:**
-   `geminiModelFriendlyNameToUse (Optional)`: Specify a particular Gemini model by its friendly name. If not provided, the tool will auto-select the most suitable Gemini Pro/Flash model.

## Examples

Basic search query:

```json
{
  "query": "latest developments in artificial intelligence 2024"
}
```

Search with custom processing instructions:

```json
{
  "query": "climate change research",
  "custom_instructions": "Focus on academic papers published in the last year and provide a bulleted list of the top 3 findings"
}
```

Search for specific information with detailed instructions:

```json
{
  "query": "React performance optimization techniques",
  "custom_instructions": "Provide a summary focused on practical implementation tips and include code examples if available in the search results"
}
```

## Configuration

### Model Requirements
- Requires a configured Gemini model with the service type "Gemini"
- The model should be a Pro or Flash variant (e.g., "gemini-pro", "gemini-flash")
- The tool automatically selects the most suitable model if none is specified

### Extra Properties Configuration
You can specify a particular Gemini model to use by setting the extra property:
- **Key:** `geminiModelFriendlyNameToUse (Optional)`
- **Value:** The friendly name of the Gemini model you want to use (e.g., "Gemini Pro", "Gemini Flash")

## Notes
-   **Model Selection:** The tool automatically identifies suitable Gemini models that support search functionality. It prefers models with "latest" in the name, then "pro" variants.
-   **Search Capabilities:** This tool uses Gemini's built-in Google Search integration, which may provide different results and formatting compared to direct Google Custom Search API calls.
-   **Processing:** Gemini can intelligently process and summarize search results based on your custom instructions, making it useful for research and information gathering tasks.
-   **No API Key Required:** Unlike GoogleCustomSearchApiTool, this tool doesn't require a separate Google Custom Search API key - it uses your configured Gemini model's built-in search capabilities.
-   **Service Provider:** Ensure your Gemini model is configured with the "Gemini" service type (not "NetOpenAi") to access the native search functionality.

## Comparison with GoogleCustomSearchApiTool

| Feature | GeminiGoogleSearchTool | GoogleCustomSearchApiTool |
|---------|------------------------|---------------------------|
| API Key Required | No (uses Gemini model) | Yes (Google Custom Search API) |
| Search Engine | Gemini's built-in search | Custom Search Engine (CSE) |
| Result Processing | AI-powered summarization | Raw formatted results |
| Customization | Custom instructions for AI processing | Search parameters (site, language, etc.) |
| Result Format | AI-generated summary and analysis | Structured list with titles, links, snippets |

Choose GeminiGoogleSearchTool when you want AI-powered analysis and summarization of search results, and GoogleCustomSearchApiTool when you need precise control over search parameters and raw result data.