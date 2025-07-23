





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
    /// Implementation of the Azure DevOps Get Pull Request Changes tool
    /// </summary>
    [McpServerToolType]
    public class AzureDevOpsGetPullRequestChangesTool : BaseToolImplementation
    {
        private readonly HttpClient _httpClient;

        public AzureDevOpsGetPullRequestChangesTool(ILogger<AzureDevOpsGetPullRequestChangesTool> logger, IGeneralSettingsService generalSettingsService, IStatusMessageService statusMessageService) 
            : base(logger, generalSettingsService, statusMessageService)
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "AiStudio4-AzureDevOps-Tool");
        }

        /// <summary>
        /// Gets the Azure DevOps Get Pull Request Changes tool definition
        /// </summary>
        public override Tool GetToolDefinition()
        {
            return new Tool
            {
                Guid = ToolGuids.AZURE_DEV_OPS_GET_PULL_REQUEST_CHANGES_TOOL_GUID,
                Name = "AzureDevOpsGetPullRequestChanges",
                Description = "Retrieves the file changes associated with a specific pull request iteration in Azure DevOps.",
                Schema = """
{
  "name": "AzureDevOpsGetPullRequestChanges",
  "description": "Retrieves the file changes associated with a specific pull request iteration in Azure DevOps.",
  "input_schema": {
    "properties": {
      "organization": { "title": "Organization", "type": "string", "description": "The Azure DevOps organization name" },
      "project": { "title": "Project", "type": "string", "description": "The Azure DevOps project name" },
      "repository_id": { "title": "Repository ID", "type": "string", "description": "The repository ID or name" },
      "pull_request_id": { "title": "Pull Request ID", "type": "integer", "description": "The pull request ID" },
      "iteration_id": { "title": "Iteration ID", "type": "integer", "description": "Specific iteration to get changes for" },
      "top": { "title": "Top", "type": "integer", "description": "Number of changes to return", "default": 100 },
      "skip": { "title": "Skip", "type": "integer", "description": "Number of changes to skip", "default": 0 },
      "compare_to": { "title": "Compare To", "type": "integer", "description": "Iteration ID to compare against (optional)" }
    },
    "required": ["organization", "project", "repository_id", "pull_request_id", "iteration_id"],
    "title": "AzureDevOpsGetPullRequestChangesArguments",
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
                SendStatusUpdate("Starting Azure DevOps Get Pull Request Changes tool execution...");
                var parameters = JsonConvert.DeserializeObject<Dictionary<string, object>>(toolParameters) ?? new Dictionary<string, object>();

                // Extract required parameters
                if (!parameters.TryGetValue("organization", out var organizationObj) || !(organizationObj is string organization) || string.IsNullOrWhiteSpace(organization))
                {
                    return CreateResult(true, true, $"Parameters: organization=<missing>, project=<unknown>, repository_id=<unknown>, pull_request_id=<unknown>\n\nError: 'organization' parameter is required.");
                }

                if (!parameters.TryGetValue("project", out var projectObj) || !(projectObj is string project) || string.IsNullOrWhiteSpace(project))
                {
                    return CreateResult(true, true, $"Parameters: organization={organization}, project=<missing>, repository_id=<unknown>, pull_request_id=<unknown>\n\nError: 'project' parameter is required.");
                }

                if (!parameters.TryGetValue("repository_id", out var repoIdObj) || !(repoIdObj is string repositoryId) || string.IsNullOrWhiteSpace(repositoryId))
                {
                    return CreateResult(true, true, $"Parameters: organization={organization}, project={project}, repository_id=<missing>, pull_request_id=<unknown>\n\nError: 'repository_id' parameter is required.");
                }

                if (!parameters.TryGetValue("pull_request_id", out var prIdObj) || !(prIdObj is long || prIdObj is int))
                {
                    return CreateResult(true, true, $"Parameters: organization={organization}, project={project}, repository_id={repositoryId}, pull_request_id=<missing>\n\nError: 'pull_request_id' parameter is required and must be an integer.");
                }

                int pullRequestId = prIdObj is long longPrId ? (int)longPrId : (int)prIdObj;

                // Extract optional parameters
                int? iterationId = null;
                if (parameters.TryGetValue("iteration_id", out var iterIdObj))
                {
                    if (iterIdObj is long longIterId)
                    {
                        iterationId = (int)longIterId;
                    }
                    else if (iterIdObj is int intIterId)
                    {
                        iterationId = intIterId;
                    }
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

                int? compareTo = null;
                if (parameters.TryGetValue("compare_to", out var compareToObj))
                {
                    if (compareToObj is long longCompareTo)
                    {
                        compareTo = (int)longCompareTo;
                    }
                    else if (compareToObj is int intCompareTo)
                    {
                        compareTo = intCompareTo;
                    }
                }

                // Get API key from settings
                string apiKey = _generalSettingsService.GetDecryptedAzureDevOpsPAT();
                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    return CreateResult(true, true, $"Parameters: organization={organization}, project={project}, repository_id={repositoryId}, pull_request_id={pullRequestId}\n\nError: Azure DevOps PAT is not configured. Please set it in File > Settings > Set Azure DevOps PAT.");
                }

                // Set up authentication header
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", 
                    Convert.ToBase64String(Encoding.ASCII.GetBytes(string.Format("{0}:{1}", "", apiKey))));

                // Make the API request
                return await GetPullRequestChangesAsync(organization, project, repositoryId, pullRequestId, iterationId, top, skip, compareTo);
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

        private async Task<BuiltinToolResult> GetPullRequestChangesAsync(string organization, string project, string repositoryId, 
            int pullRequestId, int? iterationId, int top, int skip, int? compareTo)
        {
            try
            {
                SendStatusUpdate($"Fetching pull request changes for PR #{pullRequestId} in {organization}/{project}/{repositoryId}...");
                
                // Build query parameters
                var queryParams = new List<string>();
                
                queryParams.Add($"$top={top}");
                queryParams.Add($"$skip={skip}");
                
                if (compareTo.HasValue)
                {
                    queryParams.Add($"compareTo={compareTo.Value}");
                }
                
                string queryString = queryParams.Count > 0 ? $"?{string.Join("&", queryParams)}" : "";
                
                // Build the URL based on whether an iteration ID is provided
                string url;
                if (iterationId.HasValue)
                {
                    url = $"https://dev.azure.com/{organization}/{project}/_apis/git/repositories/{repositoryId}/pullRequests/{pullRequestId}/iterations/{iterationId.Value}/changes{queryString}";
                }
                else
                {
                    url = $"https://dev.azure.com/{organization}/{project}/_apis/git/repositories/{repositoryId}/pullRequests/{pullRequestId}/changes{queryString}";
                }
                
                var response = await _httpClient.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorObj = JObject.Parse(content);
                    string errorMessage = errorObj["message"]?.ToString() ?? "Unknown error";
                    return CreateResult(true, true, $"Parameters: organization={organization}, project={project}, repository_id={repositoryId}, pull_request_id={pullRequestId}\n\nAzure DevOps API Error: {errorMessage} (Status code: {response.StatusCode})");
                }
                
                var formattedContent = FormatPullRequestChangesInfo(content);
                
                SendStatusUpdate("Successfully retrieved pull request changes information.");
                return CreateResult(true, true, $"Parameters: organization={organization}, project={project}, repository_id={repositoryId}, pull_request_id={pullRequestId}, iteration_id={iterationId}\n\n{formattedContent}");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error fetching pull request changes information");
                return CreateResult(true, true, $"Parameters: organization={organization}, project={project}, repository_id={repositoryId}, pull_request_id={pullRequestId}\n\nError fetching pull request changes information: {ex.Message}");
            }
        }

        private string FormatPullRequestChangesInfo(string jsonContent)
        {
            try
            {
                var changesData = JObject.Parse(jsonContent);
                var changeEntries = changesData["changes"] as JArray;
                var sb = new StringBuilder();
                
                sb.AppendLine("# Azure DevOps Pull Request Changes");
                sb.AppendLine();
                
                if (changeEntries == null || changeEntries.Count == 0)
                {
                    sb.AppendLine("No changes found in this pull request.");
                    return sb.ToString();
                }
                
                sb.AppendLine($"Found {changeEntries.Count} changes:\n");
                
                // Summary of changes by type
                int adds = 0, edits = 0, deletes = 0, renames = 0;
                
                foreach (var change in changeEntries)
                {
                    string changeType = change["changeType"]?.ToString();
                    switch (changeType)
                    {
                        case "add": adds++; break;
                        case "edit": edits++; break;
                        case "delete": deletes++; break;
                        case "rename": renames++; break;
                    }
                }
                
                sb.AppendLine("## Summary");
                sb.AppendLine($"- Added files: {adds}");
                sb.AppendLine($"- Modified files: {edits}");
                sb.AppendLine($"- Deleted files: {deletes}");
                sb.AppendLine($"- Renamed files: {renames}");
                sb.AppendLine();
                
                sb.AppendLine("## Changed Files");
                
                foreach (var change in changeEntries)
                {
                    string changeType = change["changeType"]?.ToString();
                    string item = change["item"]?["path"]?.ToString() ?? "Unknown path";
                    
                    string changeTypeIcon;
                    switch (changeType)
                    {
                        case "add": changeTypeIcon = "➕"; break;
                        case "edit": changeTypeIcon = "✏️"; break;
                        case "delete": changeTypeIcon = "🗑️"; break;
                        case "rename": changeTypeIcon = "📝"; break;
                        default: changeTypeIcon = "❓"; break;
                    }
                    
                    sb.AppendLine($"{changeTypeIcon} **{item}** ({changeType})");
                    
                    // If it's a rename, show the original path
                    if (changeType == "rename" && change["sourceServerItem"] != null)
                    {
                        sb.AppendLine($"   Original path: {change["sourceServerItem"]}");
                    }
                    
                    // Show content change stats if available
                    if (change["churn"] != null)
                    {
                        int adds2 = change["churn"]["adds"] != null ? (int)change["churn"]["adds"] : 0;
                        int edits2 = change["churn"]["edits"] != null ? (int)change["churn"]["edits"] : 0;
                        int deletes2 = change["churn"]["deletes"] != null ? (int)change["churn"]["deletes"] : 0;
                        
                        sb.AppendLine($"   Changes: +{adds2} -{deletes2} ~{edits2}");
                    }
                    
                    sb.AppendLine();
                }
                
                // Add iteration information if available
                if (changesData["iterationContext"] != null)
                {
                    var iteration = changesData["iterationContext"];
                    sb.AppendLine("## Iteration Information");
                    sb.AppendLine($"**Iteration ID:** {iteration["id"]}");
                    sb.AppendLine($"**Iteration Number:** {iteration["iterationNumber"]}");
                    
                    if (iteration["description"] != null)
                    {
                        sb.AppendLine($"**Description:** {iteration["description"]}");
                    }
                    
                    if (iteration["createdDate"] != null)
                    {
                        sb.AppendLine($"**Created Date:** {iteration["createdDate"]}");
                    }
                    
                    if (iteration["author"] != null)
                    {
                        sb.AppendLine($"**Author:** {iteration["author"]["displayName"]}");
                    }
                }
                
                return sb.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error formatting pull request changes information");
                return $"Error formatting pull request changes information: {ex.Message}\n\nRaw JSON:\n{jsonContent}";
            }
        }

        [McpServerTool, Description("Retrieves the file changes associated with a specific pull request iteration in Azure DevOps.")]
        public async Task<string> AzureDevOpsGetPullRequestChanges([Description("JSON parameters for AzureDevOpsGetPullRequestChanges")] string parameters = "{}")
        {
            try
            {
                var result = await ProcessAsync(parameters, new Dictionary<string, string>());
                
                if (!result.WasProcessed)
                {
                    return $"Tool was not processed successfully.";
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
