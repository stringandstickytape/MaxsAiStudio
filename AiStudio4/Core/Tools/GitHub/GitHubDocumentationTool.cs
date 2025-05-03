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
    /// Implementation of the GitHub Documentation Retrieval tool
    /// </summary>
    public class GitHubDocumentationTool : BaseToolImplementation
    {
        private readonly HttpClient _httpClient;

        public GitHubDocumentationTool(ILogger<GitHubDocumentationTool> logger, IGeneralSettingsService generalSettingsService, IStatusMessageService statusMessageService) 
            : base(logger, generalSettingsService, statusMessageService)
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "AiStudio4-GitHub-Tool");
            _httpClient.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
        }

        /// <summary>
        /// Gets the GitHub Documentation tool definition
        /// </summary>
        public override Tool GetToolDefinition()
        {
            return new Tool
            {
                Guid = "6172c3d4-e5f6-7890-1234-56789abcdef05",
                Name = "GitHubDocumentation",
                Description = "Retrieves project documentation from a GitHub repository, looking first for /llms.txt, then /readme.md if not found.",
                Schema = @"{
  ""name"": ""GitHubDocumentation"",
  ""description"": ""Retrieves project documentation from a GitHub repository, looking first for /llms.txt, then /readme.md if not found."",
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
      ""ref"": {
        ""title"": ""Reference"",
        ""type"": ""string"",
        ""description"": ""The name of the commit/branch/tag (defaults to the repository's default branch)""
      }
    },
    ""required"": [""owner"", ""repo""],
    ""title"": ""GitHubDocumentationArguments"",
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
                SendStatusUpdate("Starting GitHub Documentation tool execution...");
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

                // Try to get documentation
                var result = await GetDocumentationAsync(owner, repo, reference);
                // Append parameters info to output
                if (result.Success)
                {
                    result.Output = $"Parameters: owner={owner}, repo={repo}, ref={reference}\n\n" + result.Output;
                }
                return result;
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

        private async Task<BuiltinToolResult> GetDocumentationAsync(string owner, string repo, string reference)
        {
            try
            {
                SendStatusUpdate($"Fetching documentation for {owner}/{repo}...");
                
                // First try to get llms.txt
                var llmsResult = await TryGetFileContentAsync(owner, repo, "llms.txt", reference);
                if (llmsResult.Success)
                {
                    SendStatusUpdate("Successfully retrieved llms.txt documentation.");
                    return llmsResult;
                }
                
                // If llms.txt not found, try readme.md
                var readmeResult = await TryGetFileContentAsync(owner, repo, "readme.md", reference);
                if (readmeResult.Success)
                {
                    SendStatusUpdate("Successfully retrieved readme.md documentation.");
                    return readmeResult;
                }
                
                // If neither file was found
                SendStatusUpdate("No documentation found.");
                return CreateResult(true, true, $"No documentation found for {owner}/{repo}.");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error fetching documentation");
                return CreateResult(true, true, $"Error fetching documentation: {ex.Message}");
            }
        }

        private async Task<BuiltinToolResult> TryGetFileContentAsync(string owner, string repo, string path, string reference)
        {
            try
            {
                string url = $"https://api.github.com/repos/{owner}/{repo}/contents/{path.TrimStart('/')}";
                if (!string.IsNullOrEmpty(reference))
                {
                    url += $"?ref={Uri.EscapeDataString(reference)}";
                }
                
                var response = await _httpClient.GetAsync(url);
                
                if (!response.IsSuccessStatusCode)
                {
                    // File not found or other error
                    return CreateResult(false, false, "");
                }
                
                var content = await response.Content.ReadAsStringAsync();
                var fileContent = ExtractFileContent(content);
                
                return CreateResult(true, true, $"# Documentation from {path}\n\n{fileContent}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching {path}");
                return CreateResult(false, false, "");
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
                
                return decodedContent;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting file content");
                return $"Error extracting file content: {ex.Message}";
            }
        }
    }
}