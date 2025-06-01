# Azure DevOps Integration Design

## Purpose

This document outlines the design for a set of tools that enable AI assistants to interact with Azure DevOps repositories, work items, and pull requests. These tools will allow the AI to retrieve and analyze information from Azure DevOps, providing comprehensive support for software development workflows.

## Architecture Overview

The Azure DevOps integration consists of a set of specialized tools that directly map to Azure DevOps REST API endpoints. Each tool is designed to perform a specific function, such as retrieving work items, querying pull requests, or accessing repository content.

### Authentication

Authentication with Azure DevOps will be handled through Personal Access Tokens (PATs). Rather than requiring the PAT to be passed as a parameter to each tool, the system will:

1. Store the PAT securely outside the AI's context
2. Inject the PAT programmatically into API requests
3. Handle token refresh and expiration automatically

This approach ensures that sensitive credentials are never exposed to the AI model while maintaining seamless access to Azure DevOps resources.

### Common Parameters

All tools require the following base parameters:
- `organization`: The Azure DevOps organization name
- `project`: The Azure DevOps project name

These parameters identify the specific Azure DevOps context for each operation.

## Tool Specifications

### Work Item Tools

DONE: #### 1. GetWorkItems

Retrieves detailed information about specific work items by their IDs.

**Parameters:**
- `organization`: String (Azure DevOps organization name)
- `project`: String (project name)
- `ids`: Array[Integer] (work item IDs to retrieve)
- `fields`: Array[String] (optional - specific fields to return)
- `as_of`: String (optional - date to view work items as of)
- `expand`: String (optional - expand relations - values: "relations", "fields", "none")

DONE: #### 2. QueryWorkItems

Executes a WIQL (Work Item Query Language) query to find work items matching specific criteria.

**Parameters:**
- `organization`: String (Azure DevOps organization name)
- `project`: String (project name)
- `query`: String (WIQL query text)
- `top`: Integer (optional - number of work items to return)
- `skip`: Integer (optional - number of work items to skip)
- `time_precision`: Boolean (optional - include time precision for date fields)

DONE: #### 3. GetWorkItemUpdates

Retrieves the update history for a specific work item.

**Parameters:**
- `organization`: String (Azure DevOps organization name)
- `project`: String (project name)
- `id`: Integer (work item ID)
- `top`: Integer (optional - number of updates to return)
- `skip`: Integer (optional - number of updates to skip)

DONE: #### 4. GetWorkItemComments

Retrieves comments associated with a specific work item.

**Parameters:**
- `organization`: String (Azure DevOps organization name)
- `project`: String (project name)
- `id`: Integer (work item ID)
- `top`: Integer (optional - number of comments to return)
- `skip`: Integer (optional - number of comments to skip)

### Pull Request Tools

DONE: #### 5. GetPullRequests

Retrieves pull requests matching specified criteria.

**Parameters:**
- `organization`: String (Azure DevOps organization name)
- `project`: String (project name)
- `repository_id`: String (repository ID or name)
- `status`: String (optional - "active", "abandoned", "completed", "all")
- `creator_id`: String (optional - filter by creator ID)
- `reviewer_id`: String (optional - filter by reviewer ID)
- `source_reference_name`: String (optional - source branch)
- `target_reference_name`: String (optional - target branch)
- `top`: Integer (optional - number of PRs to return)
- `skip`: Integer (optional - number of PRs to skip)

DONE: DONE: #### 6. GetPullRequestById

Retrieves detailed information about a specific pull request.

**Parameters:**
- `organization`: String (Azure DevOps organization name)
- `project`: String (project name)
- `repository_id`: String (repository ID or name)
- `pull_request_id`: Integer (pull request ID)
- `include_commits`: Boolean (optional - include commits)
- `include_work_item_refs`: Boolean (optional - include work item references)

DONE: #### 7. GetPullRequestThreads

Retrieves comment threads associated with a specific pull request.

**Parameters:**
- `organization`: String (Azure DevOps organization name)
- `project`: String (project name)
- `repository_id`: String (repository ID or name)
- `pull_request_id`: Integer (pull request ID)
- `top`: Integer (optional - number of threads to return)
- `skip`: Integer (optional - number of threads to skip)

