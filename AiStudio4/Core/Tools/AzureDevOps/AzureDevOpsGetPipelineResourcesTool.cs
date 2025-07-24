// AiStudio4\Core\Tools\AzureDevOps\AzureDevOpsGetPipelineResourcesTool.cs

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
    /// Implementation of the Azure DevOps Get Pipeline Resources tool
    /// </summary>
    [McpServerToolType]
    public class AzureDevOpsGetPipelineResourcesTool : BaseToolImplementation
    {
        private readonly HttpClient _httpClient;

        public AzureDevOpsGetPipelineResourcesTool(ILogger<AzureDevOpsGetPipelineResourcesTool> logger, IGeneralSettingsService generalSettingsService, IStatusMessageService statusMessageService) 
            : base(logger, generalSettingsService, statusMessageService)
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "AiStudio4-AzureDevOps-Tool");
        }

        /// <summary>
        /// Gets the Azure DevOps Get Pipeline Resources tool definition
        /// </summary>
        public override Tool GetToolDefinition()
        {
            return new Tool
            {
                Guid = ToolGuids.AZURE_DEV_OPS_GET_PIPELINE_RESOURCES_TOOL_GUID,
                Name = "AzureDevOpsGetPipelineResources",
                Description = "Retrieves pipeline-related resources including variable groups, service connections, agent pools, environments, and deployment groups.",
                Schema = """
{
  "name": "AzureDevOpsGetPipelineResources",
  "description": "Retrieves pipeline-related resources including variable groups, service connections, agent pools, environments, and deployment groups.",
  "input_schema": {
    "properties": {
      "task_description": { "title": "Task Description", "type": "string", "description": "A concise, user-facing description of the task you are performing with this tool call." },
      "organization": { "title": "Organization", "type": "string", "description": "The Azure DevOps organization name" },
      "project": { "title": "Project", "type": "string", "description": "The Azure DevOps project name" },
      "resource_type": { "title": "Resource Type", "type": "string", "enum": ["variableGroups", "serviceConnections", "agentPools", "environments", "deploymentGroups", "all"], "default": "all", "description": "Type of resources to retrieve" },
      "resource_id": { "title": "Resource ID", "type": "integer", "description": "Specific resource ID (optional, for single resource)" },
      "name_filter": { "title": "Name Filter", "type": "string", "description": "Filter resources by name (supports wildcards)" },
      "include_permissions": { "title": "Include Permissions", "type": "boolean", "default": false, "description": "Include security and permission settings" },
      "include_usage": { "title": "Include Usage", "type": "boolean", "default": false, "description": "Include which pipelines use these resources" },
      "include_values": { "title": "Include Values", "type": "boolean", "default": false, "description": "Include variable values (non-secret only) and connection details" },
      "top": { "title": "Top", "type": "integer", "default": 100, "description": "Number of resources to return" },
      "skip": { "title": "Skip", "type": "integer", "default": 0, "description": "Number of resources to skip" }
    },
    "required": ["task_description", "organization", "project"],
    "title": "AzureDevOpsGetPipelineResourcesArguments",
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
                SendStatusUpdate("Starting Azure DevOps Get Pipeline Resources tool execution...");
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
                string resourceType = "all";
                if (parameters.TryGetValue("resource_type", out var resourceTypeObj) && resourceTypeObj is string resourceTypeStr && !string.IsNullOrWhiteSpace(resourceTypeStr))
                {
                    resourceType = resourceTypeStr;
                }

                int? resourceId = null;
                if (parameters.TryGetValue("resource_id", out var resourceIdObj))
                {
                    if (resourceIdObj is long resourceIdLong)
                    {
                        resourceId = (int)resourceIdLong;
                    }
                    else if (resourceIdObj is int resourceIdInt)
                    {
                        resourceId = resourceIdInt;
                    }
                }

                string nameFilter = null;
                if (parameters.TryGetValue("name_filter", out var nameFilterObj) && nameFilterObj is string nameFilterStr && !string.IsNullOrWhiteSpace(nameFilterStr))
                {
                    nameFilter = nameFilterStr;
                }

                bool includePermissions = false;
                if (parameters.TryGetValue("include_permissions", out var includePermissionsObj) && includePermissionsObj is bool includePermissionsBool)
                {
                    includePermissions = includePermissionsBool;
                }

                bool includeUsage = false;
                if (parameters.TryGetValue("include_usage", out var includeUsageObj) && includeUsageObj is bool includeUsageBool)
                {
                    includeUsage = includeUsageBool;
                }

                bool includeValues = false;
                if (parameters.TryGetValue("include_values", out var includeValuesObj) && includeValuesObj is bool includeValuesBool)
                {
                    includeValues = includeValuesBool;
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
                return await GetPipelineResourcesAsync(organization, project, resourceType, resourceId, nameFilter, 
                    includePermissions, includeUsage, includeValues, top, skip);
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

        private async Task<BuiltinToolResult> GetPipelineResourcesAsync(string organization, string project, string resourceType, 
            int? resourceId, string nameFilter, bool includePermissions, bool includeUsage, bool includeValues, int top, int skip)
        {
            try
            {
                SendStatusUpdate($"Fetching pipeline resources for {organization}/{project}...");
                
                var allResources = new Dictionary<string, List<JObject>>();
                
                // Get variable groups if requested
                if (resourceType == "all" || resourceType == "variableGroups")
                {
                    var variableGroups = await GetVariableGroupsAsync(organization, project, resourceId, nameFilter, 
                        includePermissions, includeUsage, includeValues, top, skip);
                    allResources["variableGroups"] = variableGroups;
                }
                
                // Get service connections if requested
                if (resourceType == "all" || resourceType == "serviceConnections")
                {
                    var serviceConnections = await GetServiceConnectionsAsync(organization, project, resourceId, nameFilter, 
                        includePermissions, includeUsage, includeValues, top, skip);
                    allResources["serviceConnections"] = serviceConnections;
                }
                
                // Get agent pools if requested
                if (resourceType == "all" || resourceType == "agentPools")
                {
                    var agentPools = await GetAgentPoolsAsync(organization, project, resourceId, nameFilter, 
                        includePermissions, includeUsage, includeValues, top, skip);
                    allResources["agentPools"] = agentPools;
                }
                
                // Get environments if requested
                if (resourceType == "all" || resourceType == "environments")
                {
                    var environments = await GetEnvironmentsAsync(organization, project, resourceId, nameFilter, 
                        includePermissions, includeUsage, includeValues, top, skip);
                    allResources["environments"] = environments;
                }
                
                // Get deployment groups if requested
                if (resourceType == "all" || resourceType == "deploymentGroups")
                {
                    var deploymentGroups = await GetDeploymentGroupsAsync(organization, project, resourceId, nameFilter, 
                        includePermissions, includeUsage, includeValues, top, skip);
                    allResources["deploymentGroups"] = deploymentGroups;
                }
                
                var formattedContent = FormatPipelineResourcesInfo(allResources, resourceType);
                
                SendStatusUpdate("Successfully retrieved pipeline resources information.");
                return CreateResult(true, true, $"Parameters: organization={organization}, project={project}, resource_type={resourceType}\n\n{formattedContent}");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error fetching pipeline resources information");
                return CreateResult(true, true, $"Parameters: organization={organization}, project={project}\n\nError fetching pipeline resources information: {ex.Message}");
            }
        }

        private async Task<List<JObject>> GetVariableGroupsAsync(string organization, string project, int? resourceId, 
            string nameFilter, bool includePermissions, bool includeUsage, bool includeValues, int top, int skip)
        {
            var queryParams = new List<string>();
            
            if (resourceId.HasValue)
            {
                queryParams.Add($"groupIds={resourceId.Value}");
            }
            
            if (!string.IsNullOrEmpty(nameFilter))
            {
                queryParams.Add($"groupName={Uri.EscapeDataString(nameFilter)}");
            }
            
            queryParams.Add($"$top={top}");
            queryParams.Add($"$skip={skip}");
            
            string queryString = queryParams.Count > 0 ? $"?{string.Join("&", queryParams)}" : "";
            string url = $"https://dev.azure.com/{organization}/{project}/_apis/distributedtask/variablegroups{queryString}";
            
            var response = await _httpClient.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to get variable groups: {StatusCode} - {Content}", response.StatusCode, content);
                return new List<JObject>();
            }
            
            var data = JObject.Parse(content);
            var resources = data["value"] as JArray ?? new JArray();
            
            var result = new List<JObject>();
            foreach (var resource in resources)
            {
                var resourceObj = resource as JObject;
                if (resourceObj != null)
                {
                    resourceObj["resourceType"] = "variableGroup";
                    result.Add(resourceObj);
                }
            }
            
            return result;
        }

        private async Task<List<JObject>> GetServiceConnectionsAsync(string organization, string project, int? resourceId, 
            string nameFilter, bool includePermissions, bool includeUsage, bool includeValues, int top, int skip)
        {
            var queryParams = new List<string>();
            
            if (resourceId.HasValue)
            {
                queryParams.Add($"endpointIds={resourceId.Value}");
            }
            
            if (!string.IsNullOrEmpty(nameFilter))
            {
                queryParams.Add($"endpointNames={Uri.EscapeDataString(nameFilter)}");
            }
            
            queryParams.Add($"$top={top}");
            queryParams.Add($"$skip={skip}");
            
            string queryString = queryParams.Count > 0 ? $"?{string.Join("&", queryParams)}" : "";
            string url = $"https://dev.azure.com/{organization}/{project}/_apis/serviceendpoint/endpoints{queryString}";
            
            var response = await _httpClient.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to get service connections: {StatusCode} - {Content}", response.StatusCode, content);
                return new List<JObject>();
            }
            
            var data = JObject.Parse(content);
            var resources = data["value"] as JArray ?? new JArray();
            
            var result = new List<JObject>();
            foreach (var resource in resources)
            {
                var resourceObj = resource as JObject;
                if (resourceObj != null)
                {
                    resourceObj["resourceType"] = "serviceConnection";
                    result.Add(resourceObj);
                }
            }
            
            return result;
        }

        private async Task<List<JObject>> GetAgentPoolsAsync(string organization, string project, int? resourceId, 
            string nameFilter, bool includePermissions, bool includeUsage, bool includeValues, int top, int skip)
        {
            var queryParams = new List<string>();
            
            if (resourceId.HasValue)
            {
                queryParams.Add($"poolIds={resourceId.Value}");
            }
            
            if (!string.IsNullOrEmpty(nameFilter))
            {
                queryParams.Add($"poolName={Uri.EscapeDataString(nameFilter)}");
            }
            
            string queryString = queryParams.Count > 0 ? $"?{string.Join("&", queryParams)}" : "";
            string url = $"https://dev.azure.com/{organization}/_apis/distributedtask/pools{queryString}";
            
            var response = await _httpClient.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to get agent pools: {StatusCode} - {Content}", response.StatusCode, content);
                return new List<JObject>();
            }
            
            var data = JObject.Parse(content);
            var resources = data["value"] as JArray ?? new JArray();
            
            var result = new List<JObject>();
            foreach (var resource in resources)
            {
                var resourceObj = resource as JObject;
                if (resourceObj != null)
                {
                    resourceObj["resourceType"] = "agentPool";
                    result.Add(resourceObj);
                }
            }
            
            return result;
        }

        private async Task<List<JObject>> GetEnvironmentsAsync(string organization, string project, int? resourceId, 
            string nameFilter, bool includePermissions, bool includeUsage, bool includeValues, int top, int skip)
        {
            var queryParams = new List<string>();
            
            if (resourceId.HasValue)
            {
                queryParams.Add($"environmentId={resourceId.Value}");
            }
            
            if (!string.IsNullOrEmpty(nameFilter))
            {
                queryParams.Add($"name={Uri.EscapeDataString(nameFilter)}");
            }
            
            queryParams.Add($"$top={top}");
            queryParams.Add($"$skip={skip}");
            
            string queryString = queryParams.Count > 0 ? $"?{string.Join("&", queryParams)}" : "";
            string url = $"https://dev.azure.com/{organization}/{project}/_apis/distributedtask/environments{queryString}";
            
            var response = await _httpClient.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to get environments: {StatusCode} - {Content}", response.StatusCode, content);
                return new List<JObject>();
            }
            
            var data = JObject.Parse(content);
            var resources = data["value"] as JArray ?? new JArray();
            
            var result = new List<JObject>();
            foreach (var resource in resources)
            {
                var resourceObj = resource as JObject;
                if (resourceObj != null)
                {
                    resourceObj["resourceType"] = "environment";
                    result.Add(resourceObj);
                }
            }
            
            return result;
        }

        private async Task<List<JObject>> GetDeploymentGroupsAsync(string organization, string project, int? resourceId, 
            string nameFilter, bool includePermissions, bool includeUsage, bool includeValues, int top, int skip)
        {
            var queryParams = new List<string>();
            
            if (resourceId.HasValue)
            {
                queryParams.Add($"deploymentGroupId={resourceId.Value}");
            }
            
            if (!string.IsNullOrEmpty(nameFilter))
            {
                queryParams.Add($"name={Uri.EscapeDataString(nameFilter)}");
            }
            
            queryParams.Add($"$top={top}");
            queryParams.Add($"$skip={skip}");
            
            string queryString = queryParams.Count > 0 ? $"?{string.Join("&", queryParams)}" : "";
            string url = $"https://dev.azure.com/{organization}/{project}/_apis/distributedtask/deploymentgroups{queryString}";
            
            var response = await _httpClient.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to get deployment groups: {StatusCode} - {Content}", response.StatusCode, content);
                return new List<JObject>();
            }
            
            var data = JObject.Parse(content);
            var resources = data["value"] as JArray ?? new JArray();
            
            var result = new List<JObject>();
            foreach (var resource in resources)
            {
                var resourceObj = resource as JObject;
                if (resourceObj != null)
                {
                    resourceObj["resourceType"] = "deploymentGroup";
                    result.Add(resourceObj);
                }
            }
            
            return result;
        }

        private string FormatPipelineResourcesInfo(Dictionary<string, List<JObject>> allResources, string resourceType)
        {
            try
            {
                var sb = new StringBuilder();
                
                sb.AppendLine("# Azure DevOps Pipeline Resources");
                sb.AppendLine();
                
                int totalCount = allResources.Values.Sum(list => list.Count);
                if (totalCount == 0)
                {
                    sb.AppendLine($"No {resourceType} resources found matching the criteria.");
                    return sb.ToString();
                }
                
                sb.AppendLine($"Found {totalCount} pipeline resources:\n");
                
                foreach (var resourceCategory in allResources)
                {
                    if (resourceCategory.Value.Count == 0) continue;
                    
                    sb.AppendLine($"## {resourceCategory.Key.ToUpper()} ({resourceCategory.Value.Count} items)");
                    sb.AppendLine();
                    
                    foreach (var resource in resourceCategory.Value)
                    {
                        string resourceTypeStr = resource["resourceType"]?.ToString() ?? "unknown";
                        string name = resource["name"]?.ToString() ?? "Unknown Name";
                        string id = resource["id"]?.ToString() ?? "Unknown ID";
                        
                        sb.AppendLine($"### {name} (ID: {id})");
                        
                        if (resource["description"] != null)
                        {
                            sb.AppendLine($"**Description:** {resource["description"]}");
                        }
                        
                        if (resource["type"] != null)
                        {
                            sb.AppendLine($"**Type:** {resource["type"]}");
                        }
                        
                        if (resource["createdBy"] != null)
                        {
                            sb.AppendLine($"**Created By:** {resource["createdBy"]["displayName"]}");
                        }
                        
                        if (resource["createdOn"] != null)
                        {
                            sb.AppendLine($"**Created On:** {resource["createdOn"]}");
                        }
                        
                        if (resource["modifiedBy"] != null)
                        {
                            sb.AppendLine($"**Modified By:** {resource["modifiedBy"]["displayName"]}");
                        }
                        
                        if (resource["modifiedOn"] != null)
                        {
                            sb.AppendLine($"**Modified On:** {resource["modifiedOn"]}");
                        }
                        
                        // Variable Group specific fields
                        if (resourceTypeStr == "variableGroup" && resource["variables"] != null)
                        {
                            sb.AppendLine("\n**Variables:**");
                            foreach (var variable in resource["variables"].Children<JProperty>())
                            {
                                bool isSecret = variable.Value["isSecret"]?.Value<bool>() ?? false;
                                string value = isSecret ? "[SECRET]" : (variable.Value["value"]?.ToString() ?? "");
                                sb.AppendLine($"- {variable.Name}: {value} (Secret: {isSecret})");
                            }
                        }
                        
                        // Service Connection specific fields
                        if (resourceTypeStr == "serviceConnection")
                        {
                            if (resource["authorization"] != null)
                            {
                                sb.AppendLine($"**Authorization Scheme:** {resource["authorization"]["scheme"]}");
                            }
                            
                            if (resource["serviceEndpointProjectReferences"] != null)
                            {
                                sb.AppendLine("\n**Project References:**");
                                foreach (var projectRef in resource["serviceEndpointProjectReferences"])
                                {
                                    sb.AppendLine($"- Project: {projectRef["projectReference"]["name"]}");
                                }
                            }
                        }
                        
                        // Agent Pool specific fields
                        if (resourceTypeStr == "agentPool")
                        {
                            if (resource["poolType"] != null)
                            {
                                sb.AppendLine($"**Pool Type:** {resource["poolType"]}");
                            }
                            
                            if (resource["size"] != null)
                            {
                                sb.AppendLine($"**Size:** {resource["size"]} agents");
                            }
                            
                            if (resource["isHosted"] != null)
                            {
                                sb.AppendLine($"**Is Hosted:** {resource["isHosted"]}");
                            }
                        }
                        
                        // Environment specific fields
                        if (resourceTypeStr == "environment")
                        {
                            if (resource["resources"] != null)
                            {
                                sb.AppendLine($"**Resources Count:** {((JArray)resource["resources"]).Count}");
                            }
                        }
                        
                        // Deployment Group specific fields
                        if (resourceTypeStr == "deploymentGroup")
                        {
                            if (resource["machineCount"] != null)
                            {
                                sb.AppendLine($"**Machine Count:** {resource["machineCount"]}");
                            }
                        }
                        
                        sb.AppendLine();
                    }
                    
                    sb.AppendLine("---\n");
                }
                
                return sb.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error formatting pipeline resources information");
                return $"Error formatting pipeline resources information: {ex.Message}\n\nRaw data: {JsonConvert.SerializeObject(allResources, Formatting.Indented)}";
            }
        }

        [McpServerTool, Description("Retrieves pipeline-related resources including variable groups, service connections, agent pools, environments, and deployment groups.")]
        public async Task<string> AzureDevOpsGetPipelineResources([Description("JSON parameters for AzureDevOpsGetPipelineResources")] string parameters = "{}")
        {
            return await ExecuteWithExtraProperties(parameters);
        }
    }
}