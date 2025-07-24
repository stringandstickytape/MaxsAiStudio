





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
    /// Implementation of the Azure DevOps Get Pull Request Iterations tool
    /// </summary>
    [McpServerToolType]
    public class AzureDevOpsGetPullRequestIterationsTool : BaseToolImplementation
    {
        private readonly HttpClient _httpClient;

        public AzureDevOpsGetPullRequestIterationsTool(ILogger<AzureDevOpsGetPullRequestIterationsTool> logger, IGeneralSettingsService generalSettingsService, IStatusMessageService statusMessageService) 
            : base(logger, generalSettingsService, statusMessageService)
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "AiStudio4-AzureDevOps-Tool");
        }

        /// <summary>
        /// Gets the Azure DevOps Get Pull Request Iterations tool definition
        /// </summary>
        public override Tool GetToolDefinition()
        {
            return new Tool
            {
                Guid = ToolGuids.AZURE_DEV_OPS_GET_PULL_REQUEST_ITERATIONS_TOOL_GUID,
                Name = "AzureDevOpsGetPullRequestIterations",
                Description = "Retrieves the iterations (versions) of a specific pull request in Azure DevOps.",
                Schema = """
{
  "name": "AzureDevOpsGetPullRequestIterations",
  "description": "Retrieves the iterations (versions) of a specific pull request in Azure DevOps.",
  "input_schema": {
    "properties": {
      "organization": { "title": "Organization", "type": "string", "description": "The Azure DevOps organization name" },
      "project": { "title": "Project", "type": "string", "description": "The Azure DevOps project name" },
      "repository_id": { "title": "Repository ID", "type": "string", "description": "The repository ID or name" },
      "pull_request_id": { "title": "Pull Request ID", "type": "integer", "description": "The pull request ID" }
    },
    "required": ["organization", "project", "repository_id", "pull_request_id"],
    "title": "AzureDevOpsGetPullRequestIterationsArguments",
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
                SendStatusUpdate("Starting Azure DevOps Get Pull Request Iterations tool execution...");
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

                // Extract pull_request_id and convert to integer
                int pullRequestId = 0;
                if (!parameters.TryGetValue("pull_request_id", out var prIdObj))
                {
                    return CreateResult(true, true, $"Parameters: organization={organization}, project={project}, repository_id={repositoryId}, pull_request_id=<missing>\n\nError: 'pull_request_id' parameter is required.");
                }

                if (prIdObj is long prIdLong)
                {
                    pullRequestId = (int)prIdLong;
                }
                else if (prIdObj is int prIdInt)
                {
                    pullRequestId = prIdInt;
                }
                else if (prIdObj is string prIdStr && int.TryParse(prIdStr, out int parsedId))
                {
                    pullRequestId = parsedId;
                }
                else
                {
                    return CreateResult(true, true, $"Parameters: organization={organization}, project={project}, repository_id={repositoryId}, pull_request_id={prIdObj}\n\nError: 'pull_request_id' must be an integer.");
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
                return await GetPullRequestIterationsAsync(organization, project, repositoryId, pullRequestId);
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

        private async Task<BuiltinToolResult> GetPullRequestIterationsAsync(string organization, string project, string repositoryId, int pullRequestId)
        {
            try
            {
                SendStatusUpdate($"Fetching iterations for pull request {pullRequestId} in {organization}/{project}/{repositoryId}...");
                
                string url = $"https://dev.azure.com/{organization}/{project}/_apis/git/repositories/{repositoryId}/pullRequests/{pullRequestId}/iterations";
                
                var response = await _httpClient.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorObj = JObject.Parse(content);
                    string errorMessage = errorObj["message"]?.ToString() ?? "Unknown error";
                    return CreateResult(true, true, $"Parameters: organization={organization}, project={project}, repository_id={repositoryId}, pull_request_id={pullRequestId}\n\nAzure DevOps API Error: {errorMessage} (Status code: {response.StatusCode})");
                }
                
                var formattedContent = FormatPullRequestIterationsInfo(content);
                
                SendStatusUpdate("Successfully retrieved pull request iterations information.");
                return CreateResult(true, true, $"Parameters: organization={organization}, project={project}, repository_id={repositoryId}, pull_request_id={pullRequestId}\n\n{formattedContent}");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error fetching pull request iterations information");
                return CreateResult(true, true, $"Parameters: organization={organization}, project={project}, repository_id={repositoryId}, pull_request_id={pullRequestId}\n\nError fetching pull request iterations information: {ex.Message}");
            }
        }

        private string FormatPullRequestIterationsInfo(string jsonContent)
        {
            try
            {
                var iterationsData = JObject.Parse(jsonContent);
                var iterations = iterationsData["value"] as JArray;
                var sb = new StringBuilder();
                
                sb.AppendLine("# Azure DevOps Pull Request Iterations");
                sb.AppendLine();
                
                if (iterations == null || iterations.Count == 0)
                {
                    sb.AppendLine("No iterations found for this pull request.");
                    return sb.ToString();
                }
                
                sb.AppendLine($"Found {iterations.Count} iterations:\n");
                
                foreach (var iteration in iterations)
                {
                    sb.AppendLine($"## Iteration {iteration["id"]}");
                    
                    // Common properties
                    sb.AppendLine($"**Description:** {iteration["description"] ?? "No description"}");
                    sb.AppendLine($"**Created by:** {iteration["author"]?["displayName"] ?? "Unknown"}");
                    sb.AppendLine($"**Created on:** {iteration["createdDate"]}");
                    sb.AppendLine($"**Updated on:** {iteration["updatedDate"]}");
                    
                    // Source and target commits
                    if (iteration["sourceRefCommit"] != null)
                    {
                        sb.AppendLine($"**Source Commit:** {iteration["sourceRefCommit"]["commitId"]?.ToString().Substring(0, 8)}");
                    }
                    
                    if (iteration["targetRefCommit"] != null)
                    {
                        sb.AppendLine($"**Target Commit:** {iteration["targetRefCommit"]["commitId"]?.ToString().Substring(0, 8)}");
                    }
                    
                    // Change counts
                    if (iteration["changeList"] != null)
                    {
                        int additions = 0;
                        int deletions = 0;
                        int modifications = 0;
                        
                        foreach (var change in iteration["changeList"])
                        {
                            string changeType = change["changeType"]?.ToString();
                            if (changeType == "Add")
                                additions++;
                            else if (changeType == "Delete")
                                deletions++;
                            else if (changeType == "Edit")
                                modifications++;
                        }
                        
                        sb.AppendLine($"**Changes:** {additions} additions, {deletions} deletions, {modifications} modifications");
                    }
                    
                    // Iteration context
                    if (iteration["iterationContext"] != null)
                    {
                        sb.AppendLine("\n**Iteration Context:**");
                        sb.AppendLine($"- First Comparision: {iteration["iterationContext"]["firstComparingIteration"]}");
                        sb.AppendLine($"- Second Comparision: {iteration["iterationContext"]["secondComparingIteration"]}");
                    }
                    
                    // Has changes flag
                    if (iteration["hasMoreChanges"] != null)
                    {
                        sb.AppendLine($"**Has More Changes:** {iteration["hasMoreChanges"]}");
                    }
                    
                    sb.AppendLine();
                }
                
                return sb.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error formatting pull request iterations information");
                return $"Error formatting pull request iterations information: {ex.Message}\n\nRaw JSON:\n{jsonContent}";
            }
        }

        [McpServerTool, Description("Retrieves the iterations (versions) of a specific pull request in Azure DevOps.")]
        public async Task<string> AzureDevOpsGetPullRequestIterations([Description("JSON parameters for AzureDevOpsGetPullRequestIterations")] string parameters = "{}")
        {
            return await ExecuteWithExtraProperties(parameters);
        }
    }
}
