# RetrieveTextFromUrlTool

*Fetches text content from URLs by removing HTML tags.*

## Usage

This tool allows the AI to download the content of a webpage and extract its textual information, stripping away HTML markup. This is useful for providing the AI with the content of articles, blog posts, or documentation pages for analysis or summarization.

**Parameters:**
-   `urls` (string or array of strings, required): A single URL or a list of URLs from which to retrieve text content. URLs must be complete (e.g., `https://example.com/article`).
-   `timeout` (integer, optional, default: 30): The timeout in seconds for each HTTP request.

## Examples

To retrieve text from a single article:

```json
{
  "urls": "https://en.wikipedia.org/wiki/Artificial_intelligence"
}
```

To retrieve text from multiple blog posts:

```json
{
  "urls": [
    "https://myblog.com/post1",
    "https://anotherblog.com/some-article"
  ]
}
```

## Notes

-   The tool attempts to remove scripts, styles, and HTML tags to provide clean text. The quality of extraction can vary depending on the webpage's structure.
-   If multiple URLs are provided, the tool will process each one, and the output will clearly delineate content from each URL.
-   If a URL fails to load or content extraction is problematic, an error message for that specific URL will be included in the results.
-   Respect website `robots.txt` and terms of service. This tool should not be used for aggressive scraping.

## Usage

*Details about how to use the tool, its parameters, and expected output.*

## Examples

*Provide examples of how to use this tool.*

## Notes

*Any specific considerations or tips for using this tool.*