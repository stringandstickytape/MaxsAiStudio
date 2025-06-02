# SentryTool

*Retrieves information from Sentry API including organization, project, and issue details.*

## Usage

This tool allows the AI to interact with your Sentry instance to fetch details about your error tracking and performance monitoring.

**Tool Specific Extra Properties (Configurable in Tool Library):**
These **must** be configured in the Tool Library UI by editing the `Sentry` tool:
-   `orgSlug` (string, required): Your Sentry organization slug (e.g., "my-company-slug").
-   `apiToken` (string, required): Your Sentry API authentication token. Create this in Sentry under User Settings > API Keys. It needs appropriate scopes (e.g., `project:read`, `org:read`, `event:read`).
-   `sentryApiBaseUrl` (string, optional, default: "https://sentry.io/api/0"): The base URL for the Sentry API. Only change this if you are using a self-hosted Sentry instance.

**Parameters (passed by AI in the request):**
-   `operation` (string, required): The operation to perform. Valid values:
    -   `organization`: Get details about the configured organization.
    -   `project`: Get details about a specific project.
    -   `issues`: List issues for a specific project.
-   `project_slug` (string, optional): The slug of the Sentry project (e.g., "frontend-app"). Required if `operation` is "project" or "issues".
-   `query` (string, optional): For the `issues` operation, an optional Sentry search query string to filter issues (e.g., "is:unresolved assigned:me", "error.type:TypeError"). Defaults to "is:unresolved" if not provided for the `issues` operation.

## Examples

To get organization details (assuming `orgSlug` and `apiToken` are set in Extra Properties):

```json
{
  "operation": "organization"
}
```

To get details for the project `webapp-prod`:

```json
{
  "operation": "project",
  "project_slug": "webapp-prod"
}
```

To list unresolved issues in the `mobile-app` project that are assigned to the current user (or team based on Sentry query syntax):

```json
{
  "operation": "issues",
  "project_slug": "mobile-app",
  "query": "is:unresolved assigned:me"
}
```

## Notes

-   Ensure the API token has the necessary permissions for the operations you want the AI to perform.
-   The output is formatted text summarizing the requested Sentry information.
    -   Organization details include teams and projects.
    -   Project details include platform, status, and latest release.
    -   Issues list includes title, ID, status, priority, timestamps, event/user counts, and a direct link to the issue in Sentry.