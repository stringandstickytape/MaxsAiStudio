# CheckNodeVersionTool

*Checks if Node.js and npm are installed and returns their versions.*

## Usage

This tool is used to verify that Node.js and npm (Node Package Manager) are installed and accessible in the system's PATH. It's a prerequisite check for many JavaScript/TypeScript development tasks, including working with Vite.

**Parameters:**
-   None. This tool does not require any input parameters.

## Examples

To check Node.js and npm versions:

```json
{}
```

## Output Example

If both are installed, the output might look like:

```text
Node.js version: v18.17.0
npm version: 9.6.7
```

If Node.js is not found:

```text
Error: Node.js is not installed or not in the PATH.
```

If npm is not found (but Node.js is):

```text
Node.js version: v18.17.0
Error: npm is not installed or not in the PATH.
```

## Notes

-   This tool executes `node -v` and `npm -v` commands in the system shell.
-   It's useful for the AI to run this before attempting other npm or Node.js dependent operations to ensure the environment is set up correctly.