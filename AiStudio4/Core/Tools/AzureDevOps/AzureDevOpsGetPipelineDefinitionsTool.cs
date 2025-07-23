// AiStudio4\Core\Tools\AzureDevOps\AzureDevOpsGetPipelineDefinitionsTool.cs

using AiStudio4.Core.Interfaces;
using AiStudio4.Core.Models;
using AiStudio4.InjectedDependencies;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace AiStudio4.Core.Tools.AzureDevOps
{
    /// <summary>
    /// Implementation of the Azure DevOps Get Pipeline Definitions tool
    /// </summary>
    [McpServerToolType]
    public class AzureDevOpsGetPipelineDefinitionsTool : BaseToolImplementation
    {
        private readonly HttpClient _httpClient;

        public AzureDevOpsGetPipelineDefinitionsTool(ILogger<AzureDevOpsGetPipelineDefinitionsTool> logger, IGeneralSettingsService generalSettingsService, IStatusMessageService statusMessageService) 
            : base(logger, generalSettingsService, statusMessageService)
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "AiStudio4-AzureDevOps-Tool");
        }

        /// <summary>
        /// Gets the Azure DevOps Get Pipeline Definitions tool definition
        /// </summary>
        public override Tool GetToolDefinition()
        {
            return new Tool
            {
                Guid = ToolGuids.AZURE_DEV_OPS_GET_PIPELINE_DEFINITIONS_TOOL_GUID,
                Name = "AzureDevOpsGetPipelineDefinitions",
                Description = "Retrieves pipeline definitions and configurations from Azure DevOps, including build pipelines, release pipelines, and YAML pipelines.",
                Schema = """
{
  "name": "AzureDevOpsGetPipelineDefinitions",
  "description": "Retrieves pipeline definitions and configurations from Azure DevOps, including build pipelines, release pipelines, and YAML pipelines.",
  "input_schema": {
    "properties": {
      "task_description": { "title": "Task Description", "type": "string", "description": "A concise, user-facing description of the task you are performing with this tool call." },
      "organization": { "title": "Organization", "type": "string", "description": "The Azure DevOps organization name" },
      "project": { "title": "Project", "type": "string", "description": "The Azure DevOps project name" },
      "pipeline_type": { "title": "Pipeline Type", "type": "string", "enum": ["build", "release", "yaml", "all"], "default": "all", "description": "Type of pipeline definitions to retrieve" },
      "definition_id": { "title": "Definition ID", "type": "integer", "description": "Specific pipeline definition ID (optional, for single pipeline)" },
      "name_filter": { "title": "Name Filter", "type": "string", "description": "Filter definitions by name (supports wildcards)" },
      "include_variables": { "title": "Include Variables", "type": "boolean", "default": false, "description": "Include pipeline variables and variable groups" },
      "include_triggers": { "title": "Include Triggers", "type": "boolean", "default": false, "description": "Include trigger configurations (CI, scheduled, etc.)" },
      "include_tasks": { "title": "Include Tasks", "type": "boolean", "default": false, "description": "Include detailed task/step configurations" },
      "include_permissions": { "title": "Include Permissions", "type": "boolean", "default": false, "description": "Include security and permission settings" },
      "status_filter": { "title": "Status Filter", "type": "string", "enum": ["enabled", "disabled", "all"], "default": "all", "description": "Filter by pipeline status" },
      "top": { "title": "Top", "type": "integer", "default": 100, "description": "Number of definitions to return" },
      "skip": { "title": "Skip", "type": "integer", "default": 0, "description": "Number of definitions to skip" }
    },
    "required": ["task_description", "organization", "project"],
    "title": "AzureDevOpsGetPipelineDefinitionsArguments",
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
                SendStatusUpdate("Starting Azure DevOps Get Pipeline Definitions tool execution...");
                var parameters = JsonConvert.DeserializeObject<Dictionary<string, object>>(toolParameters) ?? new Dictionary<string, object>();

                // Extract required parameters
                if (!parameters.TryGetValue("organization", out var organizationObj) || !(organizationObj is string organization) || string.IsNullOrWhiteSpace(organization))
                {
                    return CreateResult(true, true, $"Parameters: organization=<missing>, project=<unknown>\n\nError: 'organization' parameter is required.");
                }

                if (!parameters.TryGetValue("project", out var projectObj) || !(projectObj is string project) || string.IsNullOrWhiteSpace(project))
                {
                    return CreateResult(true, true, $"Parameters: organization={organization}, project=<missing>\n\nError: 'project' parameter is required.");
                }

                // Extract optional parameters
                string pipelineType = "all";
                if (parameters.TryGetValue("pipeline_type", out var pipelineTypeObj) && pipelineTypeObj is string pipelineTypeStr && !string.IsNullOrWhiteSpace(pipelineTypeStr))
                {
                    pipelineType = pipelineTypeStr;
                }

                int? definitionId = null;
                if (parameters.TryGetValue("definition_id", out var definitionIdObj))
                {
                    if (definitionIdObj is long definitionIdLong)
                    {
                        definitionId = (int)definitionIdLong;
                    }
                    else if (definitionIdObj is int definitionIdInt)
                    {
                        definitionId = definitionIdInt;
                    }
                }

                string nameFilter = null;
                if (parameters.TryGetValue("name_filter", out var nameFilterObj) && nameFilterObj is string nameFilterStr && !string.IsNullOrWhiteSpace(nameFilterStr))
                {
                    nameFilter = nameFilterStr;
                }

                bool includeVariables = false;
                if (parameters.TryGetValue("include_variables", out var includeVariablesObj) && includeVariablesObj is bool includeVariablesBool)
                {
                    includeVariables = includeVariablesBool;
                }

                bool includeTriggers = false;
                if (parameters.TryGetValue("include_triggers", out var includeTriggersObj) && includeTriggersObj is bool includeTriggersBool)
                {
                    includeTriggers = includeTriggersBool;
                }

                bool includeTasks = false;
                if (parameters.TryGetValue("include_tasks", out var includeTasksObj) && includeTasksObj is bool includeTasksBool)
                {
                    includeTasks = includeTasksBool;
                }

                bool includePermissions = false;
                if (parameters.TryGetValue("include_permissions", out var includePermissionsObj) && includePermissionsObj is bool includePermissionsBool)
                {
                    includePermissions = includePermissionsBool;
                }

                string statusFilter = "all";
                if (parameters.TryGetValue("status_filter", out var statusFilterObj) && statusFilterObj is string statusFilterStr && !string.IsNullOrWhiteSpace(statusFilterStr))
                {
                    statusFilter = statusFilterStr;
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
                    return CreateResult(true, true, $"Parameters: organization={organization}, project={project}\n\nError: Azure DevOps PAT is not configured. Please set it in File > Settings > Set Azure DevOps PAT.");
                }

                // Set up authentication header
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", 
                    Convert.ToBase64String(Encoding.ASCII.GetBytes(string.Format("{0}:{1}", "", apiKey))));

                // Make the API request
                return await GetPipelineDefinitionsAsync(organization, project, pipelineType, definitionId, nameFilter, 
                    includeVariables, includeTriggers, includeTasks, includePermissions, statusFilter, top, skip);
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

        private async Task<BuiltinToolResult> GetPipelineDefinitionsAsync(string organization, string project, string pipelineType, 
            int? definitionId, string nameFilter, bool includeVariables, bool includeTriggers, bool includeTasks, 
            bool includePermissions, string statusFilter, int top, int skip)
        {
            try
            {
                SendStatusUpdate($"Fetching pipeline definitions for {organization}/{project}...");
                
                var allDefinitions = new List<JObject>();
                
                // Get build definitions if requested
                if (pipelineType == "all" || pipelineType == "build")
                {
                    var buildDefinitions = await GetBuildDefinitionsAsync(organization, project, definitionId, nameFilter, 
                        includeVariables, includeTriggers, includeTasks, includePermissions, statusFilter, top, skip);
                    allDefinitions.AddRange(buildDefinitions);
                }
                
                // Get release definitions if requested
                if (pipelineType == "all" || pipelineType == "release")
                {
                    var releaseDefinitions = await GetReleaseDefinitionsAsync(organization, project, definitionId, nameFilter, 
                        includeVariables, includeTriggers, includeTasks, includePermissions, statusFilter, top, skip);
                    allDefinitions.AddRange(releaseDefinitions);
                }
                
                // Get YAML pipeline definitions if requested
                if (pipelineType == "all" || pipelineType == "yaml")
                {
                    var yamlDefinitions = await GetYamlPipelineDefinitionsAsync(organization, project, definitionId, nameFilter, 
                        includeVariables, includeTriggers, includeTasks, includePermissions, statusFilter, top, skip);
                    allDefinitions.AddRange(yamlDefinitions);
                }
                
                var formattedContent = FormatPipelineDefinitionsInfo(allDefinitions, pipelineType);
                
                SendStatusUpdate("Successfully retrieved pipeline definitions information.");
                return CreateResult(true, true, $"Parameters: organization={organization}, project={project}, pipeline_type={pipelineType}\n\n{formattedContent}");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error fetching pipeline definitions information");
                return CreateResult(true, true, $"Parameters: organization={organization}, project={project}\n\nError fetching pipeline definitions information: {ex.Message}");
            }
        }

        private async Task<List<JObject>> GetBuildDefinitionsAsync(string organization, string project, int? definitionId, 
            string nameFilter, bool includeVariables, bool includeTriggers, bool includeTasks, bool includePermissions, 
            string statusFilter, int top, int skip)
        {
            var queryParams = new List<string>();
            
            if (definitionId.HasValue)
            {
                queryParams.Add($"definitionIds={definitionId.Value}");
            }
            
            if (!string.IsNullOrEmpty(nameFilter))
            {
                queryParams.Add($"name={Uri.EscapeDataString(nameFilter)}");
            }
            
            if (statusFilter != "all")
            {
                queryParams.Add($"queryOrder=lastModifiedDescending");
            }
            
            queryParams.Add($"$top={top}");
            queryParams.Add($"$skip={skip}");
            
            string queryString = queryParams.Count > 0 ? $"?{string.Join("&", queryParams)}" : "";
            string url = $"https://dev.azure.com/{organization}/{project}/_apis/build/definitions{queryString}";
            
            var response = await _httpClient.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to get build definitions: {StatusCode} - {Content}", response.StatusCode, content);
                return new List<JObject>();
            }
            
            var buildData = JObject.Parse(content);
            var definitions = buildData["value"] as JArray ?? new JArray();
            
            var result = new List<JObject>();
            foreach (var def in definitions)
            {
                var definition = def as JObject;
                if (definition != null)
                {
                    definition["type"] = "build";
                    result.Add(definition);
                }
            }
            
            return result;
        }

        private async Task<List<JObject>> GetReleaseDefinitionsAsync(string organization, string project, int? definitionId, 
            string nameFilter, bool includeVariables, bool includeTriggers, bool includeTasks, bool includePermissions, 
            string statusFilter, int top, int skip)
        {
            var queryParams = new List<string>();
            
            if (definitionId.HasValue)
            {
                queryParams.Add($"definitionId={definitionId.Value}");
            }
            
            if (!string.IsNullOrEmpty(nameFilter))
            {
                queryParams.Add($"searchText={Uri.EscapeDataString(nameFilter)}");
            }
            
            queryParams.Add($"$top={top}");
            queryParams.Add($"$skip={skip}");
            
            string queryString = queryParams.Count > 0 ? $"?{string.Join("&", queryParams)}" : "";
            string url = $"https://vsrm.dev.azure.com/{organization}/{project}/_apis/release/definitions{queryString}";
            
            var response = await _httpClient.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to get release definitions: {StatusCode} - {Content}", response.StatusCode, content);
                return new List<JObject>();
            }
            
            var releaseData = JObject.Parse(content);
            var definitions = releaseData["value"] as JArray ?? new JArray();
            
            var result = new List<JObject>();
            foreach (var def in definitions)
            {
                var definition = def as JObject;
                if (definition != null)
                {
                    definition["type"] = "release";
                    result.Add(definition);
                }
            }
            
            return result;
        }

        private async Task<List<JObject>> GetYamlPipelineDefinitionsAsync(string organization, string project, int? definitionId, 
            string nameFilter, bool includeVariables, bool includeTriggers, bool includeTasks, bool includePermissions, 
            string statusFilter, int top, int skip)
        {
            var queryParams = new List<string>();
            
            if (definitionId.HasValue)
            {
                queryParams.Add($"pipelineIds={definitionId.Value}");
            }
            
            if (!string.IsNullOrEmpty(nameFilter))
            {
                queryParams.Add($"name={Uri.EscapeDataString(nameFilter)}");
            }
            
            queryParams.Add($"$top={top}");
            queryParams.Add($"$skip={skip}");
            
            string queryString = queryParams.Count > 0 ? $"?{string.Join("&", queryParams)}" : "";
            string url = $"https://dev.azure.com/{organization}/{project}/_apis/pipelines{queryString}";
            
            var response = await _httpClient.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to get YAML pipeline definitions: {StatusCode} - {Content}", response.StatusCode, content);
                return new List<JObject>();
            }
            
            var pipelineData = JObject.Parse(content);
            var definitions = pipelineData["value"] as JArray ?? new JArray();
            
            var result = new List<JObject>();
            foreach (var def in definitions)
            {
                var definition = def as JObject;
                if (definition != null)
                {
                    definition["type"] = "yaml";
                    result.Add(definition);
                }
            }
            
            return result;
        }

        private string FormatPipelineDefinitionsInfo(List<JObject> definitions, string pipelineType)
        {
            try
            {
                var sb = new StringBuilder();
                
                sb.AppendLine("# Azure DevOps Pipeline Definitions");
                sb.AppendLine();
                
                if (definitions.Count == 0)
                {
                    sb.AppendLine($"No {pipelineType} pipeline definitions found matching the criteria.");
                    return sb.ToString();
                }
                
                sb.AppendLine($"Found {definitions.Count} pipeline definitions:\n");
                
                foreach (var definition in definitions)
                {
                    string type = definition["type"]?.ToString() ?? "unknown";
                    string name = definition["name"]?.ToString() ?? "Unknown Name";
                    string id = definition["id"]?.ToString() ?? "Unknown ID";
                    
                    sb.AppendLine($"## [{type.ToUpper()}] {name} (ID: {id})");
                    
                    if (definition["path"] != null)
                    {
                        sb.AppendLine($"**Path:** {definition["path"]}");
                    }
                    
                    if (definition["repository"] != null)
                    {
                        var repo = definition["repository"];
                        sb.AppendLine($"**Repository:** {repo["name"]} ({repo["type"]})");
                        if (repo["defaultBranch"] != null)
                        {
                            sb.AppendLine($"**Default Branch:** {repo["defaultBranch"]}");
                        }
                    }
                    
                    if (definition["createdDate"] != null)
                    {
                        sb.AppendLine($"**Created:** {definition["createdDate"]}");
                    }
                    
                    if (definition["modifiedDate"] != null)
                    {
                        sb.AppendLine($"**Modified:** {definition["modifiedDate"]}");
                    }
                    
                    if (definition["createdBy"] != null)
                    {
                        sb.AppendLine($"**Created By:** {definition["createdBy"]["displayName"]}");
                    }
                    
                    if (definition["queue"] != null)
                    {
                        sb.AppendLine($"**Queue:** {definition["queue"]["name"]}");
                    }
                    
                    if (definition["variables"] != null)
                    {
                        sb.AppendLine("\n**Variables:**");
                        foreach (var variable in definition["variables"].Children<JProperty>())
                        {
                            sb.AppendLine($"- {variable.Name}: {variable.Value["value"]} (Secret: {variable.Value["isSecret"] ?? false})");
                        }
                    }
                    
                    if (definition["triggers"] != null && definition["triggers"].HasValues)
                    {
                        sb.AppendLine("\n**Triggers:**");
                        foreach (var trigger in definition["triggers"])
                        {
                            sb.AppendLine($"- Type: {trigger["triggerType"]}, Enabled: {trigger["settingsSourceType"]}");
                        }
                    }
                    
                    if (definition["process"] != null && definition["process"]["phases"] != null)
                    {
                        sb.AppendLine("\n**Build Steps:**");
                        foreach (var phase in definition["process"]["phases"])
                        {
                            if (phase["steps"] != null)
                            {
                                foreach (var step in phase["steps"])
                                {
                                    sb.AppendLine($"- {step["displayName"]} ({step["task"]["definitionType"]})");
                                }
                            }
                        }
                    }
                    
                    if (definition["_links"] != null && definition["_links"]["web"] != null)
                    {
                        sb.AppendLine($"\n**Web URL:** {definition["_links"]["web"]["href"]}");
                    }
                    
                    sb.AppendLine("\n---\n");
                }
                
                return sb.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error formatting pipeline definitions information");
                return $"Error formatting pipeline definitions information: {ex.Message}\n\nRaw data: {JsonConvert.SerializeObject(definitions, Formatting.Indented)}";
            }
        }

        [McpServerTool, Description("Retrieves pipeline definitions and configurations from Azure DevOps, including build pipelines, release pipelines, and YAML pipelines.")]
        public async Task<string> AzureDevOpsGetPipelineDefinitions([Description("JSON parameters for AzureDevOpsGetPipelineDefinitions")] string parameters = "{}")
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