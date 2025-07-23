




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
    /// Implementation of the Azure DevOps Get Commit Diffs tool
    /// </summary>
    [McpServerToolType]
    public class AzureDevOpsGetCommitDiffsTool : BaseToolImplementation
    {
        private readonly HttpClient _httpClient;

        public AzureDevOpsGetCommitDiffsTool(ILogger<AzureDevOpsGetCommitDiffsTool> logger, IGeneralSettingsService generalSettingsService, IStatusMessageService statusMessageService) 
            : base(logger, generalSettingsService, statusMessageService)
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "AiStudio4-AzureDevOps-Tool");
        }

        /// <summary>
        /// Gets the Azure DevOps Get Commit Diffs tool definition
        /// </summary>
        public override Tool GetToolDefinition()
        {
            return new Tool
            {
                Guid = ToolGuids.AZURE_DEV_OPS_GET_COMMIT_DIFFS_TOOL_GUID,
                Name = "AzureDevOpsGetCommitDiffs",
                Description = "Retrieves the file changes associated with a specific commit in an Azure DevOps repository.",
                Schema = """
{
  "name": "AzureDevOpsGetCommitDiffs",
  "description": "Retrieves the file changes associated with a specific commit in an Azure DevOps repository.",
  "input_schema": {
    "properties": {
      "organization": { "title": "Organization", "type": "string", "description": "The Azure DevOps organization name" },
      "project": { "title": "Project", "type": "string", "description": "The Azure DevOps project name" },
      "repository_id": { "title": "Repository ID", "type": "string", "description": "The repository ID or name" },
      "commit_id": { "title": "Commit ID", "type": "string", "description": "The commit ID to get changes for" },
      "top": { "title": "Top", "type": "integer", "description": "Number of changes to return", "default": 100 },
      "skip": { "title": "Skip", "type": "integer", "description": "Number of changes to skip", "default": 0 },
      "base_version": { "title": "Base Version", "type": "string", "description": "Base version to compare against (commit ID)" }
    },
    "required": ["organization", "project", "repository_id", "commit_id"],
    "title": "AzureDevOpsGetCommitDiffsArguments",
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
                SendStatusUpdate("Starting Azure DevOps Get Commit Diffs tool execution...");
                var parameters = JsonConvert.DeserializeObject<Dictionary<string, object>>(toolParameters) ?? new Dictionary<string, object>();

                // Extract required parameters
                if (!parameters.TryGetValue("organization", out var organizationObj) || !(organizationObj is string organization) || string.IsNullOrWhiteSpace(organization))
                {
                    return CreateResult(true, true, $"Parameters: organization=<missing>, project=<unknown>, repository_id=<unknown>, commit_id=<unknown>\n\nError: 'organization' parameter is required.");
                }

                if (!parameters.TryGetValue("project", out var projectObj) || !(projectObj is string project) || string.IsNullOrWhiteSpace(project))
                {
                    return CreateResult(true, true, $"Parameters: organization={organization}, project=<missing>, repository_id=<unknown>, commit_id=<unknown>\n\nError: 'project' parameter is required.");
                }

                if (!parameters.TryGetValue("repository_id", out var repoIdObj) || !(repoIdObj is string repositoryId) || string.IsNullOrWhiteSpace(repositoryId))
                {
                    return CreateResult(true, true, $"Parameters: organization={organization}, project={project}, repository_id=<missing>, commit_id=<unknown>\n\nError: 'repository_id' parameter is required.");
                }

                if (!parameters.TryGetValue("commit_id", out var commitIdObj) || !(commitIdObj is string commitId) || string.IsNullOrWhiteSpace(commitId))
                {
                    return CreateResult(true, true, $"Parameters: organization={organization}, project={project}, repository_id={repositoryId}, commit_id=<missing>\n\nError: 'commit_id' parameter is required.");
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

                string baseVersion = null;
                if (parameters.TryGetValue("base_version", out var baseVersionObj) && baseVersionObj is string baseVersionStr && !string.IsNullOrWhiteSpace(baseVersionStr))
                {
                    baseVersion = baseVersionStr;
                }

                // Get API key from settings
                string apiKey = _generalSettingsService.GetDecryptedAzureDevOpsPAT();
                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    return CreateResult(true, true, $"Parameters: organization={organization}, project={project}, repository_id={repositoryId}, commit_id={commitId}\n\nError: Azure DevOps PAT is not configured. Please set it in File > Settings > Set Azure DevOps PAT.");
                }

                // Set up authentication header
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", 
                    Convert.ToBase64String(Encoding.ASCII.GetBytes(string.Format("{0}:{1}", "", apiKey))));

                // Make the API request
                return await GetCommitDiffsAsync(organization, project, repositoryId, commitId, top, skip, baseVersion);
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

        private async Task<BuiltinToolResult> GetCommitDiffsAsync(string organization, string project, string repositoryId, 
            string commitId, int top, int skip, string baseVersion)
        {
            try
            {
                SendStatusUpdate($"Fetching commit diffs for {organization}/{project}/{repositoryId} commit {commitId}...");
                
                // Build query parameters
                var queryParams = new List<string>();
                
                queryParams.Add($"$top={top}");
                queryParams.Add($"$skip={skip}");
                
                if (!string.IsNullOrEmpty(baseVersion))
                {
                    queryParams.Add($"baseVersionDescriptor.version={baseVersion}");
                    queryParams.Add("baseVersionDescriptor.versionType=commit");
                }
                
                string queryString = queryParams.Count > 0 ? $"?{string.Join("&", queryParams)}" : "";
                string url = $"https://dev.azure.com/{organization}/{project}/_apis/git/repositories/{repositoryId}/commits/{commitId}/changes{queryString}";
                
                var response = await _httpClient.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorObj = JObject.Parse(content);
                    string errorMessage = errorObj["message"]?.ToString() ?? "Unknown error";
                    return CreateResult(true, true, $"Parameters: organization={organization}, project={project}, repository_id={repositoryId}, commit_id={commitId}\n\nAzure DevOps API Error: {errorMessage} (Status code: {response.StatusCode})");
                }
                
                var formattedContent = FormatCommitDiffsInfo(content);
                
                SendStatusUpdate("Successfully retrieved commit diffs information.");
                return CreateResult(true, true, $"Parameters: organization={organization}, project={project}, repository_id={repositoryId}, commit_id={commitId}\n\n{formattedContent}");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error fetching commit diffs information");
                return CreateResult(true, true, $"Parameters: organization={organization}, project={project}, repository_id={repositoryId}, commit_id={commitId}\n\nError fetching commit diffs information: {ex.Message}");
            }
        }

        private string FormatCommitDiffsInfo(string jsonContent)
        {
            try
            {
                var diffsData = JObject.Parse(jsonContent);
                var changes = diffsData["changes"] as JArray;
                var sb = new StringBuilder();
                
                sb.AppendLine("# Azure DevOps Commit Diffs");
                sb.AppendLine();
                
                if (changes == null || changes.Count == 0)
                {
                    sb.AppendLine("No changes found for this commit.");
                    return sb.ToString();
                }
                
                sb.AppendLine($"Found {changes.Count} changes in this commit:\n");
                
                int fileCount = 0;
                foreach (var change in changes)
                {
                    fileCount++;
                    string changeType = change["changeType"]?.ToString() ?? "Unknown";
                    string item = change["item"]?["path"]?.ToString() ?? "Unknown path";
                    
                    sb.AppendLine($"## {fileCount}. {item}");
                    sb.AppendLine($"**Change Type:** {changeType}");
                    
                    if (change["originalPath"] != null && !string.IsNullOrEmpty(change["originalPath"].ToString()) && change["originalPath"].ToString() != item)
                    {
                        sb.AppendLine($"**Original Path:** {change["originalPath"]}");
                    }
                    
                    // Add content metadata if available
                    if (change["item"]?["contentMetadata"] != null)
                    {
                        var metadata = change["item"]["contentMetadata"];
                        if (metadata["fileName"] != null)
                        {
                            sb.AppendLine($"**File Name:** {metadata["fileName"]}");
                        }
                        if (metadata["extension"] != null)
                        {
                            sb.AppendLine($"**Extension:** {metadata["extension"]}");
                        }
                    }
                    
                    // Add diff information if available
                    if (change["newContent"] != null || change["originalContent"] != null)
                    {
                        sb.AppendLine("\n**Content Changes:**");
                        
                        if (change["newContent"] != null)
                        {
                            sb.AppendLine($"- New Content Size: {change["newContent"]["contentLength"]} bytes");
                            sb.AppendLine($"- New Content Hash: {change["newContent"]["hash"]}");
                        }
                        
                        if (change["originalContent"] != null)
                        {
                            sb.AppendLine($"- Original Content Size: {change["originalContent"]["contentLength"]} bytes");
                            sb.AppendLine($"- Original Content Hash: {change["originalContent"]["hash"]}");
                        }
                    }
                    
                    // Add line change counts if available
                    if (change["changeType"]?.ToString() == "edit" && change["newContent"] != null && change["originalContent"] != null)
                    {
                        // Azure DevOps doesn't directly provide line change counts in this API
                        // We're showing content size differences as an approximation
                        if (change["newContent"]["contentLength"] != null && change["originalContent"]["contentLength"] != null)
                        {
                            long newSize = (long)change["newContent"]["contentLength"];
                            long originalSize = (long)change["originalContent"]["contentLength"];
                            long diff = newSize - originalSize;
                            
                            if (diff > 0)
                            {
                                sb.AppendLine($"- Added approximately {diff} bytes");
                            }
                            else if (diff < 0)
                            {
                                sb.AppendLine($"- Removed approximately {Math.Abs(diff)} bytes");
                            }
                        }
                    }
                    
                    sb.AppendLine();
                }
                
                return sb.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error formatting commit diffs information");
                return $"Error formatting commit diffs information: {ex.Message}\n\nRaw JSON:\n{jsonContent}";
            }
        }

        [McpServerTool, Description("Retrieves the file changes associated with a specific commit in an Azure DevOps repository.")]
        public async Task<string> AzureDevOpsGetCommitDiffs([Description("JSON parameters for AzureDevOpsGetCommitDiffs")] string parameters = "{}")
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
