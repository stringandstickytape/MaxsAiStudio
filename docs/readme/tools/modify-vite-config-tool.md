# ModifyViteConfigTool

*Modifies the Vite configuration file (`vite.config.js` or `vite.config.ts`).*

## Usage

This tool allows the AI to make changes to your Vite project's configuration. It accepts an object where keys represent paths within the Vite config object and values are the new settings to apply.

**Parameters:**
-   `projectDirectory` (string, required): The path to the Vite project directory (relative to the main Project Path).
-   `configChanges` (object, required): An object representing the configuration changes. Keys can be simple (e.g., "base") or nested using dot notation for deeper properties (e.g., "server.port"). The value will be set for that key.

## Examples

To change the `base` path and the `server.port` in a Vite project located at `my-app`:

```json
{
  "projectDirectory": "my-app",
  "configChanges": {
    "base": "/app/",
    "server.port": 3001
  }
}
```

To add or modify the `plugins` array:

```json
{
  "projectDirectory": "my-app",
  "configChanges": {
    "plugins": "[react(), legacy()]" 
  }
}
```
*Note: For `plugins`, the value should be a string representing the JavaScript array content (e.g., `"[myPlugin()]"`). You'll need to ensure any necessary plugin imports are already in the file or added separately (e.g., via `InstallVitePluginTool` or `ModifyFilesTool`).*

## Notes

-   **User Confirmation Required:** This tool will prompt you for confirmation before modifying the Vite configuration file, as it can significantly alter your project's build and development behavior.
-   The tool first tries to find and replace existing properties. If a property doesn't exist, it attempts to add it to the main `defineConfig` object.
-   Modifying complex structures like the `plugins` array via simple key-value replacement can be tricky. The tool has special handling for `plugins` but for very complex manipulations, using `ModifyFilesTool` with the exact code changes might be more reliable.
-   It looks for `vite.config.js` first, then `vite.config.ts`.