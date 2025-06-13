// AiStudio4.Core\Tools\GitHub\GitHubListPullRequestsTool.cs
﻿







using System.Net.Http;


namespace AiStudio4.Core.Tools.GitHub
{
    /// <summary>
    /// Implementation of the GitHub List Pull Requests API tool
    /// </summary>
    public class GitHubListPullRequestsTool : BaseToolImplementation
    {
        private readonly HttpClient _httpClient;

        public GitHubListPullRequestsTool(ILogger<GitHubListPullRequestsTool> logger, IGeneralSettingsService generalSettingsService, IStatusMessageService statusMessageService)
            : base(logger, generalSettingsService, statusMessageService)
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "AiStudio4-GitHub-Tool");
        }

        /// <summary>
        /// Gets the GitHub List Pull Requests tool definition
        /// </summary>
        public override Tool GetToolDefinition()
        {
            return new Tool
            {
                Guid = ToolGuids.GITHUB_LIST_PULL_REQUESTS_TOOL_GUID,
                Name = "GitHubListPullRequests",
                Description = "Lists pull requests for a GitHub repository. Requires GitHub Personal Access Token with repo permissions.",
                Schema = """
{
  "name": "GitHubListPullRequests",
  "description": "Lists pull requests for a GitHub repository. Requires GitHub Personal Access Token with repo permissions.",
  "input_schema": {
    "type": "object",
    "properties": {
      "owner": { "type": "string", "description": "Repository owner (username or organization)." },
      "repo": { "type": "string", "description": "Repository name." },
      "state": { "type": "string", "description": "State of the PR: open, closed, or all. (Optional)" },
      "head": { "type": "string", "description": "Filter by head user or branch name. (Optional)" },
      "base": { "type": "string", "description": "Filter by base branch name. (Optional)" },
      "per_page": { "type": "integer", "description": "Number of results per page (max 100). (Optional)" },
      "page": { "type": "integer", "description": "Page number of the results to fetch. (Optional)" }
    },
    "required": ["owner", "repo"]
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
            SendStatusUpdate("Starting GitHub List Pull Requests tool execution...");
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
                string state = parameters.TryGetValue("state", out var stateObj) ? stateObj?.ToString() : null;
                string head = parameters.TryGetValue("head", out var headObj) ? headObj?.ToString() : null;
                string baseBranch = parameters.TryGetValue("base", out var baseObj) ? baseObj?.ToString() : null;
                int? perPage = parameters.TryGetValue("per_page", out var perPageObj) && int.TryParse(perPageObj?.ToString(), out int pp) ? pp : (int?)null;
                int? page = parameters.TryGetValue("page", out var pageObj) && int.TryParse(pageObj?.ToString(), out int pg) ? pg : (int?)null;
                string apiKey = _generalSettingsService.GetDecryptedGitHubApiKey();
                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    return CreateResult(false, false, "Error: GitHub Personal Access Token not configured. Please set 'GitHubToken' in the tool's extra properties.");
                }
                SendStatusUpdate($"Listing pull requests for {owner}/{repo}...");
                var queryParams = new List<string>();
                if (!string.IsNullOrWhiteSpace(state)) queryParams.Add($"state={Uri.EscapeDataString(state)}");
                if (!string.IsNullOrWhiteSpace(head)) queryParams.Add($"head={Uri.EscapeDataString(head)}");
                if (!string.IsNullOrWhiteSpace(baseBranch)) queryParams.Add($"base={Uri.EscapeDataString(baseBranch)}");
                if (perPage.HasValue) queryParams.Add($"per_page={perPage.Value}");
                if (page.HasValue) queryParams.Add($"page={page.Value}");
                string query = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : string.Empty;
                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
                string url = $"https://api.github.com/repos/{owner}/{repo}/pulls{query}";
                var response = await _httpClient.GetAsync(url);
                string responseContent = await response.Content.ReadAsStringAsync();
                if (response.IsSuccessStatusCode)
                {
                    var prArray = JsonConvert.DeserializeObject<JArray>(responseContent);
                    var prList = new List<object>();
                    foreach (var pr in prArray)
                    {
                        prList.Add(new
                        {
                            number = pr["number"]?.ToObject<int>(),
                            title = pr["title"]?.ToString(),
                            state = pr["state"]?.ToString(),
                            head = pr["head"]?["ref"]?.ToString(),
                            base_branch = pr["base"]?["ref"]?.ToString(),
                            url = pr["html_url"]?.ToString(),
                            user = pr["user"]?["login"]?.ToString(),
                            created_at = pr["created_at"]?.ToString(),
                            updated_at = pr["updated_at"]?.ToString()
                        });
                    }
                    var result = new
                    {
                        success = true,
                        count = prList.Count,
                        pullRequests = prList
                    };
                    return CreateResult(true, false, JsonConvert.SerializeObject(result, Formatting.Indented));
                }
                else
                {
                    _logger.LogError("GitHub API error: {StatusCode} - {Content}", response.StatusCode, responseContent);
                    string errorMessage = "Failed to list pull requests.";
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
                _logger.LogError(httpEx, "HTTP error in GitHub List Pull Requests tool");
                return CreateResult(false, false, $"Network error: {httpEx.Message}");
            }
            catch (JsonException jsonEx)
            {
                _logger.LogError(jsonEx, "JSON parsing error in GitHub List Pull Requests tool");
                return CreateResult(false, false, $"Error parsing parameters: {jsonEx.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in GitHub List Pull Requests tool");
                return CreateResult(false, false, $"Unexpected error: {ex.Message}");
            }
        }
    }
}
