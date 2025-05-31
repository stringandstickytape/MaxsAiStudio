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
    /// Implementation of the Azure DevOps Get Pull Requests tool
    /// </summary>
    public class AzureDevOpsGetPullRequestsTool : BaseToolImplementation
    {
        private readonly HttpClient _httpClient;

        public AzureDevOpsGetPullRequestsTool(ILogger<AzureDevOpsGetPullRequestsTool> logger, IGeneralSettingsService generalSettingsService, IStatusMessageService statusMessageService) 
            : base(logger, generalSettingsService, statusMessageService)
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "AiStudio4-AzureDevOps-Tool");
        }

        /// <summary>
        /// Gets the Azure DevOps Get Pull Requests tool definition
        /// </summary>
        public override Tool GetToolDefinition()
        {
            return new Tool
            {
                Guid = "5b3e9c2a-7d8f-4e1a-9b6c-8d7f5e3a2c1b",
                Name = "AzureDevOpsGetPullRequests",
                Description = "Retrieves pull requests matching specified criteria from an Azure DevOps repository.",
                Schema = @"{
  ""name"": ""AzureDevOpsGetPullRequests"",
  ""description"": ""Retrieves pull requests matching specified criteria from an Azure DevOps repository."",
  ""input_schema"": {
    ""properties"": {
      ""organization"": {
        ""title"": ""Organization"",
        ""type"": ""string"",
        ""description"": ""The Azure DevOps organization name""
      },
      ""project"": {
        ""title"": ""Project"",
        ""type"": ""string"",
        ""description"": ""The Azure DevOps project name""
      },
      ""repository_id"": {
        ""title"": ""Repository ID"",
        ""type"": ""string"",
        ""description"": ""The repository ID or name""
      },
      ""status"": {
        ""title"": ""Status"",
        ""type"": ""string"",
        ""description"": ""Filter by pull request status (active, abandoned, completed, all)"",
        ""enum"": [""active"", ""abandoned"", ""completed"", ""all""],
        ""default"": ""active""
      },
      ""creator_id"": {
        ""title"": ""Creator ID"",
        ""type"": ""string"",
        ""description"": ""Filter by creator ID""
      },
      ""reviewer_id"": {
        ""title"": ""Reviewer ID"",
        ""type"": ""string"",
        ""description"": ""Filter by reviewer ID""
      },
      ""source_reference_name"": {
        ""title"": ""Source Branch"",
        ""type"": ""string"",
        ""description"": ""Filter by source branch name""
      },
      ""target_reference_name"": {
        ""title"": ""Target Branch"",
        ""type"": ""string"",
        ""description"": ""Filter by target branch name""
      },
      ""top"": {
        ""title"": ""Top"",
        ""type"": ""integer"",
        ""description"": ""Number of pull requests to return"",
        ""default"": 100
      },
      ""skip"": {
        ""title"": ""Skip"",
        ""type"": ""integer"",
        ""description"": ""Number of pull requests to skip"",
        ""default"": 0
      }
    },
    ""required"": [""organization"", ""project"", ""repository_id""],
    ""title"": ""AzureDevOpsGetPullRequestsArguments"",
    ""type"": ""object""
  }
}",
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
                SendStatusUpdate("Starting Azure DevOps Get Pull Requests tool execution...");
                var parameters = JsonConvert.DeserializeObject<Dictionary<string, object>>(toolParameters) ?? new Dictionary<string, object>();

                // Extract required parameters
                if (!parameters.TryGetValue("organization", out var organizationObj) || !(organizationObj is string organization) || string.IsNullOrWhiteSpace(organization))
                {
                    return CreateResult(true, true, $"Parameters: organization=<missing>, project=<unknown>, repository_id=<unknown>\n\nError: 'organization' parameter is required.");
                }

                if (!parameters.TryGetValue("project", out var projectObj) || !(projectObj is string project) || string.IsNullOrWhiteSpace(project))
                {
                    return CreateResult(true, true, $"Parameters: organization={organization}, project=<missing>, repository_id=<unknown>\n\nError: 'project' parameter is required.");
                }

                if (!parameters.TryGetValue("repository_id", out var repoIdObj) || !(repoIdObj is string repositoryId) || string.IsNullOrWhiteSpace(repositoryId))
                {
                    return CreateResult(true, true, $"Parameters: organization={organization}, project={project}, repository_id=<missing>\n\nError: 'repository_id' parameter is required.");
                }

                // Extract optional parameters
                string status = "active";
                if (parameters.TryGetValue("status", out var statusObj) && statusObj is string statusStr && !string.IsNullOrWhiteSpace(statusStr))
                {
                    status = statusStr;
                }

                string creatorId = null;
                if (parameters.TryGetValue("creator_id", out var creatorIdObj) && creatorIdObj is string creatorIdStr && !string.IsNullOrWhiteSpace(creatorIdStr))
                {
                    creatorId = creatorIdStr;
                }

                string reviewerId = null;
                if (parameters.TryGetValue("reviewer_id", out var reviewerIdObj) && reviewerIdObj is string reviewerIdStr && !string.IsNullOrWhiteSpace(reviewerIdStr))
                {
                    reviewerId = reviewerIdStr;
                }

                string sourceBranch = null;
                if (parameters.TryGetValue("source_reference_name", out var sourceRefObj) && sourceRefObj is string sourceRefStr && !string.IsNullOrWhiteSpace(sourceRefStr))
                {
                    sourceBranch = sourceRefStr;
                }

                string targetBranch = null;
                if (parameters.TryGetValue("target_reference_name", out var targetRefObj) && targetRefObj is string targetRefStr && !string.IsNullOrWhiteSpace(targetRefStr))
                {
                    targetBranch = targetRefStr;
                }

                int top = 100;
                if (parameters.TryGetValue("top", out var topObj))
                {
                    if (topObj is long topLong)
                    {
                        top = (int)topLong;
                    }
                    else if (topObj is int topInt)
                    {
                        top = topInt;
                    }
                }

                int skip = 0;
                if (parameters.TryGetValue("skip", out var skipObj))
                {
                    if (skipObj is long skipLong)
                    {
                        skip = (int)skipLong;
                    }
                    else if (skipObj is int skipInt)
                    {
                        skip = skipInt;
                    }
                }

                // Get API key from settings
                string apiKey = _generalSettingsService.GetDecryptedAzureDevOpsPAT();
                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    return CreateResult(true, true, $"Parameters: organization={organization}, project={project}, repository_id={repositoryId}\n\nError: Azure DevOps PAT is not configured. Please set it in File > Settings > Set Azure DevOps PAT.");
                }

                // Set up authentication header
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", 
                    Convert.ToBase64String(Encoding.ASCII.GetBytes(string.Format("{0}:{1}", "", apiKey))));

                // Make the API request
                return await GetPullRequestsAsync(organization, project, repositoryId, status, creatorId, reviewerId, sourceBranch, targetBranch, top, skip);
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

        private async Task<BuiltinToolResult> GetPullRequestsAsync(string organization, string project, string repositoryId, 
            string status, string creatorId, string reviewerId, string sourceBranch, string targetBranch, int top, int skip)
        {
            try
            {
                SendStatusUpdate($"Fetching pull requests for {organization}/{project}/{repositoryId}...");
                
                // Build query parameters
                var queryParams = new List<string>();
                
                // Map status to Azure DevOps API parameter
                if (!string.IsNullOrEmpty(status) && status != "all")
                {
                    queryParams.Add($"searchCriteria.status={status}");
                }
                
                if (!string.IsNullOrEmpty(creatorId))
                {
                    queryParams.Add($"searchCriteria.creatorId={creatorId}");
                }
                
                if (!string.IsNullOrEmpty(reviewerId))
                {
                    queryParams.Add($"searchCriteria.reviewerId={reviewerId}");
                }
                
                if (!string.IsNullOrEmpty(sourceBranch))
                {
                    queryParams.Add($"searchCriteria.sourceRefName={sourceBranch}");
                }
                
                if (!string.IsNullOrEmpty(targetBranch))
                {
                    queryParams.Add($"searchCriteria.targetRefName={targetBranch}");
                }
                
                queryParams.Add($"$top={top}");
                queryParams.Add($"$skip={skip}");
                
                string queryString = queryParams.Count > 0 ? $"?{string.Join("&", queryParams)}" : "";
                string url = $"https://dev.azure.com/{organization}/{project}/_apis/git/repositories/{repositoryId}/pullrequests{queryString}";
                
                var response = await _httpClient.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorObj = JObject.Parse(content);
                    string errorMessage = errorObj["message"]?.ToString() ?? "Unknown error";
                    return CreateResult(true, true, $"Parameters: organization={organization}, project={project}, repository_id={repositoryId}\n\nAzure DevOps API Error: {errorMessage} (Status code: {response.StatusCode})");
                }
                
                var formattedContent = FormatPullRequestsInfo(content);
                
                SendStatusUpdate("Successfully retrieved pull requests information.");
                return CreateResult(true, true, $"Parameters: organization={organization}, project={project}, repository_id={repositoryId}, status={status}\n\n{formattedContent}");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error fetching pull requests information");
                return CreateResult(true, true, $"Parameters: organization={organization}, project={project}, repository_id={repositoryId}\n\nError fetching pull requests information: {ex.Message}");
            }
        }

        private string FormatPullRequestsInfo(string jsonContent)
        {
            try
            {
                var prData = JObject.Parse(jsonContent);
                var pullRequests = prData["value"] as JArray;
                var sb = new StringBuilder();
                
                sb.AppendLine("# Azure DevOps Pull Requests");
                sb.AppendLine();
                
                if (pullRequests == null || pullRequests.Count == 0)
                {
                    sb.AppendLine("No pull requests found matching the criteria.");
                    return sb.ToString();
                }
                
                sb.AppendLine($"Found {pullRequests.Count} pull requests:\n");
                
                foreach (var pr in pullRequests)
                {
                    sb.AppendLine($"## PR #{pr["pullRequestId"]} - {pr["title"]}");
                    sb.AppendLine($"**Status:** {pr["status"]}");
                    sb.AppendLine($"**Created by:** {pr["createdBy"]?["displayName"] ?? "Unknown"}");
                    sb.AppendLine($"**Created on:** {pr["creationDate"]}");
                    
                    if (pr["closedDate"] != null)
                    {
                        sb.AppendLine($"**Closed on:** {pr["closedDate"]}");
                    }
                    
                    sb.AppendLine($"**Source branch:** {pr["sourceRefName"]?.ToString().Replace("refs/heads/", "")}");
                    sb.AppendLine($"**Target branch:** {pr["targetRefName"]?.ToString().Replace("refs/heads/", "")}");
                    sb.AppendLine($"**URL:** {pr["url"]}");
                    
                    if (pr["description"] != null && !string.IsNullOrWhiteSpace(pr["description"].ToString()))
                    {
                        sb.AppendLine("\n**Description:**");
                        sb.AppendLine(pr["description"].ToString());
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
                        }
                    }
                    
                    // Add work item references if available
                    if (pr["workItemRefs"] is JArray workItems && workItems.Count > 0)
                    {
                        sb.AppendLine("\n**Related Work Items:**");
                        foreach (var workItem in workItems)
                        {
                            sb.AppendLine($"- [{workItem["id"]}] {workItem["name"]}");
                        }
                    }
                    
                    sb.AppendLine();
                }
                
                return sb.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error formatting pull requests information");
                return $"Error formatting pull requests information: {ex.Message}\n\nRaw JSON:\n{jsonContent}";
            }
        }
    }
}
