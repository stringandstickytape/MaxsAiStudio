﻿using AiStudio4.Core.Interfaces;
using AiStudio4.Core.Models;
using AiStudio4.InjectedDependencies;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace AiStudio4.Core.Tools.AzureDevOps
{
    /// <summary>
    /// Implementation of the Azure DevOps Get Wiki Page Content tool
    /// </summary>
    public class AzureDevOpsGetWikiPageContentTool : BaseToolImplementation
    {
        private readonly HttpClient _httpClient;

        public AzureDevOpsGetWikiPageContentTool(ILogger<AzureDevOpsGetWikiPageContentTool> logger, IGeneralSettingsService generalSettingsService, IStatusMessageService statusMessageService)
            : base(logger, generalSettingsService, statusMessageService)
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json")); // For metadata
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain")); // For content
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "AiStudio4-AzureDevOps-Tool");
        }

        /// <summary>
        /// Gets the Azure DevOps Get Wiki Page Content tool definition
        /// </summary>
        public override Tool GetToolDefinition()
        {
            return new Tool
            {
                Guid = ToolGuids.AZURE_DEV_OPS_GET_WIKI_PAGE_CONTENT_TOOL_GUID,
                Name = "AzureDevOpsGetWikiPageContent",
                Description = "Retrieves the content of a specific wiki page from Azure DevOps.",
                Schema = """
{
  "name": "AzureDevOpsGetWikiPageContent",
  "description": "Retrieves the content of a specific wiki page from Azure DevOps. Content is typically Markdown.",
  "input_schema": {
    "properties": {
      "organization": { "title": "Organization", "type": "string", "description": "The Azure DevOps organization name" },
      "project": { "title": "Project", "type": "string", "description": "The Azure DevOps project name" },
      "wiki_id": { "title": "Wiki ID or Name", "type": "string", "description": "The ID or name of the wiki (wikiIdentifier)" },
      "path": { "title": "Page Path", "type": "string", "description": "Path to the specific wiki page (e.g., '/parent/page')." },
      "version": { "title": "Version", "type": "string", "description": "Wiki version (e.g., branch name like 'wikiMaster')" }
    },
    "required": ["organization", "project", "wiki_id", "path"],
    "title": "AzureDevOpsGetWikiPageContentArguments",
    "type": "object"
  }
}
""",
                Categories = new List<string> { "AzureDevOps" },
                OutputFileType = "txt", // Content will be text/markdown
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
                SendStatusUpdate("Starting Azure DevOps Get Wiki Page Content tool execution...");
                var parameters = JsonConvert.DeserializeObject<Dictionary<string, object>>(toolParameters) ?? new Dictionary<string, object>();

                // Extract required parameters
                if (!parameters.TryGetValue("organization", out var organizationObj) || !(organizationObj is string organization) || string.IsNullOrWhiteSpace(organization))
                {
                    return CreateResult(true, true, $"Parameters: organization=<missing>, project=<unknown>, wiki_id=<unknown>, path=<unknown>\n\nError: 'organization' parameter is required.");
                }

                if (!parameters.TryGetValue("project", out var projectObj) || !(projectObj is string project) || string.IsNullOrWhiteSpace(project))
                {
                    return CreateResult(true, true, $"Parameters: organization={organization}, project=<missing>, wiki_id=<unknown>, path=<unknown>\n\nError: 'project' parameter is required.");
                }

                if (!parameters.TryGetValue("wiki_id", out var wikiIdObj) || !(wikiIdObj is string wikiId) || string.IsNullOrWhiteSpace(wikiId))
                {
                    return CreateResult(true, true, $"Parameters: organization={organization}, project={project}, wiki_id=<missing>, path=<unknown>\n\nError: 'wiki_id' parameter is required.");
                }

                if (!parameters.TryGetValue("path", out var pathObj) || !(pathObj is string path) || string.IsNullOrWhiteSpace(path))
                {
                    return CreateResult(true, true, $"Parameters: organization={organization}, project={project}, wiki_id={wikiId}, path=<missing>\n\nError: 'path' parameter is required.");
                }

                // Extract optional parameters
                string version = null;
                if (parameters.TryGetValue("version", out var versionObj) && versionObj is string versionStr && !string.IsNullOrWhiteSpace(versionStr))
                {
                    version = versionStr;
                }
                
                string apiKey = _generalSettingsService.GetDecryptedAzureDevOpsPAT();
                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    return CreateResult(true, true, $"Parameters: organization={organization}, project={project}, wiki_id={wikiId}, path={path}\n\nError: Azure DevOps PAT is not configured. Please set it in File > Settings > Set Azure DevOps PAT.");
                }

                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                    Convert.ToBase64String(Encoding.ASCII.GetBytes(string.Format("{0}:{1}", "", apiKey))));

                return await GetWikiPageContentAsync(organization, project, wikiId, path, version);
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
        
        private async Task<BuiltinToolResult> GetWikiPageContentAsync(string organization, string project, string wikiId, string path, string version)
        {
            try
            {
                SendStatusUpdate($"Fetching content for wiki page '{path}' in {organization}/{project}, Wiki: {wikiId}...");

                var queryParams = new List<string>();
                // Path is mandatory for this API to get a specific page for content
                queryParams.Add($"path={HttpUtility.UrlEncode(path)}");
                queryParams.Add("includeContent=true"); // Always true for this tool
                //recursionLevel=None is implied when fetching a specific page's content by path

                if (!string.IsNullOrWhiteSpace(version))
                {
                    queryParams.Add($"versionDescriptor.version={HttpUtility.UrlEncode(version)}");
                }
                
                // queryParams.Add("api-version=6.0");

                string queryString = queryParams.Count > 0 ? $"?{string.Join("&", queryParams)}" : "";
                // This endpoint returns the page object (JSON) with content inside it.
                string url = $"https://dev.azure.com/{organization}/{project}/_apis/wiki/wikis/{HttpUtility.UrlEncode(wikiId)}/pages{queryString}";

                var response = await _httpClient.GetAsync(url);
                var responseContentString = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    string errorMessage = "Unknown error";
                    try {
                        var errorObj = JObject.Parse(responseContentString);
                        errorMessage = errorObj?["message"]?.ToString() ?? responseContentString;
                    } catch { /* Use raw content if not JSON */ }
                    return CreateResult(true, true, $"Parameters: organization={organization}, project={project}, wiki_id={wikiId}, path={path}\n\nAzure DevOps API Error: {errorMessage} (Status code: {response.StatusCode})");
                }

                // The response for .../pages?includeContent=true is a JSON object representing the page,
                // with a "content" field.
                JObject pageData;
                try
                {
                    pageData = JObject.Parse(responseContentString);
                }
                catch (JsonException ex)
                {
                     _logger.LogError(ex, "Failed to parse page data JSON response.");
                    return CreateResult(true, true, $"Parameters: organization={organization}, project={project}, wiki_id={wikiId}, path={path}\n\nError: Failed to parse API response as JSON. Content: {responseContentString}");
                }

                string pageContent = pageData?["content"]?.ToString();

                if (pageContent == null)
                {
                    // This could happen if the page has no content or the path was wrong but didn't 404 (e.g. a folder path)
                     SendStatusUpdate("Page found, but no content field or content is null.");
                    return CreateResult(true, true, $"Parameters: organization={organization}, project={project}, wiki_id={wikiId}, path={path}, version={version}\n\n# Wiki Page: {path}\n\n[No content found for this page or path may refer to a folder without content retrieval.]");
                }
                
                var formattedOutput = FormatWikiPageContent(path, pageContent);

                SendStatusUpdate("Successfully retrieved wiki page content.");
                return CreateResult(true, true, $"Parameters: organization={organization}, project={project}, wiki_id={wikiId}, path={path}, version={version}\n\n{formattedOutput}");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error fetching wiki page content");
                return CreateResult(true, true, $"Parameters: organization={organization}, project={project}, wiki_id={wikiId}, path={path}\n\nError fetching wiki page content: {ex.Message}");
            }
        }

        private string FormatWikiPageContent(string pagePath, string content)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"# Wiki Page: {pagePath}");
            sb.AppendLine();
            
            // Assuming content is Markdown, wrap in a code block for clarity in text output.
            // Or, if it's intended to be rendered as markdown by the viewer, just append.
            // For a .txt output, a code block is safer.
            sb.AppendLine("```markdown");
            sb.AppendLine(content);
            sb.AppendLine("```");
            
            return sb.ToString();
        }
    }
}
