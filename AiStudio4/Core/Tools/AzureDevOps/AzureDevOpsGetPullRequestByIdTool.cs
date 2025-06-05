using AiStudio4.Core.Interfaces;
using AiStudio4.Core.Models;
using AiStudio4.InjectedDependencies;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace AiStudio4.Core.Tools.AzureDevOps
{
    /// <summary>
    /// Implementation of the Azure DevOps Get Pull Request By ID tool
    /// </summary>
    public class AzureDevOpsGetPullRequestByIdTool : BaseToolImplementation
    {
        private readonly HttpClient _httpClient;

        public AzureDevOpsGetPullRequestByIdTool(ILogger<AzureDevOpsGetPullRequestByIdTool> logger, IGeneralSettingsService generalSettingsService, IStatusMessageService statusMessageService) 
            : base(logger, generalSettingsService, statusMessageService)
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "AiStudio4-AzureDevOps-Tool");
        }

        /// <summary>
        /// Gets the Azure DevOps Get Pull Request By ID tool definition
        /// </summary>
        public override Tool GetToolDefinition()
        {
            return new Tool
            {
                Guid = ToolGuids.AZURE_DEV_OPS_GET_PULL_REQUEST_BY_ID_TOOL_GUID,
                Name = "AzureDevOpsGetPullRequestById",
                Description = "Retrieves detailed information about a specific pull request in an Azure DevOps repository.",
                Schema = """
{
  "name": "AzureDevOpsGetPullRequestById",
  "description": "Retrieves detailed information about a specific pull request in an Azure DevOps repository.",
  "input_schema": {
    "properties": {
      "organization": { "title": "Organization", "type": "string", "description": "The Azure DevOps organization name" },
      "project": { "title": "Project", "type": "string", "description": "The Azure DevOps project name" },
      "repository_id": { "title": "Repository ID", "type": "string", "description": "The repository ID or name" },
      "pull_request_id": { "title": "Pull Request ID", "type": "integer", "description": "The pull request ID" },
      "include_commits": { "title": "Include Commits", "type": "boolean", "description": "Include commits in the response", "default": false },
      "include_work_item_refs": { "title": "Include Work Item References", "type": "boolean", "description": "Include work item references in the response", "default": false }
    },
    "required": ["organization", "project", "repository_id", "pull_request_id"],
    "title": "AzureDevOpsGetPullRequestByIdArguments",
    "type": "object"
  }
}
""",
                Categories = new List<string> { "AzureDevOps" },
                OutputFileType = "txt",
                Filetype = string.Empty,
                LastModified = DateTime.UtcNow,
                ExtraProperties = new Dictionary<string, string> {
                    { "azureDevOpsPAT", "" }
                }
            };
        }

        public override async Task<BuiltinToolResult> ProcessAsync(string toolParameters, Dictionary<string, string> extraProperties)
        {
            try
            {
                SendStatusUpdate("Starting Azure DevOps Get Pull Request By ID tool execution...");
                var parameters = JsonConvert.DeserializeObject<Dictionary<string, object>>(toolParameters) ?? new Dictionary<string, object>();

                // Extract required parameters
                if (!parameters.TryGetValue("organization", out var organizationObj) || !(organizationObj is string organization) || string.IsNullOrWhiteSpace(organization))
                {
                    return CreateResult(true, true, $"Parameters: organization=<missing>, project=<unknown>, repository_id=<unknown>, pull_request_id=<unknown>\n\nError: 'organization' parameter is required.");
                }

                if (!parameters.TryGetValue("project", out var projectObj) || !(projectObj is string project) || string.IsNullOrWhiteSpace(project))
                {
                    return CreateResult(true, true, $"Parameters: organization={organization}, project=<missing>, repository_id=<unknown>, pull_request_id=<unknown>\n\nError: 'project' parameter is required.");
                }

                if (!parameters.TryGetValue("repository_id", out var repoIdObj) || !(repoIdObj is string repositoryId) || string.IsNullOrWhiteSpace(repositoryId))
                {
                    return CreateResult(true, true, $"Parameters: organization={organization}, project={project}, repository_id=<missing>, pull_request_id=<unknown>\n\nError: 'repository_id' parameter is required.");
                }

                if (!parameters.TryGetValue("pull_request_id", out var prIdObj) || !(prIdObj is long || prIdObj is int))
                {
                    return CreateResult(true, true, $"Parameters: organization={organization}, project={project}, repository_id={repositoryId}, pull_request_id=<missing>\n\nError: 'pull_request_id' parameter is required and must be an integer.");
                }

                int pullRequestId = prIdObj is long longId ? (int)longId : (int)prIdObj;

                // Extract optional parameters
                bool includeCommits = false;
                if (parameters.TryGetValue("include_commits", out var includeCommitsObj) && includeCommitsObj is bool includeCommitsBool)
                {
                    includeCommits = includeCommitsBool;
                }

                bool includeWorkItemRefs = false;
                if (parameters.TryGetValue("include_work_item_refs", out var includeWorkItemRefsObj) && includeWorkItemRefsObj is bool includeWorkItemRefsBool)
                {
                    includeWorkItemRefs = includeWorkItemRefsBool;
                }

                // Get API key from settings
                string apiKey = _generalSettingsService.GetDecryptedAzureDevOpsPAT();
                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    return CreateResult(true, true, $"Parameters: organization={organization}, project={project}, repository_id={repositoryId}, pull_request_id={pullRequestId}\n\nError: Azure DevOps PAT is not configured. Please set it in File > Settings > Set Azure DevOps PAT.");
                }

                // Set up authentication header
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", 
                    Convert.ToBase64String(Encoding.ASCII.GetBytes(string.Format("{0}:{1}", "", apiKey))));

                // Make the API request
                return await GetPullRequestByIdAsync(organization, project, repositoryId, pullRequestId, includeCommits, includeWorkItemRefs);
            }
            catch (JsonException jsonEx)
            {
                _logger.LogError(jsonEx, "Error deserializing Azure DevOps tool parameters");
                return CreateResult(true, true, $"Parameters: <invalid JSON>\n\nError processing Azure DevOps tool parameters: Invalid JSON format. {jsonEx.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Azure DevOps tool");
                return CreateResult(true, true, $"Parameters: <unknown>\n\nError processing Azure DevOps tool: {ex.Message}");
            }
        }

        private async Task<BuiltinToolResult> GetPullRequestByIdAsync(string organization, string project, string repositoryId, 
            int pullRequestId, bool includeCommits, bool includeWorkItemRefs)
        {
            try
            {
                SendStatusUpdate($"Fetching pull request {pullRequestId} from {organization}/{project}/{repositoryId}...");
                
                // Build query parameters
                var queryParams = new List<string>();
                
                // Add optional parameters
                if (includeCommits)
                {
                    queryParams.Add("includeCommits=true");
                }
                
                if (includeWorkItemRefs)
                {
                    queryParams.Add("includeWorkItemRefs=true");
                }
                
                string queryString = queryParams.Count > 0 ? $"?{string.Join("&", queryParams)}" : "";
                string url = $"https://dev.azure.com/{organization}/{project}/_apis/git/repositories/{repositoryId}/pullrequests/{pullRequestId}{queryString}";
                
                var response = await _httpClient.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorObj = JObject.Parse(content);
                    string errorMessage = errorObj["message"]?.ToString() ?? "Unknown error";
                    return CreateResult(true, true, $"Parameters: organization={organization}, project={project}, repository_id={repositoryId}, pull_request_id={pullRequestId}\n\nAzure DevOps API Error: {errorMessage} (Status code: {response.StatusCode})");
                }
                
                // If we need to get additional data, make those requests
                var prData = JObject.Parse(content);
                
                // Get commits if requested but not included in the response
                if (includeCommits && (prData["commits"] == null || !(prData["commits"] is JArray)))
                {
                    var commitsData = await GetPullRequestCommitsAsync(organization, project, repositoryId, pullRequestId);
                    if (commitsData != null)
                    {
                        prData["commits"] = commitsData;
                    }
                }
                
                // Get work item references if requested but not included in the response
                if (includeWorkItemRefs && (prData["workItemRefs"] == null || !(prData["workItemRefs"] is JArray)))
                {
                    var workItemsData = await GetPullRequestWorkItemsAsync(organization, project, repositoryId, pullRequestId);
                    if (workItemsData != null)
                    {
                        prData["workItemRefs"] = workItemsData;
                    }
                }
                
                var formattedContent = FormatPullRequestInfo(prData.ToString(), includeCommits, includeWorkItemRefs);
                
                SendStatusUpdate("Successfully retrieved pull request information.");
                return CreateResult(true, true, $"Parameters: organization={organization}, project={project}, repository_id={repositoryId}, pull_request_id={pullRequestId}, include_commits={includeCommits}, include_work_item_refs={includeWorkItemRefs}\n\n{formattedContent}");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error fetching pull request information");
                return CreateResult(true, true, $"Parameters: organization={organization}, project={project}, repository_id={repositoryId}, pull_request_id={pullRequestId}\n\nError fetching pull request information: {ex.Message}");
            }
        }

        private async Task<JArray> GetPullRequestCommitsAsync(string organization, string project, string repositoryId, int pullRequestId)
        {
            try
            {
                string url = $"https://dev.azure.com/{organization}/{project}/_apis/git/repositories/{repositoryId}/pullrequests/{pullRequestId}/commits";
                
                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }
                
                var content = await response.Content.ReadAsStringAsync();
                var commitsData = JObject.Parse(content);
                return commitsData["value"] as JArray;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching pull request commits");
                return null;
            }
        }

        private async Task<JArray> GetPullRequestWorkItemsAsync(string organization, string project, string repositoryId, int pullRequestId)
        {
            try
            {
                string url = $"https://dev.azure.com/{organization}/{project}/_apis/git/repositories/{repositoryId}/pullrequests/{pullRequestId}/workitems";
                
                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }
                
                var content = await response.Content.ReadAsStringAsync();
                var workItemsData = JObject.Parse(content);
                return workItemsData["value"] as JArray;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching pull request work items");
                return null;
            }
        }

        private string FormatPullRequestInfo(string jsonContent, bool includeCommits, bool includeWorkItemRefs)
        {
            try
            {
                var pr = JObject.Parse(jsonContent);
                var sb = new StringBuilder();
                
                sb.AppendLine("# Pull Request Details");
                sb.AppendLine();
                
                sb.AppendLine($"## PR #{pr["pullRequestId"]} - {pr["title"]}");
                sb.AppendLine($"**Status:** {pr["status"]}");
                sb.AppendLine($"**Created by:** {pr["createdBy"]?["displayName"] ?? "Unknown"}");
                sb.AppendLine($"**Created on:** {pr["creationDate"]}");
                
                if (pr["closedDate"] != null)
                {
                    sb.AppendLine($"**Closed on:** {pr["closedDate"]}");
                }
                
                if (pr["completionQueueTime"] != null)
                {
                    sb.AppendLine($"**Completion queued:** {pr["completionQueueTime"]}");
                }
                
                sb.AppendLine($"**Source branch:** {pr["sourceRefName"]?.ToString().Replace("refs/heads/", "")}");
                sb.AppendLine($"**Target branch:** {pr["targetRefName"]?.ToString().Replace("refs/heads/", "")}");
                sb.AppendLine($"**URL:** {pr["url"]}");
                
                if (pr["description"] != null && !string.IsNullOrWhiteSpace(pr["description"].ToString()))
                {
                    sb.AppendLine("\n**Description:**");
                    sb.AppendLine(pr["description"].ToString());
                }
                
                // Add merge status information
                sb.AppendLine("\n**Merge Status:**");
                sb.AppendLine($"- Merge Status: {pr["mergeStatus"]}");
                sb.AppendLine($"- Is Draft: {pr["isDraft"]}");
                sb.AppendLine($"- Auto Complete Set: {pr["autoCompleteSetBy"] != null}");
                
                if (pr["mergeFailureMessage"] != null && !string.IsNullOrWhiteSpace(pr["mergeFailureMessage"].ToString()))
                {
                    sb.AppendLine($"- Merge Failure: {pr["mergeFailureMessage"]}");
                }
                
                // Add reviewer information if available
                if (pr["reviewers"] is JArray reviewers && reviewers.Count > 0)
                {
                    sb.AppendLine("\n**Reviewers:**");
                    foreach (var reviewer in reviewers)
                    {
                        string voteStatus = "";
                        int vote = reviewer["vote"] != null ? (int)reviewer["vote"] : 0;
                        
                        switch (vote)
                        {
                            case 10: voteStatus = "✅ Approved"; break;
                            case 5: voteStatus = "✓ Approved with suggestions"; break;
                            case 0: voteStatus = "⬜ No vote"; break;
                            case -5: voteStatus = "⚠️ Waiting for author"; break;
                            case -10: voteStatus = "❌ Rejected"; break;
                            default: voteStatus = $"Unknown ({vote})"; break;
                        }
                        
                        sb.AppendLine($"- {reviewer["displayName"]}: {voteStatus}");
                        
                        if (reviewer["isRequired"] != null && (bool)reviewer["isRequired"])
                        {
                            sb.AppendLine("  (Required Reviewer)");
                        }
                    }
                }
                
                // Add commits if available and requested
                if (includeCommits && pr["commits"] is JArray commits && commits.Count > 0)
                {
                    sb.AppendLine("\n**Commits:**");
                    foreach (var commit in commits)
                    {
                        string commitId = commit["commitId"]?.ToString();
                        if (commitId != null && commitId.Length > 8)
                        {
                            commitId = commitId.Substring(0, 8); // Shortened commit ID
                        }
                        
                        sb.AppendLine($"- [{commitId}] {commit["comment"]}");
                        sb.AppendLine($"  Author: {commit["author"]?["name"]} on {commit["author"]?["date"]}");
                    }
                }
                
                // Add work item references if available and requested
                if (includeWorkItemRefs && pr["workItemRefs"] is JArray workItems && workItems.Count > 0)
                {
                    sb.AppendLine("\n**Related Work Items:**");
                    foreach (var workItem in workItems)
                    {
                        sb.AppendLine($"- [{workItem["id"]}] {workItem["name"] ?? workItem["title"] ?? "Work Item"}");
                        if (workItem["url"] != null)
                        {
                            sb.AppendLine($"  URL: {workItem["url"]}");
                        }
                    }
                }
                
                // Add labels if available
                if (pr["labels"] is JArray labels && labels.Count > 0)
                {
                    sb.AppendLine("\n**Labels:**");
                    foreach (var label in labels)
                    {
                        sb.AppendLine($"- {label["name"]}");
                    }
                }
                
                return sb.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error formatting pull request information");
                return $"Error formatting pull request information: {ex.Message}\n\nRaw JSON:\n{jsonContent}";
            }
        }
    }
}
