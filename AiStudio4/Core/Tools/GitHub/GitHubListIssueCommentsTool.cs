// AiStudio4\Core\Tools\GitHub\GitHubListIssueCommentsTool.cs
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
    /// Implementation of the GitHub List Issue Comments API tool
    /// </summary>
    [McpServerToolType]
    public class GitHubListIssueCommentsTool : BaseToolImplementation
    {
        private readonly HttpClient _httpClient;

        public GitHubListIssueCommentsTool(ILogger<GitHubListIssueCommentsTool> logger, IGeneralSettingsService generalSettingsService, IStatusMessageService statusMessageService) 
            : base(logger, generalSettingsService, statusMessageService)
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "AiStudio4-GitHub-Tool");
            _httpClient.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
        }

        /// <summary>
        /// Gets the GitHub List Issue Comments tool definition
        /// </summary>
        public override Tool GetToolDefinition()
        {
            return new Tool
            {
                Guid = ToolGuids.GITHUB_LIST_ISSUE_COMMENTS_TOOL_GUID,
                Name = "GitHubListIssueComments",
                Description = "Retrieves all comments for a specific issue, ordered by creation date.",
                Schema = """
{
  "name": "GitHubListIssueComments",
  "description": "Retrieves all comments for a specific issue, ordered by creation date.",
  "input_schema": {
    "type": "object",
    "properties": {
      "owner": { "type": "string", "description": "Repository owner." },
      "repo": { "type": "string", "description": "Repository name." },
      "issue_number": { "type": "integer", "description": "The number of the issue." },
      "since": { "type": "string", "description": "Only show comments updated at or after this time (ISO 8601 format: YYYY-MM-DDTHH:MM:SSZ). (Optional)" },
      "per_page": { "type": "integer", "description": "Results per page (max 100).", "default": 30 },
      "page": { "type": "integer", "description": "Page number of the results to fetch.", "default": 1 }
    },
    "required": ["owner", "repo", "issue_number"]
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
                SendStatusUpdate("Starting GitHub List Issue Comments tool execution...");
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

                // Extract optional parameters with defaults
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
                return await ListCommentsAsync(owner, repo, issueNumber, since, perPage, page);
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

        private async Task<BuiltinToolResult> ListCommentsAsync(string owner, string repo, int issueNumber, string since, int perPage, int page)
        {
            try
            {
                SendStatusUpdate($"Fetching comments for issue #{issueNumber} from {owner}/{repo}...");
                
                // Build query parameters
                var queryParams = new List<string>();
                if (!string.IsNullOrEmpty(since)) queryParams.Add($"since={HttpUtility.UrlEncode(since)}");
                queryParams.Add($"per_page={perPage}");
                queryParams.Add($"page={page}");

                string url = $"https://api.github.com/repos/{owner}/{repo}/issues/{issueNumber}/comments";
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
                
                var formattedResult = FormatCommentsList(content, owner, repo, issueNumber, page, perPage);
                
                SendStatusUpdate("Successfully retrieved comments list.");
                return CreateResult(true, true, formattedResult);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error fetching comments list");
                return CreateResult(true, true, $"Error fetching comments list: {ex.Message}");
            }
        }

        private string FormatCommentsList(string jsonContent, string owner, string repo, int issueNumber, int page, int perPage)
        {
            try
            {
                var comments = JArray.Parse(jsonContent);
                var sb = new StringBuilder();
                
                sb.AppendLine($"# Comments for Issue #{issueNumber} in {owner}/{repo}");
                sb.AppendLine();
                sb.AppendLine($"**Total comments on this page:** {comments.Count}");
                sb.AppendLine($"**Page:** {page} (showing up to {perPage} results per page)");
                sb.AppendLine();

                if (comments.Count == 0)
                {
                    sb.AppendLine("No comments found for this issue.");
                    return sb.ToString();
                }

                foreach (var comment in comments)
                {
                    string id = comment["id"]?.ToString() ?? "Unknown";
                    string body = comment["body"]?.ToString() ?? "No content";
                    string createdAt = comment["created_at"]?.ToString() ?? "";
                    string updatedAt = comment["updated_at"]?.ToString() ?? "";
                    string htmlUrl = comment["html_url"]?.ToString() ?? "";
                    
                    // Get author
                    string author = "unknown";
                    var userObj = comment["user"];
                    if (userObj != null && userObj.Type != JTokenType.Null)
                    {
                        author = userObj["login"]?.ToString() ?? "unknown";
                    }

                    sb.AppendLine($"## Comment by {author}");
                    sb.AppendLine($"**ID:** {id}");
                    sb.AppendLine($"**Created:** {createdAt}");
                    if (createdAt != updatedAt)
                    {
                        sb.AppendLine($"**Updated:** {updatedAt}");
                    }
                    sb.AppendLine($"**URL:** {htmlUrl}");
                    sb.AppendLine();
                    sb.AppendLine("### Content");
                    sb.AppendLine();
                    if (!string.IsNullOrWhiteSpace(body))
                    {
                        sb.AppendLine(body);
                    }
                    else
                    {
                        sb.AppendLine("*No content*");
                    }
                    sb.AppendLine();
                    sb.AppendLine("---");
                    sb.AppendLine();
                }
                
                return sb.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error formatting comments list");
                return $"Error formatting comments list: {ex.Message}\n\nRaw JSON:\n{jsonContent}";
            }
        }

        [McpServerTool, Description("Retrieves all comments for a specific issue, ordered by creation date.")]
        public async Task<string> GitHubListIssueComments([Description("JSON parameters for GitHubListIssueComments")] string parameters = "{}")
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
