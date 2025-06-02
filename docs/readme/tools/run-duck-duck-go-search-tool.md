# RunDuckDuckGoSearchTool

*Searches DuckDuckGo and returns formatted search results.*

## Usage

This tool enables the AI to perform web searches using the DuckDuckGo search engine. It's useful for finding information, current events, or general knowledge when the AI's internal knowledge base is insufficient.

**Parameters:**
-   `query` (string, required): The search query term(s) to send to DuckDuckGo.
-   `maxResults` (integer, optional, default: 10): The maximum number of search results to return. The API typically limits this, so very high numbers might not yield more results.

## Examples

To search for "latest advancements in quantum computing":

```json
{
  "query": "latest advancements in quantum computing",
  "maxResults": 5
}
```

## Notes

-   This tool uses the HTML version of DuckDuckGo for scraping results, so it doesn't require an API key.
-   The output is a formatted list including the title, URL, and a snippet for each search result.
-   The actual URLs are extracted from DuckDuckGo's redirect links to point directly to the source page.
-   The number of results returned may be less than `maxResults` if DuckDuckGo provides fewer matches.

## Usage

*Details about how to use the tool, its parameters, and expected output.*

## Examples

*Provide examples of how to use this tool.*

## Notes

*Any specific considerations or tips for using this tool.*