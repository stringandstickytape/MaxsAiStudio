﻿// AiStudio4\Core\Tools\GitHub\GitHubCreateIssueTool.cs
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
    /// Implementation of the GitHub Create Issue API tool
    /// </summary>
    public class GitHubCreateIssueTool : BaseToolImplementation
    {
        private readonly HttpClient _httpClient;

        public GitHubCreateIssueTool(ILogger<GitHubCreateIssueTool> logger, IGeneralSettingsService generalSettingsService, IStatusMessageService statusMessageService) 
            : base(logger, generalSettingsService, statusMessageService)
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "AiStudio4-GitHub-Tool");
            _httpClient.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
        }

        /// <summary>
        /// Gets the GitHub Create Issue tool definition
        /// </summary>
        public override Tool GetToolDefinition()
        {
            return new Tool
            {
                Guid = ToolGuids.GITHUB_CREATE_ISSUE_TOOL_GUID,
                Name = "GitHubCreateIssue",
                Description = "Creates a new issue in the specified repository.",
                Schema = """
{
  "name": "GitHubCreateIssue",
  "description": "Creates a new issue in the specified repository.",
  "input_schema": {
    "type": "object",
    "properties": {
      "owner": { "type": "string", "description": "Repository owner." },
      "repo": { "type": "string", "description": "Repository name." },
      "title": { "type": "string", "description": "The title of the issue." },
      "body": { "type": "string", "description": "The contents of the issue (Markdown). (Optional)" },
      "labels": { "type": "array", "description": "Array of label names to apply. (Optional)", "items": { "type": "string" } },
      "assignees": { "type": "array", "description": "Array of login names to assign. (Optional)", "items": { "type": "string" } },
      "milestone": { "type": "integer", "description": "The number of the milestone to associate this issue with. (Optional)" }
    },
    "required": ["owner", "repo", "title"]
  }
}
""",
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
                SendStatusUpdate("Starting GitHub Create Issue tool execution...");
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

                if (!parameters.TryGetValue("title", out var titleObj) || !(titleObj is string title) || string.IsNullOrWhiteSpace(title))
                {
                    return CreateResult(true, true, "Error: 'title' parameter is required.");
                }

                // Extract optional parameters
                string body = parameters.TryGetValue("body", out var bodyObj) && bodyObj is string bodyStr ? bodyStr : null;
                
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
                if (parameters.TryGetValue("milestone", out var milestoneObj) && milestoneObj != null)
                {
                    if (int.TryParse(milestoneObj.ToString(), out int milestoneInt))
                    {
                        milestone = milestoneInt;
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
                return await CreateIssueAsync(owner, repo, title, body, labels, assignees, milestone);
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

        private async Task<BuiltinToolResult> CreateIssueAsync(string owner, string repo, string title, string body, List<string> labels, List<string> assignees, int? milestone)
        {
            try
            {
                SendStatusUpdate($"Creating issue in {owner}/{repo}...");
                
                // Prepare the request body with attribution if body is provided
                var requestBody = new JObject
                {
                    ["title"] = title
                };

                if (!string.IsNullOrEmpty(body))
                {
                    string attribution = $"\n\n---\n*Content created by {GetToolDefinition().Name} via Max's AI Studio*";
                    string contentToSubmit = body + attribution;
                    requestBody["body"] = contentToSubmit;
                }

                if (labels != null && labels.Count > 0)
                {
                    requestBody["labels"] = new JArray(labels);
                }

                if (assignees != null && assignees.Count > 0)
                {
                    requestBody["assignees"] = new JArray(assignees);
                }

                if (milestone.HasValue)
                {
                    requestBody["milestone"] = milestone.Value;
                }

                string url = $"https://api.github.com/repos/{owner}/{repo}/issues";
                var jsonContent = new StringContent(requestBody.ToString(), Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync(url, jsonContent);
                var content = await response.Content.ReadAsStringAsync();
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorObj = JObject.Parse(content);
                    string errorMessage = errorObj["message"]?.ToString() ?? "Unknown error";
                    return CreateResult(true, true, $"GitHub API Error: {errorMessage} (Status code: {response.StatusCode})");
                }
                
                var createdIssue = JObject.Parse(content);
                string issueNumber = createdIssue["number"]?.ToString() ?? "Unknown";
                string issueUrl = createdIssue["html_url"]?.ToString() ?? "";
                
                SendStatusUpdate("Successfully created issue.");
                return CreateResult(true, true, $"✅ Issue created successfully!\n\n**Issue #{issueNumber}:** {title}\n**URL:** {issueUrl}");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error creating issue");
                return CreateResult(true, true, $"Error creating issue: {ex.Message}");
            }
        }
    }
}