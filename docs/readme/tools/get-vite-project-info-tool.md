# GetViteProjectInfoTool

*Returns information about the Vite project.*

## Usage

This tool inspects a specified Vite project directory (relative to the main Project Path) and provides a summary of its configuration and structure. This helps the AI understand the setup of an existing Vite project.

**Parameters:**
-   `projectDirectory` (string, required): The path to the directory containing the Vite project (e.g., "my-vite-app", or "frontend/client"). This path is relative to the main Project Path set in AiStudio4.

## Examples

If your Vite project is in a subfolder named `my-vite-app` within your main AiStudio4 Project Path:

```json
{
  "projectDirectory": "my-vite-app"
}
```

## Output Example

The tool returns a formatted text summary, including:

```text
# Vite Project Information

## Project: my-vite-app
Version: 0.0.0

## Scripts
```json
{
  "dev": "vite",
  "build": "vite build",
  "preview": "vite preview"
}
```

## Dependencies
```json
{
  "react": "^18.2.0",
  "react-dom": "^18.2.0"
}
```

## Dev Dependencies
```json
{
  "@vitejs/plugin-react": "^4.0.3",
  "vite": "^4.4.5"
}
```

## Vite Configuration
```javascript
// Contents of vite.config.js or vite.config.ts
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [react()],
})
```

## Project Structure
```
src/: main.jsx, App.jsx, ...
public/: vite.svg, ...
```

## Notes

-   The tool reads `package.json` for project name, version, scripts, and dependencies.
-   It attempts to read `vite.config.js` or `vite.config.ts` for Vite-specific configuration.
-   It lists top-level files in common directories like `src` and `public` to give a sense of the project structure.