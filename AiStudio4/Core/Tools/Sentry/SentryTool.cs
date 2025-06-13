







using System.Net.Http;
using System.Net.Http.Headers;



namespace AiStudio4.Core.Tools.Sentry
{
    /// <summary>
    /// Implementation of the Sentry API tool
    /// </summary>
    public class SentryTool : BaseToolImplementation
    {
        private readonly HttpClient _httpClient;
        private Dictionary<string, string> _extraProperties { get; set; } = new Dictionary<string, string>();

        public SentryTool(ILogger<SentryTool> logger, IGeneralSettingsService generalSettingsService, IStatusMessageService statusMessageService) 
            : base(logger, generalSettingsService, statusMessageService)
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        /// <summary>
        /// Gets the Sentry tool definition
        /// </summary>
        public override Tool GetToolDefinition()
        {
            return new Tool
            {
                Guid = ToolGuids.SENTRY_TOOL_GUID,
                Name = "Sentry",
                Description = "Retrieves information from Sentry API including organization, project, and issue details.",
                Schema = """
{
  "name": "Sentry",
  "description": "Retrieves information from Sentry API including organization, project, and issue details.",
  "input_schema": {
    "properties": {
      "operation": { "title": "Operation", "type": "string", "description": "The operation to perform (organization, project, issues)", "enum": ["organization", "project", "issues"] },
      "project_slug": { "title": "Project Slug", "type": "string", "description": "The Sentry project slug (required for project and issues operations)" },
      "query": { "title": "Query", "type": "string", "description": "Optional query string for filtering issues (e.g., is:unresolved)" }
    },
    "required": ["operation"],
    "title": "SentryArguments",
    "type": "object"
  }
}
""",
                Categories = new List<string> { "APITools" },
                OutputFileType = "txt",
                Filetype = string.Empty,
                LastModified = DateTime.UtcNow,
                ExtraProperties = new Dictionary<string, string> {
                    { "orgSlug", "" },
                    { "apiToken", "" },
                    { "sentryApiBaseUrl", "https://sentry.io/api/0" }
                }
            };
        }

        public override async Task<BuiltinToolResult> ProcessAsync(string toolParameters, Dictionary<string, string> extraProperties)
        {
            _extraProperties = extraProperties;

            try
            {
                SendStatusUpdate("Starting Sentry API tool execution...");
                var parameters = JsonConvert.DeserializeObject<Dictionary<string, object>>(toolParameters) ?? new Dictionary<string, object>();

                // Extract parameters
                if (!parameters.TryGetValue("operation", out var operationObj) || !(operationObj is string operation))
                {
                    return CreateResult(true, true, "Error: 'operation' parameter is required.");
                }

                // Get API configuration from extra properties
                if (!_extraProperties.TryGetValue("orgSlug", out var orgSlug) || string.IsNullOrWhiteSpace(orgSlug))
                {
                    return CreateResult(true, true, "Error: 'orgSlug' must be configured in the tool properties.");
                }

                if (!_extraProperties.TryGetValue("apiToken", out var apiToken) || string.IsNullOrWhiteSpace(apiToken))
                {
                    return CreateResult(true, true, "Error: 'apiToken' must be configured in the tool properties.");
                }

                string sentryApiBaseUrl = "https://sentry.io/api/0";
                if (_extraProperties.TryGetValue("sentryApiBaseUrl", out var configuredUrl) && !string.IsNullOrWhiteSpace(configuredUrl))
                {
                    sentryApiBaseUrl = configuredUrl;
                }

                // Set up authentication header
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiToken);

                // Process based on operation
                switch (operation.ToLowerInvariant())
                {
                    case "organization":
                        return await GetOrganizationDetailsAsync(sentryApiBaseUrl, orgSlug);
                    
                    case "project":
                        if (!parameters.TryGetValue("project_slug", out var projectSlugObj) || !(projectSlugObj is string projectSlug) || string.IsNullOrWhiteSpace(projectSlug))
                        {
                            return CreateResult(true, true, "Error: 'project_slug' parameter is required for project operation.");
                        }
                        return await GetProjectDetailsAsync(sentryApiBaseUrl, orgSlug, projectSlug);
                    
                    case "issues":
                        if (!parameters.TryGetValue("project_slug", out var issuesProjectSlugObj) || !(issuesProjectSlugObj is string issuesProjectSlug) || string.IsNullOrWhiteSpace(issuesProjectSlug))
                        {
                            return CreateResult(true, true, "Error: 'project_slug' parameter is required for issues operation.");
                        }
                        
                        string query = "is:unresolved"; // Default query
                        if (parameters.TryGetValue("query", out var queryObj) && queryObj is string customQuery && !string.IsNullOrWhiteSpace(customQuery))
                        {
                            query = customQuery;
                        }
                        
                        return await GetIssuesListAsync(sentryApiBaseUrl, orgSlug, issuesProjectSlug, query);
                    
                    default:
                        return CreateResult(true, true, $"Error: Unknown operation '{operation}'. Supported operations are: organization, project, issues.");
                }
            }
            catch (JsonException jsonEx)
            {
                _logger.LogError(jsonEx, "Error deserializing Sentry tool parameters");
                return CreateResult(true, true, $"Error processing Sentry tool parameters: Invalid JSON format. {jsonEx.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Sentry tool");
                return CreateResult(true, true, $"Error processing Sentry tool: {ex.Message}");
            }
        }

        private async Task<BuiltinToolResult> GetOrganizationDetailsAsync(string baseUrl, string orgSlug)
        {
            try
            {
                SendStatusUpdate("Fetching organization details...");
                string url = $"{baseUrl}/organizations/{orgSlug}/";
                
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                
                var content = await response.Content.ReadAsStringAsync();
                var formattedContent = FormatOrganizationDetails(content);
                
                SendStatusUpdate("Successfully retrieved organization details.");
                return CreateResult(true, true, formattedContent);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error fetching organization details");
                return CreateResult(true, true, $"Error fetching organization details: {ex.Message}");
            }
        }

