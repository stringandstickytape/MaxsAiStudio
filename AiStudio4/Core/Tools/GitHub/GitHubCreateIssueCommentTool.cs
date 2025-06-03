// AiStudio4\Core\Tools\GitHub\GitHubCreateIssueCommentTool.cs
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
    /// Implementation of the GitHub Create Issue Comment API tool
    /// </summary>
    public class GitHubCreateIssueCommentTool : BaseToolImplementation
    {
        private readonly HttpClient _httpClient;

        public GitHubCreateIssueCommentTool(ILogger<GitHubCreateIssueCommentTool> logger, IGeneralSettingsService generalSettingsService, IStatusMessageService statusMessageService) 
            : base(logger, generalSettingsService, statusMessageService)
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "AiStudio4-GitHub-Tool");
            _httpClient.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
        }

        /// <summary>
        /// Gets the GitHub Create Issue Comment tool definition
        /// </summary>
        public override Tool GetToolDefinition()
        {
            return new Tool
            {
                Guid = "e1f2a3b4-c5d6-e7f8-a9b0-e1f2a3b4c5d6",
                Name = "GitHubCreateIssueComment",
                Description = "Adds a new comment to a specified issue.",
                Schema = @"{
  ""name"": ""GitHubCreateIssueComment"",
  ""description"": ""Adds a new comment to a specified issue."",
  ""input_schema"": {
    ""type"": ""object"",
    ""properties"": {
      ""owner"": { ""type"": ""string"", ""description"": ""Repository owner."" },
      ""repo"": { ""type"": ""string"", ""description"": ""Repository name."" },
      ""issue_number"": { ""type"": ""integer"", ""description"": ""The number of the issue to comment on."" },
      ""body"": { ""type"": ""string"", ""description"": ""The content of the comment (Markdown)."" }
    },
    ""required"": [""owner"", ""repo"", ""issue_number"", ""body""]
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
                SendStatusUpdate("Starting GitHub Create Issue Comment tool execution...");
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

                if (!parameters.TryGetValue("body", out var bodyObj) || !(bodyObj is string body) || string.IsNullOrWhiteSpace(body))
                {
                    return CreateResult(true, true, "Error: 'body' parameter is required.");
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
                return await CreateCommentAsync(owner, repo, issueNumber, body);
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

        private async Task<BuiltinToolResult> CreateCommentAsync(string owner, string repo, int issueNumber, string body)
        {
            try
            {
                SendStatusUpdate($"Creating comment on issue #{issueNumber} in {owner}/{repo}...");
                
                // Prepare the request body with attribution
                string attribution = $"\n\n---\n*Comment created by {GetToolDefinition().Name} via Max's AI Studio*";
                string contentToSubmit = body + attribution;

                var requestBody = new JObject
                {
                    ["body"] = contentToSubmit
                };

                string url = $"https://api.github.com/repos/{owner}/{repo}/issues/{issueNumber}/comments";
                var jsonContent = new StringContent(requestBody.ToString(), Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync(url, jsonContent);
                var content = await response.Content.ReadAsStringAsync();
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorObj = JObject.Parse(content);
                    string errorMessage = errorObj["message"]?.ToString() ?? "Unknown error";
                    return CreateResult(true, true, $"GitHub API Error: {errorMessage} (Status code: {response.StatusCode})");
                }
                
                var createdComment = JObject.Parse(content);
                string commentId = createdComment["id"]?.ToString() ?? "Unknown";
                string commentUrl = createdComment["html_url"]?.ToString() ?? "";
                
                SendStatusUpdate("Successfully created comment.");
                return CreateResult(true, true, $"✅ Comment created successfully!\n\n**Comment ID:** {commentId}\n**URL:** {commentUrl}");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error creating comment");
                return CreateResult(true, true, $"Error creating comment: {ex.Message}");
            }
        }
    }
}