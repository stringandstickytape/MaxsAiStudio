# StartViteDevServerTool

*Starts the Vite development server for a project.*

## Usage

This tool initiates the Vite development server (`npm run dev` or equivalent) in a specified project directory. This is typically used to run a web application locally for development or testing.

**Parameters:**
-   `workingDirectory` (string, required): The path to the Vite project directory containing `package.json` (relative to the main Project Path).
-   `port` (integer, optional, default: 5173): The port number on which to run the Vite dev server. *Note: The tool currently defaults to 5174, but Vite's own default is 5173. This might be an AiStudio4 specific default to avoid conflicts.*
-   `host` (string, optional, default: "localhost"): The host address to bind the server to (e.g., "localhost", "0.0.0.0").

## Examples

To start the Vite dev server for a project in `my-vite-project` subfolder on the default port (5174 as per current tool default):

```json
{
  "workingDirectory": "my-vite-project"
}
```

To start it on port 3000 and make it accessible on the local network:

```json
{
  "workingDirectory": "my-vite-project",
  "port": 3000,
  "host": "0.0.0.0"
}
```

## Notes

-   **User Confirmation Required:** This tool will execute a command that starts a web server and potentially opens network ports. You will be prompted for confirmation before it proceeds.
-   The tool requires `package.json` and a Vite dev script (usually `dev` in `package.json`) to be present in the `workingDirectory`.
-   If a Vite dev server is already running from a previous call by this tool, it will be stopped before a new one is started.
-   The tool attempts to start the server in a new minimized command window (`cmd /K start /MIN ...`).
-   The output will confirm that the server has been started and usually includes the URL (e.g., `http://localhost:5174`). The actual server output (logs, etc.) will be in the new command window, not returned by the tool itself, though a small snippet of initial output might be captured.