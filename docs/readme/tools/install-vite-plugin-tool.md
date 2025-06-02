# InstallVitePluginTool

*Installs a Vite plugin and updates the configuration to use it.*

## Usage

This tool automates the two-step process of installing a Vite plugin via npm (or pnpm/yarn if configured) and then modifying the `vite.config.js` or `vite.config.ts` file to import and include the plugin in the `plugins` array.

**Parameters:**
-   `pluginName` (string, required): The name of the Vite plugin package to install (e.g., "@vitejs/plugin-legacy", "vite-plugin-mkcert").
-   `projectDirectory` (string, required): The path to the directory containing the Vite project (relative to the main Project Path).

## Examples

To install the `@vitejs/plugin-legacy` plugin into a Vite project located in the `frontend` subfolder:

```json
{
  "pluginName": "@vitejs/plugin-legacy",
  "projectDirectory": "frontend"
}
```

## Notes

-   **User Confirmation Required:** Before executing, this tool will prompt you (the user) for confirmation because it involves running `npm install` (which can execute arbitrary code) and modifying project configuration files.
-   The tool installs the plugin as a dev dependency (`--save-dev`).
-   It attempts to intelligently add the import statement for the plugin at the top of the Vite config file and add the plugin to the `plugins` array within the `defineConfig` call.
-   If `vite.config.ts` is found, it assumes a TypeScript project; otherwise, it looks for `vite.config.js`.
-   The output will confirm the success of the installation and configuration update, or provide error messages if issues occur.