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

namespace AiStudio4.Core.Tools.AzureDevOps
{
    /// <summary>
    /// Implementation of the Azure DevOps Get Work Item Updates tool
    /// </summary>
    public class AzureDevOpsGetWorkItemUpdatesTool : BaseToolImplementation
    {
        private readonly HttpClient _httpClient;

        public AzureDevOpsGetWorkItemUpdatesTool(ILogger<AzureDevOpsGetWorkItemUpdatesTool> logger, IGeneralSettingsService generalSettingsService, IStatusMessageService statusMessageService) 
            : base(logger, generalSettingsService, statusMessageService)
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "AiStudio4-AzureDevOps-Tool");
        }

        /// <summary>
        /// Gets the Azure DevOps Get Work Item Updates tool definition
        /// </summary>
        public override Tool GetToolDefinition()
        {
            return new Tool
            {
                Guid = ToolGuids.AZURE_DEV_OPS_GET_WORK_ITEM_UPDATES_TOOL_GUID,
                Name = "AzureDevOpsGetWorkItemUpdates",
                Description = "Retrieves the update history for a specific work item in Azure DevOps.",
                Schema = """
{
  "name": "AzureDevOpsGetWorkItemUpdates",
  "description": "Retrieves the update history for a specific work item in Azure DevOps.",
  "input_schema": {
    "properties": {
      "organization": { "title": "Organization", "type": "string", "description": "The Azure DevOps organization name" },
      "project": { "title": "Project", "type": "string", "description": "The Azure DevOps project name" },
      "id": { "title": "Work Item ID", "type": "integer", "description": "The ID of the work item to retrieve updates for" },
      "top": { "title": "Top", "type": "integer", "description": "Number of updates to return", "default": 100 },
      "skip": { "title": "Skip", "type": "integer", "description": "Number of updates to skip", "default": 0 }
    },
    "required": ["organization", "project", "id"],
    "title": "AzureDevOpsGetWorkItemUpdatesArguments",
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
                SendStatusUpdate("Starting Azure DevOps Get Work Item Updates tool execution...");
                var parameters = JsonConvert.DeserializeObject<Dictionary<string, object>>(toolParameters) ?? new Dictionary<string, object>();

                // Extract required parameters
                if (!parameters.TryGetValue("organization", out var organizationObj) || !(organizationObj is string organization) || string.IsNullOrWhiteSpace(organization))
                {
                    return CreateResult(true, true, $"Parameters: organization=<missing>, project=<unknown>, id=<unknown>\n\nError: 'organization' parameter is required.");
                }

                if (!parameters.TryGetValue("project", out var projectObj) || !(projectObj is string project) || string.IsNullOrWhiteSpace(project))
                {
                    return CreateResult(true, true, $"Parameters: organization={organization}, project=<missing>, id=<unknown>\n\nError: 'project' parameter is required.");
                }

                if (!parameters.TryGetValue("id", out var idObj) || !(idObj is long || idObj is int))
                {
                    return CreateResult(true, true, $"Parameters: organization={organization}, project={project}, id=<missing>\n\nError: 'id' parameter is required and must be an integer.");
                }

                int workItemId;
                if (idObj is long idLong)
                {
                    workItemId = (int)idLong;
                }
                else
                {
                    workItemId = (int)idObj;
                }

                // Extract optional parameters
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
                    return CreateResult(true, true, $"Parameters: organization={organization}, project={project}, id={workItemId}\n\nError: Azure DevOps PAT is not configured. Please set it in File > Settings > Set Azure DevOps PAT.");
                }

                // Set up authentication header
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", 
                    Convert.ToBase64String(Encoding.ASCII.GetBytes(string.Format("{0}:{1}", "", apiKey))));

                // Make the API request
                return await GetWorkItemUpdatesAsync(organization, project, workItemId, top, skip);
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

        private async Task<BuiltinToolResult> GetWorkItemUpdatesAsync(string organization, string project, int workItemId, int top, int skip)
        {
            try
            {
                SendStatusUpdate($"Fetching work item updates for {organization}/{project}/workitem/{workItemId}...");
                
                // Build query parameters
                var queryParams = new List<string>();
                queryParams.Add($"$top={top}");
                queryParams.Add($"$skip={skip}");
                
                string queryString = queryParams.Count > 0 ? $"?{string.Join("&", queryParams)}" : "";
                string url = $"https://dev.azure.com/{organization}/{project}/_apis/wit/workitems/{workItemId}/updates{queryString}";
                
                var response = await _httpClient.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorObj = JObject.Parse(content);
                    string errorMessage = errorObj["message"]?.ToString() ?? "Unknown error";
                    return CreateResult(true, true, $"Parameters: organization={organization}, project={project}, id={workItemId}\n\nAzure DevOps API Error: {errorMessage} (Status code: {response.StatusCode})");
                }
                
                var formattedContent = FormatWorkItemUpdatesInfo(content, workItemId);
                
                SendStatusUpdate("Successfully retrieved work item updates information.");
                return CreateResult(true, true, $"Parameters: organization={organization}, project={project}, id={workItemId}, top={top}, skip={skip}\n\n{formattedContent}");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error fetching work item updates information");
                return CreateResult(true, true, $"Parameters: organization={organization}, project={project}, id={workItemId}\n\nError fetching work item updates information: {ex.Message}");
            }
        }

        private string FormatWorkItemUpdatesInfo(string jsonContent, int workItemId)
        {
            try
            {
                var updatesData = JObject.Parse(jsonContent);
                var updates = updatesData["value"] as JArray;
                var sb = new StringBuilder();
                
                sb.AppendLine($"# Azure DevOps Work Item #{workItemId} Update History");
                sb.AppendLine();
                
                if (updates == null || updates.Count == 0)
                {
                    sb.AppendLine("No updates found for this work item.");
                    return sb.ToString();
                }
                
                sb.AppendLine($"Found {updates.Count} updates:\n");
                
                foreach (var update in updates)
                {
                    var updateId = update["id"];
                    var revisedBy = update["revisedBy"]?["displayName"] ?? "Unknown";
                    var revisedDate = update["revisedDate"];
                    
                    sb.AppendLine($"## Update {updateId} - {revisedDate}");
                    sb.AppendLine($"**Updated by:** {revisedBy}");
                    
                    // Check for field updates
                    if (update["fields"] != null && update["fields"].HasValues)
                    {
                        sb.AppendLine("\n**Field Updates:**");
                        
                        foreach (var field in update["fields"].Children<JProperty>())
                        {
                            string fieldName = field.Name;
                            var fieldValue = field.Value;
                            
                            // Get old and new values if available
                            string oldValue = fieldValue["oldValue"]?.ToString() ?? "<none>";
                            string newValue = fieldValue["newValue"]?.ToString() ?? "<none>";
                            
                            sb.AppendLine($"- **{fieldName}**: Changed from '{oldValue}' to '{newValue}'");
                        }
                    }
                    
                    // Check for relation changes
                    if (update["relations"] != null && update["relations"].HasValues)
                    {
                        sb.AppendLine("\n**Relation Changes:**");
                        
                        foreach (var relation in update["relations"].Children<JProperty>())
                        {
                            string relationType = relation.Name;
                            var relationValue = relation.Value;
                            
                            if (relationValue["added"] != null && relationValue["added"].HasValues)
                            {
                                sb.AppendLine($"- **{relationType}**: Added relationships:");
                                foreach (var added in relationValue["added"])
                                {
                                    string url = added["url"]?.ToString() ?? "<unknown>";
                                    sb.AppendLine($"  - Added: {url}");
                                }
                            }
                            
                            if (relationValue["removed"] != null && relationValue["removed"].HasValues)
                            {
                                sb.AppendLine($"- **{relationType}**: Removed relationships:");
                                foreach (var removed in relationValue["removed"])
                                {
                                    string url = removed["url"]?.ToString() ?? "<unknown>";
                                    sb.AppendLine($"  - Removed: {url}");
                                }
                            }
                        }
                    }
                    
                    // Check for comment updates
                    if (update["workItemComments"] != null)
                    {
                        var comments = update["workItemComments"]["comments"] as JArray;
                        if (comments != null && comments.Count > 0)
                        {
                            sb.AppendLine("\n**Comments:**");
                            foreach (var comment in comments)
                            {
                                string commentText = comment["text"]?.ToString() ?? "<empty comment>";
                                string commentBy = comment["revisedBy"]?["displayName"].ToString() ?? "Unknown";
                                string commentDate = comment["revisedDate"]?.ToString() ?? "Unknown date";
                                
                                sb.AppendLine($"- **{commentBy}** on {commentDate}:\n  \"{commentText}\"\n");
                            }
                        }
                    }
                    
                    sb.AppendLine();
                }
                
                return sb.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error formatting work item updates information");
                return $"Error formatting work item updates information: {ex.Message}\n\nRaw JSON:\n{jsonContent}";
            }
        }
    }
}
