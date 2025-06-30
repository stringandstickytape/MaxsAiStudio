// AiStudio4\Core\Tools\AzureDevOps\AzureDevOpsGetPipelineRunsTool.cs

using AiStudio4.Core.Interfaces;
using AiStudio4.Core.Models;
using AiStudio4.InjectedDependencies;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace AiStudio4.Core.Tools.AzureDevOps
{
    /// <summary>
    /// Implementation of the Azure DevOps Get Pipeline Runs tool
    /// </summary>
    public class AzureDevOpsGetPipelineRunsTool : BaseToolImplementation
    {
        private readonly HttpClient _httpClient;

        public AzureDevOpsGetPipelineRunsTool(ILogger<AzureDevOpsGetPipelineRunsTool> logger, IGeneralSettingsService generalSettingsService, IStatusMessageService statusMessageService) 
            : base(logger, generalSettingsService, statusMessageService)
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "AiStudio4-AzureDevOps-Tool");
        }

        /// <summary>
        /// Gets the Azure DevOps Get Pipeline Runs tool definition
        /// </summary>
        public override Tool GetToolDefinition()
        {
            return new Tool
            {
                Guid = ToolGuids.AZURE_DEV_OPS_GET_PIPELINE_RUNS_TOOL_GUID,
                Name = "AzureDevOpsGetPipelineRuns",
                Description = "Retrieves pipeline execution data including builds, releases, deployment history, and associated artifacts and test results.",
                Schema = """
{
  "name": "AzureDevOpsGetPipelineRuns",
  "description": "Retrieves pipeline execution data including builds, releases, deployment history, and associated artifacts and test results.",
  "input_schema": {
    "properties": {
      "task_description": { "title": "Task Description", "type": "string", "description": "A concise, user-facing description of the task you are performing with this tool call." },
      "organization": { "title": "Organization", "type": "string", "description": "The Azure DevOps organization name" },
      "project": { "title": "Project", "type": "string", "description": "The Azure DevOps project name" },
      "run_type": { "title": "Run Type", "type": "string", "enum": ["build", "release", "yaml", "all"], "default": "all", "description": "Type of pipeline runs to retrieve" },
      "definition_id": { "title": "Definition ID", "type": "integer", "description": "Filter by specific pipeline definition ID" },
      "run_id": { "title": "Run ID", "type": "integer", "description": "Specific run ID (for single run details)" },
      "status_filter": { "title": "Status Filter", "type": "array", "items": { "type": "string", "enum": ["inProgress", "completed", "cancelling", "postponed", "notStarted", "all"] }, "default": ["all"], "description": "Filter by run status" },
      "result_filter": { "title": "Result Filter", "type": "array", "items": { "type": "string", "enum": ["succeeded", "partiallySucceeded", "failed", "canceled", "all"] }, "default": ["all"], "description": "Filter by run result (for completed runs)" },
      "branch_name": { "title": "Branch Name", "type": "string", "description": "Filter by source branch" },
      "from_date": { "title": "From Date", "type": "string", "description": "Filter runs from this date (ISO 8601 format)" },
      "to_date": { "title": "To Date", "type": "string", "description": "Filter runs until this date (ISO 8601 format)" },
      "include_logs": { "title": "Include Logs", "type": "boolean", "default": false, "description": "Include summary logs and error details" },
      "include_artifacts": { "title": "Include Artifacts", "type": "boolean", "default": false, "description": "Include artifact information" },
      "include_tests": { "title": "Include Tests", "type": "boolean", "default": false, "description": "Include test results and coverage data" },
      "include_timeline": { "title": "Include Timeline", "type": "boolean", "default": false, "description": "Include detailed task timeline and durations" },
      "top": { "title": "Top", "type": "integer", "default": 100, "description": "Number of runs to return" },
      "skip": { "title": "Skip", "type": "integer", "default": 0, "description": "Number of runs to skip" }
    },
    "required": ["task_description", "organization", "project"],
    "title": "AzureDevOpsGetPipelineRunsArguments",
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
                SendStatusUpdate("Starting Azure DevOps Get Pipeline Runs tool execution...");
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
                string runType = "all";
                if (parameters.TryGetValue("run_type", out var runTypeObj) && runTypeObj is string runTypeStr && !string.IsNullOrWhiteSpace(runTypeStr))
                {
                    runType = runTypeStr;
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

                int? runId = null;
                if (parameters.TryGetValue("run_id", out var runIdObj))
                {
                    if (runIdObj is long runIdLong)
                    {
                        runId = (int)runIdLong;
                    }
                    else if (runIdObj is int runIdInt)
                    {
                        runId = runIdInt;
                    }
                }

                List<string> statusFilter = new List<string> { "all" };
                if (parameters.TryGetValue("status_filter", out var statusFilterObj) && statusFilterObj is JArray statusFilterArray)
                {
                    statusFilter = statusFilterArray.Select(x => x.ToString()).ToList();
                }

                List<string> resultFilter = new List<string> { "all" };
                if (parameters.TryGetValue("result_filter", out var resultFilterObj) && resultFilterObj is JArray resultFilterArray)
                {
                    resultFilter = resultFilterArray.Select(x => x.ToString()).ToList();
                }

                string branchName = null;
                if (parameters.TryGetValue("branch_name", out var branchNameObj) && branchNameObj is string branchNameStr && !string.IsNullOrWhiteSpace(branchNameStr))
                {
                    branchName = branchNameStr;
                }

                string fromDate = null;
                if (parameters.TryGetValue("from_date", out var fromDateObj) && fromDateObj is string fromDateStr && !string.IsNullOrWhiteSpace(fromDateStr))
                {
                    fromDate = fromDateStr;
                }

                string toDate = null;
                if (parameters.TryGetValue("to_date", out var toDateObj) && toDateObj is string toDateStr && !string.IsNullOrWhiteSpace(toDateStr))
                {
                    toDate = toDateStr;
                }

                bool includeLogs = false;
                if (parameters.TryGetValue("include_logs", out var includeLogsObj) && includeLogsObj is bool includeLogsBool)
                {
                    includeLogs = includeLogsBool;
                }

                bool includeArtifacts = false;
                if (parameters.TryGetValue("include_artifacts", out var includeArtifactsObj) && includeArtifactsObj is bool includeArtifactsBool)
                {
                    includeArtifacts = includeArtifactsBool;
                }

                bool includeTests = false;
                if (parameters.TryGetValue("include_tests", out var includeTestsObj) && includeTestsObj is bool includeTestsBool)
                {
                    includeTests = includeTestsBool;
                }

                bool includeTimeline = false;
                if (parameters.TryGetValue("include_timeline", out var includeTimelineObj) && includeTimelineObj is bool includeTimelineBool)
                {
                    includeTimeline = includeTimelineBool;
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
                return await GetPipelineRunsAsync(organization, project, runType, definitionId, runId, statusFilter, 
                    resultFilter, branchName, fromDate, toDate, includeLogs, includeArtifacts, includeTests, includeTimeline, top, skip);
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

        private async Task<BuiltinToolResult> GetPipelineRunsAsync(string organization, string project, string runType, 
            int? definitionId, int? runId, List<string> statusFilter, List<string> resultFilter, string branchName, 
            string fromDate, string toDate, bool includeLogs, bool includeArtifacts, bool includeTests, bool includeTimeline, int top, int skip)
        {
            try
            {
                SendStatusUpdate($"Fetching pipeline runs for {organization}/{project}...");
                
                var allRuns = new List<JObject>();
                
                // Get build runs if requested
                if (runType == "all" || runType == "build")
                {
                    var buildRuns = await GetBuildRunsAsync(organization, project, definitionId, runId, statusFilter, 
                        resultFilter, branchName, fromDate, toDate, includeLogs, includeArtifacts, includeTests, includeTimeline, top, skip);
                    allRuns.AddRange(buildRuns);
                }
                
                // Get release runs if requested
                if (runType == "all" || runType == "release")
                {
                    var releaseRuns = await GetReleaseRunsAsync(organization, project, definitionId, runId, statusFilter, 
                        resultFilter, branchName, fromDate, toDate, includeLogs, includeArtifacts, includeTests, includeTimeline, top, skip);
                    allRuns.AddRange(releaseRuns);
                }
                
                // Get YAML pipeline runs if requested
                if (runType == "all" || runType == "yaml")
                {
                    var yamlRuns = await GetYamlPipelineRunsAsync(organization, project, definitionId, runId, statusFilter, 
                        resultFilter, branchName, fromDate, toDate, includeLogs, includeArtifacts, includeTests, includeTimeline, top, skip);
                    allRuns.AddRange(yamlRuns);
                }
                
                var formattedContent = FormatPipelineRunsInfo(allRuns, runType);
                
                SendStatusUpdate("Successfully retrieved pipeline runs information.");
                return CreateResult(true, true, $"Parameters: organization={organization}, project={project}, run_type={runType}\n\n{formattedContent}");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error fetching pipeline runs information");
                return CreateResult(true, true, $"Parameters: organization={organization}, project={project}\n\nError fetching pipeline runs information: {ex.Message}");
            }
        }

        private async Task<List<JObject>> GetBuildRunsAsync(string organization, string project, int? definitionId, int? runId, 
            List<string> statusFilter, List<string> resultFilter, string branchName, string fromDate, string toDate, 
            bool includeLogs, bool includeArtifacts, bool includeTests, bool includeTimeline, int top, int skip)
        {
            var queryParams = new List<string>();
            
            if (definitionId.HasValue)
            {
                queryParams.Add($"definitions={definitionId.Value}");
            }
            
            if (runId.HasValue)
            {
                queryParams.Add($"buildIds={runId.Value}");
            }
            
            if (!string.IsNullOrEmpty(branchName))
            {
                queryParams.Add($"branchName={Uri.EscapeDataString(branchName)}");
            }
            
            if (!string.IsNullOrEmpty(fromDate))
            {
                queryParams.Add($"minTime={Uri.EscapeDataString(fromDate)}");
            }
            
            if (!string.IsNullOrEmpty(toDate))
            {
                queryParams.Add($"maxTime={Uri.EscapeDataString(toDate)}");
            }
            
            if (statusFilter != null && statusFilter.Count > 0 && !statusFilter.Contains("all"))
            {
                queryParams.Add($"statusFilter={string.Join(",", statusFilter)}");
            }
            
            if (resultFilter != null && resultFilter.Count > 0 && !resultFilter.Contains("all"))
            {
                queryParams.Add($"resultFilter={string.Join(",", resultFilter)}");
            }
            
            queryParams.Add($"$top={top}");
            queryParams.Add($"$skip={skip}");
            
            string queryString = queryParams.Count > 0 ? $"?{string.Join("&", queryParams)}" : "";
            string url = $"https://dev.azure.com/{organization}/{project}/_apis/build/builds{queryString}";
            
            var response = await _httpClient.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to get build runs: {StatusCode} - {Content}", response.StatusCode, content);
                return new List<JObject>();
            }
            
            var buildData = JObject.Parse(content);
            var runs = buildData["value"] as JArray ?? new JArray();
            
            var result = new List<JObject>();
            foreach (var run in runs)
            {
                var runObj = run as JObject;
                if (runObj != null)
                {
                    runObj["type"] = "build";
                    result.Add(runObj);
                }
            }
            
            return result;
        }

        private async Task<List<JObject>> GetReleaseRunsAsync(string organization, string project, int? definitionId, int? runId, 
            List<string> statusFilter, List<string> resultFilter, string branchName, string fromDate, string toDate, 
            bool includeLogs, bool includeArtifacts, bool includeTests, bool includeTimeline, int top, int skip)
        {
            var queryParams = new List<string>();
            
            if (definitionId.HasValue)
            {
                queryParams.Add($"definitionId={definitionId.Value}");
            }
            
            if (runId.HasValue)
            {
                queryParams.Add($"releaseIdFilter={runId.Value}");
            }
            
            if (!string.IsNullOrEmpty(fromDate))
            {
                queryParams.Add($"minCreatedTime={Uri.EscapeDataString(fromDate)}");
            }
            
            if (!string.IsNullOrEmpty(toDate))
            {
                queryParams.Add($"maxCreatedTime={Uri.EscapeDataString(toDate)}");
            }
            
            queryParams.Add($"$top={top}");
            queryParams.Add($"$skip={skip}");
            
            string queryString = queryParams.Count > 0 ? $"?{string.Join("&", queryParams)}" : "";
            string url = $"https://vsrm.dev.azure.com/{organization}/{project}/_apis/release/releases{queryString}";
            
            var response = await _httpClient.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to get release runs: {StatusCode} - {Content}", response.StatusCode, content);
                return new List<JObject>();
            }
            
            var releaseData = JObject.Parse(content);
            var runs = releaseData["value"] as JArray ?? new JArray();
            
            var result = new List<JObject>();
            foreach (var run in runs)
            {
                var runObj = run as JObject;
                if (runObj != null)
                {
                    runObj["type"] = "release";
                    result.Add(runObj);
                }
            }
            
            return result;
        }

        private async Task<List<JObject>> GetYamlPipelineRunsAsync(string organization, string project, int? definitionId, int? runId, 
            List<string> statusFilter, List<string> resultFilter, string branchName, string fromDate, string toDate, 
            bool includeLogs, bool includeArtifacts, bool includeTests, bool includeTimeline, int top, int skip)
        {
            var queryParams = new List<string>();
            
            if (definitionId.HasValue)
            {
                queryParams.Add($"pipelineId={definitionId.Value}");
            }
            
            if (runId.HasValue)
            {
                queryParams.Add($"runId={runId.Value}");
            }
            
            queryParams.Add($"$top={top}");
            queryParams.Add($"$skip={skip}");
            
            string queryString = queryParams.Count > 0 ? $"?{string.Join("&", queryParams)}" : "";
            string url = $"https://dev.azure.com/{organization}/{project}/_apis/pipelines/runs{queryString}";
            
            var response = await _httpClient.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to get YAML pipeline runs: {StatusCode} - {Content}", response.StatusCode, content);
                return new List<JObject>();
            }
            
            var pipelineData = JObject.Parse(content);
            var runs = pipelineData["value"] as JArray ?? new JArray();
            
            var result = new List<JObject>();
            foreach (var run in runs)
            {
                var runObj = run as JObject;
                if (runObj != null)
                {
                    runObj["type"] = "yaml";
                    result.Add(runObj);
                }
            }
            
            return result;
        }

        private string FormatPipelineRunsInfo(List<JObject> runs, string runType)
        {
            try
            {
                var sb = new StringBuilder();
                
                sb.AppendLine("# Azure DevOps Pipeline Runs");
                sb.AppendLine();
                
                if (runs.Count == 0)
                {
                    sb.AppendLine($"No {runType} pipeline runs found matching the criteria.");
                    return sb.ToString();
                }
                
                sb.AppendLine($"Found {runs.Count} pipeline runs:\n");
                
                foreach (var run in runs)
                {
                    string type = run["type"]?.ToString() ?? "unknown";
                    string id = run["id"]?.ToString() ?? "Unknown ID";
                    string buildNumber = run["buildNumber"]?.ToString() ?? run["name"]?.ToString() ?? "Unknown";
                    
                    sb.AppendLine($"## [{type.ToUpper()}] {buildNumber} (ID: {id})");
                    
                    if (run["definition"] != null)
                    {
                        sb.AppendLine($"**Definition:** {run["definition"]["name"]} (ID: {run["definition"]["id"]})");
                    }
                    
                    if (run["status"] != null)
                    {
                        sb.AppendLine($"**Status:** {run["status"]}");
                    }
                    
                    if (run["result"] != null)
                    {
                        sb.AppendLine($"**Result:** {run["result"]}");
                    }
                    
                    if (run["queueTime"] != null)
                    {
                        sb.AppendLine($"**Queued:** {run["queueTime"]}");
                    }
                    
                    if (run["startTime"] != null)
                    {
                        sb.AppendLine($"**Started:** {run["startTime"]}");
                    }
                    
                    if (run["finishTime"] != null)
                    {
                        sb.AppendLine($"**Finished:** {run["finishTime"]}");
                    }
                    
                    if (run["sourceBranch"] != null)
                    {
                        sb.AppendLine($"**Source Branch:** {run["sourceBranch"]}");
                    }
                    
                    if (run["sourceVersion"] != null)
                    {
                        string shortVersion = run["sourceVersion"].ToString();
                        if (shortVersion.Length > 8)
                        {
                            shortVersion = shortVersion.Substring(0, 8);
                        }
                        sb.AppendLine($"**Source Version:** {shortVersion}");
                    }
                    
                    if (run["requestedBy"] != null)
                    {
                        sb.AppendLine($"**Requested By:** {run["requestedBy"]["displayName"]}");
                    }
                    
                    if (run["requestedFor"] != null)
                    {
                        sb.AppendLine($"**Requested For:** {run["requestedFor"]["displayName"]}");
                    }
                    
                    if (run["reason"] != null)
                    {
                        sb.AppendLine($"**Reason:** {run["reason"]}");
                    }
                    
                    // Calculate duration if both start and finish times are available
                    if (run["startTime"] != null && run["finishTime"] != null)
                    {
                        if (DateTime.TryParse(run["startTime"].ToString(), out DateTime startTime) && 
                            DateTime.TryParse(run["finishTime"].ToString(), out DateTime finishTime))
                        {
                            var duration = finishTime - startTime;
                            sb.AppendLine($"**Duration:** {duration:hh\\:mm\\:ss}");
                        }
                    }
                    
                    if (run["_links"] != null && run["_links"]["web"] != null)
                    {
                        sb.AppendLine($"\n**Web URL:** {run["_links"]["web"]["href"]}");
                    }
                    
                    sb.AppendLine("\n---\n");
                }
                
                return sb.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error formatting pipeline runs information");
                return $"Error formatting pipeline runs information: {ex.Message}\n\nRaw data: {JsonConvert.SerializeObject(runs, Formatting.Indented)}";
            }
        }
    }
}