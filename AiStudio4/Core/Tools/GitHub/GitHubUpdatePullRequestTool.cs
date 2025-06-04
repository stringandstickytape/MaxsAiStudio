// AiStudio4.Core\Tools\GitHub\GitHubUpdatePullRequestTool.cs
using AiStudio4.Core.Interfaces;
using AiStudio4.Core.Models;
using AiStudio4.InjectedDependencies;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace AiStudio4.Core.Tools.GitHub
{
    /// <summary>
    /// Implementation of the GitHub Update Pull Request API tool
    /// </summary>
    public class GitHubUpdatePullRequestTool : BaseToolImplementation
    {
        private readonly HttpClient _httpClient;

        public GitHubUpdatePullRequestTool(ILogger<GitHubUpdatePullRequestTool> logger, IGeneralSettingsService generalSettingsService, IStatusMessageService statusMessageService)
            : base(logger, generalSettingsService, statusMessageService)
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "AiStudio4-GitHub-Tool");
        }

        /// <summary>
        /// Gets the GitHub Update Pull Request tool definition
        /// </summary>
        public override Tool GetToolDefinition()
        {
            return new Tool
            {
                Guid = "b7c8d9e0-f2a1-4567-8901-bcdef2345678",
                Name = "GitHubUpdatePullRequest",
                Description = "Updates an existing pull request in a GitHub repository. Requires GitHub Personal Access Token with repo permissions.",
                Schema = @"{
  ""name"": ""GitHubUpdatePullRequest"",
  ""description"": ""Updates an existing pull request in a GitHub repository. Requires GitHub Personal Access Token with repo permissions."",
  ""input_schema"": {
    ""type"": ""object"",
    ""properties"": {
      ""owner"": { ""type"": ""string"", ""description"": ""Repository owner (username or organization)."" },
      ""repo"": { ""type"": ""string"", ""description"": ""Repository name."" },
      ""pull_number"": { ""type"": ""integer"", ""description"": ""The number of the pull request to update."" },
      ""title"": { ""type"": ""string"", ""description"": ""The new title for the pull request. (Optional)"" },
      ""body"": { ""type"": ""string"", ""description"": ""The new contents of the pull request (Markdown). (Optional)"" },
      ""state"": { ""type"": ""string"", ""description"": ""State of the PR: open or closed. (Optional)"" },
      ""base"": { ""type"": ""string"", ""description"": ""The branch you want the changes pulled into. (Optional)"" },
      ""maintainer_can_modify"": { ""type"": ""boolean"", ""description"": ""Whether maintainers can modify the PR. (Optional)"" }
    },
    ""required"": [""owner"", ""repo"", ""pull_number""]
  }
}",
                Categories = new List<string> {"APITools", "GitHub" },
                OutputFileType = "json",
                Filetype = string.Empty,
                LastModified = DateTime.UtcNow
            };
        }

        public override async Task<BuiltinToolResult> ProcessAsync(string toolParameters, Dictionary<string, string> extraProperties)
        {
            SendStatusUpdate("Starting GitHub Update Pull Request tool execution...");
            try
            {
                var parameters = JsonConvert.DeserializeObject<Dictionary<string, object>>(toolParameters) ?? new Dictionary<string, object>();
                if (!parameters.TryGetValue("owner", out var ownerObj) || string.IsNullOrWhiteSpace(ownerObj?.ToString()))
                {
                    return CreateResult(false, false, "Error: 'owner' parameter is required and cannot be empty.");
                }
                string owner = ownerObj.ToString();
                if (!parameters.TryGetValue("repo", out var repoObj) || string.IsNullOrWhiteSpace(repoObj?.ToString()))
                {
                    return CreateResult(false, false, "Error: 'repo' parameter is required and cannot be empty.");
                }
                string repo = repoObj.ToString();
                if (!parameters.TryGetValue("pull_number", out var pullNumberObj) || !int.TryParse(pullNumberObj.ToString(), out int pullNumber))
                {
                    return CreateResult(false, false, "Error: 'pull_number' parameter is required and must be an integer.");
                }
                // Optional fields
                string title = parameters.TryGetValue("title", out var titleObj) ? titleObj?.ToString() : null;
                string body = parameters.TryGetValue("body", out var bodyObj) ? bodyObj?.ToString() : null;
                string state = parameters.TryGetValue("state", out var stateObj) ? stateObj?.ToString() : null;
                string baseBranch = parameters.TryGetValue("base", out var baseObj) ? baseObj?.ToString() : null;
                bool? maintainerCanModify = parameters.TryGetValue("maintainer_can_modify", out var maintainerObj) && bool.TryParse(maintainerObj?.ToString(), out bool maintainerValue) ? maintainerValue : (bool?)null;

                string apiKey = _generalSettingsService.GetDecryptedGitHubApiKey();
                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    return CreateResult(false, false, "Error: GitHub Personal Access Token not configured. Please set 'GitHubToken' in the tool's extra properties.");
                }
                SendStatusUpdate($"Updating pull request #{pullNumber} in {owner}/{repo}...");
                var requestBody = new JObject();
                if (title != null) requestBody["title"] = title;
                if (body != null) requestBody["body"] = body;
                if (state != null) requestBody["state"] = state;
                if (baseBranch != null) requestBody["base"] = baseBranch;
                if (maintainerCanModify.HasValue) requestBody["maintainer_can_modify"] = maintainerCanModify.Value;
                string jsonBody = requestBody.ToString();
                var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
                string url = $"https://api.github.com/repos/{owner}/{repo}/pulls/{pullNumber}";
                var response = await _httpClient.PatchAsync(url, content);
                string responseContent = await response.Content.ReadAsStringAsync();
                if (response.IsSuccessStatusCode)
                {
                    var pullRequest = JsonConvert.DeserializeObject<JObject>(responseContent);
                    SendStatusUpdate($"Pull request #{pullNumber} updated successfully.");
                    var result = new
                    {
                        success = true,
                        pullRequest = new
                        {
                            number = pullRequest["number"]?.ToString(),
                            url = pullRequest["html_url"]?.ToString(),
                            title = pullRequest["title"]?.ToString(),
                            state = pullRequest["state"]?.ToString(),
                            @base = pullRequest["base"]?["ref"]?.ToString(),
                            updated_at = pullRequest["updated_at"]?.ToString()
                        },
                        message = $"✅ Pull request #{pullNumber} updated successfully!"
                    };
                    return CreateResult(true, false, JsonConvert.SerializeObject(result, Formatting.Indented));
                }
                else
                {
                    _logger.LogError("GitHub API error: {StatusCode} - {Content}", response.StatusCode, responseContent);
                    string errorMessage = "Failed to update pull request.";
                    try
                    {
                        var errorResponse = JsonConvert.DeserializeObject<JObject>(responseContent);
                        if (errorResponse["message"] != null)
                        {
                            errorMessage = $"GitHub API Error: {errorResponse["message"]}";
                            if (errorResponse["errors"] != null)
                            {
                                var errors = errorResponse["errors"] as JArray;
                                if (errors?.Count > 0)
                                {
                                    errorMessage += $"\nDetails: {string.Join(", ", errors.Select(e => e["message"]?.ToString()))}";
                                }
                            }
                        }
                    }
                    catch
                    {
                        errorMessage = $"GitHub API Error: {response.StatusCode} - {responseContent}";
                    }
                    return CreateResult(false, false, errorMessage);
                }
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, "HTTP error in GitHub Update Pull Request tool");
                return CreateResult(false, false, $"Network error: {httpEx.Message}");
            }
            catch (JsonException jsonEx)
            {
                _logger.LogError(jsonEx, "JSON parsing error in GitHub Update Pull Request tool");
                return CreateResult(false, false, $"Error parsing parameters: {jsonEx.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in GitHub Update Pull Request tool");
                return CreateResult(false, false, $"Unexpected error: {ex.Message}");
            }
        }
    }
}