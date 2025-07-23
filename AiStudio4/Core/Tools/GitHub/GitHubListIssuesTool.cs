// AiStudio4\Core\Tools\GitHub\GitHubListIssuesTool.cs
﻿







using System.Net.Http;
using System.Net.Http.Headers;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Web;

namespace AiStudio4.Core.Tools.GitHub
{
    /// <summary>
    /// Implementation of the GitHub List Issues API tool
    /// </summary>
    [McpServerToolType]
    public class GitHubListIssuesTool : BaseToolImplementation
    {
        private readonly HttpClient _httpClient;

        public GitHubListIssuesTool(ILogger<GitHubListIssuesTool> logger, IGeneralSettingsService generalSettingsService, IStatusMessageService statusMessageService) 
            : base(logger, generalSettingsService, statusMessageService)
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "AiStudio4-GitHub-Tool");
            _httpClient.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
        }

        /// <summary>
        /// Gets the GitHub List Issues tool definition
        /// </summary>
        public override Tool GetToolDefinition()
        {
            return new Tool
            {
                Guid = ToolGuids.GITHUB_LIST_ISSUES_TOOL_GUID,
                Name = "GitHubListIssues",
                Description = "Retrieves a list of issues for the specified repository. Supports filtering by state, labels, assignee, milestone, etc.",
                Schema = """
{
  "name": "GitHubListIssues",
  "description": "Retrieves a list of issues for the specified repository. Supports filtering by state, labels, assignee, milestone, etc.",
  "input_schema": {
    "type": "object",
    "properties": {
      "owner": { "type": "string", "description": "Repository owner (username or organization)." },
      "repo": { "type": "string", "description": "Repository name." },
      "milestone": { "type": "string", "description": "Milestone number or '*' for any, 'none' for no milestone. (Optional)" },
      "state": { "type": "string", "description": "Issue state.", "enum": ["open", "closed", "all"], "default": "open" },
      "assignee": { "type": "string", "description": "Login of the assignee or '*' for any, 'none' for no assignee. (Optional)" },
      "creator": { "type": "string", "description": "Login of the issue creator. (Optional)" },
      "mentioned": { "type": "string", "description": "Login of a user mentioned in an issue. (Optional)" },
      "labels": { "type": "string", "description": "Comma-separated list of label names (e.g., bug,enhancement). (Optional)" },
      "sort": { "type": "string", "description": "What to sort results by.", "enum": ["created", "updated", "comments"], "default": "created" },
      "direction": { "type": "string", "description": "The direction of the sort.", "enum": ["asc", "desc"], "default": "desc" },
      "since": { "type": "string", "description": "Only show issues updated at or after this time (ISO 8601 format: YYYY-MM-DDTHH:MM:SSZ). (Optional)" },
      "per_page": { "type": "integer", "description": "Results per page (max 100).", "default": 30 },
      "page": { "type": "integer", "description": "Page number of the results to fetch.", "default": 1 }
    },
    "required": ["owner", "repo"]
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
                SendStatusUpdate("Starting GitHub List Issues tool execution...");
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

                // Extract optional parameters with defaults
                string milestone = parameters.TryGetValue("milestone", out var milestoneObj) && milestoneObj is string milestoneStr ? milestoneStr : null;
                string state = parameters.TryGetValue("state", out var stateObj) && stateObj is string stateStr ? stateStr : "open";
                string assignee = parameters.TryGetValue("assignee", out var assigneeObj) && assigneeObj is string assigneeStr ? assigneeStr : null;
                string creator = parameters.TryGetValue("creator", out var creatorObj) && creatorObj is string creatorStr ? creatorStr : null;
                string mentioned = parameters.TryGetValue("mentioned", out var mentionedObj) && mentionedObj is string mentionedStr ? mentionedStr : null;
                string labels = parameters.TryGetValue("labels", out var labelsObj) && labelsObj is string labelsStr ? labelsStr : null;
                string sort = parameters.TryGetValue("sort", out var sortObj) && sortObj is string sortStr ? sortStr : "created";
                string direction = parameters.TryGetValue("direction", out var directionObj) && directionObj is string directionStr ? directionStr : "desc";
                string since = parameters.TryGetValue("since", out var sinceObj) && sinceObj is string sinceStr ? sinceStr : null;
                int perPage = parameters.TryGetValue("per_page", out var perPageObj) && perPageObj != null ? Convert.ToInt32(perPageObj) : 30;
                int page = parameters.TryGetValue("page", out var pageObj) && pageObj != null ? Convert.ToInt32(pageObj) : 1;

                // Get API key from settings
                string apiKey = _generalSettingsService.GetDecryptedGitHubApiKey();
                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    return CreateResult(true, true, "Error: GitHub API Key is not configured. Please set it in File > Settings > Set GitHub API Key.");
                }

                // Set up authentication header
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

                // Make the API request
                return await ListIssuesAsync(owner, repo, milestone, state, assignee, creator, mentioned, labels, sort, direction, since, perPage, page);
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

        private async Task<BuiltinToolResult> ListIssuesAsync(string owner, string repo, string milestone, string state, string assignee, string creator, string mentioned, string labels, string sort, string direction, string since, int perPage, int page)
        {
            try
            {
                SendStatusUpdate($"Fetching issues from {owner}/{repo}...");
                
                // Build query parameters
                var queryParams = new List<string>();
                if (!string.IsNullOrEmpty(milestone)) queryParams.Add($"milestone={HttpUtility.UrlEncode(milestone)}");
                if (!string.IsNullOrEmpty(state)) queryParams.Add($"state={HttpUtility.UrlEncode(state)}");
                if (!string.IsNullOrEmpty(assignee)) queryParams.Add($"assignee={HttpUtility.UrlEncode(assignee)}");
                if (!string.IsNullOrEmpty(creator)) queryParams.Add($"creator={HttpUtility.UrlEncode(creator)}");
                if (!string.IsNullOrEmpty(mentioned)) queryParams.Add($"mentioned={HttpUtility.UrlEncode(mentioned)}");
                if (!string.IsNullOrEmpty(labels)) queryParams.Add($"labels={HttpUtility.UrlEncode(labels)}");
                if (!string.IsNullOrEmpty(sort)) queryParams.Add($"sort={HttpUtility.UrlEncode(sort)}");
                if (!string.IsNullOrEmpty(direction)) queryParams.Add($"direction={HttpUtility.UrlEncode(direction)}");
                if (!string.IsNullOrEmpty(since)) queryParams.Add($"since={HttpUtility.UrlEncode(since)}");
                queryParams.Add($"per_page={perPage}");
                queryParams.Add($"page={page}");

                string url = $"https://api.github.com/repos/{owner}/{repo}/issues";
                if (queryParams.Count > 0)
                {
                    url += "?" + string.Join("&", queryParams);
                }
                
                var response = await _httpClient.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorObj = JObject.Parse(content);
                    string errorMessage = errorObj["message"]?.ToString() ?? "Unknown error";
                    return CreateResult(true, true, $"GitHub API Error: {errorMessage} (Status code: {response.StatusCode})");
                }
                
                var formattedResult = FormatIssuesList(content, owner, repo, page, perPage);
                
                SendStatusUpdate("Successfully retrieved issues list.");
                return CreateResult(true, true, formattedResult);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error fetching issues list");
                return CreateResult(true, true, $"Error fetching issues list: {ex.Message}");
            }
        }

        private string FormatIssuesList(string jsonContent, string owner, string repo, int page, int perPage)
        {
            try
            {
                var issues = JArray.Parse(jsonContent);
                var sb = new StringBuilder();
                
                sb.AppendLine($"# Issues for {owner}/{repo}");
                sb.AppendLine();
                sb.AppendLine($"**Total results on this page:** {issues.Count}");
                sb.AppendLine($"**Page:** {page} (showing up to {perPage} results per page)");
                sb.AppendLine();

                if (issues.Count == 0)
                {
                    sb.AppendLine("No issues found matching the specified criteria.");
                    return sb.ToString();
                }

                foreach (var issue in issues)
                {
                    string number = issue["number"]?.ToString() ?? "Unknown";
                    string title = issue["title"]?.ToString() ?? "No title";
                    string state = issue["state"]?.ToString() ?? "unknown";
                    string htmlUrl = issue["html_url"]?.ToString() ?? "";
                    
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

                    // Get assignee
                    string assignee = "none";
                    var assigneeObj = issue["assignee"];
                    if (assigneeObj != null && assigneeObj.Type != JTokenType.Null)
                    {
                        assignee = assigneeObj["login"]?.ToString() ?? "unknown";
                    }

                    sb.AppendLine($"## Issue #{number}: {title}");
                    sb.AppendLine($"**State:** {state}");
                    sb.AppendLine($"**Labels:** {labelsText}");
                    sb.AppendLine($"**Assignee:** {assignee}");
                    sb.AppendLine($"**URL:** {htmlUrl}");
                    sb.AppendLine();
                }
                
                return sb.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error formatting issues list");
                return $"Error formatting issues list: {ex.Message}\n\nRaw JSON:\n{jsonContent}";
            }
        }

        [McpServerTool, Description("Retrieves a list of issues for the specified repository. Supports filtering by state, labels, assignee, milestone, etc.")]
        public async Task<string> GitHubListIssues([Description("JSON parameters for GitHubListIssues")] string parameters = "{}")
        {
            try
            {
                var result = await ProcessAsync(parameters, new Dictionary<string, string>());
                
                if (!result.WasProcessed)
                {
                    return "Tool was not processed successfully.";
                }
                
                return result.ResultMessage ?? "Tool executed successfully with no output.";
            }
            catch (Exception ex)
            {
                return $"Error executing tool: {ex.Message}";
            }
        }
    }
}
