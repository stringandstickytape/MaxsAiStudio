# LaunchUrlTool

*Launches one or more URLs in the default web browser.*

## Usage

This tool allows the AI to open web pages in your system's default browser. It can be used to direct you to documentation, search results, or any other web resource.

**Parameters:**
-   `urls` (string or array of strings, required): A single URL or a list of URLs to open. Each URL must be a complete, valid web address (e.g., starting with `http://` or `https://`).

## Examples

To open a single URL:

```json
{
  "urls": "https://www.typescriptlang.org/docs/"
}
```

To open multiple URLs (each will typically open in a new tab or window, depending on your browser settings):

```json
{
  "urls": [
    "https://react.dev/learn",
    "https://vitejs.dev/guide/"
  ]
}
```

## Notes

-   The tool uses your operating system's default mechanism for opening URLs, which usually means your default web browser.
-   Ensure URLs are valid and complete (including the protocol `http://` or `https://`). Invalid URLs may fail to open or open an error page.
-   The tool will report success or failure for each URL it attempts to launch.