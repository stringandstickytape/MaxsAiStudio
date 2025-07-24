// AiStudio4.Core\Tools\GitHub\GitHubCreatePullRequestTool.cs








using System.Net.Http;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using System.ComponentModel;



namespace AiStudio4.Core.Tools.GitHub
{
    /// <summary>
    /// Implementation of the GitHub Create Pull Request API tool
    /// </summary>
    [McpServerToolType]
    public class GitHubCreatePullRequestTool : BaseToolImplementation
    {
        private readonly HttpClient _httpClient;

        public GitHubCreatePullRequestTool(ILogger<GitHubCreatePullRequestTool> logger, IGeneralSettingsService generalSettingsService, IStatusMessageService statusMessageService) 
            : base(logger, generalSettingsService, statusMessageService)
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "AiStudio4-GitHub-Tool");
        }

        /// <summary>
        /// Gets the GitHub Create Pull Request tool definition
        /// </summary>
        public override Tool GetToolDefinition()
        {
            return new Tool
            {
                Guid = ToolGuids.GITHUB_CREATE_PULL_REQUEST_TOOL_GUID,
                Name = "GitHubCreatePullRequest",
                Description = "Creates a new pull request in a GitHub repository. Requires GitHub Personal Access Token with repo permissions.",
                Schema = """
{
  "name": "GitHubCreatePullRequest",
  "description": "Creates a new pull request in a GitHub repository. Requires GitHub Personal Access Token with repo permissions.",
  "input_schema": {
    "type": "object",
    "properties": {
      "owner": { "type": "string", "description": "Repository owner (username or organization)." },
      "repo": { "type": "string", "description": "Repository name." },
      "title": { "type": "string", "description": "The title of the pull request." },
      "head": { "type": "string", "description": "The name of the branch where your changes are implemented. For cross-repository pull requests in the same network, namespace head with a user like this: username:branch." },
      "base": { "type": "string", "description": "The name of the branch you want the changes pulled into. This should be an existing branch on the current repository." },
      "body": { "type": "string", "description": "The contents of the pull request (Markdown). (Optional)" },
      "draft": { "type": "boolean", "description": "Indicates whether the pull request is a draft. (Optional)", "default": false },
      "maintainer_can_modify": { "type": "boolean", "description": "Indicates whether maintainers can modify the pull request. (Optional)", "default": true }
    },
    "required": ["owner", "repo", "title", "head", "base"]
  }
}
""",
                Categories = new List<string> {"APITools", "GitHub" },
                OutputFileType = "json",
                Filetype = string.Empty,
                LastModified = DateTime.UtcNow
            };
        }

        public override async Task<BuiltinToolResult> ProcessAsync(string toolParameters, Dictionary<string, string> extraProperties)
        {
            SendStatusUpdate("Starting GitHub Create Pull Request tool execution...");
            
            try
            {
                var parameters = JsonConvert.DeserializeObject<Dictionary<string, object>>(toolParameters) ?? new Dictionary<string, object>();
                
                // Extract required parameters
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

                if (!parameters.TryGetValue("title", out var titleObj) || string.IsNullOrWhiteSpace(titleObj?.ToString()))
                {
                    return CreateResult(false, false, "Error: 'title' parameter is required and cannot be empty.");
                }
                string title = titleObj.ToString();

                if (!parameters.TryGetValue("head", out var headObj) || string.IsNullOrWhiteSpace(headObj?.ToString()))
                {
                    return CreateResult(false, false, "Error: 'head' parameter is required and cannot be empty.");
                }
                string head = headObj.ToString();

                if (!parameters.TryGetValue("base", out var baseObj) || string.IsNullOrWhiteSpace(baseObj?.ToString()))
                {
                    return CreateResult(false, false, "Error: 'base' parameter is required and cannot be empty.");
                }
                string baseBranch = baseObj.ToString();

                // Extract optional parameters
                string body = parameters.TryGetValue("body", out var bodyObj) ? bodyObj?.ToString() : null;
                bool draft = parameters.TryGetValue("draft", out var draftObj) && bool.TryParse(draftObj?.ToString(), out bool draftValue) && draftValue;
                bool maintainerCanModify = !parameters.TryGetValue("maintainer_can_modify", out var maintainerObj) || !bool.TryParse(maintainerObj?.ToString(), out bool maintainerValue) || maintainerValue;

                string apiKey = _generalSettingsService.GetDecryptedGitHubApiKey();

                // Get GitHub token from extra properties
                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    return CreateResult(false, false, "Error: GitHub Personal Access Token not configured. Please set 'GitHubToken' in the tool's extra properties.");
                }

                SendStatusUpdate($"Creating pull request '{title}' in {owner}/{repo}...");

                // Prepare the request body
                var requestBody = new
                {
                    title = title,
                    head = head,
                    @base = baseBranch,
                    body = body,
                    draft = draft,
                    maintainer_can_modify = maintainerCanModify
                };

                string jsonBody = JsonConvert.SerializeObject(requestBody);
                var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                // Set authorization header
                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

                // Make the API request
                string url = $"https://api.github.com/repos/{owner}/{repo}/pulls";
                var response = await _httpClient.PostAsync(url, content);
                string responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var pullRequest = JsonConvert.DeserializeObject<JObject>(responseContent);
                    string prNumber = pullRequest["number"]?.ToString();
                    string prUrl = pullRequest["html_url"]?.ToString();
                    
                    SendStatusUpdate($"Pull request #{prNumber} created successfully.");
                    
                    var result = new
                    {
                        success = true,
                        pullRequest = new
                        {
                            number = prNumber,
                            url = prUrl,
                            title = title,
                            head = head,
                            @base = baseBranch,
                            draft = draft,
                            state = pullRequest["state"]?.ToString(),
                            created_at = pullRequest["created_at"]?.ToString()
                        },
                        message = $"✅ Pull request created successfully!\n\n**Pull Request #{prNumber}:** {title}\n**URL:** {prUrl}\n**From:** {head} → {baseBranch}\n**Status:** {(draft ? "Draft" : "Ready for review")}"
                    };

                    return CreateResult(true, false, JsonConvert.SerializeObject(result, Formatting.Indented));
                }
                else
                {
                    _logger.LogError("GitHub API error: {StatusCode} - {Content}", response.StatusCode, responseContent);
                    
                    string errorMessage = "Failed to create pull request.";
                    try
                    {
                        var errorResponse = JsonConvert.DeserializeObject<JObject>(responseContent);
                        if (errorResponse["message"] != null)
                        {
                            errorMessage = $"GitHub API Error: {errorResponse["message"]}";
                            
                            // Add more specific error details if available
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
                _logger.LogError(httpEx, "HTTP error in GitHub Create Pull Request tool");
                return CreateResult(false, false, $"Network error: {httpEx.Message}");
            }
            catch (JsonException jsonEx)
            {
                _logger.LogError(jsonEx, "JSON parsing error in GitHub Create Pull Request tool");
                return CreateResult(false, false, $"Error parsing parameters: {jsonEx.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in GitHub Create Pull Request tool");
                return CreateResult(false, false, $"Unexpected error: {ex.Message}");
            }
        }

        [McpServerTool, Description("Creates a new pull request in a GitHub repository. Requires GitHub Personal Access Token with repo permissions.")]
        public async Task<string> GitHubCreatePullRequest([Description("JSON parameters for GitHubCreatePullRequest")] string parameters = "{}")
        {
            return await ExecuteWithExtraProperties(parameters);
        }
    }
}
