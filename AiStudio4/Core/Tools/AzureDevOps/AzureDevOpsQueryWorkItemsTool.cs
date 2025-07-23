







using System.Net.Http;
using System.Net.Http.Headers;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using System.ComponentModel;



namespace AiStudio4.Core.Tools.AzureDevOps
{
    
    
    
    [McpServerToolType]
    public class AzureDevOpsQueryWorkItemsTool : BaseToolImplementation
    {
        private readonly HttpClient _httpClient;

        public AzureDevOpsQueryWorkItemsTool(ILogger<AzureDevOpsQueryWorkItemsTool> logger, IGeneralSettingsService generalSettingsService, IStatusMessageService statusMessageService) 
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
                Guid = ToolGuids.AZURE_DEV_OPS_QUERY_WORK_ITEMS_TOOL_GUID,
                Name = "AzureDevOpsQueryWorkItems",
                Description = "Executes a WIQL (Work Item Query Language) query to find work items matching specific criteria.",
                Schema = """
{
  "name": "AzureDevOpsQueryWorkItems",
  "description": "Executes a WIQL (Work Item Query Language) query to find work items matching specific criteria.",
  "input_schema": {
    "properties": {
      "organization": { "title": "Organization", "type": "string", "description": "The Azure DevOps organization name" },
      "project": { "title": "Project", "type": "string", "description": "The Azure DevOps project name" },
      "query": { "title": "Query", "type": "string", "description": "WIQL query text" },
      "top": { "title": "Top", "type": "integer", "description": "Number of work items to return", "default": 100 },
      "skip": { "title": "Skip", "type": "integer", "description": "Number of work items to skip", "default": 0 },
      "time_precision": { "title": "Time Precision", "type": "boolean", "description": "Include time precision for date fields", "default": false }
    },
    "required": ["organization", "project", "query"],
    "title": "AzureDevOpsQueryWorkItemsArguments",
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
                SendStatusUpdate("Starting Azure DevOps Query Work Items tool execution...");
                var parameters = JsonConvert.DeserializeObject<Dictionary<string, object>>(toolParameters) ?? new Dictionary<string, object>();

                
                if (!parameters.TryGetValue("organization", out var organizationObj) || !(organizationObj is string organization) || string.IsNullOrWhiteSpace(organization))
                {
                    return CreateResult(true, true, $"Parameters: organization=<missing>, project=<unknown>\n\nError: 'organization' parameter is required.");
                }

                if (!parameters.TryGetValue("project", out var projectObj) || !(projectObj is string project) || string.IsNullOrWhiteSpace(project))
                {
                    return CreateResult(true, true, $"Parameters: organization={organization}, project=<missing>\n\nError: 'project' parameter is required.");
                }

                if (!parameters.TryGetValue("query", out var queryObj) || !(queryObj is string query) || string.IsNullOrWhiteSpace(query))
                {
                    return CreateResult(true, true, $"Parameters: organization={organization}, project={project}, query=<missing>\n\nError: 'query' parameter is required.");
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

                bool timePrecision = false;
                if (parameters.TryGetValue("time_precision", out var timePrecisionObj) && timePrecisionObj is bool timePrecisionBool)
                {
                    timePrecision = timePrecisionBool;
                }

                
                string apiKey = _generalSettingsService.GetDecryptedAzureDevOpsPAT();
                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    return CreateResult(true, true, $"Parameters: organization={organization}, project={project}\n\nError: Azure DevOps PAT is not configured. Please set it in File > Settings > Set Azure DevOps PAT.");
                }

                
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", 
                    Convert.ToBase64String(Encoding.ASCII.GetBytes(string.Format("{0}:{1}", "", apiKey))));

                
                return await QueryWorkItemsAsync(organization, project, query, top, skip, timePrecision);
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

        private async Task<BuiltinToolResult> QueryWorkItemsAsync(string organization, string project, string query, int top, int skip, bool timePrecision)
        {
            try
            {
                SendStatusUpdate($"Executing WIQL query for {organization}/{project}...");
                
                
                var wiqlRequest = new
                {
                    query = query
                };
                
                var content = new StringContent(JsonConvert.SerializeObject(wiqlRequest), Encoding.UTF8, "application/json");
                
                
                string wiqlUrl = $"https://dev.azure.com/{organization}/{project}/_apis/wit/wiql";
                var wiqlResponse = await _httpClient.PostAsync(wiqlUrl, content);
                var wiqlContent = await wiqlResponse.Content.ReadAsStringAsync();
                
                if (!wiqlResponse.IsSuccessStatusCode)
                {
                    var errorObj = JObject.Parse(wiqlContent);
                    string errorMessage = errorObj["message"]?.ToString() ?? "Unknown error";
                    return CreateResult(true, true, $"Parameters: organization={organization}, project={project}\n\nAzure DevOps API Error: {errorMessage} (Status code: {wiqlResponse.StatusCode})");
                }
                
                
                var wiqlResult = JObject.Parse(wiqlContent);
                var workItemRefs = wiqlResult["workItems"] as JArray;
                
                if (workItemRefs == null || workItemRefs.Count == 0)
                {
                    return CreateResult(true, true, $"Parameters: organization={organization}, project={project}\n\n# Azure DevOps Work Items\n\nNo work items found matching the query criteria.");
                }
                
                
                var paginatedWorkItemRefs = workItemRefs
                    .Skip(skip)
                    .Take(top);
                
                
                var workItemIds = new List<int>();
                foreach (var workItemRef in paginatedWorkItemRefs)
                {
                    if (workItemRef["id"] != null && int.TryParse(workItemRef["id"].ToString(), out int id))
                    {
                        workItemIds.Add(id);
                    }
                }
                
                if (workItemIds.Count == 0)
                {
                    return CreateResult(true, true, $"Parameters: organization={organization}, project={project}\n\n# Azure DevOps Work Items\n\nNo valid work item IDs found in the query results.");
                }
                
                
                string idsParam = string.Join(",", workItemIds);
                string timePrecisionParam = timePrecision ? "&timePrecision=true" : "";
                string workItemsUrl = $"https://dev.azure.com/{organization}/{project}/_apis/wit/workitems?ids={idsParam}&$expand=all{timePrecisionParam}";
                
                var workItemsResponse = await _httpClient.GetAsync(workItemsUrl);
                var workItemsContent = await workItemsResponse.Content.ReadAsStringAsync();
                
                if (!workItemsResponse.IsSuccessStatusCode)
                {
                    var errorObj = JObject.Parse(workItemsContent);
                    string errorMessage = errorObj["message"]?.ToString() ?? "Unknown error";
                    return CreateResult(true, true, $"Parameters: organization={organization}, project={project}\n\nAzure DevOps API Error when fetching work items: {errorMessage} (Status code: {workItemsResponse.StatusCode})");
                }
                
                var formattedContent = FormatWorkItemsInfo(organization, project, workItemsContent, workItemRefs.Count, skip, top);
                
                SendStatusUpdate("Successfully retrieved work items information.");
                return CreateResult(true, true, $"Parameters: organization={organization}, project={project}, top={top}, skip={skip}, time_precision={timePrecision}\n\n{formattedContent}");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error fetching work items information");
                return CreateResult(true, true, $"Parameters: organization={organization}, project={project}\n\nError fetching work items information: {ex.Message}");
            }
        }

        private string FormatWorkItemsInfo(string organization, string project, string jsonContent, int totalCount, int skip, int top)
        {
            try
            {
                var workItemsData = JObject.Parse(jsonContent);
                var workItems = workItemsData["value"] as JArray;
                var sb = new StringBuilder();
                
                sb.AppendLine("# Azure DevOps Work Items");
                sb.AppendLine();
                
                if (workItems == null || workItems.Count == 0)
                {
                    sb.AppendLine("No work items found matching the criteria.");
                    return sb.ToString();
                }
                
                sb.AppendLine($"Found {totalCount} total work items, showing {workItems.Count} (skip={skip}, top={top}):\n");
                
                foreach (var workItem in workItems)
                {
                    var fields = workItem["fields"] as JObject;
                    if (fields == null) continue;
                    
                    
                    string id = workItem["id"]?.ToString() ?? "Unknown";
                    string title = fields["System.Title"]?.ToString() ?? "No Title";
                    string state = fields["System.State"]?.ToString() ?? "Unknown State";
                    string workItemType = fields["System.WorkItemType"]?.ToString() ?? "Unknown Type";
                    string assignedTo = fields["System.AssignedTo"]?["displayName"]?.ToString() ?? "Unassigned";
                    string createdDate = fields["System.CreatedDate"]?.ToString() ?? "Unknown";
                    string changedDate = fields["System.ChangedDate"]?.ToString() ?? "Unknown";
                    
                    sb.AppendLine($"## [{workItemType}] #{id} - {title}");
                    sb.AppendLine($"**State:** {state}");
                    sb.AppendLine($"**Assigned To:** {assignedTo}");
                    sb.AppendLine($"**Created:** {createdDate}");
                    sb.AppendLine($"**Last Updated:** {changedDate}");
                    
                    
                    if (fields["System.Description"] != null)
                    {
                        string description = fields["System.Description"].ToString();
                        if (!string.IsNullOrWhiteSpace(description))
                        {
                            sb.AppendLine("\n**Description:**");
                            sb.AppendLine(description);
                        }
                    }
                    
                    
                    if (fields["Microsoft.VSTS.Common.AcceptanceCriteria"] != null)
                    {
                        string acceptanceCriteria = fields["Microsoft.VSTS.Common.AcceptanceCriteria"].ToString();
                        if (!string.IsNullOrWhiteSpace(acceptanceCriteria))
                        {
                            sb.AppendLine("\n**Acceptance Criteria:**");
                            sb.AppendLine(acceptanceCriteria);
                        }
                    }
                    
                    
                    if (fields["Microsoft.VSTS.Common.Priority"] != null)
                    {
                        sb.AppendLine($"\n**Priority:** {fields["Microsoft.VSTS.Common.Priority"]}");
                    }
                    
                    if (fields["Microsoft.VSTS.Scheduling.Effort"] != null || 
                        fields["Microsoft.VSTS.Scheduling.StoryPoints"] != null || 
                        fields["Microsoft.VSTS.Scheduling.Size"] != null)
                    {
                        string effort = fields["Microsoft.VSTS.Scheduling.Effort"]?.ToString() ?? 
                                      fields["Microsoft.VSTS.Scheduling.StoryPoints"]?.ToString() ?? 
                                      fields["Microsoft.VSTS.Scheduling.Size"]?.ToString() ?? 
                                      "Not specified";
                        sb.AppendLine($"**Effort/Size:** {effort}");
                    }
                    
                    
                    if (fields["System.IterationPath"] != null)
                    {
                        sb.AppendLine($"**Iteration:** {fields["System.IterationPath"]}");
                    }
                    
                    if (fields["System.AreaPath"] != null)
                    {
                        sb.AppendLine($"**Area:** {fields["System.AreaPath"]}");
                    }
                    
                    
                    if (workItem["relations"] is JArray relations && relations.Count > 0)
                    {
                        var relatedItems = new List<string>();
                        foreach (var relation in relations)
                        {
                            string relationType = relation["rel"]?.ToString();
                            string url = relation["url"]?.ToString();
                            
                            if (!string.IsNullOrEmpty(relationType) && !string.IsNullOrEmpty(url))
                            {
                                
                                if (url.Contains("_apis/wit/workItems/"))
                                {
                                    string[] urlParts = url.Split('/');
                                    string relatedId = urlParts[urlParts.Length - 1];
                                    string relationName = GetRelationName(relationType);
                                    
                                    relatedItems.Add($"{relationName} #{relatedId}");
                                }
                            }
                        }
                        
                        if (relatedItems.Count > 0)
                        {
                            sb.AppendLine("\n**Related Work Items:**");
                            foreach (var item in relatedItems)
                            {
                                sb.AppendLine($"- {item}");
                            }
                        }
                    }
                    
                    
                    string projectName = fields["System.TeamProject"]?.ToString() ?? project;
                    sb.AppendLine($"\n**URL:** https://dev.azure.com/{organization}/{projectName}/_workitems/edit/{id}");
                    
                    sb.AppendLine();
                }
                
                return sb.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error formatting work items information");
                return $"Error formatting work items information: {ex.Message}\n\nRaw JSON:\n{jsonContent}";
            }
        }
        
        private string GetRelationName(string relationType)
        {
            switch (relationType)
            {
                case "System.LinkTypes.Hierarchy-Forward":
                    return "Parent of";
                case "System.LinkTypes.Hierarchy-Reverse":
                    return "Child of";
                case "System.LinkTypes.Related":
                    return "Related to";
                case "System.LinkTypes.Dependency-Forward":
                    return "Successor of";
                case "System.LinkTypes.Dependency-Reverse":
                    return "Predecessor of";
                default:
                    return relationType.Replace("System.LinkTypes.", "");
            }
        }

        [McpServerTool, Description("Executes a WIQL (Work Item Query Language) query to find work items matching specific criteria.")]
        public async Task<string> AzureDevOpsQueryWorkItems([Description("JSON parameters for AzureDevOpsQueryWorkItems")] string parameters = "{}")
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
