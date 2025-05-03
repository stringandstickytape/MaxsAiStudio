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

namespace AiStudio4.Core.Tools.GitHub
{
    /// <summary>
    /// Implementation of the GitHub Get Content API tool
    /// </summary>
    public class GitHubGetContentTool : BaseToolImplementation
    {
        private readonly HttpClient _httpClient;

        public GitHubGetContentTool(ILogger<GitHubGetContentTool> logger, IGeneralSettingsService generalSettingsService, IStatusMessageService statusMessageService) 
            : base(logger, generalSettingsService, statusMessageService)
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "AiStudio4-GitHub-Tool");
            _httpClient.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
        }

        /// <summary>
        /// Gets the GitHub Get Content tool definition
        /// </summary>
        public override Tool GetToolDefinition()
        {
            return new Tool
            {
                Guid = "6172c3d4-e5f6-7890-1234-56789abcdef03",
                Name = "GitHubGetContent",
                Description = "Retrieves the content of a specific file from a GitHub repository using the /repos/{owner}/{repo}/contents/{path} endpoint.",
                Schema = @"{
  ""name"": ""GitHubGetContent"",
  ""description"": ""Retrieves the content of a specific file from a GitHub repository using the /repos/{owner}/{repo}/contents/{path} endpoint."",
  ""input_schema"": {
    ""properties"": {
      ""owner"": {
        ""title"": ""Owner"",
        ""type"": ""string"",
        ""description"": ""The GitHub username or organization that owns the repository""
      },
      ""repo"": {
        ""title"": ""Repository"",
        ""type"": ""string"",
        ""description"": ""The name of the repository""
      },
      ""path"": {
        ""title"": ""Path"",
        ""type"": ""string"",
        ""description"": ""The path to the file within the repository""
      },
      ""ref"": {
        ""title"": ""Reference"",
        ""type"": ""string"",
        ""description"": ""The name of the commit/branch/tag (defaults to the repository's default branch)""
      }
    },
    ""required"": [""owner"", ""repo"", ""path""],
    ""title"": ""GitHubGetContentArguments"",
    ""type"": ""object""
  }
}",
                Categories = new List<string> {"APITools", "GitHub" },
                OutputFileType = "txt",
                Filetype = string.Empty,
                LastModified = DateTime.UtcNow,
                ExtraProperties = new Dictionary<string, string> {
                    { "githubApiKey", "" }
                }
            };
        }

        public override async Task<BuiltinToolResult> ProcessAsync(string toolParameters, Dictionary<string, string> extraProperties)
        {
            try
            {
                SendStatusUpdate("Starting GitHub Get Content tool execution...");
                var parameters = JsonConvert.DeserializeObject<Dictionary<string, object>>(toolParameters) ?? new Dictionary<string, object>();

                // Extract parameters
                if (!parameters.TryGetValue("owner", out var ownerObj) || !(ownerObj is string owner) || string.IsNullOrWhiteSpace(owner))
                {
                    return CreateResult(true, true, "Error: 'owner' parameter is required.");
                }

                if (!parameters.TryGetValue("repo", out var repoObj) || !(repoObj is string repo) || string.IsNullOrWhiteSpace(repo))
                {
                    return CreateResult(true, true, "Error: 'repo' parameter is required.");
                }

                if (!parameters.TryGetValue("path", out var pathObj) || !(pathObj is string path) || string.IsNullOrWhiteSpace(path))
                {
                    return CreateResult(true, true, "Error: 'path' parameter is required.");
                }

                // Optional parameter
                string reference = null;
                if (parameters.TryGetValue("ref", out var refObj) && refObj is string refStr && !string.IsNullOrWhiteSpace(refStr))
                {
                    reference = refStr;
                }

                // Get API key from settings
                string apiKey = _generalSettingsService?.CurrentSettings?.GitHubApiKey;
                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    return CreateResult(true, true, "Error: GitHub API Key is not configured. Please set it in File > Settings > Set GitHub API Key.");
                }

                // Set up authentication header
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

                // Make the API request
                return await GetFileContentAsync(owner, repo, path, reference);
            }
            catch (JsonException jsonEx)
            {
                _logger.LogError(jsonEx, "Error deserializing GitHub tool parameters");
                return CreateResult(true, true, $"Error processing GitHub tool parameters: Invalid JSON format. {jsonEx.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing GitHub tool");
                return CreateResult(true, true, $"Error processing GitHub tool: {ex.Message}");
            }
        }

        private async Task<BuiltinToolResult> GetFileContentAsync(string owner, string repo, string path, string reference)
        {
            try
            {
                SendStatusUpdate($"Fetching content of file {path} from {owner}/{repo}...");
                
                string url = $"https://api.github.com/repos/{owner}/{repo}/contents/{path.TrimStart('/')}";
                if (!string.IsNullOrEmpty(reference))
                {
                    url += $"?ref={Uri.EscapeDataString(reference)}";
                }
                
                var response = await _httpClient.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorObj = JObject.Parse(content);
                    string errorMessage = errorObj["message"]?.ToString() ?? "Unknown error";
                    return CreateResult(true, true, $"GitHub API Error: {errorMessage} (Status code: {response.StatusCode})");
                }
                
                var fileContent = ExtractFileContent(content);
                
                SendStatusUpdate("Successfully retrieved file content.");
                return CreateResult(true, true, $"Parameters: owner={owner}, repo={repo}, path={path}, ref={reference}\n\n" + fileContent);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error fetching file content");
                return CreateResult(true, true, $"Error fetching file content: {ex.Message}");
            }
        }

        private string ExtractFileContent(string jsonContent)
        {
            try
            {
                var fileObj = JObject.Parse(jsonContent);
                
                // Check if it's a file (not a directory)
                if (fileObj["type"]?.ToString() != "file")
                {
                    return "Error: The specified path does not point to a file.";
                }
                
                // Get file metadata
                string name = fileObj["name"]?.ToString() ?? "Unknown";
                string path = fileObj["path"]?.ToString() ?? "Unknown";
                string sha = fileObj["sha"]?.ToString() ?? "Unknown";
                int size = fileObj["size"]?.ToObject<int>() ?? 0;
                string htmlUrl = fileObj["html_url"]?.ToString() ?? "Unknown";
                
                // Get content (base64 encoded)
                string encodedContent = fileObj["content"]?.ToString();
                if (string.IsNullOrEmpty(encodedContent))
                {
                    return "Error: File content is empty or not available.";
                }
                
                // Decode content
                // GitHub API returns base64 with line breaks, so remove them first
                encodedContent = encodedContent.Replace("\n", "");
                byte[] data = Convert.FromBase64String(encodedContent);
                string decodedContent = Encoding.UTF8.GetString(data);
                
                // Build result with metadata and content
                var sb = new StringBuilder();
                sb.AppendLine($"# File: {name}");
                sb.AppendLine();
                sb.AppendLine($"**Path:** {path}");
                sb.AppendLine($"**Size:** {size} bytes");
                sb.AppendLine($"**SHA:** {sha}");
                sb.AppendLine($"**URL:** {htmlUrl}");
                sb.AppendLine();
                sb.AppendLine("## Content");
                sb.AppendLine();
                sb.AppendLine("```");
                sb.AppendLine(decodedContent);
                sb.AppendLine("```");
                
                return sb.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting file content");
                return $"Error extracting file content: {ex.Message}\n\nRaw JSON:\n{jsonContent}";
            }
        }
    }
}