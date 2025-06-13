







using System.Net.Http;
using System.Net.Http.Headers;



namespace AiStudio4.Core.Tools.AzureDevOps
{
    /// <summary>
    /// Implementation of the Azure DevOps Get Repositories tool
    /// </summary>
    public class AzureDevOpsGetRepositoriesTool : BaseToolImplementation
    {
        private readonly HttpClient _httpClient;

        public AzureDevOpsGetRepositoriesTool(ILogger<AzureDevOpsGetRepositoriesTool> logger, IGeneralSettingsService generalSettingsService, IStatusMessageService statusMessageService) 
            : base(logger, generalSettingsService, statusMessageService)
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "AiStudio4-AzureDevOps-Tool");
        }

        /// <summary>
        /// Gets the Azure DevOps Get Repositories tool definition
        /// </summary>
        public override Tool GetToolDefinition()
        {
            return new Tool
            {
                Guid = ToolGuids.AZURE_DEV_OPS_GET_REPOSITORIES_TOOL_GUID,
                Name = "AzureDevOpsGetRepositories",
                Description = "Retrieves repositories in the specified Azure DevOps project.",
                Schema = """
{
  "name": "AzureDevOpsGetRepositories",
  "description": "Retrieves repositories in the specified Azure DevOps project.",
  "input_schema": {
    "properties": {
      "organization": { "title": "Organization", "type": "string", "description": "The Azure DevOps organization name" },
      "project": { "title": "Project", "type": "string", "description": "The Azure DevOps project name" },
      "include_hidden": { "title": "Include Hidden", "type": "boolean", "description": "Include hidden repositories", "default": false },
      "include_all_urls": { "title": "Include All URLs", "type": "boolean", "description": "Include all remote URLs", "default": false }
    },
    "required": ["organization", "project"],
    "title": "AzureDevOpsGetRepositoriesArguments",
    "type": "object"
  }
}
""",
                Categories = new List<string> { "AzureDevOps" },
                OutputFileType = "txt",
                Filetype = string.Empty,
                LastModified = DateTime.UtcNow,
                ExtraProperties = new Dictionary<string, string> {
                    { "azureDevOpsApiKey", "" }
                }
            };
        }

        public override async Task<BuiltinToolResult> ProcessAsync(string toolParameters, Dictionary<string, string> extraProperties)
        {
            try
            {
                SendStatusUpdate("Starting Azure DevOps Get Repositories tool execution...");
                var parameters = JsonConvert.DeserializeObject<Dictionary<string, object>>(toolParameters) ?? new Dictionary<string, object>();

                // Extract parameters
                if (!parameters.TryGetValue("organization", out var organizationObj) || !(organizationObj is string organization) || string.IsNullOrWhiteSpace(organization))
                {
                    return CreateResult(true, true, $"Parameters: organization=<missing>, project=<unknown>\n\nError: 'organization' parameter is required.");
                }

                if (!parameters.TryGetValue("project", out var projectObj) || !(projectObj is string project) || string.IsNullOrWhiteSpace(project))
                {
                    return CreateResult(true, true, $"Parameters: organization={organization}, project=<missing>\n\nError: 'project' parameter is required.");
                }

                // Extract optional parameters
                bool includeHidden = false;
                if (parameters.TryGetValue("include_hidden", out var includeHiddenObj) && includeHiddenObj is bool includeHiddenBool)
                {
                    includeHidden = includeHiddenBool;
                }

                bool includeAllUrls = false;
                if (parameters.TryGetValue("include_all_urls", out var includeAllUrlsObj) && includeAllUrlsObj is bool includeAllUrlsBool)
                {
                    includeAllUrls = includeAllUrlsBool;
                }

                // Get API key from settings
                string apiKey = _generalSettingsService.GetDecryptedAzureDevOpsPAT();
                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    return CreateResult(true, true, $"Parameters: organization={organization}, project={project}\n\nError: Azure DevOps API Key is not configured. Please set it in File > Settings > Set Azure DevOps API Key.");
                }

                // Set up authentication header
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", 
                    Convert.ToBase64String(Encoding.ASCII.GetBytes(string.Format("{0}:{1}", "", apiKey))));

                // Make the API request
                return await GetRepositoriesAsync(organization, project, includeHidden, includeAllUrls);
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

        private async Task<BuiltinToolResult> GetRepositoriesAsync(string organization, string project, bool includeHidden, bool includeAllUrls)
        {
            try
            {
                SendStatusUpdate($"Fetching repositories for {organization}/{project}...");
                
                // Build query parameters
                var queryParams = new List<string>();
                if (includeHidden)
                {
                    queryParams.Add("includeHidden=true");
                }
                if (includeAllUrls)
                {
                    queryParams.Add("includeLinks=true");
                }
                
                string queryString = queryParams.Count > 0 ? $"?{string.Join("&", queryParams)}" : "";
                string url = $"https://dev.azure.com/{organization}/{project}/_apis/git/repositories{queryString}";
                
                var response = await _httpClient.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorObj = JObject.Parse(content);
                    string errorMessage = errorObj["message"]?.ToString() ?? "Unknown error";
                    return CreateResult(true, true, $"Parameters: organization={organization}, project={project}\n\nAzure DevOps API Error: {errorMessage} (Status code: {response.StatusCode})");
                }
                
                var formattedContent = FormatRepositoriesInfo(content);
                
                SendStatusUpdate("Successfully retrieved repositories information.");
                return CreateResult(true, true, $"Parameters: organization={organization}, project={project}, include_hidden={includeHidden}, include_all_urls={includeAllUrls}\n\n{formattedContent}");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error fetching repositories information");
                return CreateResult(true, true, $"Parameters: organization={organization}, project={project}\n\nError fetching repositories information: {ex.Message}");
            }
        }

        private string FormatRepositoriesInfo(string jsonContent)
        {
            try
            {
                var repoData = JObject.Parse(jsonContent);
                var repositories = repoData["value"] as JArray;
                var sb = new StringBuilder();
                
                sb.AppendLine("# Azure DevOps Repositories");
                sb.AppendLine();
                
                if (repositories == null || repositories.Count == 0)
                {
                    sb.AppendLine("No repositories found in this project.");
                    return sb.ToString();
                }
                
                sb.AppendLine($"Found {repositories.Count} repositories:\n");
                
                foreach (var repo in repositories)
                {
                    sb.AppendLine($"## {repo["name"]}");
                    sb.AppendLine($"**ID:** {repo["id"]}");
                    sb.AppendLine($"**Default Branch:** {repo["defaultBranch"] ?? "Not specified"}");
                    sb.AppendLine($"**URL:** {repo["webUrl"]}");
                    sb.AppendLine($"**Project:** {repo["project"]?["name"]}");
                    sb.AppendLine($"**Size:** {repo["size"]} bytes");
                    sb.AppendLine($"**Is Fork:** {repo["isFork"]}");
                    
                    if (repo["remoteUrl"] != null)
                    {
                        sb.AppendLine($"**Remote URL:** {repo["remoteUrl"]}");
                    }
                    
                    if (repo["sshUrl"] != null)
                    {
                        sb.AppendLine($"**SSH URL:** {repo["sshUrl"]}");
                    }
                    
                    // Add additional URLs if available
                    if (repo["_links"] != null)
                    {
                        sb.AppendLine("\n**Links:**");
                        foreach (var link in repo["_links"].Children<JProperty>())
                        {
                            if (link.Value["href"] != null)
                            {
                                sb.AppendLine($"- {link.Name}: {link.Value["href"]}");
                            }
                        }
                    }
                    
                    sb.AppendLine();
                }
                
                return sb.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error formatting repositories information");
                return $"Error formatting repositories information: {ex.Message}\n\nRaw JSON:\n{jsonContent}";
            }
        }
    }
}
