# NpmCreateViteTool

*Creates a new Vite project using `npm create vite@latest`.*

## Usage

This tool scaffolds a new Vite project. You can specify the project name, template (e.g., react, vue, vanilla), whether to use TypeScript, and the target directory for creation.

**Parameters:**
-   `projectName` (string, required): The name for the new Vite project (this will also be the folder name created).
-   `template` (string, optional, default: "react"): The framework template to use (e.g., "react", "vue", "vanilla", "svelte", "preact").
-   `typescript` (boolean, optional, default: false): If `true`, uses the TypeScript variant of the chosen template (e.g., "react-ts").
-   `targetDirectory` (string, required): The directory *under which* the new project folder (`projectName`) will be created. This path is relative to the main AiStudio4 Project Path.

## Examples

To create a new React project with TypeScript named `my-new-vite-app` inside the `projects` subfolder of your main Project Path:

```json
{
  "projectName": "my-new-vite-app",
  "template": "react",
  "typescript": true,
  "targetDirectory": "projects"
}
```

If `targetDirectory` is an empty string `""` or `"."`, the project will be created directly in the AiStudio4 Project Path.

## Notes

-   **User Confirmation Required:** This tool will create new files and folders and run `npm create vite@latest`. You will be prompted for confirmation before it proceeds.
-   The `targetDirectory` is relative to your AiStudio4 Project Path. The actual project will be created at `{ProjectPath}/{targetDirectory}/{projectName}`.
-   Ensure Node.js and npm are installed and in your system's PATH.
-   The output will include the console output from the `npm create vite@latest` command.