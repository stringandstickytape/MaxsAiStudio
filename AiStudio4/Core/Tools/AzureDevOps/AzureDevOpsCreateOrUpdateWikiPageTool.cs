using System.Net.Http;
using System.Net.Http.Headers;
using System.Web;
using System.Diagnostics;
using System.IO;

namespace AiStudio4.Core.Tools.AzureDevOps
{
    /// <summary>
    /// Implementation of the Azure DevOps Create or Update Wiki Page tool
    /// </summary>
    public class AzureDevOpsCreateOrUpdateWikiPageTool : BaseToolImplementation
    {
        private readonly HttpClient _httpClient;
        private readonly IDialogService _dialogService;

        public AzureDevOpsCreateOrUpdateWikiPageTool(
            ILogger<AzureDevOpsCreateOrUpdateWikiPageTool> logger, 
            IGeneralSettingsService generalSettingsService, 
            IStatusMessageService statusMessageService,
            IDialogService dialogService)
            : base(logger, generalSettingsService, statusMessageService)
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "AiStudio4-AzureDevOps-Tool");
            _dialogService = dialogService;
        }

        /// <summary>
        /// Gets the Azure DevOps Create or Update Wiki Page tool definition
        /// </summary>
        public override Tool GetToolDefinition()
        {
            return new Tool
            {
                Guid = ToolGuids.AZURE_DEV_OPS_CREATE_OR_UPDATE_WIKI_PAGE_TOOL_GUID,
                Name = "AzureDevOpsCreateOrUpdateWikiPage",
                Description = """
Creates or updates a wiki page at the specified path in Azure DevOps.

If the page exists, its content will be overwritten with the provided content; if it does not exist, the page will be created.

Important:
- Before updating a page, this tool will:
  1. Retrieve the current content.
  2. Generate and present a git-style diff (git diff --no-index) between the current and new content.
  3. Require user approval before proceeding.
- If git is not found on the system, the update will be aborted and a warning shown.
- Never overwrite a page without reviewing the proposed changes.
""",
                Schema = """
{
  "name": "AzureDevOpsCreateOrUpdateWikiPage",
  "description": "Creates or updates a wiki page at the specified path in Azure DevOps. Shows a diff and requires approval before updating existing pages.",
  "input_schema": {
    "properties": {
      "organization": { "title": "Organization", "type": "string", "description": "The Azure DevOps organization name" },
      "project": { "title": "Project", "type": "string", "description": "The Azure DevOps project name" },
      "wiki_id": { "title": "Wiki ID or Name", "type": "string", "description": "The ID or name of the wiki (wikiIdentifier)" },
      "path": { "title": "Page Path", "type": "string", "description": "Path where the page should be created/updated (e.g., '/parent/page')." },
      "content": { "title": "Content", "type": "string", "description": "The markdown content for the wiki page" },
      "comment": { "title": "Comment", "type": "string", "description": "Optional comment for the page update" },
      "version": { "title": "Version", "type": "string", "description": "Optional version/branch name (e.g., 'wikiMaster')" }
    },
    "required": ["organization", "project", "wiki_id", "path", "content"],
    "title": "AzureDevOpsCreateOrUpdateWikiPageArguments",
    "type": "object"
  }
}
""",
                Categories = new List<string> { "AzureDevOps" },
                OutputFileType = "txt",
                Filetype = string.Empty,
                LastModified = DateTime.UtcNow,
                ExtraProperties = new Dictionary<string, string> { }
            };
        }

        public override async Task<BuiltinToolResult> ProcessAsync(string toolParameters, Dictionary<string, string> extraProperties)
        {
            try
            {
                SendStatusUpdate("Starting Azure DevOps Create or Update Wiki Page tool execution...");
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

                if (!parameters.TryGetValue("content", out var contentObj) || !(contentObj is string content))
                {
                    return CreateResult(true, true, $"Parameters: organization={organization}, project={project}, wiki_id={wikiId}, path={path}\n\nError: 'content' parameter is required.");
                }

                // Extract optional parameters
                string comment = null;
                if (parameters.TryGetValue("comment", out var commentObj) && commentObj is string commentStr && !string.IsNullOrWhiteSpace(commentStr))
                {
                    comment = commentStr;
                }

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

                return await CreateOrUpdateWikiPageAsync(organization, project, wikiId, path, content, comment, version);
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

        private async Task<BuiltinToolResult> CreateOrUpdateWikiPageAsync(string organization, string project, string wikiId, string path, string newContent, string comment, string version)
        {
            try
            {
                // First, check if git is available
                if (!await IsGitAvailable())
                {
                    SendStatusUpdate("Git is not installed or not found in PATH. Cannot proceed with wiki page update.");
                    return CreateResult(true, true, $"Parameters: organization={organization}, project={project}, wiki_id={wikiId}, path={path}\n\nError: Git is not installed or not found in PATH. Wiki page update requires git for generating diffs. Please install git and ensure it's in your PATH.");
                }

                // Try to get existing content
                SendStatusUpdate($"Checking if wiki page '{path}' exists...");
                var existingContent = await GetExistingWikiPageContent(organization, project, wikiId, path, version);
                
                if (existingContent != null)
                {
                    // Page exists, show diff and get confirmation
                    SendStatusUpdate("Page exists. Generating diff...");
                    
                    var diffResult = await GenerateGitDiff(existingContent, newContent, path);
                    if (!diffResult.Success)
                    {
                        return CreateResult(true, true, $"Parameters: organization={organization}, project={project}, wiki_id={wikiId}, path={path}\n\nError generating diff: {diffResult.Error}");
                    }

                    // Build confirmation dialog
                    string confirmationPrompt = $"AI wants to update the wiki page '{path}' in {organization}/{project}. This will overwrite the existing content. Review the changes below and proceed?";
                    string commandForDisplay = diffResult.Diff;

                    bool confirmed = await _dialogService.ShowConfirmationAsync("Confirm Wiki Page Update", confirmationPrompt, commandForDisplay);
                    if (!confirmed)
                    {
                        SendStatusUpdate($"Wiki page update for '{path}' cancelled by user.");
                        return CreateResult(true, false, "Operation cancelled by user.");
                    }
                }
                else
                {
                    // New page, get confirmation for creation
                    string confirmationPrompt = $"AI wants to create a new wiki page at '{path}' in {organization}/{project}. This will create a new page with the provided content. Proceed?";
                    string commandForDisplay = $"Create new wiki page: {path}\n\nContent preview:\n{newContent.Substring(0, Math.Min(500, newContent.Length))}...";

                    bool confirmed = await _dialogService.ShowConfirmationAsync("Confirm Wiki Page Creation", confirmationPrompt, commandForDisplay);
                    if (!confirmed)
                    {
                        SendStatusUpdate($"Wiki page creation for '{path}' cancelled by user.");
                        return CreateResult(true, false, "Operation cancelled by user.");
                    }
                }

                // Proceed with the update/creation
                SendStatusUpdate($"Updating wiki page '{path}'...");
                
                var queryParams = new List<string>();
                queryParams.Add($"path={HttpUtility.UrlEncode(path)}");
                queryParams.Add("api-version=6.0");
                
                if (!string.IsNullOrWhiteSpace(version))
                {
                    queryParams.Add($"versionDescriptor.version={HttpUtility.UrlEncode(version)}");
                }

                string queryString = string.Join("&", queryParams);
                string url = $"https://dev.azure.com/{organization}/{project}/_apis/wiki/wikis/{HttpUtility.UrlEncode(wikiId)}/pages?{queryString}";

                var requestBody = new
                {
                    content = newContent,
                    comment = comment ?? "Updated via AiStudio4"
                };

                var jsonContent = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");
                
                // Use PUT for create or update
                var response = await _httpClient.PutAsync(url, jsonContent);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    string errorMessage = "Unknown error";
                    try
                    {
                        var errorObj = JObject.Parse(responseContent);
                        errorMessage = errorObj?["message"]?.ToString() ?? responseContent;
                    }
                    catch { /* Use raw content if not JSON */ }
                    return CreateResult(true, true, $"Parameters: organization={organization}, project={project}, wiki_id={wikiId}, path={path}\n\nAzure DevOps API Error: {errorMessage} (Status code: {response.StatusCode})");
                }

                SendStatusUpdate($"Successfully {(existingContent != null ? "updated" : "created")} wiki page '{path}'.");
                
                var resultMessage = existingContent != null 
                    ? $"Successfully updated wiki page '{path}' in {organization}/{project}/{wikiId}"
                    : $"Successfully created new wiki page '{path}' in {organization}/{project}/{wikiId}";
                    
                if (!string.IsNullOrWhiteSpace(comment))
                {
                    resultMessage += $"\nComment: {comment}";
                }

                return CreateResult(true, true, $"Parameters: organization={organization}, project={project}, wiki_id={wikiId}, path={path}\n\n{resultMessage}");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error creating/updating wiki page");
                return CreateResult(true, true, $"Parameters: organization={organization}, project={project}, wiki_id={wikiId}, path={path}\n\nError creating/updating wiki page: {ex.Message}");
            }
        }

        private async Task<string> GetExistingWikiPageContent(string organization, string project, string wikiId, string path, string version)
        {
            try
            {
                var queryParams = new List<string>();
                queryParams.Add($"path={HttpUtility.UrlEncode(path)}");
                queryParams.Add("includeContent=true");
                
                if (!string.IsNullOrWhiteSpace(version))
                {
                    queryParams.Add($"versionDescriptor.version={HttpUtility.UrlEncode(version)}");
                }

                string queryString = string.Join("&", queryParams);
                string url = $"https://dev.azure.com/{organization}/{project}/_apis/wiki/wikis/{HttpUtility.UrlEncode(wikiId)}/pages?{queryString}";

                var response = await _httpClient.GetAsync(url);
                
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    // Page doesn't exist
                    return null;
                }

                if (!response.IsSuccessStatusCode)
                {
                    // Other error, log but continue (treat as new page)
                    _logger.LogWarning($"Error checking existing wiki page: {response.StatusCode}");
                    return null;
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var pageData = JObject.Parse(responseContent);
                return pageData?["content"]?.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving existing wiki page content");
                // Treat as new page if we can't get existing content
                return null;
            }
        }

        private async Task<bool> IsGitAvailable()
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = "--version",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(psi))
                {
                    await process.WaitForExitAsync();
                    return process.ExitCode == 0;
                }
            }
            catch
            {
                return false;
            }
        }

        private async Task<(bool Success, string Diff, string Error)> GenerateGitDiff(string oldContent, string newContent, string pagePath)
        {
            try
            {
                // Create temporary files
                var tempPath = Path.GetTempPath();
                var oldFile = Path.Combine(tempPath, $"wiki_old_{Guid.NewGuid()}.md");
                var newFile = Path.Combine(tempPath, $"wiki_new_{Guid.NewGuid()}.md");

                try
                {
                    // Write content to temporary files
                    await File.WriteAllTextAsync(oldFile, oldContent);
                    await File.WriteAllTextAsync(newFile, newContent);

                    // Run git diff
                    var psi = new ProcessStartInfo
                    {
                        FileName = "git",
                        Arguments = $"diff --no-index --no-prefix \"{oldFile}\" \"{newFile}\"",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    using (var process = new Process { StartInfo = psi })
                    {
                        var outputBuilder = new StringBuilder();
                        var errorBuilder = new StringBuilder();

                        process.OutputDataReceived += (s, e) => { if (e.Data != null) outputBuilder.AppendLine(e.Data); };
                        process.ErrorDataReceived += (s, e) => { if (e.Data != null) errorBuilder.AppendLine(e.Data); };

                        process.Start();
                        process.BeginOutputReadLine();
                        process.BeginErrorReadLine();
                        await process.WaitForExitAsync();

                        string output = outputBuilder.ToString();
                        string error = errorBuilder.ToString();

                        // Git diff returns exit code 1 when there are differences, which is normal
                        if (process.ExitCode == 0 || process.ExitCode == 1)
                        {
                            // Replace temp file names with meaningful names in the diff
                            output = output.Replace(oldFile, $"a/{pagePath}")
                                         .Replace(newFile, $"b/{pagePath}");
                            
                            return (true, output, null);
                        }
                        else
                        {
                            return (false, null, $"Git diff failed with exit code {process.ExitCode}: {error}");
                        }
                    }
                }
                finally
                {
                    // Clean up temporary files
                    try { File.Delete(oldFile); } catch { }
                    try { File.Delete(newFile); } catch { }
                }
            }
            catch (Exception ex)
            {
                return (false, null, $"Exception generating diff: {ex.Message}");
            }
        }
    }
}