DONE: #### 8. GetPullRequestIterations

Retrieves the iterations (versions) of a specific pull request.

**Parameters:**
- `organization`: String (Azure DevOps organization name)
- `project`: String (project name)
- `repository_id`: String (repository ID or name)
- `pull_request_id`: Integer (pull request ID)

DONE: #### 9. GetPullRequestChanges

Retrieves the file changes associated with a specific pull request iteration.

**Parameters:**
- `organization`: String (Azure DevOps organization name)
- `project`: String (project name)
- `repository_id`: String (repository ID or name)
- `pull_request_id`: Integer (pull request ID)
- `iteration_id`: Integer (optional - specific iteration to get changes for)
- `top`: Integer (optional - number of changes to return)
- `skip`: Integer (optional - number of changes to skip)
- `compare_to`: Integer (optional - iteration ID to compare against)

### Repository Tools

DONE: #### 10. GetRepositories

Retrieves repositories in the specified project.

**Parameters:**
- `organization`: String (Azure DevOps organization name)
- `project`: String (project name)
- `include_hidden`: Boolean (optional - include hidden repositories)
- `include_all_urls`: Boolean (optional - include all remote URLs)

#### 11. GetCommits

Retrieves commits matching specified criteria.

**Parameters:**
- `organization`: String (Azure DevOps organization name)
- `project`: String (project name)
- `repository_id`: String (repository ID or name)
- `from_date`: String (optional - from date)
- `to_date`: String (optional - to date)
- `author`: String (optional - author email or name)
- `item_path`: String (optional - path to filter commits)
- `top`: Integer (optional - number of commits to return)
- `skip`: Integer (optional - number of commits to skip)

#### 12. GetCommitDiffs

Retrieves the file changes associated with a specific commit.

**Parameters:**
- `organization`: String (Azure DevOps organization name)
- `project`: String (project name)
- `repository_id`: String (repository ID or name)
- `commit_id`: String (commit ID)
- `top`: Integer (optional - number of changes to return)
- `skip`: Integer (optional - number of changes to skip)
- `base_version`: String (optional - base version to compare against)

#### 13. GetItemContent

Retrieves the content of a specific file from the repository.

**Parameters:**
- `organization`: String (Azure DevOps organization name)
- `project`: String (project name)
- `repository_id`: String (repository ID or name)
- `path`: String (path to the item)
- `version_type`: String (optional - "branch", "commit", "tag")
- `version`: String (optional - version identifier)

## Implementation Considerations

### Error Handling

Each tool should implement robust error handling to address common scenarios:

1. Authentication failures
2. Resource not found
3. Permission issues
4. Rate limiting
5. Network connectivity problems

Errors should be returned in a standardized format with clear error codes and human-readable messages.

### Pagination

Many Azure DevOps API endpoints return paginated results. The tools handle this through the `top` and `skip` parameters, allowing the AI to request specific pages of results. For large result sets, the AI may need to make multiple calls with different pagination parameters.

### Performance Optimization

To optimize performance and reduce API calls:

1. Tools should implement caching where appropriate
2. Responses should include only the requested fields
3. Batch operations should be used where possible (e.g., retrieving multiple work items in a single call)

## Security Considerations

1. PATs should have the minimum required permissions for the operations they need to perform
2. PATs should be rotated regularly
3. All API calls should be logged for audit purposes
4. The AI should not have direct access to the PAT
5. Input validation should be performed on all parameters before making API calls

## Future Enhancements

1. Support for additional Azure DevOps features (e.g., Pipelines, Test Plans)
2. Integration with other DevOps platforms (GitHub, GitLab, etc.)
3. Enhanced caching and performance optimizations
4. Support for webhook notifications to enable real-time updates
5. Expanded query capabilities for more complex filtering scenarios

## Conclusion

This design provides a comprehensive set of tools for AI assistants to interact with Azure DevOps. By closely mirroring the Azure DevOps API structure and parameter naming conventions, these tools enable seamless integration with Azure DevOps repositories, work items, and pull requests, enhancing the AI's ability to support software development workflows.