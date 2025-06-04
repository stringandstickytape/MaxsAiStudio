# GitHubCreatePullRequest Tool

*Creates a new pull request in a GitHub repository. Requires GitHub Personal Access Token with repo permissions.*

## Overview

This tool allows the AI to create pull requests directly in GitHub repositories, completing the development workflow from issue creation to pull request submission. It integrates with GitHub's API to create pull requests with full metadata including title, description, base and head branches.

## Parameters

- `owner` (string, required): Repository owner (username or organization).
- `repo` (string, required): Repository name.
- `title` (string, required): The title of the pull request.
- `head` (string, required): The name of the branch where your changes are implemented. For cross-repository pull requests, use format `username:branch`.
- `base` (string, required): The name of the branch you want the changes pulled into (target branch).
- `body` (string, optional): The contents of the pull request description (Markdown supported).
- `draft` (boolean, optional, default: false): Indicates whether the pull request is a draft.
- `maintainer_can_modify` (boolean, optional, default: true): Indicates whether maintainers can modify the pull request.

## Tool Configuration

**Required Extra Properties (Configure in Tool Library):**
- `GitHubToken`: Your GitHub Personal Access Token with `repo` permissions

To configure:
1. Open the Tool Library
2. Find and edit the "GitHubCreatePullRequest" tool
3. Add your GitHub Personal Access Token in the Extra Properties section

## Examples

### Create a basic pull request
```json
{
  "owner": "myusername",
  "repo": "my-project",
  "title": "Add user authentication feature",
  "head": "feature/user-auth",
  "base": "main",
  "body": "This PR adds user authentication functionality including:\n\n- Login/logout endpoints\n- JWT token management\n- Password hashing\n- User session handling\n\nFixes #123"
}
```

### Create a draft pull request
```json
{
  "owner": "myorg",
  "repo": "web-app",
  "title": "WIP: Refactor database layer",
  "head": "refactor/database",
  "base": "develop",
  "body": "Work in progress for database layer refactoring.\n\n**TODO:**\n- [ ] Update migration scripts\n- [ ] Add unit tests\n- [ ] Update documentation",
  "draft": true
}
```

### Cross-repository pull request
```json
{
  "owner": "upstream-owner",
  "repo": "main-project",
  "title": "Fix memory leak in data processing",
  "head": "myusername:bugfix/memory-leak",
  "base": "main",
  "body": "Fixes memory leak identified in issue #456.\n\nChanges:\n- Added proper cleanup in data processor\n- Updated memory management in worker threads"
}
```

## Output

The tool returns a JSON object containing:
- `success`: Boolean indicating if the pull request was created successfully
- `pullRequest`: Object with pull request details:
  - `number`: The pull request number
  - `url`: Direct URL to the pull request
  - `title`: The pull request title
  - `head`: Source branch
  - `base`: Target branch
  - `draft`: Whether it's a draft
  - `state`: Current state (usually "open")
  - `created_at`: Creation timestamp
- `message`: Formatted success message with pull request details

## Important Notes

- **Authentication Required**: You must configure a GitHub Personal Access Token with `repo` permissions in the tool's extra properties.
- **Branch Requirements**: Both `head` and `base` branches must exist in their respective repositories.
- **Permissions**: You need write access to the target repository to create pull requests.
- **Draft PRs**: Draft pull requests are useful for work-in-progress changes that aren't ready for review.
- **Cross-Repository**: For pull requests from forks, use the format `username:branch` for the `head` parameter.

## Common Use Cases

1. **Feature Development**: Create PR after completing a feature branch
2. **Bug Fixes**: Submit bug fix pull requests with detailed descriptions
3. **Documentation Updates**: Create PRs for documentation improvements
4. **Code Reviews**: Submit changes for team review and collaboration
5. **Open Source Contributions**: Contribute to open source projects via pull requests

## Integration with Development Workflow

This tool completes the full development workflow when combined with other tools:

1. **GitHubCreateIssue**: Create an issue to track the work
2. **GitBranch**: Create a new branch for the feature/fix
3. **File modification tools**: Make code changes (ModifyFiles, CreateNewFile, etc.)
4. **GitCommit**: Commit and push changes
5. **GitHubCreatePullRequest**: Create pull request for review
6. **GitHubCreateIssueComment**: Add updates to the original issue

## Error Handling

The tool provides detailed error messages for common scenarios:
- Invalid or missing GitHub token
- Repository not found or access denied
- Branch does not exist
- Pull request already exists for the branch
- Insufficient permissions
- Network connectivity issues

## Example Output

```json
{
  "success": true,
  "pullRequest": {
    "number": "42",
    "url": "https://github.com/myusername/my-project/pull/42",
    "title": "Add user authentication feature",
    "head": "feature/user-auth",
    "base": "main",
    "draft": false,
    "state": "open",
    "created_at": "2024-01-15T10:30:00Z"
  },
  "message": "✅ Pull request created successfully!\n\n**Pull Request #42:** Add user authentication feature\n**URL:** https://github.com/myusername/my-project/pull/42\n**From:** feature/user-auth → main\n**Status:** Ready for review"
}
```

This tool enables seamless integration between AI-driven development and GitHub's collaborative workflow, making it easy to submit changes for review and merge into the main codebase.