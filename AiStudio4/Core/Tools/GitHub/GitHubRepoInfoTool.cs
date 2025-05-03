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
    /// Implementation of the GitHub Repository Info API tool
    /// </summary>
    public class GitHubRepoInfoTool : BaseToolImplementation
    {
        private readonly HttpClient _httpClient;

        public GitHubRepoInfoTool(ILogger<GitHubRepoInfoTool> logger, IGeneralSettingsService generalSettingsService, IStatusMessageService statusMessageService) 
            : base(logger, generalSettingsService, statusMessageService)
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "AiStudio4-GitHub-Tool");
            _httpClient.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
        }

        /// <summary>
        /// Gets the GitHub Repository Info tool definition
        /// </summary>
        public override Tool GetToolDefinition()
        {
            return new Tool
            {
                Guid = "6172c3d4-e5f6-7890-1234-56789abcdef01",
                Name = "GitHubRepoInfo",
                Description = "Retrieves basic metadata about a GitHub repository using the /repos/{owner}/{repo} endpoint.",
                Schema = @"{
  ""name"": ""GitHubRepoInfo"",
  ""description"": ""Retrieves basic metadata about a GitHub repository using the /repos/{owner}/{repo} endpoint."",
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
      }
    },
    ""required"": [""owner"", ""repo""],
    ""title"": ""GitHubRepoInfoArguments"",
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
                SendStatusUpdate("Starting GitHub Repository Info tool execution...");
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

                // Get API key from settings
                string apiKey = _generalSettingsService?.CurrentSettings?.GitHubApiKey;
                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    return CreateResult(true, true, "Error: GitHub API Key is not configured. Please set it in File > Settings > Set GitHub API Key.");
                }

                // Set up authentication header
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

                // Make the API request
                return await GetRepositoryInfoAsync(owner, repo);
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

        private async Task<BuiltinToolResult> GetRepositoryInfoAsync(string owner, string repo)
        {
            try
            {
                SendStatusUpdate($"Fetching repository information for {owner}/{repo}...");
                string url = $"https://api.github.com/repos/{owner}/{repo}";
                
                var response = await _httpClient.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorObj = JObject.Parse(content);
                    string errorMessage = errorObj["message"]?.ToString() ?? "Unknown error";
                    return CreateResult(true, true, $"GitHub API Error: {errorMessage} (Status code: {response.StatusCode})");
                }
                
                var formattedContent = FormatRepositoryInfo(content);
                
                SendStatusUpdate("Successfully retrieved repository information.");
                return CreateResult(true, true, formattedContent);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error fetching repository information");
                return CreateResult(true, true, $"Error fetching repository information: {ex.Message}");
            }
        }

        private string FormatRepositoryInfo(string jsonContent)
        {
            try
            {
                var repo = JObject.Parse(jsonContent);
                var sb = new StringBuilder();
                
                sb.AppendLine("# Repository Information");
                sb.AppendLine();
                sb.AppendLine($"**Full Name:** {repo["full_name"]}");
                sb.AppendLine($"**Description:** {repo["description"]}");
                sb.AppendLine($"**URL:** {repo["html_url"]}");
                sb.AppendLine($"**Default Branch:** {repo["default_branch"]}");
                sb.AppendLine($"**Created:** {repo["created_at"]}");
                sb.AppendLine($"**Last Updated:** {repo["updated_at"]}");
                sb.AppendLine($"**Last Pushed:** {repo["pushed_at"]}");
                sb.AppendLine();
                
                sb.AppendLine("## Statistics");
                sb.AppendLine($"**Stars:** {repo["stargazers_count"]}");
                sb.AppendLine($"**Watchers:** {repo["watchers_count"]}");
                sb.AppendLine($"**Forks:** {repo["forks_count"]}");
                sb.AppendLine($"**Open Issues:** {repo["open_issues_count"]}");
                sb.AppendLine($"**Size:** {repo["size"]} KB");
                sb.AppendLine();
                
                sb.AppendLine("## Repository Details");
                sb.AppendLine($"**Language:** {repo["language"]}");
                sb.AppendLine($"**License:** {repo["license"]?["name"] ?? "Not specified"}");
                sb.AppendLine($"**Visibility:** {repo["visibility"]}");
                sb.AppendLine($"**Is Fork:** {repo["fork"]}");
                sb.AppendLine($"**Has Wiki:** {repo["has_wiki"]}");
                sb.AppendLine($"**Has Issues:** {repo["has_issues"]}");
                sb.AppendLine($"**Has Projects:** {repo["has_projects"]}");
                sb.AppendLine($"**Has Downloads:** {repo["has_downloads"]}");
                sb.AppendLine($"**Has Pages:** {repo["has_pages"]}");
                sb.AppendLine($"**Has Discussions:** {repo["has_discussions"]}");
                
                return sb.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error formatting repository information");
                return $"Error formatting repository information: {ex.Message}\n\nRaw JSON:\n{jsonContent}";
            }
        }
    }
}