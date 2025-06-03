// AiStudio4\Core\Tools\GitHub\GitHubGetIssueTool.cs
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

namespace AiStudio4.Core.Tools.GitHub
{
    /// <summary>
    /// Implementation of the GitHub Get Issue API tool
    /// </summary>
    public class GitHubGetIssueTool : BaseToolImplementation
    {
        private readonly HttpClient _httpClient;

        public GitHubGetIssueTool(ILogger<GitHubGetIssueTool> logger, IGeneralSettingsService generalSettingsService, IStatusMessageService statusMessageService) 
            : base(logger, generalSettingsService, statusMessageService)
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "AiStudio4-GitHub-Tool");
            _httpClient.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
        }

        /// <summary>
        /// Gets the GitHub Get Issue tool definition
        /// </summary>
        public override Tool GetToolDefinition()
        {
            return new Tool
            {
                Guid = "b1c2d3e4-f5a6-b7c8-d9e0-b1c2d3e4f5a6",
                Name = "GitHubGetIssue",
                Description = "Retrieves detailed information for a specific issue by its number.",
                Schema = @"{
  ""name"": ""GitHubGetIssue"",
  ""description"": ""Retrieves detailed information for a specific issue by its number."",
  ""input_schema"": {
    ""type"": ""object"",
    ""properties"": {
      ""owner"": { ""type"": ""string"", ""description"": ""Repository owner."" },
      ""repo"": { ""type"": ""string"", ""description"": ""Repository name."" },
      ""issue_number"": { ""type"": ""integer"", ""description"": ""The number of the issue to retrieve."" }
    },
    ""required"": [""owner"", ""repo"", ""issue_number""]
  }
}",
                Categories = new List<string> {"APITools", "GitHub" },
                OutputFileType = "txt",
                Filetype = string.Empty,
                LastModified = DateTime.UtcNow,
                ExtraProperties = new Dictionary<string, string> {
                    { "githubApiKey", "" }
                }
            };
        }

        public override async Task<BuiltinToolResult> ProcessAsync(string toolParameters, Dictionary<string, string> extraProperties)
        {
            try
            {
                SendStatusUpdate("Starting GitHub Get Issue tool execution...");
                var parameters = JsonConvert.DeserializeObject<Dictionary<string, object>>(toolParameters) ?? new Dictionary<string, object>();

                // Extract required parameters
                if (!parameters.TryGetValue("owner", out var ownerObj) || !(ownerObj is string owner) || string.IsNullOrWhiteSpace(owner))
                {
                    return CreateResult(true, true, "Error: 'owner' parameter is required.");
                }

                if (!parameters.TryGetValue("repo", out var repoObj) || !(repoObj is string repo) || string.IsNullOrWhiteSpace(repo))
                {
                    return CreateResult(true, true, "Error: 'repo' parameter is required.");
                }

                if (!parameters.TryGetValue("issue_number", out var issueNumberObj) || !int.TryParse(issueNumberObj.ToString(), out int issueNumber))
                {
                    return CreateResult(true, true, "Error: 'issue_number' parameter is required and must be a valid integer.");
                }

                // Get API key from settings
                string apiKey = _generalSettingsService.GetDecryptedGitHubApiKey();
                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    return CreateResult(true, true, "Error: GitHub API Key is not configured. Please set it in File > Settings > Set GitHub API Key.");
                }

                // Set up authentication header
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

                // Make the API request
                return await GetIssueAsync(owner, repo, issueNumber);
            }
            catch (JsonException jsonEx)
            {
                _logger.LogError(jsonEx, "Error deserializing GitHub tool parameters");
                return CreateResult(true, true, $"Error processing GitHub tool parameters: Invalid JSON format. {jsonEx.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing GitHub tool");
                return CreateResult(true, true, $"Error processing GitHub tool: {ex.Message}");
            }
        }

        private async Task<BuiltinToolResult> GetIssueAsync(string owner, string repo, int issueNumber)
        {
            try
            {
                SendStatusUpdate($"Fetching issue #{issueNumber} from {owner}/{repo}...");
                
                string url = $"https://api.github.com/repos/{owner}/{repo}/issues/{issueNumber}";
                
                var response = await _httpClient.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorObj = JObject.Parse(content);
                    string errorMessage = errorObj["message"]?.ToString() ?? "Unknown error";
                    return CreateResult(true, true, $"GitHub API Error: {errorMessage} (Status code: {response.StatusCode})");
                }
                
                var formattedResult = FormatIssueDetails(content);
                
                SendStatusUpdate("Successfully retrieved issue details.");
                return CreateResult(true, true, formattedResult);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error fetching issue details");
                return CreateResult(true, true, $"Error fetching issue details: {ex.Message}");
            }
        }

        private string FormatIssueDetails(string jsonContent)
        {
            try
            {
                var issue = JObject.Parse(jsonContent);
                var sb = new StringBuilder();
                
                string number = issue["number"]?.ToString() ?? "Unknown";
                string title = issue["title"]?.ToString() ?? "No title";
                string body = issue["body"]?.ToString() ?? "No description";
                string state = issue["state"]?.ToString() ?? "unknown";
                string htmlUrl = issue["html_url"]?.ToString() ?? "";
                string createdAt = issue["created_at"]?.ToString() ?? "";
                string updatedAt = issue["updated_at"]?.ToString() ?? "";
                int comments = issue["comments"]?.ToObject<int>() ?? 0;

                // Get creator
                string creator = "unknown";
                var userObj = issue["user"];
                if (userObj != null && userObj.Type != JTokenType.Null)
                {
                    creator = userObj["login"]?.ToString() ?? "unknown";
                }

                // Get labels
                var labelsArray = issue["labels"] as JArray;
                var labelNames = new List<string>();
                if (labelsArray != null)
                {
                    foreach (var label in labelsArray)
                    {
                        string labelName = label["name"]?.ToString();
                        if (!string.IsNullOrEmpty(labelName))
                        {
                            labelNames.Add(labelName);
                        }
                    }
                }
                string labelsText = labelNames.Count > 0 ? string.Join(", ", labelNames) : "none";

                // Get assignees
                var assigneesArray = issue["assignees"] as JArray;
                var assigneeNames = new List<string>();
                if (assigneesArray != null)
                {
                    foreach (var assignee in assigneesArray)
                    {
                        string assigneeName = assignee["login"]?.ToString();
                        if (!string.IsNullOrEmpty(assigneeName))
                        {
                            assigneeNames.Add(assigneeName);
                        }
                    }
                }
                string assigneesText = assigneeNames.Count > 0 ? string.Join(", ", assigneeNames) : "none";

                // Get milestone
                string milestone = "none";
                var milestoneObj = issue["milestone"];
                if (milestoneObj != null && milestoneObj.Type != JTokenType.Null)
                {
                    milestone = milestoneObj["title"]?.ToString() ?? "unknown";
                }

                sb.AppendLine($"# Issue #{number}: {title}");
                sb.AppendLine();
                sb.AppendLine($"**State:** {state}");
                sb.AppendLine($"**Creator:** {creator}");
                sb.AppendLine($"**Created:** {createdAt}");
                sb.AppendLine($"**Last Updated:** {updatedAt}");
                sb.AppendLine($"**Labels:** {labelsText}");
                sb.AppendLine($"**Assignees:** {assigneesText}");
                sb.AppendLine($"**Milestone:** {milestone}");
                sb.AppendLine($"**Comments:** {comments}");
                sb.AppendLine($"**URL:** {htmlUrl}");
                sb.AppendLine();
                sb.AppendLine("## Description");
                sb.AppendLine();
                if (!string.IsNullOrWhiteSpace(body))
                {
                    sb.AppendLine(body);
                }
                else
                {
                    sb.AppendLine("*No description provided*");
                }
                
                return sb.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error formatting issue details");
                return $"Error formatting issue details: {ex.Message}\n\nRaw JSON:\n{jsonContent}";
            }
        }
    }
}