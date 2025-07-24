








using System.Net.Http;
using System.Net.Http.Headers;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using System.ComponentModel;



namespace AiStudio4.Core.Tools.AzureDevOps
{
    
    
    
    [McpServerToolType]
    public class AzureDevOpsGetWorkItemsTool : BaseToolImplementation
    {
        private readonly HttpClient _httpClient;

        public AzureDevOpsGetWorkItemsTool(ILogger<AzureDevOpsGetWorkItemsTool> logger, IGeneralSettingsService generalSettingsService, IStatusMessageService statusMessageService) 
            : base(logger, generalSettingsService, statusMessageService)
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "AiStudio4-AzureDevOps-Tool");
        }

        
        
        
        public override Tool GetToolDefinition()
        {
            return new Tool
            {
                Guid = ToolGuids.AZURE_DEV_OPS_GET_WORK_ITEMS_TOOL_GUID,
                Name = "AzureDevOpsGetWorkItems",
                Description = "Retrieves detailed information about specific work items by their IDs from Azure DevOps.",
                Schema = """
{
  "name": "AzureDevOpsGetWorkItems",
  "description": "Retrieves detailed information about specific work items by their IDs from Azure DevOps.",
  "input_schema": {
    "properties": {
      "organization": { "title": "Organization", "type": "string", "description": "The Azure DevOps organization name" },
      "project": { "title": "Project", "type": "string", "description": "The Azure DevOps project name" },
      "ids": { "title": "IDs", "type": "array", "items": { "type": "integer" }, "description": "Work item IDs to retrieve" },
      "fields": { "title": "Fields", "type": "array", "items": { "type": "string" }, "description": "Specific fields to return (optional)" },
      "as_of": { "title": "As Of", "type": "string", "description": "Date to view work items as of (optional)" },
      "expand": { "title": "Expand", "type": "string", "description": "Expand relations (values: 'relations', 'fields', 'none')", "enum": ["relations", "fields", "none"], "default": "none" }
    },
    "required": ["organization", "project", "ids"],
    "title": "AzureDevOpsGetWorkItemsArguments",
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
                SendStatusUpdate("Starting Azure DevOps Get Work Items tool execution...");
                var parameters = JsonConvert.DeserializeObject<Dictionary<string, object>>(toolParameters) ?? new Dictionary<string, object>();

                
                if (!parameters.TryGetValue("organization", out var organizationObj) || !(organizationObj is string organization) || string.IsNullOrWhiteSpace(organization))
                {
                    return CreateResult(true, true, $"Parameters: organization=<missing>, project=<unknown>, ids=<unknown>\n\nError: 'organization' parameter is required.");
                }

                if (!parameters.TryGetValue("project", out var projectObj) || !(projectObj is string project) || string.IsNullOrWhiteSpace(project))
                {
                    return CreateResult(true, true, $"Parameters: organization={organization}, project=<missing>, ids=<unknown>\n\nError: 'project' parameter is required.");
                }

                if (!parameters.TryGetValue("ids", out var idsObj) || !(idsObj is Newtonsoft.Json.Linq.JArray idsArray) || idsArray.Count == 0)
                {
                    return CreateResult(true, true, $"Parameters: organization={organization}, project={project}, ids=<missing or empty>\n\nError: 'ids' parameter is required and must contain at least one work item ID.");
                }

                
                var ids = new List<int>();
                foreach (var idToken in idsArray)
                {
                    if (idToken.Type == JTokenType.Integer)
                    {
                        ids.Add(idToken.Value<int>());
                    }
                    else if (idToken.Type == JTokenType.String && int.TryParse(idToken.Value<string>(), out int idValue))
                    {
                        ids.Add(idValue);
                    }
                }

                if (ids.Count == 0)
                {
                    return CreateResult(true, true, $"Parameters: organization={organization}, project={project}, ids=<invalid>\n\nError: 'ids' parameter must contain valid integer work item IDs.");
                }

                
                List<string> fields = null;
                if (parameters.TryGetValue("fields", out var fieldsObj) && fieldsObj is Newtonsoft.Json.Linq.JArray fieldsArray && fieldsArray.Count > 0)
                {
                    fields = new List<string>();
                    foreach (var fieldToken in fieldsArray)
                    {
                        if (fieldToken.Type == JTokenType.String)
                        {
                            fields.Add(fieldToken.Value<string>());
                        }
                    }
                }

                string asOf = null;
                if (parameters.TryGetValue("as_of", out var asOfObj) && asOfObj is string asOfStr && !string.IsNullOrWhiteSpace(asOfStr))
                {
                    asOf = asOfStr;
                }

                string expand = "none";
                if (parameters.TryGetValue("expand", out var expandObj) && expandObj is string expandStr && !string.IsNullOrWhiteSpace(expandStr))
                {
                    expand = expandStr;
                }

                
                string apiKey = _generalSettingsService.GetDecryptedAzureDevOpsPAT();
                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    return CreateResult(true, true, $"Parameters: organization={organization}, project={project}, ids={string.Join(",", ids)}\n\nError: Azure DevOps PAT is not configured. Please set it in File > Settings > Set Azure DevOps PAT.");
                }

                
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", 
                    Convert.ToBase64String(Encoding.ASCII.GetBytes(string.Format("{0}:{1}", "", apiKey))));

                
                return await GetWorkItemsAsync(organization, project, ids, fields, asOf, expand);
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

        private async Task<BuiltinToolResult> GetWorkItemsAsync(string organization, string project, List<int> ids, 
            List<string> fields, string asOf, string expand)
        {
            try
            {
                SendStatusUpdate($"Fetching work items for {organization}/{project}...");
                
                
                var queryParams = new List<string>();
                
                
                if (fields != null && fields.Count > 0)
                {
                    queryParams.Add($"fields={string.Join(",", fields)}");
                }
                
                
                if (!string.IsNullOrEmpty(asOf))
                {
                    queryParams.Add($"asOf={asOf}");
                }
                
                
                if (!string.IsNullOrEmpty(expand) && expand != "none")
                {
                    queryParams.Add($"$expand={expand}");
                }
                
                string queryString = queryParams.Count > 0 ? $"?{string.Join("&", queryParams)}" : "";
                
                
                string idsString = string.Join(",", ids);
                string url = $"https://dev.azure.com/{organization}/{project}/_apis/wit/workitems?ids={idsString}{queryString}";
                
                var response = await _httpClient.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorObj = JObject.Parse(content);
                    string errorMessage = errorObj["message"]?.ToString() ?? "Unknown error";
                    return CreateResult(true, true, $"Parameters: organization={organization}, project={project}, ids={string.Join(",", ids)}\n\nAzure DevOps API Error: {errorMessage} (Status code: {response.StatusCode})");
                }
                
                var formattedContent = FormatWorkItemsInfo(content);
                
                SendStatusUpdate("Successfully retrieved work items information.");
                return CreateResult(true, true, $"Parameters: organization={organization}, project={project}, ids={string.Join(",", ids)}\n\n{formattedContent}");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error fetching work items information");
                return CreateResult(true, true, $"Parameters: organization={organization}, project={project}, ids={string.Join(",", ids)}\n\nError fetching work items information: {ex.Message}");
            }
        }

        private string FormatWorkItemsInfo(string jsonContent)
        {
            try
            {
                var wiData = JObject.Parse(jsonContent);
                var workItems = wiData["value"] as JArray;
                var sb = new StringBuilder();
                
                sb.AppendLine("# Azure DevOps Work Items");
                sb.AppendLine();
                
                if (workItems == null || workItems.Count == 0)
                {
                    sb.AppendLine("No work items found matching the specified IDs.");
                    return sb.ToString();
                }
                
                sb.AppendLine($"Found {workItems.Count} work items:\n");
                
                foreach (var workItem in workItems)
                {
                    var id = workItem["id"];
                    var fields = workItem["fields"] as JObject;
                    
                    if (fields == null)
                    {
                        sb.AppendLine($"## Work Item #{id} - No fields available");
                        continue;
                    }
                    
                    
                    string workItemType = fields["System.WorkItemType"]?.ToString() ?? "Unknown Type";
                    string title = fields["System.Title"]?.ToString() ?? "No Title";
                    string state = fields["System.State"]?.ToString() ?? "Unknown State";
                    string createdBy = fields["System.CreatedBy"]?["displayName"]?.ToString() ?? "Unknown";
                    string createdDate = fields["System.CreatedDate"]?.ToString() ?? "Unknown";
                    string assignedTo = fields["System.AssignedTo"]?["displayName"]?.ToString() ?? "Unassigned";
                    
                    sb.AppendLine($"## [{workItemType}] #{id} - {title}");
                    sb.AppendLine($"**State:** {state}");
                    sb.AppendLine($"**Created by:** {createdBy}");
                    sb.AppendLine($"**Created on:** {createdDate}");
                    sb.AppendLine($"**Assigned to:** {assignedTo}");
                    
                    
                    if (fields["System.Description"] != null)
                    {
                        sb.AppendLine("\n**Description:**");
                        sb.AppendLine(fields["System.Description"].ToString());
                    }
                    
                    
                    if (fields["Microsoft.VSTS.Common.AcceptanceCriteria"] != null)
                    {
                        sb.AppendLine("\n**Acceptance Criteria:**");
                        sb.AppendLine(fields["Microsoft.VSTS.Common.AcceptanceCriteria"].ToString());
                    }
                    
                    
                    if (fields["System.IterationPath"] != null)
                    {
                        sb.AppendLine($"\n**Iteration Path:** {fields["System.IterationPath"]}");
                    }
                    
                    if (fields["System.AreaPath"] != null)
                    {
                        sb.AppendLine($"**Area Path:** {fields["System.AreaPath"]}");
                    }
                    
                    
                    if (fields["Microsoft.VSTS.Scheduling.StoryPoints"] != null || fields["Microsoft.VSTS.Scheduling.Effort"] != null)
                    {
                        var points = fields["Microsoft.VSTS.Scheduling.StoryPoints"] ?? fields["Microsoft.VSTS.Scheduling.Effort"];
                        sb.AppendLine($"**Story Points/Effort:** {points}");
                    }
                    
                    
                    if (fields["Microsoft.VSTS.Common.Priority"] != null)
                    {
                        sb.AppendLine($"**Priority:** {fields["Microsoft.VSTS.Common.Priority"]}");
                    }
                    
                    
                    if (workItem["relations"] is JArray relations && relations.Count > 0)
                    {
                        sb.AppendLine("\n**Relations:**");
                        foreach (var relation in relations)
                        {
                            string relationType = relation["rel"]?.ToString() ?? "Unknown";
                            string url = relation["url"]?.ToString() ?? "";
                            
                            
                            string relatedId = "";
                            if (url.Contains("workitems"))
                            {
                                relatedId = url.Split('/').Last();
                                sb.AppendLine($"- {relationType}: Work Item #{relatedId}");
                            }
                            else
                            {
                                sb.AppendLine($"- {relationType}: {url}");
                            }
                        }
                    }
                    
                    
                    sb.AppendLine("\n---\n");
                }
                
                return sb.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error formatting work items information");
                return $"Error formatting work items information: {ex.Message}\n\nRaw JSON:\n{jsonContent}";
            }
        }

        [McpServerTool, Description("Retrieves detailed information about specific work items by their IDs from Azure DevOps.")]
        public async Task<string> AzureDevOpsGetWorkItems([Description("JSON parameters for AzureDevOpsGetWorkItems")] string parameters = "{}")
        {
            return await ExecuteWithExtraProperties(parameters);
        }
    }
}
