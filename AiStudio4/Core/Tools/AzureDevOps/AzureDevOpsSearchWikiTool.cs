// C:/Users/maxhe/source/repos/MaxsAiStudio/AiStudio4/Core/Tools/AzureDevOps/AzureDevOpsSearchWikiTool.cs

using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Web;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace AiStudio4.Core.Tools.AzureDevOps
{
    /// <summary>
    /// Implementation of the Azure DevOps Search Wiki tool
    /// </summary>
    [McpServerToolType]
    public class AzureDevOpsSearchWikiTool : BaseToolImplementation
    {
        private readonly HttpClient _httpClient;

        public AzureDevOpsSearchWikiTool(ILogger<AzureDevOpsSearchWikiTool> logger, IGeneralSettingsService generalSettingsService, IStatusMessageService statusMessageService)
            : base(logger, generalSettingsService, statusMessageService)
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "AiStudio4-AzureDevOps-Tool");
        }

        /// <summary>
        /// Gets the Azure DevOps Search Wiki tool definition
        /// </summary>
        public override Tool GetToolDefinition()
        {
            return new Tool
            {
                Guid = ToolGuids.AZURE_DEV_OPS_SEARCH_WIKI_TOOL_GUID,
                Name = "AzureDevOpsSearchWiki",
                Description = "Searches for content across Azure DevOps wiki pages using the Azure DevOps Search API.",
                Schema = """
{
  "name": "AzureDevOpsSearchWiki",
  "description": "Searches for content across Azure DevOps wiki pages using the Azure DevOps Search API.",
  "input_schema": {
    "properties": {
      "organization": { "title": "Organization", "type": "string", "description": "The Azure DevOps organization name" },
      "project": { "title": "Project", "type": "string", "description": "The Azure DevOps project name (optional - if not specified, searches across all accessible projects)" },
      "search_text": { "title": "Search Text", "type": "string", "description": "The text to search for in wiki pages" },
      "top": { "title": "Top Results", "type": "integer", "description": "Number of results to return (default: 25, max: 200)", "default": 25, "minimum": 1, "maximum": 200 },
      "skip": { "title": "Skip Results", "type": "integer", "description": "Number of results to skip for pagination (default: 0)", "default": 0, "minimum": 0 },
      "include_facets": { "title": "Include Facets", "type": "boolean", "description": "Include facet information (breakdown by project/wiki) in results", "default": false },
      "project_filters": { "title": "Project Filters", "type": "array", "items": { "type": "string" }, "description": "Array of project names to filter results to specific projects" },
      "sort_by": { "title": "Sort By", "type": "string", "description": "Field to sort results by", "enum": ["relevance", "filename", "lastmodifieddate"], "default": "relevance" },
      "sort_order": { "title": "Sort Order", "type": "string", "description": "Sort order for results", "enum": ["asc", "desc"], "default": "desc" }
    },
    "required": ["organization", "search_text"],
    "title": "AzureDevOpsSearchWikiArguments",
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
                SendStatusUpdate("Starting Azure DevOps Search Wiki tool execution...");
                var parameters = JsonConvert.DeserializeObject<Dictionary<string, object>>(toolParameters) ?? new Dictionary<string, object>();

                // Extract required parameters
                if (!parameters.TryGetValue("organization", out var organizationObj) || !(organizationObj is string organization) || string.IsNullOrWhiteSpace(organization))
                {
                    return CreateResult(true, true, $"Parameters: organization=<missing>, search_text=<unknown>\n\nError: 'organization' parameter is required.");
                }

                if (!parameters.TryGetValue("search_text", out var searchTextObj) || !(searchTextObj is string searchText) || string.IsNullOrWhiteSpace(searchText))
                {
                    return CreateResult(true, true, $"Parameters: organization={organization}, search_text=<missing>\n\nError: 'search_text' parameter is required.");
                }

                // Extract optional parameters
                string project = null;
                if (parameters.TryGetValue("project", out var projectObj) && projectObj is string projectStr && !string.IsNullOrWhiteSpace(projectStr))
                {
                    project = projectStr;
                }

                int top = 25; // Default from schema
                if (parameters.TryGetValue("top", out var topObj) && topObj != null)
                {
                    if (topObj is long topLong) top = (int)Math.Min(Math.Max(topLong, 1), 200);
                    else if (int.TryParse(topObj.ToString(), out var topInt)) top = Math.Min(Math.Max(topInt, 1), 200);
                }

                int skip = 0; // Default from schema
                if (parameters.TryGetValue("skip", out var skipObj) && skipObj != null)
                {
                    if (skipObj is long skipLong) skip = (int)Math.Max(skipLong, 0);
                    else if (int.TryParse(skipObj.ToString(), out var skipInt)) skip = Math.Max(skipInt, 0);
                }

                bool includeFacets = false; // Default from schema
                if (parameters.TryGetValue("include_facets", out var includeFacetsObj) && includeFacetsObj is bool includeFacetsBool)
                {
                    includeFacets = includeFacetsBool;
                }

                List<string> projectFilters = new List<string>();
                if (parameters.TryGetValue("project_filters", out var projectFiltersObj) && projectFiltersObj is JArray projectFiltersArray)
                {
                    projectFilters = projectFiltersArray.ToObject<List<string>>() ?? new List<string>();
                }

                string sortBy = "relevance"; // Default from schema
                if (parameters.TryGetValue("sort_by", out var sortByObj) && sortByObj is string sortByStr && !string.IsNullOrWhiteSpace(sortByStr))
                {
                    sortBy = sortByStr;
                }

                string sortOrder = "desc"; // Default from schema
                if (parameters.TryGetValue("sort_order", out var sortOrderObj) && sortOrderObj is string sortOrderStr && !string.IsNullOrWhiteSpace(sortOrderStr))
                {
                    sortOrder = sortOrderStr;
                }

                // Get API key from settings
                string apiKey = _generalSettingsService.GetDecryptedAzureDevOpsPAT();
                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    return CreateResult(true, true, $"Parameters: organization={organization}, search_text={searchText}\n\nError: Azure DevOps PAT is not configured. Please set it in File > Settings > Set Azure DevOps PAT.");
                }

                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                    Convert.ToBase64String(Encoding.ASCII.GetBytes(string.Format("{0}:{1}", "", apiKey))));

                return await SearchWikiAsync(organization, project, searchText, top, skip, includeFacets, projectFilters, sortBy, sortOrder);
            }
            catch (JsonException jsonEx)
            {
                _logger.LogError(jsonEx, "Error deserializing Azure DevOps Search Wiki tool parameters");
                return CreateResult(true, true, $"Parameters: <invalid JSON>\n\nError processing Azure DevOps Search Wiki tool parameters: Invalid JSON format. {jsonEx.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Azure DevOps Search Wiki tool");
                return CreateResult(true, true, $"Parameters: <unknown>\n\nError processing Azure DevOps Search Wiki tool: {ex.Message}");
            }
        }

        private async Task<BuiltinToolResult> SearchWikiAsync(string organization, string project, string searchText, int top, int skip, bool includeFacets, List<string> projectFilters, string sortBy, string sortOrder)
        {
            try
            {
                SendStatusUpdate($"Searching wiki content for '{searchText}' in {organization}...");

                // Build the search request body
                var searchRequest = new Dictionary<string, object>
                {
                    ["searchText"] = searchText,
                    ["$top"] = top,
                    ["$skip"] = skip,
                    ["includeFacets"] = includeFacets,
                    ["filters"] = BuildFilters(projectFilters),
                    ["$orderBy"] = BuildOrderBy(sortBy, sortOrder),
                    ["includeSnippet"] = true,
                    ["$searchFilters"] = new { }
                };

                var requestJson = JsonConvert.SerializeObject(searchRequest, Formatting.None, new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                });

                // Construct the URL - note the different base URL for search API
                string url;
                if (!string.IsNullOrWhiteSpace(project))
                {
                    url = $"https://almsearch.dev.azure.com/{HttpUtility.UrlEncode(organization)}/{HttpUtility.UrlEncode(project)}/_apis/search/wikisearchresults?api-version=6.0";
                }
                else
                {
                    url = $"https://almsearch.dev.azure.com/{HttpUtility.UrlEncode(organization)}/_apis/search/wikisearchresults?api-version=6.0";
                }

                var content = new StringContent(requestJson, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(url, content);
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
                    return CreateResult(true, true, $"Parameters: organization={organization}, project={project}, search_text={searchText}\n\nAzure DevOps Search API Error: {errorMessage} (Status code: {response.StatusCode})");
                }

                var searchResults = JObject.Parse(responseContent);

                var formattedContent = FormatSearchResults(searchResults, searchText, organization, project);

                SendStatusUpdate("Successfully retrieved wiki search results.");
                return CreateResult(true, true, $"Parameters: organization={organization}, project={project}, search_text={searchText}, top={top}, skip={skip}\n\n{formattedContent}");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error searching wiki content");
                return CreateResult(true, true, $"Parameters: organization={organization}, project={project}, search_text={searchText}\n\nError searching wiki content: {ex.Message}");
            }
            catch (JsonException jsonEx)
            {
                _logger.LogError(jsonEx, "Error parsing wiki search response");
                return CreateResult(true, true, $"Parameters: organization={organization}, project={project}, search_text={searchText}\n\nError parsing wiki search response: {jsonEx.Message}");
            }
        }

        private object BuildFilters(List<string> projectFilters)
        {
            if (projectFilters == null || projectFilters.Count == 0)
                return null;

            return new
            {
                Project = projectFilters
            };
        }

        private object BuildOrderBy(string sortBy, string sortOrder)
        {
            if (sortBy == "relevance")
                return null; // Default relevance sorting

            var field = sortBy switch
            {
                "filename" => "fileName",
                "lastmodifieddate" => "lastModifiedDate",
                _ => "fileName"
            };

            return new[]
            {
                new
                {
                    field = field,
                    sortOrder = sortOrder.ToUpperInvariant()
                }
            };
        }

        private string FormatSearchResults(JObject searchResults, string searchText, string organization, string project)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# Azure DevOps Wiki Search Results");
            sb.AppendLine();

            var count = searchResults["count"]?.ToObject<int>() ?? 0;
            var infoCode = searchResults["infoCode"]?.ToObject<int>() ?? 0;

            sb.AppendLine($"**Search Query:** {searchText}");
            sb.AppendLine($"**Organization:** {organization}");
            if (!string.IsNullOrWhiteSpace(project))
            {
                sb.AppendLine($"**Project:** {project}");
            }
            sb.AppendLine($"**Total Results:** {count}");

            if (infoCode != 0)
            {
                var infoMessage = GetInfoCodeMessage(infoCode);
                sb.AppendLine($"**Info:** {infoMessage}");
            }

            sb.AppendLine();

            var results = searchResults["results"] as JArray;

            if (results == null || results.Count == 0)
            {
                sb.AppendLine("No wiki pages found matching the search criteria.");

                return sb.ToString();
            }

            sb.AppendLine("## Search Results");
            sb.AppendLine();

            for (int i = 0; i < results.Count; i++)
            {
                var result = results[i];
                FormatSingleResult(result, sb, i + 1);
            }

            // Add facets if available
            var facets = searchResults["facets"];
            if (facets != null && facets.HasValues)
            {
                sb.AppendLine();
                sb.AppendLine("## Result Breakdown");
                FormatFacets(facets, sb);
            }

            return sb.ToString();
        }

        private void FormatSingleResult(JToken result, StringBuilder sb, int index)
        {
            var fileName = result["fileName"]?.ToString() ?? "Unknown File";
            var path = result["path"]?.ToString() ?? "";
            var projectInfo = result["project"];
            var wikiInfo = result["wiki"];
            var hits = result["hits"] as JArray;

            sb.AppendLine($"### {index}. {fileName}");
            sb.AppendLine();

            if (!string.IsNullOrWhiteSpace(path))
            {
                sb.AppendLine($"**Path:** {path}");
            }

            if (projectInfo != null)
            {
                var projectName = projectInfo["name"]?.ToString();
                if (!string.IsNullOrWhiteSpace(projectName))
                {
                    sb.AppendLine($"**Project:** {projectName}");
                }
            }

            if (wikiInfo != null)
            {
                var wikiName = wikiInfo["name"]?.ToString();
                var wikiVersion = wikiInfo["version"]?.ToString();
                if (!string.IsNullOrWhiteSpace(wikiName))
                {
                    sb.AppendLine($"**Wiki:** {wikiName}");
                }
                if (!string.IsNullOrWhiteSpace(wikiVersion))
                {
                    sb.AppendLine($"**Version:** {wikiVersion}");
                }
            }

            // Format highlighted snippets
            if (hits != null && hits.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("**Matched Content:**");
                foreach (var hit in hits)
                {
                    var fieldName = hit["fieldReferenceName"]?.ToString();
                    var highlights = hit["highlights"] as JArray;

                    if (highlights != null && highlights.Count > 0)
                    {
                        var fieldDisplayName = GetFieldDisplayName(fieldName);
                        sb.AppendLine($"- **{fieldDisplayName}:**");
                        foreach (var highlight in highlights)
                        {
                            sb.AppendLine($"  - {highlight}");
                        }
                    }
                }
            }

            sb.AppendLine();
        }

        private void FormatFacets(JToken facets, StringBuilder sb)
        {
            var projectFacets = facets["Project"] as JArray;
            if (projectFacets != null && projectFacets.Count > 0)
            {
                sb.AppendLine("**By Project:**");
                foreach (var facet in projectFacets)
                {
                    var name = facet["name"]?.ToString();
                    var count = facet["resultCount"]?.ToObject<int>() ?? 0;
                    sb.AppendLine($"- {name}: {count} results");
                }
            }
        }

        private string GetFieldDisplayName(string fieldName)
        {
            return fieldName switch
            {
                "content" => "Content",
                "fileNames" => "File Name",
                "title" => "Title",
                _ => fieldName ?? "Unknown Field"
            };
        }

        private string GetInfoCodeMessage(int infoCode)
        {
            return infoCode switch
            {
                0 => "Ok",
                1 => "Account is being reindexed",
                2 => "Account indexing has not started",
                3 => "Invalid Request",
                4 => "Prefix wildcard query not supported",
                5 => "MultiWords with code facet not supported",
                6 => "Account is being onboarded",
                7 => "Account is being onboarded or reindexed",
                8 => "Top value trimmed to max result allowed",
                9 => "Branches are being indexed",
                10 => "Faceting not enabled",
                11 => "Work items not accessible",
                19 => "Phrase queries with code type filters not supported",
                20 => "Wildcard queries with code type filters not supported",
                _ => $"Info code: {infoCode}"
            };
        }

        [McpServerTool, Description("Searches for content across Azure DevOps wiki pages using the Azure DevOps Search API.")]
        public async Task<string> AzureDevOpsSearchWiki([Description("JSON parameters for AzureDevOpsSearchWiki")] string parameters = "{}")
        {
            return await ExecuteWithExtraProperties(parameters);
        }
    }
}