using AiStudio4.Core.Interfaces;
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
    /// Implementation of the Azure DevOps Get Item Content tool
    /// </summary>
    public class AzureDevOpsGetItemContentTool : BaseToolImplementation
    {
        private readonly HttpClient _httpClient;

        public AzureDevOpsGetItemContentTool(ILogger<AzureDevOpsGetItemContentTool> logger, IGeneralSettingsService generalSettingsService, IStatusMessageService statusMessageService) 
            : base(logger, generalSettingsService, statusMessageService)
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "AiStudio4-AzureDevOps-Tool");
        }

        /// <summary>
        /// Gets the Azure DevOps Get Item Content tool definition
        /// </summary>
        public override Tool GetToolDefinition()
        {
            return new Tool
            {
                Guid = "3d7c9e5b-8f2a-4d1e-9c6b-7d8f5e3a2c1d",
                Name = "AzureDevOpsGetItemContent",
                Description = "Retrieves the content of a specific file from an Azure DevOps repository.",
                Schema = @"{
  ""name"": ""AzureDevOpsGetItemContent"",
  ""description"": ""Retrieves the content of a specific file from an Azure DevOps repository."",
  ""input_schema"": {
    ""properties"": {
      ""organization"": {
        ""title"": ""Organization"",
        ""type"": ""string"",
        ""description"": ""The Azure DevOps organization name""
      },
      ""project"": {
        ""title"": ""Project"",
        ""type"": ""string"",
        ""description"": ""The Azure DevOps project name""
      },
      ""repository_id"": {
        ""title"": ""Repository ID"",
        ""type"": ""string"",
        ""description"": ""The repository ID or name""
      },
      ""path"": {
        ""title"": ""Path"",
        ""type"": ""string"",
        ""description"": ""Path to the item in the repository""
      },
      ""version_type"": {
        ""title"": ""Version Type"",
        ""type"": ""string"",
        ""description"": ""Type of version identifier (branch, commit, tag)"",
        ""enum"": [""branch"", ""commit"", ""tag""],
        ""default"": ""branch""
      },
      ""version"": {
        ""title"": ""Version"",
        ""type"": ""string"",
        ""description"": ""Version identifier (branch name, commit ID, or tag name)"",
        ""default"": ""main""
      }
    },
    ""required"": [""organization"", ""project"", ""repository_id"", ""path""],
    ""title"": ""AzureDevOpsGetItemContentArguments"",
    ""type"": ""object""
  }
}",
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
                SendStatusUpdate("Starting Azure DevOps Get Item Content tool execution...");
                var parameters = JsonConvert.DeserializeObject<Dictionary<string, object>>(toolParameters) ?? new Dictionary<string, object>();

                // Extract required parameters
                if (!parameters.TryGetValue("organization", out var organizationObj) || !(organizationObj is string organization) || string.IsNullOrWhiteSpace(organization))
                {
                    return CreateResult(true, true, $"Parameters: organization=<missing>, project=<unknown>, repository_id=<unknown>, path=<unknown>\n\nError: 'organization' parameter is required.");
                }

                if (!parameters.TryGetValue("project", out var projectObj) || !(projectObj is string project) || string.IsNullOrWhiteSpace(project))
                {
                    return CreateResult(true, true, $"Parameters: organization={organization}, project=<missing>, repository_id=<unknown>, path=<unknown>\n\nError: 'project' parameter is required.");
                }

                if (!parameters.TryGetValue("repository_id", out var repoIdObj) || !(repoIdObj is string repositoryId) || string.IsNullOrWhiteSpace(repositoryId))
                {
                    return CreateResult(true, true, $"Parameters: organization={organization}, project={project}, repository_id=<missing>, path=<unknown>\n\nError: 'repository_id' parameter is required.");
                }

                if (!parameters.TryGetValue("path", out var pathObj) || !(pathObj is string path) || string.IsNullOrWhiteSpace(path))
                {
                    return CreateResult(true, true, $"Parameters: organization={organization}, project={project}, repository_id={repositoryId}, path=<missing>\n\nError: 'path' parameter is required.");
                }

                // Extract optional parameters
                string versionType = "branch";
                if (parameters.TryGetValue("version_type", out var versionTypeObj) && versionTypeObj is string versionTypeStr && !string.IsNullOrWhiteSpace(versionTypeStr))
                {
                    versionType = versionTypeStr;
                }

                string version = "main";
                if (parameters.TryGetValue("version", out var versionObj) && versionObj is string versionStr && !string.IsNullOrWhiteSpace(versionStr))
                {
                    version = versionStr;
                }

                // Get API key from settings
                string apiKey = _generalSettingsService?.CurrentSettings?.AzureDevOpsPAT;
                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    return CreateResult(true, true, $"Parameters: organization={organization}, project={project}, repository_id={repositoryId}, path={path}\n\nError: Azure DevOps PAT is not configured. Please set it in File > Settings > Set Azure DevOps PAT.");
                }

                // Set up authentication header
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", 
                    Convert.ToBase64String(Encoding.ASCII.GetBytes(string.Format("{0}:{1}", "", apiKey))));

                // Make the API request
                return await GetItemContentAsync(organization, project, repositoryId, path, versionType, version);
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

        private async Task<BuiltinToolResult> GetItemContentAsync(string organization, string project, string repositoryId, 
            string path, string versionType, string version)
        {
            try
            {
                SendStatusUpdate($"Fetching content for {path} from {organization}/{project}/{repositoryId}...");
                
                // URL encode the path
                string encodedPath = HttpUtility.UrlEncode(path);
                
                // Build query parameters
                var queryParams = new List<string>();
                
                // Add version parameters if specified
                if (!string.IsNullOrEmpty(versionType) && !string.IsNullOrEmpty(version))
                {
                    // Convert version type to Azure DevOps API parameter
                    string versionParameter = versionType.ToLowerInvariant() switch
                    {
                        "branch" => "versionDescriptor.versionType=branch&versionDescriptor.version=" + HttpUtility.UrlEncode(version),
                        "commit" => "versionDescriptor.versionType=commit&versionDescriptor.version=" + HttpUtility.UrlEncode(version),
                        "tag" => "versionDescriptor.versionType=tag&versionDescriptor.version=" + HttpUtility.UrlEncode(version),
                        _ => "versionDescriptor.versionType=branch&versionDescriptor.version=main"
                    };
                    
                    queryParams.Add(versionParameter);
                }
                
                // Add download parameter to get raw content
                queryParams.Add("download=true");
                
                string queryString = queryParams.Count > 0 ? $"?{string.Join("&", queryParams)}" : "";
                string url = $"https://dev.azure.com/{organization}/{project}/_apis/git/repositories/{repositoryId}/items{queryString}&path={encodedPath}";
                
                var response = await _httpClient.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();
                
                if (!response.IsSuccessStatusCode)
                {
                    // Try to parse error message from JSON response
                    try
                    {
                        var errorObj = JObject.Parse(content);
                        string errorMessage = errorObj["message"]?.ToString() ?? "Unknown error";
                        return CreateResult(true, true, $"Parameters: organization={organization}, project={project}, repository_id={repositoryId}, path={path}\n\nAzure DevOps API Error: {errorMessage} (Status code: {response.StatusCode})");
                    }
                    catch
                    {
                        // If parsing fails, return the raw error
                        return CreateResult(true, true, $"Parameters: organization={organization}, project={project}, repository_id={repositoryId}, path={path}\n\nAzure DevOps API Error: (Status code: {response.StatusCode})\n\n{content}");
                    }
                }
                
                // Format the response based on file type
                var formattedContent = FormatItemContent(path, content);
                
                SendStatusUpdate("Successfully retrieved item content.");
                return CreateResult(true, true, $"Parameters: organization={organization}, project={project}, repository_id={repositoryId}, path={path}, version_type={versionType}, version={version}\n\n{formattedContent}");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error fetching item content");
                return CreateResult(true, true, $"Parameters: organization={organization}, project={project}, repository_id={repositoryId}, path={path}\n\nError fetching item content: {ex.Message}");
            }
        }

        private string FormatItemContent(string path, string content)
        {
            try
            {
                var sb = new StringBuilder();
                
                // Add file information header
                sb.AppendLine($"# File: {path}");
                sb.AppendLine();
                
                // Try to determine if this is a binary file
                bool isBinary = IsPossiblyBinaryContent(content);
                
                if (isBinary)
                {
                    sb.AppendLine("[Binary file content not displayed]");
                }
                else
                {
                    // For text files, add the content with appropriate formatting
                    string fileExtension = System.IO.Path.GetExtension(path).ToLowerInvariant();
                    
                    // Add markdown code block with language hint based on file extension
                    string languageHint = GetLanguageHintFromExtension(fileExtension);
                    sb.AppendLine($"```{languageHint}");
                    sb.AppendLine(content);
                    sb.AppendLine("```");
                }
                
                return sb.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error formatting item content");
                return $"Error formatting item content: {ex.Message}\n\nRaw content:\n{content}";
            }
        }
        
        private bool IsPossiblyBinaryContent(string content)
        {
            // Simple heuristic to detect binary content
            // Check for null bytes or high concentration of non-printable characters
            if (string.IsNullOrEmpty(content))
                return false;
                
            // If content contains null bytes, it's likely binary
            if (content.Contains("\0"))
                return true;
                
            // Count non-printable characters
            int nonPrintableCount = 0;
            int sampleSize = Math.Min(content.Length, 1000); // Check first 1000 chars
            
            for (int i = 0; i < sampleSize; i++)
            {
                char c = content[i];
                if (c < 32 && c != '\n' && c != '\r' && c != '\t')
                {
                    nonPrintableCount++;
                }
            }
            
            // If more than 10% are non-printable, consider it binary
            return nonPrintableCount > (sampleSize * 0.1);
        }
        
        private string GetLanguageHintFromExtension(string extension)
        {
            return extension switch
            {
                ".cs" => "csharp",
                ".js" => "javascript",
                ".ts" => "typescript",
                ".html" => "html",
                ".css" => "css",
                ".json" => "json",
                ".xml" => "xml",
                ".md" => "markdown",
                ".py" => "python",
                ".java" => "java",
                ".cpp" => "cpp",
                ".c" => "c",
                ".go" => "go",
                ".rb" => "ruby",
                ".php" => "php",
                ".ps1" => "powershell",
                ".sh" => "bash",
                ".yaml" => "yaml",
                ".yml" => "yaml",
                ".sql" => "sql",
                _ => ""
            };
        }
    }
}