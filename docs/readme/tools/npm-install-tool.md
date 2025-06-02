# NpmInstallTool

*Installs npm dependencies for a project, or a specific package.*

## Usage

This tool runs `npm install` (or `pnpm install` if `pnpm` is detected as the package manager via `pnpm-lock.yaml`) in a specified project directory. It can be used to install all dependencies listed in `package.json` or to install a specific package (optionally with a version and as a dev dependency).

**Parameters:**
-   `workingDirectory` (string, required): The path to the project directory containing `package.json` (relative to the main Project Path).
-   `packageName` (string, optional): If provided, only this specific package will be installed. If omitted, `npm install` (or `pnpm install`) is run to install all dependencies from `package.json`.
-   `isDev` (boolean, optional, default: false): If `true` and `packageName` is provided, installs the package as a dev dependency (`--save-dev`).
-   `version` (string, optional): If `packageName` is provided, specifies a particular version to install (e.g., "^1.2.3", "latest").

## Examples

To install all dependencies for a project in the `my-vite-app` subfolder:

```json
{
  "workingDirectory": "my-vite-app"
}
```

To install the `lodash` package as a regular dependency in the `api-service` project:

```json
{
  "workingDirectory": "api-service",
  "packageName": "lodash"
}
```

To install `eslint` version 8 as a dev dependency:

```json
{
  "workingDirectory": "frontend",
  "packageName": "eslint",
  "isDev": true,
  "version": "8"
}
```

## Notes

-   **User Confirmation Required:** Running `npm install` can execute scripts from packages. You will be prompted for confirmation before the command is executed.
-   The tool requires `package.json` to be present in the `workingDirectory`.
-   It will automatically attempt to use `pnpm install` if a `pnpm-lock.yaml` file is detected in the `workingDirectory`, otherwise it defaults to `npm install`.
-   Output from the install command will be returned.