# Sentry Tool Implementation

## Overview
The Sentry Tool provides integration with the Sentry API, allowing users to retrieve information about organizations, projects, and issues directly from within the AiStudio4 application.

## Features
- Retrieve organization details including teams and projects
- Get detailed information about specific projects
- List and filter issues from projects

## Implementation Details

### Tool Definition
The tool is implemented as a standard BaseToolImplementation with the following schema:

```json
{
  "name": "Sentry",
  "description": "Retrieves information from Sentry API including organization, project, and issue details.",
  "input_schema": {
    "properties": {
      "operation": {
        "title": "Operation",
        "type": "string",
        "description": "The operation to perform (organization, project, issues)",
        "enum": ["organization", "project", "issues"]
      },
      "project_slug": {
        "title": "Project Slug",
        "type": "string",
        "description": "The Sentry project slug (required for project and issues operations)"
      },
      "query": {
        "title": "Query",
        "type": "string",
        "description": "Optional query string for filtering issues (e.g., is:unresolved)"
      }
    },
    "required": ["operation"],
    "title": "SentryArguments",
    "type": "object"
  }
}
```

### Configuration Properties
The tool requires the following configuration properties:

- **orgSlug**: The Sentry organization slug
- **apiToken**: The Sentry API token for authentication
- **sentryApiBaseUrl**: The base URL for the Sentry API (default: "https://sentry.io/api/0")

### Operations

#### Organization Details
Retrieves information about the configured organization including:
- Basic organization information (name, slug, status)
- List of teams
- List of projects

#### Project Details
Retrieves detailed information about a specific project:
- Basic project information (name, slug, platform)
- Status and creation date
- Latest release information
- Enabled features

#### Issues List
Retrieves a list of issues for a specific project:
- Issue title and ID
- Status and priority
- First and last seen timestamps
- Event count and affected users
- Direct link to the issue in Sentry

### Output Formatting
The tool formats the API responses into readable markdown text, making it easy for the AI to understand and present the information to users.

## Future Enhancements

- Add support for creating and updating issues
- Implement release management operations
- Add ability to retrieve event details for specific issues
- Support for retrieving performance metrics
- Integration with project alerts and notifications

## Security Considerations

- The API token is stored as a tool property and should be handled securely
- The tool only performs read operations, minimizing potential security risks
- All API requests use HTTPS for secure communication