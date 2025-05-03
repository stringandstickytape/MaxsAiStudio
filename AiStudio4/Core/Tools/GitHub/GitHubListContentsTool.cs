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
    /// Implementation of the GitHub List Contents API tool
    /// </summary>
    public class GitHubListContentsTool : BaseToolImplementation
    {
        private readonly HttpClient _httpClient;

        public GitHubListContentsTool(ILogger<GitHubListContentsTool> logger, IGeneralSettingsService generalSettingsService, IStatusMessageService statusMessageService) 
            : base(logger, generalSettingsService, statusMessageService)
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "AiStudio4-GitHub-Tool");
            _httpClient.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
        }

        /// <summary>
        /// Gets the GitHub List Contents tool definition
        /// </summary>
        public override Tool GetToolDefinition()
        {
            return new Tool
            {
                Guid = "6172c3d4-e5f6-7890-1234-56789abcdef02",
                Name = "GitHubListContents",
                Description = "Lists files and directories within a specified path in a GitHub repository using the /repos/{owner}/{repo}/contents/{path} endpoint.",
                Schema = @"{
  ""name"": ""GitHubListContents"",
  ""description"": ""Lists files and directories within a specified path in a GitHub repository using the /repos/{owner}/{repo}/contents/{path} endpoint."",
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
        ""description"": ""The directory path within the repository to list contents from (defaults to root)""
      },
      ""ref"": {
        ""title"": ""Reference"",
        ""type"": ""string"",
        ""description"": ""The name of the commit/branch/tag (defaults to the repository's default branch)""
      }
    },
    ""required"": [""owner"", ""repo""],
    ""title"": ""GitHubListContentsArguments"",
    ""type"": ""object""
  }
}",
                Categories = new List<string> { "APITools", "GitHub" },
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
                SendStatusUpdate("Starting GitHub List Contents tool execution...");
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

                // Optional parameters
                string path = "";
                if (parameters.TryGetValue("path", out var pathObj) && pathObj is string pathStr)
                {
                    path = pathStr.TrimStart('/');
                }

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
                return await ListContentsAsync(owner, repo, path, reference);
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

        private async Task<BuiltinToolResult> ListContentsAsync(string owner, string repo, string path, string reference)
        {
            try
            {
                string pathDisplay = string.IsNullOrEmpty(path) ? "root" : path;
                SendStatusUpdate($"Fetching contents of {pathDisplay} in {owner}/{repo}...");
                
                string url = $"https://api.github.com/repos/{owner}/{repo}/contents/{path}";
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
                
                var formattedContent = FormatContentsListing(content, pathDisplay, owner, repo, reference);
                
                SendStatusUpdate("Successfully retrieved contents listing.");
                return CreateResult(true, true, formattedContent);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error fetching contents listing");
                return CreateResult(true, true, $"Error fetching contents listing: {ex.Message}");
            }
        }

        private string FormatContentsListing(string jsonContent, string path, string owner, string repo, string reference)
        {
            try
            {
                var contents = JArray.Parse(jsonContent);
                var sb = new StringBuilder();
                
                string refDisplay = string.IsNullOrEmpty(reference) ? "default branch" : reference;
                sb.AppendLine($"# Contents of {owner}/{repo}/{path} ({refDisplay})");
                sb.AppendLine();
                
                // Group by type (directory first, then file)
                var directories = new List<JToken>();
                var files = new List<JToken>();
                
                foreach (var item in contents)
                {
                    if (item["type"].ToString() == "dir")
                    {
                        directories.Add(item);
                    }
                    else
                    {
                        files.Add(item);
                    }
                }
                
                // List directories
                if (directories.Count > 0)
                {
                    sb.AppendLine("## Directories");
                    foreach (var dir in directories.OrderBy(d => d["name"].ToString()))
                    {
                        sb.AppendLine($"- 📁 [{dir["name"]}]({dir["html_url"]})");
                    }
                    sb.AppendLine();
                }
                
                // List files
                if (files.Count > 0)
                {
                    sb.AppendLine("## Files");
                    foreach (var file in files.OrderBy(f => f["name"].ToString()))
                    {
                        sb.AppendLine($"- 📄 [{file["name"]}]({file["html_url"]}) ({file["size"]} bytes)");
                    }
                    sb.AppendLine();
                }
                
                // Summary
                sb.AppendLine("## Summary");
                sb.AppendLine($"- Total items: {contents.Count}");
                sb.AppendLine($"- Directories: {directories.Count}");
                sb.AppendLine($"- Files: {files.Count}");
                
                return sb.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error formatting contents listing");
                return $"Error formatting contents listing: {ex.Message}\n\nRaw JSON:\n{jsonContent}";
            }
        }
    }
}