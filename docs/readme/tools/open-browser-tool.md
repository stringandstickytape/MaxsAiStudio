# OpenBrowserTool

*Opens a URL in the default or specified web browser.*

## Usage

This tool allows the AI to launch a web URL. It's primarily used by other tools (like `StartViteDevServerTool`) to automatically open the application or relevant page after a server starts.

**Parameters:**
-   `url` (string, required): The URL to open. Must be a fully qualified URL (e.g., `http://localhost:5173` or `https://example.com`).
-   `browser` (string, optional): The name or path of a specific browser executable to use. If omitted, the system's default web browser is used.

## Examples

To open the local Vite development server URL:

```json
{
  "url": "http://localhost:5173"
}
```

To attempt to open a URL with Google Chrome (path might vary):

```json
{
  "url": "https://vitejs.dev/",
  "browser": "chrome.exe"
}
```

## Notes

-   The URL must be valid and include the protocol (http/https).
-   If a specific `browser` is specified and not found or fails to launch, the tool might error or do nothing, depending on the system configuration.
-   This tool typically doesn't produce extensive output, other than confirming the action was attempted.