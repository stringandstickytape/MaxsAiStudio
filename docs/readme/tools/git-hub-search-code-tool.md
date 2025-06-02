# GitHubSearchCodeTool

*Searches for code using GitHub's code search API via the `/search/code` endpoint.*

## Usage

This tool allows the AI to perform powerful code searches across GitHub. It uses GitHub's search syntax, which supports various qualifiers like `repo:`, `user:`, `org:`, `language:`, `path:`, `filename:`, in:file`, `in:path`, etc.

**Parameters:**
-   `q` (string, required): The search query. This should follow GitHub's code search syntax (e.g., "MyFunction repo:owner/myrepo language:python").
-   `per_page` (integer, optional, default: 30, max: 100): Number of results to return per page.
-   `page` (integer, optional, default: 1): Page number for paginated results.
-   `sort` (string, optional): Sort field. Can be "indexed" (default if not specified) or "best-match". `best-match` is often preferred for relevance.
-   `order` (string, optional, default: "desc"): Sort order. Can be "desc" or "asc".

## Examples

To search for the term `useState` in JavaScript files within the `facebook/react` repository, sorted by best match:

```json
{
  "q": "useState repo:facebook/react language:javascript",
  "sort": "best-match",
  "per_page": 5
}
```

## Notes

-   A GitHub PAT with appropriate scopes is required, configured in AiStudio4.
-   GitHub's code search API has rate limits and may sometimes return incomplete results for complex queries or very large repositories.
-   The output includes total result count, page information, and for each matching item: file name, path, repository, HTML URL, and snippets of matching code fragments.