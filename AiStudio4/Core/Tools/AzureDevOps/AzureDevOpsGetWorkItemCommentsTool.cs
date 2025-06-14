







using System.Net.Http;
using System.Net.Http.Headers;



namespace AiStudio4.Core.Tools.AzureDevOps
{
    /// <summary>
    /// Implementation of the Azure DevOps Get Work Item Comments tool
    /// </summary>
    public class AzureDevOpsGetWorkItemCommentsTool : BaseToolImplementation
    {
        private readonly HttpClient _httpClient;

        public AzureDevOpsGetWorkItemCommentsTool(ILogger<AzureDevOpsGetWorkItemCommentsTool> logger, IGeneralSettingsService generalSettingsService, IStatusMessageService statusMessageService) 
            : base(logger, generalSettingsService, statusMessageService)
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "AiStudio4-AzureDevOps-Tool");
        }

        /// <summary>
        /// Gets the Azure DevOps Get Work Item Comments tool definition
        /// </summary>
        public override Tool GetToolDefinition()
        {
            return new Tool
            {
                Guid = ToolGuids.AZURE_DEV_OPS_GET_WORK_ITEM_COMMENTS_TOOL_GUID,
                Name = "AzureDevOpsGetWorkItemComments",
                Description = "Retrieves comments associated with a specific work item in Azure DevOps.",
                Schema = """
{
  "name": "AzureDevOpsGetWorkItemComments",
  "description": "Retrieves comments associated with a specific work item in Azure DevOps.",
  "input_schema": {
    "properties": {
      "organization": { "title": "Organization", "type": "string", "description": "The Azure DevOps organization name" },
      "project": { "title": "Project", "type": "string", "description": "The Azure DevOps project name" },
      "id": { "title": "Work Item ID", "type": "integer", "description": "The work item ID" },
      "top": { "title": "Top", "type": "integer", "description": "Number of comments to return", "default": 100 },
      "skip": { "title": "Skip", "type": "integer", "description": "Number of comments to skip", "default": 0 }
    },
    "required": ["organization", "project", "id"],
    "title": "AzureDevOpsGetWorkItemCommentsArguments",
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
                SendStatusUpdate("Starting Azure DevOps Get Work Item Comments tool execution...");
                var parameters = JsonConvert.DeserializeObject<Dictionary<string, object>>(toolParameters) ?? new Dictionary<string, object>();

                // Extract required parameters
                if (!parameters.TryGetValue("organization", out var organizationObj) || !(organizationObj is string organization) || string.IsNullOrWhiteSpace(organization))
                {
                    return CreateResult(true, true, $"Parameters: organization=<missing>, project=<unknown>, id=<unknown>\n\nError: 'organization' parameter is required.");
                }

                if (!parameters.TryGetValue("project", out var projectObj) || !(projectObj is string project) || string.IsNullOrWhiteSpace(project))
                {
                    return CreateResult(true, true, $"Parameters: organization={organization}, project=<missing>, id=<unknown>\n\nError: 'project' parameter is required.");
                }

                if (!parameters.TryGetValue("id", out var idObj) || !(idObj is long || idObj is int))
                {
                    return CreateResult(true, true, $"Parameters: organization={organization}, project={project}, id=<missing>\n\nError: 'id' parameter is required and must be an integer.");
                }

                int workItemId;
                if (idObj is long idLong)
                {
                    workItemId = (int)idLong;
                }
                else
                {
                    workItemId = (int)idObj;
                }

                // Extract optional parameters
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
                    return CreateResult(true, true, $"Parameters: organization={organization}, project={project}, id={workItemId}\n\nError: Azure DevOps PAT is not configured. Please set it in File > Settings > Set Azure DevOps PAT.");
                }

                // Set up authentication header
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", 
                    Convert.ToBase64String(Encoding.ASCII.GetBytes(string.Format("{0}:{1}", "", apiKey))));

                // Make the API request
                return await GetWorkItemCommentsAsync(organization, project, workItemId, top, skip);
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

        private async Task<BuiltinToolResult> GetWorkItemCommentsAsync(string organization, string project, int workItemId, int top, int skip)
        {
            try
            {
                SendStatusUpdate($"Fetching comments for work item {workItemId} in {organization}/{project}...");
                
                // Build query parameters
                var queryParams = new List<string>();
                queryParams.Add($"$top={top}");
                queryParams.Add($"$skip={skip}");
                
                string queryString = queryParams.Count > 0 ? $"?{string.Join("&", queryParams)}" : "";
                string url = $"https://dev.azure.com/{organization}/{project}/_apis/wit/workItems/{workItemId}/comments{queryString}";
                
                var response = await _httpClient.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorObj = JObject.Parse(content);
                    string errorMessage = errorObj["message"]?.ToString() ?? "Unknown error";
                    return CreateResult(true, true, $"Parameters: organization={organization}, project={project}, id={workItemId}\n\nAzure DevOps API Error: {errorMessage} (Status code: {response.StatusCode})");
                }
                
                var formattedContent = FormatWorkItemCommentsInfo(content, workItemId);
                
                SendStatusUpdate("Successfully retrieved work item comments.");
                return CreateResult(true, true, $"Parameters: organization={organization}, project={project}, id={workItemId}, top={top}, skip={skip}\n\n{formattedContent}");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error fetching work item comments");
                return CreateResult(true, true, $"Parameters: organization={organization}, project={project}, id={workItemId}\n\nError fetching work item comments: {ex.Message}");
            }
        }

        private string FormatWorkItemCommentsInfo(string jsonContent, int workItemId)
        {
            try
            {
                var commentsData = JObject.Parse(jsonContent);
                var comments = commentsData["comments"] as JArray;
                var sb = new StringBuilder();
                
                sb.AppendLine($"# Comments for Work Item #{workItemId}");
                sb.AppendLine();
                
                if (comments == null || comments.Count == 0)
                {
                    sb.AppendLine("No comments found for this work item.");
                    return sb.ToString();
                }
                
                sb.AppendLine($"Found {comments.Count} comments:\n");
                
                foreach (var comment in comments)
                {
                    string commentId = comment["id"]?.ToString() ?? "Unknown ID";
                    string text = comment["text"]?.ToString() ?? "";
                    string createdBy = comment["createdBy"]?["displayName"]?.ToString() ?? "Unknown";
                    string createdDate = comment["createdDate"]?.ToString() ?? "Unknown date";
                    string modifiedDate = comment["modifiedDate"]?.ToString() ?? createdDate;
                    bool isDeleted = comment["isDeleted"] != null && (bool)comment["isDeleted"];
                    
                    sb.AppendLine($"## Comment {commentId}");
                    sb.AppendLine($"**Author:** {createdBy}");
                    sb.AppendLine($"**Created:** {createdDate}");
                    
                    if (createdDate != modifiedDate)
                    {
                        sb.AppendLine($"**Modified:** {modifiedDate}");
                    }
                    
                    if (isDeleted)
                    {
                        sb.AppendLine("**Status:** Deleted");
                    }
                    
                    sb.AppendLine("\n**Content:**");
                    sb.AppendLine(text);
                    sb.AppendLine();
                }
                
                // Add pagination information if available
                if (commentsData["count"] != null && commentsData["totalCount"] != null)
                {
                    int count = (int)commentsData["count"];
                    int totalCount = (int)commentsData["totalCount"];
                    
                    if (totalCount > count)
                    {
                        sb.AppendLine($"Showing {count} of {totalCount} total comments. Use 'top' and 'skip' parameters to paginate.");
                    }
                }
                
                return sb.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error formatting work item comments information");
                return $"Error formatting work item comments information: {ex.Message}\n\nRaw JSON:\n{jsonContent}";
            }
        }
    }
}