        private async Task<BuiltinToolResult> GetProjectDetailsAsync(string baseUrl, string orgSlug, string projectSlug)
        {
            try
            {
                SendStatusUpdate($"Fetching project details for {projectSlug}...");
                string url = $"{baseUrl}/projects/{orgSlug}/{projectSlug}/";
                
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                
                var content = await response.Content.ReadAsStringAsync();
                var formattedContent = FormatProjectDetails(content);
                
                SendStatusUpdate("Successfully retrieved project details.");
                return CreateResult(true, true, formattedContent);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error fetching project details");
                return CreateResult(true, true, $"Error fetching project details: {ex.Message}");
            }
        }

        private async Task<BuiltinToolResult> GetIssuesListAsync(string baseUrl, string orgSlug, string projectSlug, string query)
        {
            try
            {
                SendStatusUpdate($"Fetching issues list for {projectSlug} with query: {query}...");
                string url = $"{baseUrl}/projects/{orgSlug}/{projectSlug}/issues/?query={Uri.EscapeDataString(query)}";
                
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                
                var content = await response.Content.ReadAsStringAsync();
                var formattedContent = FormatIssuesList(content);
                
                SendStatusUpdate("Successfully retrieved issues list.");
                return CreateResult(true, true, formattedContent);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error fetching issues list");
                return CreateResult(true, true, $"Error fetching issues list: {ex.Message}");
            }
        }

        private string FormatOrganizationDetails(string jsonContent)
        {
            try
            {
                var org = JObject.Parse(jsonContent);
                var sb = new StringBuilder();
                
                sb.AppendLine("# Organization Details");
                sb.AppendLine();
                sb.AppendLine($"**Name:** {org["name"]}");
                sb.AppendLine($"**Slug:** {org["slug"]}");
                sb.AppendLine($"**Status:** {org["status"]?["name"]}");
                sb.AppendLine($"**Date Created:** {org["dateCreated"]}");
                sb.AppendLine();
                
                sb.AppendLine("## Teams");
                if (org["teams"] is JArray teams && teams.Count > 0)
                {
                    foreach (var team in teams)
                    {
                        sb.AppendLine($"- {team["name"]} ({team["slug"]}) - Members: {team["memberCount"]}");
                    }
                }
                else
                {
                    sb.AppendLine("No teams found.");
                }
                
                sb.AppendLine();
                sb.AppendLine("## Projects");
                if (org["projects"] is JArray projects && projects.Count > 0)
                {
                    foreach (var project in projects)
                    {
                        sb.AppendLine($"- {project["name"]} ({project["slug"]}) - Platform: {project["platform"]}");
                    }
                }
                else
                {
                    sb.AppendLine("No projects found.");
                }
                
                return sb.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error formatting organization details");
                return $"Error formatting organization details: {ex.Message}\n\nRaw JSON:\n{jsonContent}";
            }
        }

        private string FormatProjectDetails(string jsonContent)
        {
            try
            {
                var project = JObject.Parse(jsonContent);
                var sb = new StringBuilder();
                
                sb.AppendLine("# Project Details");
                sb.AppendLine();
                sb.AppendLine($"**Name:** {project["name"]}");
                sb.AppendLine($"**Slug:** {project["slug"]}");
                sb.AppendLine($"**Platform:** {project["platform"]}");
                sb.AppendLine($"**Status:** {project["status"]}");
                sb.AppendLine($"**Date Created:** {project["dateCreated"]}");
                sb.AppendLine($"**First Event:** {project["firstEvent"]}");
                
                if (project["latestRelease"] != null)
                {
                    sb.AppendLine($"**Latest Release:** {project["latestRelease"]["version"]}");
                }
                
                sb.AppendLine();
                sb.AppendLine("## Features");
                if (project["features"] is JArray features && features.Count > 0)
                {
                    foreach (var feature in features)
                    {
                        sb.AppendLine($"- {feature}");
                    }
                }
                else
                {
                    sb.AppendLine("No features found.");
                }
                
                return sb.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error formatting project details");
                return $"Error formatting project details: {ex.Message}\n\nRaw JSON:\n{jsonContent}";
            }
        }

        private string FormatIssuesList(string jsonContent)
        {
            try
            {
                var issues = JArray.Parse(jsonContent);
                var sb = new StringBuilder();
                
                sb.AppendLine("# Issues List");
                sb.AppendLine();
                
                if (issues.Count > 0)
                {
                    sb.AppendLine($"Found {issues.Count} issues:\n");
                    
                    foreach (var issue in issues)
                    {
                        sb.AppendLine($"## {issue["shortId"]} - {issue["title"]}");
                        sb.AppendLine($"**Status:** {issue["status"]} ({issue["substatus"]})");
                        sb.AppendLine($"**Level:** {issue["level"]}");
                        sb.AppendLine($"**First Seen:** {issue["firstSeen"]}");
                        sb.AppendLine($"**Last Seen:** {issue["lastSeen"]}");
                        sb.AppendLine($"**Events:** {issue["count"]}");
                        sb.AppendLine($"**Users Affected:** {issue["userCount"]}");
                        sb.AppendLine($"**Priority:** {issue["priority"]}");
                        sb.AppendLine($"**Link:** {issue["permalink"]}");
                        sb.AppendLine();
                    }
                }
                else
                {
                    sb.AppendLine("No issues found matching the query.");
                }
                
                return sb.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error formatting issues list");
                return $"Error formatting issues list: {ex.Message}\n\nRaw JSON:\n{jsonContent}";
            }
        }
    }
}
