# GoogleCustomSearchApiTool

*Performs a search using Google Custom Search API. Requires a configured API Key and a Custom Search Engine ID (cx).*

## Usage

This tool allows the AI to perform targeted web searches using a pre-configured Google Custom Search Engine (CSE). You need to provide your Google Custom Search API key in the application settings and the specific Custom Search Engine ID (cx) as a parameter for each search.

**Parameters:**
-   `query` (string, required): The search term(s).
-   `custom_search_engine_id` (string, required): The ID of your Google Custom Search Engine (CSE). This is found in your CSE control panel.
-   `num_results` (integer, optional, default: 10, min: 1, max: 10): The number of search results to return. Note: The free tier of the Custom Search JSON API is limited to 10 results per query.
-   `start_index` (integer, optional, default: 1, min: 1): The starting index of the results (1-based). Useful for pagination.
-   `language_restrict` (string, optional): Restricts search to documents in a specific language (e.g., "lang_en" for English, "lang_es" for Spanish).
-   `country_restrict` (string, optional): Restricts search to documents from a specific country (e.g., "countryUS", "countryGB").
-   `site_search` (string, optional): Restricts results to a specific website or domain (e.g., "developer.mozilla.org").
-   `exact_terms` (string, optional): Specifies a phrase that all search results must contain.

## Examples

To search for "React state management" using your CSE with ID `YOUR_CSE_ID_HERE`:

```json
{
  "query": "React state management best practices",
  "custom_search_engine_id": "YOUR_CSE_ID_HERE",
  "num_results": 5
}
```

To search for "Python async" tutorials only on `realpython.com` and in English:

```json
{
  "query": "Python async tutorial",
  "custom_search_engine_id": "YOUR_CSE_ID_HERE",
  "site_search": "realpython.com",
  "language_restrict": "lang_en"
}
```

## Notes
-   **API Key Configuration:** You must configure your Google Custom Search API Key in AiStudio4 via `File > Settings > Set Google Custom Search API Key...`.
-   **Custom Search Engine (CSE) ID:** The `custom_search_engine_id` (cx) parameter is **required** for every call. You create and configure CSEs in the [Google Programmable Search Engine control panel](https://programmablesearchengine.google.com/).
-   **Output:** The tool returns a formatted list of search results, including the title, link, and a snippet for each item.
-   **Quotas & Pricing:** Be aware of Google's API quotas and pricing. The free tier provides a limited number of queries per day. Excessive use might require enabling billing in your Google Cloud project.
-   Refer to the [Google Custom Search JSON API documentation](https://developers.google.com/custom-search/v1/using_rest) for more details on parameters and capabilities.