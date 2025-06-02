# NpmRunScriptTool

*Runs an npm script defined in a project's `package.json` file.*

## Usage

This tool executes a script specified in the `scripts` section of your `package.json` file. It's equivalent to running `npm run <scriptName>` or `pnpm run <scriptName>` in the terminal.

**Parameters:**
-   `workingDirectory` (string, required): The path to the project directory containing `package.json` (relative to the main Project Path).
-   `scriptName` (string, required): The name of the script to run (e.g., "dev", "build", "test").
-   `args` (array of strings, optional): Additional arguments to pass to the script. These are appended after `--` (e.g., if `args` is `["--port", "8080"]`, the command becomes `npm run myscript -- --port 8080`).

## Examples

To run the `build` script in a project located at `frontend-app`:

```json
{
  "workingDirectory": "frontend-app",
  "scriptName": "build"
}
```

To run a `test` script and pass an argument to watch for changes:

```json
{
  "workingDirectory": "my-library",
  "scriptName": "test",
  "args": ["--watch"]
}
```

## Notes

-   **User Confirmation Required:** Running npm scripts can execute arbitrary code. You will be prompted for confirmation before the command is executed.
-   The tool requires `package.json` to be present in the `workingDirectory` and the specified `scriptName` to be defined within its `scripts` section.
-   It attempts to use `pnpm run` if a `pnpm-lock.yaml` is detected, otherwise defaults to `npm run`.
-   The console output from the executed script will be returned.