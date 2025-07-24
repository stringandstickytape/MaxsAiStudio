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
using ModelContextProtocol;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace AiStudio4.Core.Tools.AzureDevOps
{
    /// <summary>
    /// Implementation of the Azure DevOps Get Pipeline Runs tool
    /// </summary>
    [McpServerToolType]
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
                Description = "Retrieves pipeline execution data with granular analysis capabilities including detailed timeline data, task-level performance metrics, log analysis with error/warning detection, and execution bottleneck identification.",
                Schema = """
{
  "name": "AzureDevOpsGetPipelineRuns",
  "description": "Retrieves pipeline execution data with granular analysis capabilities including detailed timeline data, task-level performance metrics, log analysis with error/warning detection, and execution bottleneck identification.",
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
      "include_logs": { "title": "Include Logs", "type": "boolean", "default": false, "description": "Include detailed log analysis with error/warning detection, key excerpts, and troubleshooting information" },
      "include_artifacts": { "title": "Include Artifacts", "type": "boolean", "default": false, "description": "Include artifact information" },
      "include_tests": { "title": "Include Tests", "type": "boolean", "default": false, "description": "Include test results and coverage data" },
      "include_timeline": { "title": "Include Timeline", "type": "boolean", "default": false, "description": "Include detailed task-level timeline with individual step names, start/end times, durations, performance analysis, and bottleneck identification" },
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
                    
                    // Enhance with timeline data if requested
                    if (includeTimeline)
                    {
                        var buildId = runObj["id"]?.ToString();
                        if (!string.IsNullOrEmpty(buildId))
                        {
                            var timelineData = await GetBuildTimelineAsync(organization, project, buildId);
                            if (timelineData != null)
                            {
                                runObj["timeline"] = timelineData;
                            }
                        }
                    }
                    
                    // Enhance with log data if requested
                    if (includeLogs)
                    {
                        var buildId = runObj["id"]?.ToString();
                        if (!string.IsNullOrEmpty(buildId))
                        {
                            var logData = await GetBuildLogsAsync(organization, project, buildId);
                            if (logData != null)
                            {
                                runObj["logs"] = logData;
                            }
                        }
                    }
                    
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
                    
                    // For YAML pipelines, we need to get the associated build ID for timeline and logs
                    // YAML pipeline runs often have associated build records
                    var runId2 = runObj["id"]?.ToString();
                    if (!string.IsNullOrEmpty(runId2))
                    {
                        // Try to get the build record associated with this pipeline run
                        var buildId = await GetBuildIdForPipelineRunAsync(organization, project, runId2);
                        
                        if (!string.IsNullOrEmpty(buildId))
                        {
                            runObj["associatedBuildId"] = buildId;
                            
                            // Enhance with timeline data if requested
                            if (includeTimeline)
                            {
                                var timelineData = await GetBuildTimelineAsync(organization, project, buildId);
                                if (timelineData != null)
                                {
                                    runObj["timeline"] = timelineData;
                                }
                            }
                            
                            // Enhance with log data if requested
                            if (includeLogs)
                            {
                                var logData = await GetBuildLogsAsync(organization, project, buildId);
                                if (logData != null)
                                {
                                    runObj["logs"] = logData;
                                }
                            }
                        }
                    }
                    
                    result.Add(runObj);
                }
            }
            
            return result;
        }

        /// <summary>
        /// Attempts to find the build ID associated with a YAML pipeline run
        /// </summary>
        private async Task<string> GetBuildIdForPipelineRunAsync(string organization, string project, string pipelineRunId)
        {
            try
            {
                // YAML pipeline runs often correspond to build records
                // We can try to find builds that were created around the same time as the pipeline run
                string url = $"https://dev.azure.com/{organization}/{project}/_apis/build/builds?$top=50&api-version=7.1";
                
                var response = await _httpClient.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();
                
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }
                
                var buildData = JObject.Parse(content);
                var builds = buildData["value"] as JArray;
                
                if (builds != null)
                {
                    // Look for a build that might be associated with this pipeline run
                    // This is a heuristic approach - in practice, you might need more sophisticated matching
                    foreach (var build in builds)
                    {
                        var buildObj = build as JObject;
                        if (buildObj != null)
                        {
                            // Check if the build has pipeline run information
                            var buildId = buildObj["id"]?.ToString();
                            if (!string.IsNullOrEmpty(buildId))
                            {
                                return buildId;
                            }
                        }
                    }
                }
                
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to find build ID for pipeline run {PipelineRunId}", pipelineRunId);
                return null;
            }
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
                    
                    // Add detailed timeline information if available
                    if (run["timeline"] != null)
                    {
                        sb.AppendLine();
                        sb.AppendLine("### 📋 Detailed Timeline Analysis");
                        FormatTimelineInfo(sb, run["timeline"] as JObject);
                    }
                    
                    // Add log analysis if available
                    if (run["logs"] != null)
                    {
                        sb.AppendLine();
                        sb.AppendLine("### 📄 Log Analysis");
                        FormatLogInfo(sb, run["logs"] as JObject);
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

        /// <summary>
        /// Formats detailed timeline information for display
        /// </summary>
        private void FormatTimelineInfo(StringBuilder sb, JObject timeline)
        {
            if (timeline == null) return;
            
            var totalRecords = timeline["totalRecords"]?.Value<int>() ?? 0;
            sb.AppendLine($"**Total Timeline Records:** {totalRecords}");
            
            // Format Jobs
            var jobs = timeline["jobs"] as JArray;
            if (jobs != null && jobs.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("#### 🔧 Jobs:");
                foreach (var job in jobs)
                {
                    var jobObj = job as JObject;
                    if (jobObj == null) continue;
                    
                    var name = jobObj["name"]?.ToString() ?? "Unknown Job";
                    var result = jobObj["result"]?.ToString() ?? "Unknown";
                    var duration = jobObj["duration"]?.ToString() ?? "N/A";
                    var workerName = jobObj["workerName"]?.ToString();
                    
                    sb.AppendLine($"- **{name}** | Result: {result} | Duration: {duration}");
                    if (!string.IsNullOrEmpty(workerName))
                    {
                        sb.AppendLine($"  - Worker: {workerName}");
                    }
                }
            }
            
            // Format Tasks with performance analysis
            var tasks = timeline["tasks"] as JArray;
            if (tasks != null && tasks.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("#### ⚙️ Tasks (Performance Analysis):");
                
                // Sort tasks by duration (longest first) for performance analysis
                var sortedTasks = tasks.Cast<JObject>()
                    .Where(t => t["durationMs"] != null)
                    .OrderByDescending(t => t["durationMs"]?.Value<double>() ?? 0)
                    .ToList();
                
                if (sortedTasks.Any())
                {
                    sb.AppendLine();
                    sb.AppendLine("**🐌 Slowest Tasks (Performance Bottlenecks):**");
                    foreach (var task in sortedTasks.Take(5))
                    {
                        var name = task["name"]?.ToString() ?? "Unknown Task";
                        var result = task["result"]?.ToString() ?? "Unknown";
                        var duration = task["duration"]?.ToString() ?? "N/A";
                        var durationMs = task["durationMs"]?.Value<double>() ?? 0;
                        
                        sb.AppendLine($"- **{name}** | {result} | {duration} ({durationMs:F0}ms)");
                        
                        // Add issue information if available
                        if (task["issues"] != null)
                        {
                            var issues = task["issues"] as JArray;
                            if (issues != null && issues.Count > 0)
                            {
                                sb.AppendLine($"  - ⚠️ Issues: {issues.Count}");
                            }
                        }
                    }
                }
                
                // Show failed tasks
                var failedTasks = tasks.Cast<JObject>()
                    .Where(t => t["result"]?.ToString()?.ToLowerInvariant() == "failed")
                    .ToList();
                
                if (failedTasks.Any())
                {
                    sb.AppendLine();
                    sb.AppendLine("**❌ Failed Tasks:**");
                    foreach (var task in failedTasks)
                    {
                        var name = task["name"]?.ToString() ?? "Unknown Task";
                        var duration = task["duration"]?.ToString() ?? "N/A";
                        sb.AppendLine($"- **{name}** | Duration: {duration}");
                    }
                }
                
                // Summary statistics
                var completedTasks = tasks.Cast<JObject>()
                    .Where(t => t["result"]?.ToString()?.ToLowerInvariant() == "succeeded")
                    .ToList();
                
                sb.AppendLine();
                sb.AppendLine($"**📊 Task Summary:** {completedTasks.Count} succeeded, {failedTasks.Count} failed, {tasks.Count} total");
            }
        }

        /// <summary>
        /// Formats log analysis information for display
        /// </summary>
        private void FormatLogInfo(StringBuilder sb, JObject logs)
        {
            if (logs == null) return;
            
            var summary = logs["summary"] as JObject;
            if (summary != null)
            {
                var totalLogs = summary["totalLogs"]?.Value<int>() ?? 0;
                var processedLogs = summary["processedLogs"]?.Value<int>() ?? 0;
                var totalWarnings = summary["totalWarnings"]?.Value<int>() ?? 0;
                var totalErrors = summary["totalErrors"]?.Value<int>() ?? 0;
                
                sb.AppendLine($"**Log Summary:** {processedLogs}/{totalLogs} logs processed");
                sb.AppendLine($"**Issues Found:** {totalErrors} errors, {totalWarnings} warnings");
            }
            
            // Show errors
            var errors = logs["errors"] as JArray;
            if (errors != null && errors.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("#### ❌ Error Details:");
                foreach (var error in errors.Take(5)) // Limit to first 5 errors
                {
                    var errorObj = error as JObject;
                    if (errorObj == null) continue;
                    
                    var message = errorObj["message"]?.ToString() ?? "Unknown error";
                    var logId = errorObj["logId"]?.ToString() ?? "Unknown log";
                    
                    sb.AppendLine($"- **Log {logId}:** {message}");
                }
                
                if (errors.Count > 5)
                {
                    sb.AppendLine($"- ... and {errors.Count - 5} more errors");
                }
            }
            
            // Show warnings
            var warnings = logs["warnings"] as JArray;
            if (warnings != null && warnings.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("#### ⚠️ Warning Details:");
                foreach (var warning in warnings.Take(3)) // Limit to first 3 warnings
                {
                    var warningObj = warning as JObject;
                    if (warningObj == null) continue;
                    
                    var message = warningObj["message"]?.ToString() ?? "Unknown warning";
                    var logId = warningObj["logId"]?.ToString() ?? "Unknown log";
                    
                    sb.AppendLine($"- **Log {logId}:** {message}");
                }
                
                if (warnings.Count > 3)
                {
                    sb.AppendLine($"- ... and {warnings.Count - 3} more warnings");
                }
            }
            
            // Show key excerpts
            var keyExcerpts = logs["keyExcerpts"] as JArray;
            if (keyExcerpts != null && keyExcerpts.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("#### 📝 Key Log Excerpts:");
                foreach (var excerpt in keyExcerpts.Take(5)) // Limit to first 5 excerpts
                {
                    var excerptObj = excerpt as JObject;
                    if (excerptObj == null) continue;
                    
                    var message = excerptObj["excerpt"]?.ToString() ?? "Unknown excerpt";
                    var logId = excerptObj["logId"]?.ToString() ?? "Unknown log";
                    
                    sb.AppendLine($"- **Log {logId}:** {message}");
                }
            }
        }

        /// <summary>
        /// Fetches detailed timeline data for a specific build including individual task/step information
        /// </summary>
        private async Task<JObject> GetBuildTimelineAsync(string organization, string project, string buildId)
        {
            try
            {
                string url = $"https://dev.azure.com/{organization}/{project}/_apis/build/builds/{buildId}/timeline?api-version=7.1";
                
                var response = await _httpClient.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to get build timeline for build {BuildId}: {StatusCode} - {Content}", buildId, response.StatusCode, content);
                    return null;
                }
                
                var timelineData = JObject.Parse(content);
                
                // Process and enhance timeline data
                var processedTimeline = new JObject();
                var records = timelineData["records"] as JArray;
                
                if (records != null)
                {
                    var tasks = new JArray();
                    var phases = new JArray();
                    var jobs = new JArray();
                    
                    foreach (var record in records)
                    {
                        var recordObj = record as JObject;
                        if (recordObj == null) continue;
                        
                        var type = recordObj["type"]?.ToString();
                        var name = recordObj["name"]?.ToString();
                        var startTime = recordObj["startTime"]?.ToString();
                        var finishTime = recordObj["finishTime"]?.ToString();
                        var result = recordObj["result"]?.ToString();
                        var state = recordObj["state"]?.ToString();
                        var id = recordObj["id"]?.ToString();
                        var parentId = recordObj["parentId"]?.ToString();
                        
                        // Calculate duration
                        TimeSpan? duration = null;
                        if (!string.IsNullOrEmpty(startTime) && !string.IsNullOrEmpty(finishTime))
                        {
                            if (DateTime.TryParse(startTime, out DateTime start) && DateTime.TryParse(finishTime, out DateTime finish))
                            {
                                duration = finish - start;
                            }
                        }
                        
                        var processedRecord = new JObject
                        {
                            ["id"] = id,
                            ["parentId"] = parentId,
                            ["type"] = type,
                            ["name"] = name,
                            ["startTime"] = startTime,
                            ["finishTime"] = finishTime,
                            ["duration"] = duration?.ToString(@"hh\:mm\:ss\.fff"),
                            ["durationMs"] = duration?.TotalMilliseconds,
                            ["result"] = result,
                            ["state"] = state,
                            ["order"] = recordObj["order"],
                            ["workerName"] = recordObj["workerName"]
                        };
                        
                        // Add error/warning information if available
                        if (recordObj["issues"] != null)
                        {
                            processedRecord["issues"] = recordObj["issues"];
                        }
                        
                        // Add log reference if available
                        if (recordObj["log"] != null)
                        {
                            processedRecord["logId"] = recordObj["log"]["id"];
                            processedRecord["logUrl"] = recordObj["log"]["url"];
                        }
                        
                        // Categorize by type
                        switch (type?.ToLower())
                        {
                            case "task":
                                tasks.Add(processedRecord);
                                break;
                            case "phase":
                                phases.Add(processedRecord);
                                break;
                            case "job":
                                jobs.Add(processedRecord);
                                break;
                        }
                    }
                    
                    processedTimeline["tasks"] = tasks;
                    processedTimeline["phases"] = phases;
                    processedTimeline["jobs"] = jobs;
                    processedTimeline["totalRecords"] = records.Count;
                }
                
                return processedTimeline;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching build timeline for build {BuildId}", buildId);
                return null;
            }
        }

        /// <summary>
        /// Fetches log summary information including warning counts, error details, and key excerpts
        /// </summary>
        private async Task<JObject> GetBuildLogsAsync(string organization, string project, string buildId)
        {
            try
            {
                // First, get the list of logs
                string logsUrl = $"https://dev.azure.com/{organization}/{project}/_apis/build/builds/{buildId}/logs?api-version=7.1";
                
                var logsResponse = await _httpClient.GetAsync(logsUrl);
                var logsContent = await logsResponse.Content.ReadAsStringAsync();
                
                if (!logsResponse.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to get build logs list for build {BuildId}: {StatusCode} - {Content}", buildId, logsResponse.StatusCode, logsContent);
                    return null;
                }
                
                var logsData = JObject.Parse(logsContent);
                var logsList = logsData["value"] as JArray;
                
                if (logsList == null || logsList.Count == 0)
                {
                    return new JObject
                    {
                        ["summary"] = "No logs available",
                        ["logCount"] = 0
                    };
                }
                
                var logSummary = new JObject();
                var logEntries = new JArray();
                int totalWarnings = 0;
                int totalErrors = 0;
                var errorDetails = new JArray();
                var warningDetails = new JArray();
                var keyExcerpts = new JArray();
                
                // Process each log (limit to first 5 logs to avoid excessive data)
                int processedLogs = 0;
                foreach (var log in logsList.Take(5))
                {
                    var logObj = log as JObject;
                    if (logObj == null) continue;
                    
                    var logId = logObj["id"]?.ToString();
                    var logUrl = logObj["url"]?.ToString();
                    
                    if (string.IsNullOrEmpty(logId)) continue;
                    
                    try
                    {
                        // Get individual log content
                        string logContentUrl = $"https://dev.azure.com/{organization}/{project}/_apis/build/builds/{buildId}/logs/{logId}?api-version=7.1";
                        var logContentResponse = await _httpClient.GetAsync(logContentUrl);
                        
                        if (logContentResponse.IsSuccessStatusCode)
                        {
                            var logContent = await logContentResponse.Content.ReadAsStringAsync();
                            var logAnalysis = AnalyzeLogContent(logContent, logId);
                            
                            totalWarnings += logAnalysis.WarningCount;
                            totalErrors += logAnalysis.ErrorCount;
                            
                            if (logAnalysis.Errors.Any())
                            {
                                foreach (var error in logAnalysis.Errors)
                                {
                                    errorDetails.Add(new JObject
                                    {
                                        ["logId"] = logId,
                                        ["message"] = error,
                                        ["timestamp"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
                                    });
                                }
                            }
                            
                            if (logAnalysis.Warnings.Any())
                            {
                                foreach (var warning in logAnalysis.Warnings)
                                {
                                    warningDetails.Add(new JObject
                                    {
                                        ["logId"] = logId,
                                        ["message"] = warning,
                                        ["timestamp"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
                                    });
                                }
                            }
                            
                            if (logAnalysis.KeyExcerpts.Any())
                            {
                                foreach (var excerpt in logAnalysis.KeyExcerpts)
                                {
                                    keyExcerpts.Add(new JObject
                                    {
                                        ["logId"] = logId,
                                        ["excerpt"] = excerpt,
                                        ["type"] = "info"
                                    });
                                }
                            }
                            
                            logEntries.Add(new JObject
                            {
                                ["logId"] = logId,
                                ["url"] = logUrl,
                                ["lineCount"] = logAnalysis.LineCount,
                                ["warningCount"] = logAnalysis.WarningCount,
                                ["errorCount"] = logAnalysis.ErrorCount,
                                ["sizeBytes"] = logContent.Length
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to process log {LogId} for build {BuildId}", logId, buildId);
                    }
                    
                    processedLogs++;
                }
                
                logSummary["summary"] = new JObject
                {
                    ["totalLogs"] = logsList.Count,
                    ["processedLogs"] = processedLogs,
                    ["totalWarnings"] = totalWarnings,
                    ["totalErrors"] = totalErrors
                };
                
                logSummary["logs"] = logEntries;
                logSummary["errors"] = errorDetails;
                logSummary["warnings"] = warningDetails;
                logSummary["keyExcerpts"] = keyExcerpts;
                
                return logSummary;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching build logs for build {BuildId}", buildId);
                return null;
            }
        }

        /// <summary>
        /// Analyzes log content to extract errors, warnings, and key information
        /// </summary>
        private (int LineCount, int ErrorCount, int WarningCount, List<string> Errors, List<string> Warnings, List<string> KeyExcerpts) AnalyzeLogContent(string logContent, string logId)
        {
            var errors = new List<string>();
            var warnings = new List<string>();
            var keyExcerpts = new List<string>();
            
            if (string.IsNullOrEmpty(logContent))
            {
                return (0, 0, 0, errors, warnings, keyExcerpts);
            }
            
            var lines = logContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            int lineCount = lines.Length;
            
            foreach (var line in lines)
            {
                var lowerLine = line.ToLowerInvariant();
                
                // Detect errors
                if (lowerLine.Contains("error") || lowerLine.Contains("failed") || lowerLine.Contains("exception"))
                {
                    if (!lowerLine.Contains("0 error") && !lowerLine.Contains("no error"))
                    {
                        errors.Add(line.Trim());
                    }
                }
                
                // Detect warnings
                if (lowerLine.Contains("warning") || lowerLine.Contains("warn"))
                {
                    if (!lowerLine.Contains("0 warning") && !lowerLine.Contains("no warning"))
                    {
                        warnings.Add(line.Trim());
                    }
                }
                
                // Detect key information
                if (lowerLine.Contains("starting") || lowerLine.Contains("finishing") || 
                    lowerLine.Contains("completed") || lowerLine.Contains("duration") ||
                    lowerLine.Contains("test") || lowerLine.Contains("build") ||
                    lowerLine.Contains("deploy") || lowerLine.Contains("publish"))
                {
                    keyExcerpts.Add(line.Trim());
                }
            }
            
            // Limit the number of items to prevent excessive output
            errors = errors.Take(10).ToList();
            warnings = warnings.Take(10).ToList();
            keyExcerpts = keyExcerpts.Take(15).ToList();
            
            return (lineCount, errors.Count, warnings.Count, errors, warnings, keyExcerpts);
        }

        [McpServerTool, Description("Retrieves pipeline execution data with granular analysis capabilities including detailed timeline data, task-level performance metrics, log analysis with error/warning detection, and execution bottleneck identification.")]
        public async Task<string> AzureDevOpsGetPipelineRuns([Description("JSON parameters for AzureDevOpsGetPipelineRuns")] string parameters = "{}")
        {
            return await ExecuteWithExtraProperties(parameters);
        }
    }
}