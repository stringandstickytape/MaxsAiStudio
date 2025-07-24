







using System.Net.Http;
using System.Net.Http.Headers;


using System.Web;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace AiStudio4.Core.Tools.AzureDevOps
{
    /// <summary>
    /// Implementation of the Azure DevOps Get Wiki Pages tool
    /// </summary>
    [McpServerToolType]
    public class AzureDevOpsGetWikiPagesTool : BaseToolImplementation
    {
        private readonly HttpClient _httpClient;

        public AzureDevOpsGetWikiPagesTool(ILogger<AzureDevOpsGetWikiPagesTool> logger, IGeneralSettingsService generalSettingsService, IStatusMessageService statusMessageService)
            : base(logger, generalSettingsService, statusMessageService)
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "AiStudio4-AzureDevOps-Tool");
        }

        /// <summary>
        /// Gets the Azure DevOps Get Wiki Pages tool definition
        /// </summary>
        public override Tool GetToolDefinition()
        {
            return new Tool
            {
                Guid = ToolGuids.AZURE_DEV_OPS_GET_WIKI_PAGES_TOOL_GUID,
                Name = "AzureDevOpsGetWikiPages",
                Description = "Retrieves a list of wiki pages from an Azure DevOps wiki, potentially with their hierarchy and content.",
                Schema = """
{
  "name": "AzureDevOpsGetWikiPages",
  "description": "Retrieves a list of wiki pages from an Azure DevOps wiki, potentially with their hierarchy and content.",
  "input_schema": {
    "properties": {
      "organization": { "title": "Organization", "type": "string", "description": "The Azure DevOps organization name" },
      "project": { "title": "Project", "type": "string", "description": "The Azure DevOps project name" },
      "wiki_id": { "title": "Wiki ID or Name", "type": "string", "description": "The ID or name of the wiki (wikiIdentifier)" },
      "path": { "title": "Path", "type": "string", "description": "Path to a specific wiki page or directory (e.g., '/' for root, '/parent/page'). If not specified, root is assumed.", "default": "" },
      "recursion_level": { "title": "Recursion Level", "type": "string", "description": "How deep to retrieve pages (none, oneLevel, full). 'none' gets the specified page, 'oneLevel' gets its direct children, 'full' gets all descendants.", "enum": ["none", "oneLevel", "full"], "default": "none" },
      "version": { "title": "Version", "type": "string", "description": "Wiki version (e.g., branch name like 'wikiMaster')" },
      "include_content": { "title": "Include Content", "type": "boolean", "description": "Whether to include page content in the response", "default": false }
    },
    "required": ["organization", "project", "wiki_id"],
    "title": "AzureDevOpsGetWikiPagesArguments",
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
                SendStatusUpdate("Starting Azure DevOps Get Wiki Pages tool execution...");
                var parameters = JsonConvert.DeserializeObject<Dictionary<string, object>>(toolParameters) ?? new Dictionary<string, object>();

                // Extract required parameters
                if (!parameters.TryGetValue("organization", out var organizationObj) || !(organizationObj is string organization) || string.IsNullOrWhiteSpace(organization))
                {
                    return CreateResult(true, true, $"Parameters: organization=<missing>, project=<unknown>, wiki_id=<unknown>\n\nError: 'organization' parameter is required.");
                }

                if (!parameters.TryGetValue("project", out var projectObj) || !(projectObj is string project) || string.IsNullOrWhiteSpace(project))
                {
                    return CreateResult(true, true, $"Parameters: organization={organization}, project=<missing>, wiki_id=<unknown>\n\nError: 'project' parameter is required.");
                }

                if (!parameters.TryGetValue("wiki_id", out var wikiIdObj) || !(wikiIdObj is string wikiId) || string.IsNullOrWhiteSpace(wikiId))
                {
                    return CreateResult(true, true, $"Parameters: organization={organization}, project={project}, wiki_id=<missing>\n\nError: 'wiki_id' parameter is required.");
                }

                // Extract optional parameters
                string path = null;
                if (parameters.TryGetValue("path", out var pathObj) && pathObj is string pathStr && !string.IsNullOrWhiteSpace(pathStr))
                {
                    path = pathStr;
                }

                string recursionLevel = "none"; // Default from schema
                if (parameters.TryGetValue("recursion_level", out var recursionLevelObj) && recursionLevelObj is string recursionLevelStr && !string.IsNullOrWhiteSpace(recursionLevelStr))
                {
                    recursionLevel = recursionLevelStr;
                }

                string version = null;
                if (parameters.TryGetValue("version", out var versionObj) && versionObj is string versionStr && !string.IsNullOrWhiteSpace(versionStr))
                {
                    version = versionStr;
                }

                bool includeContent = false; // Default from schema
                if (parameters.TryGetValue("include_content", out var includeContentObj) && includeContentObj is bool includeContentBool)
                {
                    includeContent = includeContentBool;
                }

                // Get API key from settings
                string apiKey = _generalSettingsService.GetDecryptedAzureDevOpsPAT();
                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    return CreateResult(true, true, $"Parameters: organization={organization}, project={project}, wiki_id={wikiId}\n\nError: Azure DevOps PAT is not configured. Please set it in File > Settings > Set Azure DevOps PAT.");
                }

                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                    Convert.ToBase64String(Encoding.ASCII.GetBytes(string.Format("{0}:{1}", "", apiKey))));

                return await GetWikiPagesAsync(organization, project, wikiId, path, recursionLevel, version, includeContent);
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

        private string MapRecursionLevelToApiValue(string level)
        {
            return level?.ToLowerInvariant() switch
            {
                "none" => "None",
                "onelevel" => "OneLevel",
                "full" => "Full",
                _ => "None", // Default or for null/empty
            };
        }

        private async Task<BuiltinToolResult> GetWikiPagesAsync(string organization, string project, string wikiId, string path, string recursionLevel, string version, bool includeContent)
        {
            try
            {
                SendStatusUpdate($"Fetching wiki pages for {organization}/{project}, Wiki: {wikiId}...");

                var queryParams = new List<string>();
                if (!string.IsNullOrWhiteSpace(path))
                {
                    queryParams.Add($"path={HttpUtility.UrlEncode(path)}");
                }
                
                queryParams.Add($"recursionLevel={MapRecursionLevelToApiValue(recursionLevel)}");

                if (!string.IsNullOrWhiteSpace(version))
                {
                    queryParams.Add($"versionDescriptor.version={HttpUtility.UrlEncode(version)}");
                    // Assuming branch version type by default if not specified otherwise by a more complex schema
                    // queryParams.Add("versionDescriptor.versionType=branch"); 
                }

                if (includeContent)
                {
                    queryParams.Add("includeContent=true");
                }
                
                // The API version is often good practice, but other tools here don't specify it.
                // queryParams.Add("api-version=6.0"); 

                string queryString = queryParams.Count > 0 ? $"?{string.Join("&", queryParams)}" : "";
                string url = $"https://dev.azure.com/{organization}/{project}/_apis/wiki/wikis/{HttpUtility.UrlEncode(wikiId)}/pages{queryString}";

                var response = await _httpClient.GetAsync(url);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    string errorMessage = "Unknown error";
                    try
                    {
                        var errorObj = JObject.Parse(responseContent);
                        errorMessage = errorObj?["message"]?.ToString() ?? responseContent;
                    }
                    catch { /* Parsing errorObj failed, use raw content */ }
                    return CreateResult(true, true, $"Parameters: organization={organization}, project={project}, wiki_id={wikiId}, path={path}, recursion_level={recursionLevel}\n\nAzure DevOps API Error: {errorMessage} (Status code: {response.StatusCode})");
                }

                var pageData = JObject.Parse(responseContent);
                var formattedContent = FormatWikiPagesInfo(pageData, includeContent);

                SendStatusUpdate("Successfully retrieved wiki pages information.");
                return CreateResult(true, true, $"Parameters: organization={organization}, project={project}, wiki_id={wikiId}, path={path}, recursion_level={recursionLevel}, include_content={includeContent}, version={version}\n\n{formattedContent}");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error fetching wiki pages");
                return CreateResult(true, true, $"Parameters: organization={organization}, project={project}, wiki_id={wikiId}\n\nError fetching wiki pages: {ex.Message}");
            }
            catch (JsonException jsonEx)
            {
                _logger.LogError(jsonEx, "Error parsing wiki pages response");
                return CreateResult(true, true, $"Parameters: organization={organization}, project={project}, wiki_id={wikiId}\n\nError parsing wiki pages response: {jsonEx.Message}");
            }
        }

        private string FormatWikiPagesInfo(JObject rootPageData, bool wasContentRequested)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# Azure DevOps Wiki Pages");
            sb.AppendLine();

            if (rootPageData == null || !rootPageData.HasValues)
            {
                sb.AppendLine("No page data found at the specified path or for the wiki.");
                return sb.ToString();
            }
            
            FormatSinglePageInfo(rootPageData, sb, 0, wasContentRequested);

            return sb.ToString();
        }

        private void FormatSinglePageInfo(JToken page, StringBuilder sb, int level, bool wasContentRequested)
        {
            if (page == null) return;

            string pagePath = page["path"]?.ToString() ?? "Unknown Path";
            string pageUrl = page["url"]?.ToString();
            string gitPath = page["gitItemPath"]?.ToString();
            string order = page["order"]?.ToString();
            bool isParentPage = page["isParentPage"]?.ToObject<bool>() ?? false;
            bool hasContent = !string.IsNullOrWhiteSpace(page["content"]?.ToString());

            sb.Append(new string(' ', level * 2)); // Indentation
            sb.Append("- ");
            sb.AppendLine($"**{pagePath}** {(isParentPage ? "(Folder)" : "")}");

            if (!string.IsNullOrWhiteSpace(order))
            {
                sb.Append(new string(' ', level * 2 + 2));
                sb.AppendLine($"  Order: {order}");
            }
            if (!string.IsNullOrWhiteSpace(pageUrl))
            {
                sb.Append(new string(' ', level * 2 + 2));
                sb.AppendLine($"  URL: {pageUrl}");
            }
            if (!string.IsNullOrWhiteSpace(gitPath))
            {
                sb.Append(new string(' ', level * 2 + 2));
                sb.AppendLine($"  Git Path: {gitPath}");
            }

            if (wasContentRequested)
            {
                sb.Append(new string(' ', level * 2 + 2));
                if (hasContent)
                {
                    sb.AppendLine("  Content: (Included)");
                }
                else
                {
                    sb.AppendLine("  Content: (Not included or empty)");
                }
            }
            sb.AppendLine();


            if (page["subPages"] is JArray subPages && subPages.Count > 0)
            {
                foreach (var subPage in subPages)
                {
                    FormatSinglePageInfo(subPage, sb, level + 1, wasContentRequested);
                }
            }
        }

        [McpServerTool, Description("Retrieves a list of wiki pages from an Azure DevOps wiki, potentially with their hierarchy and content.")]
        public async Task<string> AzureDevOpsGetWikiPages([Description("JSON parameters for AzureDevOpsGetWikiPages")] string parameters = "{}")
        {
            return await ExecuteWithExtraProperties(parameters);
        }
    }
}
