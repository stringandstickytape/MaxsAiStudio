





using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using System.ComponentModel;
namespace AiStudio4.Core.Tools.AzureDevOps
{
    /// <summary>
    /// Implementation of the Azure DevOps Get Commits tool
    /// </summary>
    [McpServerToolType]
    public class AzureDevOpsGetCommitsTool : BaseToolImplementation
    {
        private readonly HttpClient _httpClient;

        public AzureDevOpsGetCommitsTool(ILogger<AzureDevOpsGetCommitsTool> logger, IGeneralSettingsService generalSettingsService, IStatusMessageService statusMessageService) 
            : base(logger, generalSettingsService, statusMessageService)
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "AiStudio4-AzureDevOps-Tool");
        }

        /// <summary>
        /// Gets the Azure DevOps Get Commits tool definition
        /// </summary>
        public override Tool GetToolDefinition()
        {
            return new Tool
            {
                Guid = ToolGuids.AZURE_DEV_OPS_GET_COMMITS_TOOL_GUID,
                Name = "AzureDevOpsGetCommits",
                Description = "Retrieves commits matching specified criteria from an Azure DevOps repository.",
                Schema = """
{
  "name": "AzureDevOpsGetCommits",
  "description": "Retrieves commits matching specified criteria from an Azure DevOps repository.",
  "input_schema": {
    "properties": {
      "organization": { "title": "Organization", "type": "string", "description": "The Azure DevOps organization name" },
      "project": { "title": "Project", "type": "string", "description": "The Azure DevOps project name" },
      "repository_id": { "title": "Repository ID", "type": "string", "description": "The repository ID or name" },
      "from_date": { "title": "From Date", "type": "string", "description": "Filter commits from this date (ISO 8601 format)" },
      "to_date": { "title": "To Date", "type": "string", "description": "Filter commits until this date (ISO 8601 format)" },
      "author": { "title": "Author", "type": "string", "description": "Filter by author email or name" },
      "item_path": { "title": "Item Path", "type": "string", "description": "Path to filter commits that touch this file or folder" },
      "top": { "title": "Top", "type": "integer", "description": "Number of commits to return", "default": 100 },
      "skip": { "title": "Skip", "type": "integer", "description": "Number of commits to skip", "default": 0 }
    },
    "required": ["organization", "project", "repository_id"],
    "title": "AzureDevOpsGetCommitsArguments",
    "type": "object"
  }
}
""",
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
                SendStatusUpdate("Starting Azure DevOps Get Commits tool execution...");
                var parameters = JsonConvert.DeserializeObject<Dictionary<string, object>>(toolParameters) ?? new Dictionary<string, object>();

                // Extract required parameters
                if (!parameters.TryGetValue("organization", out var organizationObj) || !(organizationObj is string organization) || string.IsNullOrWhiteSpace(organization))
                {
                    return CreateResult(true, true, $"Parameters: organization=<missing>, project=<unknown>, repository_id=<unknown>\n\nError: 'organization' parameter is required.");
                }

                if (!parameters.TryGetValue("project", out var projectObj) || !(projectObj is string project) || string.IsNullOrWhiteSpace(project))
                {
                    return CreateResult(true, true, $"Parameters: organization={organization}, project=<missing>, repository_id=<unknown>\n\nError: 'project' parameter is required.");
                }

                if (!parameters.TryGetValue("repository_id", out var repoIdObj) || !(repoIdObj is string repositoryId) || string.IsNullOrWhiteSpace(repositoryId))
                {
                    return CreateResult(true, true, $"Parameters: organization={organization}, project={project}, repository_id=<missing>\n\nError: 'repository_id' parameter is required.");
                }

                // Extract optional parameters
                string fromDate = null;
                if (parameters.TryGetValue("from_date", out var fromDateObj) && fromDateObj is string fromDateStr && !string.IsNullOrWhiteSpace(fromDateStr))
                {
                    fromDate = fromDateStr;
                }

                string toDate = null;
                if (parameters.TryGetValue("to_date", out var toDateObj) && toDateObj is string toDateStr && !string.IsNullOrWhiteSpace(toDateStr))
                {
                    toDate = toDateStr;
                }

                string author = null;
                if (parameters.TryGetValue("author", out var authorObj) && authorObj is string authorStr && !string.IsNullOrWhiteSpace(authorStr))
                {
                    author = authorStr;
                }

                string itemPath = null;
                if (parameters.TryGetValue("item_path", out var itemPathObj) && itemPathObj is string itemPathStr && !string.IsNullOrWhiteSpace(itemPathStr))
                {
                    itemPath = itemPathStr;
                }

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
                }

                // Get API key from settings
                string apiKey = _generalSettingsService.GetDecryptedAzureDevOpsPAT();
                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    return CreateResult(true, true, $"Parameters: organization={organization}, project={project}, repository_id={repositoryId}\n\nError: Azure DevOps PAT is not configured. Please set it in File > Settings > Set Azure DevOps PAT.");
                }

                // Set up authentication header
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", 
                    Convert.ToBase64String(Encoding.ASCII.GetBytes(string.Format("{0}:{1}", "", apiKey))));

                // Make the API request
                return await GetCommitsAsync(organization, project, repositoryId, fromDate, toDate, author, itemPath, top, skip);
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

        private async Task<BuiltinToolResult> GetCommitsAsync(string organization, string project, string repositoryId, 
            string fromDate, string toDate, string author, string itemPath, int top, int skip)
        {
            try
            {
                SendStatusUpdate($"Fetching commits for {organization}/{project}/{repositoryId}...");
                
                // Build query parameters
                var queryParams = new List<string>();
                
                if (!string.IsNullOrEmpty(fromDate))
                {
                    queryParams.Add($"searchCriteria.fromDate={Uri.EscapeDataString(fromDate)}");
                }
                
                if (!string.IsNullOrEmpty(toDate))
                {
                    queryParams.Add($"searchCriteria.toDate={Uri.EscapeDataString(toDate)}");
                }
                
                if (!string.IsNullOrEmpty(author))
                {
                    queryParams.Add($"searchCriteria.author={Uri.EscapeDataString(author)}");
                }
                
                if (!string.IsNullOrEmpty(itemPath))
                {
                    queryParams.Add($"searchCriteria.itemPath={Uri.EscapeDataString(itemPath)}");
                }
                
                queryParams.Add($"$top={top}");
                queryParams.Add($"$skip={skip}");
                
                string queryString = queryParams.Count > 0 ? $"?{string.Join("&", queryParams)}" : "";
                string url = $"https://dev.azure.com/{organization}/{project}/_apis/git/repositories/{repositoryId}/commits{queryString}";
                
                var response = await _httpClient.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorObj = JObject.Parse(content);
                    string errorMessage = errorObj["message"]?.ToString() ?? "Unknown error";
                    return CreateResult(true, true, $"Parameters: organization={organization}, project={project}, repository_id={repositoryId}\n\nAzure DevOps API Error: {errorMessage} (Status code: {response.StatusCode})");
                }
                
                var formattedContent = FormatCommitsInfo(content);
                
                SendStatusUpdate("Successfully retrieved commits information.");
                return CreateResult(true, true, $"Parameters: organization={organization}, project={project}, repository_id={repositoryId}\n\n{formattedContent}");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error fetching commits information");
                return CreateResult(true, true, $"Parameters: organization={organization}, project={project}, repository_id={repositoryId}\n\nError fetching commits information: {ex.Message}");
            }
        }

        private string FormatCommitsInfo(string jsonContent)
        {
            try
            {
                var commitsData = JObject.Parse(jsonContent);
                var commits = commitsData["value"] as JArray;
                var sb = new StringBuilder();
                
                sb.AppendLine("# Azure DevOps Commits");
                sb.AppendLine();
                
                if (commits == null || commits.Count == 0)
                {
                    sb.AppendLine("No commits found matching the criteria.");
                    return sb.ToString();
                }
                
                sb.AppendLine($"Found {commits.Count} commits:\n");
                
                foreach (var commit in commits)
                {
                    string commitId = commit["commitId"]?.ToString();
                    string shortCommitId = commitId?.Substring(0, Math.Min(8, commitId.Length)) ?? "Unknown";
                    
                    sb.AppendLine($"## Commit {shortCommitId}");
                    sb.AppendLine($"**Full Commit ID:** {commitId}");
                    sb.AppendLine($"**Author:** {commit["author"]?["name"]} <{commit["author"]?["email"]}>");
                    sb.AppendLine($"**Date:** {commit["author"]?["date"]}");
                    sb.AppendLine($"**Committer:** {commit["committer"]?["name"]} <{commit["committer"]?["email"]}>");
                    sb.AppendLine($"**Committer Date:** {commit["committer"]?["date"]}");
                    
                    // Format commit message
                    string comment = commit["comment"]?.ToString() ?? "";
                    if (!string.IsNullOrWhiteSpace(comment))
                    {
                        // Extract first line as title
                        string[] commentLines = comment.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                        string title = commentLines.Length > 0 ? commentLines[0] : "";
                        
                        sb.AppendLine($"**Message:** {title}");
                        
                        // If there are more lines, add them as full message
                        if (commentLines.Length > 1)
                        {
                            sb.AppendLine("\n**Full Message:**");
                            sb.AppendLine("```");
                            sb.AppendLine(comment);
                            sb.AppendLine("```");
                        }
                    }
                    
                    // Add URL if available
                    if (commit["url"] != null)
                    {
                        sb.AppendLine($"**URL:** {commit["url"]}");
                    }
                    
                    // Add changed items if available
                    if (commit["changeCounts"] != null)
                    {
                        int adds = commit["changeCounts"]["Add"] != null ? (int)commit["changeCounts"]["Add"] : 0;
                        int edits = commit["changeCounts"]["Edit"] != null ? (int)commit["changeCounts"]["Edit"] : 0;
                        int deletes = commit["changeCounts"]["Delete"] != null ? (int)commit["changeCounts"]["Delete"] : 0;
                        
                        sb.AppendLine($"**Changes:** +{adds} ~{edits} -{deletes}");
                    }
                    
                    // Add remote URL if available
                    if (commit["remoteUrl"] != null)
                    {
                        sb.AppendLine($"**Remote URL:** {commit["remoteUrl"]}");
                    }
                    
                    sb.AppendLine();
                }
                
                return sb.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error formatting commits information");
                return $"Error formatting commits information: {ex.Message}\n\nRaw JSON:\n{jsonContent}";
            }
        }

        [McpServerTool, Description("Retrieves commits matching specified criteria from an Azure DevOps repository.")]
        public async Task<string> AzureDevOpsGetCommits([Description("JSON parameters for AzureDevOpsGetCommits")] string parameters = "{}")
        {
            return await ExecuteWithExtraProperties(parameters);
        }
    }
}
