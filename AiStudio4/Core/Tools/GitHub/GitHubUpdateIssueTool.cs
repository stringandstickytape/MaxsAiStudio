// AiStudio4\Core\Tools\GitHub\GitHubUpdateIssueTool.cs








using System.Net.Http;
using System.Net.Http.Headers;



namespace AiStudio4.Core.Tools.GitHub
{
    /// <summary>
    /// Implementation of the GitHub Update Issue API tool
    /// </summary>
    public class GitHubUpdateIssueTool : BaseToolImplementation
    {
        private readonly HttpClient _httpClient;

        public GitHubUpdateIssueTool(ILogger<GitHubUpdateIssueTool> logger, IGeneralSettingsService generalSettingsService, IStatusMessageService statusMessageService) 
            : base(logger, generalSettingsService, statusMessageService)
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "AiStudio4-GitHub-Tool");
            _httpClient.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
        }

        /// <summary>
        /// Gets the GitHub Update Issue tool definition
        /// </summary>
        public override Tool GetToolDefinition()
        {
            return new Tool
            {
                Guid = "d1e2f3a4-b5c6-d7e8-f9a0-d1e2f3a4b5c6",
                Name = "GitHubUpdateIssue",
                Description = "Updates an existing issue's title, body, state, labels, assignees, or milestone. Provide only the fields to be changed.",
                Schema = @"{
  ""name"": ""GitHubUpdateIssue"",
  ""description"": ""Updates an existing issue's title, body, state, labels, assignees, or milestone. Provide only the fields to be changed."",
  ""input_schema"": {
    ""type"": ""object"",
    ""properties"": {
      ""owner"": { ""type"": ""string"", ""description"": ""Repository owner."" },
      ""repo"": { ""type"": ""string"", ""description"": ""Repository name."" },
      ""issue_number"": { ""type"": ""integer"", ""description"": ""The number of the issue to update."" },
      ""title"": { ""type"": ""string"", ""description"": ""New title for the issue. (Optional)"" },
      ""body"": { ""type"": ""string"", ""description"": ""New body for the issue (Markdown). (Optional)"" },
      ""state"": { ""type"": ""string"", ""description"": ""New state for the issue. (Optional)"", ""enum"": [""open"", ""closed""] },
      ""state_reason"": { ""type"": ""string"", ""description"": ""Reason for closing the issue (if state is 'closed'). (Optional)"", ""enum"": [""completed"", ""not_planned""] },
      ""labels"": { ""type"": ""array"", ""description"": ""Array of label names. This will REPLACE all existing labels. (Optional)"", ""items"": { ""type"": ""string"" } },
      ""assignees"": { ""type"": ""array"", ""description"": ""Array of login names. This will REPLACE all existing assignees. (Optional)"", ""items"": { ""type"": ""string"" } },
      ""milestone"": { ""type"": ""integer"", ""description"": ""The number of the milestone to associate, or -1 to remove milestone. (Optional)"" }
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
                SendStatusUpdate("Starting GitHub Update Issue tool execution...");
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

                // Extract optional parameters
                string title = parameters.TryGetValue("title", out var titleObj) && titleObj is string titleStr ? titleStr : null;
                string body = parameters.TryGetValue("body", out var bodyObj) && bodyObj is string bodyStr ? bodyStr : null;
                string state = parameters.TryGetValue("state", out var stateObj) && stateObj is string stateStr ? stateStr : null;
                string stateReason = parameters.TryGetValue("state_reason", out var stateReasonObj) && stateReasonObj is string stateReasonStr ? stateReasonStr : null;
                
                List<string> labels = null;
                if (parameters.TryGetValue("labels", out var labelsObj) && labelsObj is JArray labelsArray)
                {
                    labels = new List<string>();
                    foreach (var label in labelsArray)
                    {
                        if (label.Type == JTokenType.String && !string.IsNullOrWhiteSpace(label.Value<string>()))
                        {
                            labels.Add(label.Value<string>());
                        }
                    }
                }

                List<string> assignees = null;
                if (parameters.TryGetValue("assignees", out var assigneesObj) && assigneesObj is JArray assigneesArray)
                {
                    assignees = new List<string>();
                    foreach (var assignee in assigneesArray)
                    {
                        if (assignee.Type == JTokenType.String && !string.IsNullOrWhiteSpace(assignee.Value<string>()))
                        {
                            assignees.Add(assignee.Value<string>());
                        }
                    }
                }

                int? milestone = null;
                bool removeMilestone = false;
                if (parameters.TryGetValue("milestone", out var milestoneObj))
                {
                    if (int.TryParse(milestoneObj.ToString(), out int milestoneInt))
                    {
                        if (milestoneInt == -1)
                        {
                            removeMilestone = true;
                        }
                        else
                        {
                            milestone = milestoneInt;
                        }
                    }
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
                return await UpdateIssueAsync(owner, repo, issueNumber, title, body, state, stateReason, labels, assignees, milestone, removeMilestone);
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

        private async Task<BuiltinToolResult> UpdateIssueAsync(string owner, string repo, int issueNumber, string title, string body, string state, string stateReason, List<string> labels, List<string> assignees, int? milestone, bool removeMilestone)
        {
            try
            {
                SendStatusUpdate($"Updating issue #{issueNumber} in {owner}/{repo}...");
                
                // Prepare the request body - only include fields that are being updated
                var requestBody = new JObject();

                if (!string.IsNullOrEmpty(title))
                {
                    requestBody["title"] = title;
                }

                if (!string.IsNullOrEmpty(body))
                {
                    string attribution = $"\n\n---\n*Content updated by {GetToolDefinition().Name} via Max's AI Studio*";
                    string contentToSubmit = body + attribution;
                    requestBody["body"] = contentToSubmit;
                }

                if (!string.IsNullOrEmpty(state))
                {
                    requestBody["state"] = state;
                    if (!string.IsNullOrEmpty(stateReason))
                    {
                        requestBody["state_reason"] = stateReason;
                    }
                }

                if (labels != null)
                {
                    requestBody["labels"] = new JArray(labels);
                }

                if (assignees != null)
                {
                    requestBody["assignees"] = new JArray(assignees);
                }

                if (milestone.HasValue)
                {
                    requestBody["milestone"] = milestone.Value;
                }
                else if (removeMilestone)
                {
                    requestBody["milestone"] = null;
                }

                // Only proceed if there are fields to update
                if (requestBody.Count == 0)
                {
                    return CreateResult(true, true, "Error: No fields specified for update. Please provide at least one field to update.");
                }

                string url = $"https://api.github.com/repos/{owner}/{repo}/issues/{issueNumber}";
                var jsonContent = new StringContent(requestBody.ToString(), Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PatchAsync(url, jsonContent);
                var content = await response.Content.ReadAsStringAsync();
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorObj = JObject.Parse(content);
                    string errorMessage = errorObj["message"]?.ToString() ?? "Unknown error";
                    return CreateResult(true, true, $"GitHub API Error: {errorMessage} (Status code: {response.StatusCode})");
                }
                
                var updatedIssue = JObject.Parse(content);
                string issueTitle = updatedIssue["title"]?.ToString() ?? "Unknown";
                string issueUrl = updatedIssue["html_url"]?.ToString() ?? "";
                
                SendStatusUpdate("Successfully updated issue.");
                return CreateResult(true, true, $"✅ Issue updated successfully!\n\n**Issue #{issueNumber}:** {issueTitle}\n**URL:** {issueUrl}");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error updating issue");
                return CreateResult(true, true, $"Error updating issue: {ex.Message}");
            }
        }
    }
}
