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
    /// Implementation of the Azure DevOps Get Pull Request Threads tool
    /// </summary>
    public class AzureDevOpsGetPullRequestThreadsTool : BaseToolImplementation
    {
        private readonly HttpClient _httpClient;

        public AzureDevOpsGetPullRequestThreadsTool(ILogger<AzureDevOpsGetPullRequestThreadsTool> logger, IGeneralSettingsService generalSettingsService, IStatusMessageService statusMessageService) 
            : base(logger, generalSettingsService, statusMessageService)
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "AiStudio4-AzureDevOps-Tool");
        }

        /// <summary>
        /// Gets the Azure DevOps Get Pull Request Threads tool definition
        /// </summary>
        public override Tool GetToolDefinition()
        {
            return new Tool
            {
                Guid = "7c4d8e9f-2a3b-4c5d-6e7f-8a9b0c1d2e3f",
                Name = "AzureDevOpsGetPullRequestThreads",
                Description = "Retrieves comment threads associated with a specific pull request in Azure DevOps.",
                Schema = @"{
  ""name"": ""AzureDevOpsGetPullRequestThreads"",
  ""description"": ""Retrieves comment threads associated with a specific pull request in Azure DevOps."",
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
      ""pull_request_id"": {
        ""title"": ""Pull Request ID"",
        ""type"": ""integer"",
        ""description"": ""The pull request ID""
      },
      ""top"": {
        ""title"": ""Top"",
        ""type"": ""integer"",
        ""description"": ""Number of threads to return"",
        ""default"": 100
      },
      ""skip"": {
        ""title"": ""Skip"",
        ""type"": ""integer"",
        ""description"": ""Number of threads to skip"",
        ""default"": 0
      }
    },
    ""required"": [""organization"", ""project"", ""repository_id"", ""pull_request_id""],
    ""title"": ""AzureDevOpsGetPullRequestThreadsArguments"",
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
                SendStatusUpdate("Starting Azure DevOps Get Pull Request Threads tool execution...");
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

                if (!parameters.TryGetValue("pull_request_id", out var prIdObj))
                {
                    return CreateResult(true, true, $"Parameters: organization={organization}, project={project}, repository_id={repositoryId}, pull_request_id=<missing>\n\nError: 'pull_request_id' parameter is required.");
                }

                int pullRequestId;
                if (prIdObj is long prIdLong)
                {
                    pullRequestId = (int)prIdLong;
                }
                else if (prIdObj is int prIdInt)
                {
                    pullRequestId = prIdInt;
                }
                else if (prIdObj is string prIdStr && int.TryParse(prIdStr, out int parsedPrId))
                {
                    pullRequestId = parsedPrId;
                }
                else
                {
                    return CreateResult(true, true, $"Parameters: organization={organization}, project={project}, repository_id={repositoryId}, pull_request_id={prIdObj}\n\nError: 'pull_request_id' must be an integer.");
                }

                // Extract optional parameters
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
                    else if (topObj is string topStr && int.TryParse(topStr, out int parsedTop))
                    {
                        top = parsedTop;
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
                    else if (skipObj is string skipStr && int.TryParse(skipStr, out int parsedSkip))
                    {
                        skip = parsedSkip;
                    }
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
                return await GetPullRequestThreadsAsync(organization, project, repositoryId, pullRequestId, top, skip);
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

        private async Task<BuiltinToolResult> GetPullRequestThreadsAsync(string organization, string project, string repositoryId, 
            int pullRequestId, int top, int skip)
        {
            try
            {
                SendStatusUpdate($"Fetching pull request threads for PR #{pullRequestId} in {organization}/{project}/{repositoryId}...");
                
                // Build query parameters
                var queryParams = new List<string>();
                queryParams.Add($"$top={top}");
                queryParams.Add($"$skip={skip}");
                
                string queryString = queryParams.Count > 0 ? $"?{string.Join("&", queryParams)}" : "";
                string url = $"https://dev.azure.com/{organization}/{project}/_apis/git/repositories/{repositoryId}/pullRequests/{pullRequestId}/threads{queryString}";
                
                var response = await _httpClient.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorObj = JObject.Parse(content);
                    string errorMessage = errorObj["message"]?.ToString() ?? "Unknown error";
                    return CreateResult(true, true, $"Parameters: organization={organization}, project={project}, repository_id={repositoryId}, pull_request_id={pullRequestId}\n\nAzure DevOps API Error: {errorMessage} (Status code: {response.StatusCode})");
                }
                
                var formattedContent = FormatPullRequestThreadsInfo(content);
                
                SendStatusUpdate("Successfully retrieved pull request threads information.");
                return CreateResult(true, true, $"Parameters: organization={organization}, project={project}, repository_id={repositoryId}, pull_request_id={pullRequestId}\n\n{formattedContent}");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error fetching pull request threads information");
                return CreateResult(true, true, $"Parameters: organization={organization}, project={project}, repository_id={repositoryId}, pull_request_id={pullRequestId}\n\nError fetching pull request threads information: {ex.Message}");
            }
        }

        private string FormatPullRequestThreadsInfo(string jsonContent)
        {
            try
            {
                var threadData = JObject.Parse(jsonContent);
                var threads = threadData["value"] as JArray;
                var sb = new StringBuilder();
                
                sb.AppendLine("# Pull Request Comment Threads");
                sb.AppendLine();
                
                if (threads == null || threads.Count == 0)
                {
                    sb.AppendLine("No comment threads found for this pull request.");
                    return sb.ToString();
                }
                
                sb.AppendLine($"Found {threads.Count} comment threads:\n");
                
                foreach (var thread in threads)
                {
                    // Get thread properties
                    string threadId = thread["id"]?.ToString() ?? "Unknown ID";
                    string status = thread["status"]?.ToString() ?? "Unknown";
                    bool isDeleted = thread["isDeleted"] != null && (bool)thread["isDeleted"];
                    
                    // Get file path information if this is a code comment
                    string filePath = "";
                    int? lineNumber = null;
                    
                    if (thread["threadContext"] != null)
                    {
                        if (thread["threadContext"]["filePath"] != null)
                        {
                            filePath = thread["threadContext"]["filePath"].ToString();
                        }
                        
                        if (thread["threadContext"]["rightFileStart"] != null && 
                            thread["threadContext"]["rightFileStart"]["line"] != null)
                        {
                            lineNumber = (int)thread["threadContext"]["rightFileStart"]["line"];
                        }
                        else if (thread["threadContext"]["leftFileStart"] != null && 
                                thread["threadContext"]["leftFileStart"]["line"] != null)
                        {
                            lineNumber = (int)thread["threadContext"]["leftFileStart"]["line"];
                        }
                    }
                    
                    // Thread header
                    sb.AppendLine($"## Thread {threadId}");
                    sb.AppendLine($"**Status:** {status}{(isDeleted ? " (Deleted)" : "")}");
                    
                    if (!string.IsNullOrEmpty(filePath))
                    {
                        sb.AppendLine($"**File:** {filePath}{(lineNumber.HasValue ? $" (Line {lineNumber})" : "")}");
                    }
                    
                    // Get comments in this thread
                    var comments = thread["comments"] as JArray;
                    if (comments != null && comments.Count > 0)
                    {
                        sb.AppendLine("\n**Comments:**");
                        
                        foreach (var comment in comments)
                        {
                            string commentId = comment["id"]?.ToString() ?? "Unknown";
                            string author = comment["author"]?["displayName"]?.ToString() ?? "Unknown";
                            string content = comment["content"]?.ToString() ?? "";
                            string date = comment["publishedDate"]?.ToString() ?? "Unknown date";
                            bool isDeleted2 = comment["isDeleted"] != null && (bool)comment["isDeleted"];
                            
                            sb.AppendLine($"\n### Comment {commentId} by {author} on {date}{(isDeleted2 ? " (Deleted)" : "")}:");
                            sb.AppendLine(content);
                        }
                    }
                    else
                    {
                        sb.AppendLine("\n**Comments:** None");
                    }
                    
                    sb.AppendLine();
                }
                
                return sb.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error formatting pull request threads information");
                return $"Error formatting pull request threads information: {ex.Message}\n\nRaw JSON:\n{jsonContent}";
            }
        }
    }
}
