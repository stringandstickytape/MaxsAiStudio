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
    /// Implementation of the GitHub Search Code API tool
    /// </summary>
    public class GitHubSearchCodeTool : BaseToolImplementation
    {
        private readonly HttpClient _httpClient;

        public GitHubSearchCodeTool(ILogger<GitHubSearchCodeTool> logger, IGeneralSettingsService generalSettingsService, IStatusMessageService statusMessageService) 
            : base(logger, generalSettingsService, statusMessageService)
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "AiStudio4-GitHub-Tool");
            _httpClient.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
        }

        /// <summary>
        /// Gets the GitHub Search Code tool definition
        /// </summary>
        public override Tool GetToolDefinition()
        {
            return new Tool
            {
                Guid = "6172c3d4-e5f6-7890-1234-56789abcdef04",
                Name = "GitHubSearchCode",
                Description = "Searches for code using GitHub's code search API via the /search/code endpoint.",
                Schema = @"{
  ""name"": ""GitHubSearchCode"",
  ""description"": ""Searches for code using GitHub's code search API via the /search/code endpoint."",
  ""input_schema"": {
    ""properties"": {
      ""q"": {
        ""title"": ""Query"",
        ""type"": ""string"",
        ""description"": ""The search query with qualifiers (e.g., 'addClass repo:jquery/jquery language:javascript')""
      },
      ""per_page"": {
        ""title"": ""Results Per Page"",
        ""type"": ""integer"",
        ""description"": ""Number of results per page (default: 30, max: 100)""
      },
      ""page"": {
        ""title"": ""Page"",
        ""type"": ""integer"",
        ""description"": ""Page number for paginated results (default: 1)""
      },
      ""sort"": {
        ""title"": ""Sort"",
        ""type"": ""string"",
        ""description"": ""Sort field, can be 'indexed' (default) or 'best-match'""
      },
      ""order"": {
        ""title"": ""Order"",
        ""type"": ""string"",
        ""description"": ""Sort order, can be 'desc' (default) or 'asc'""
      }
    },
    ""required"": [""q""],
    ""title"": ""GitHubSearchCodeArguments"",
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
                SendStatusUpdate("Starting GitHub Search Code tool execution...");
                var parameters = JsonConvert.DeserializeObject<Dictionary<string, object>>(toolParameters) ?? new Dictionary<string, object>();

                // Extract required parameter
                if (!parameters.TryGetValue("q", out var queryObj) || !(queryObj is string query) || string.IsNullOrWhiteSpace(query))
                {
                    return CreateResult(true, true, "Error: 'q' parameter is required.");
                }

                // Extract optional parameters
                int perPage = 30; // Default value
                if (parameters.TryGetValue("per_page", out var perPageObj))
                {
                    if (perPageObj is long perPageLong)
                    {
                        perPage = (int)perPageLong;
                    }
                    else if (perPageObj is int perPageInt)
                    {
                        perPage = perPageInt;
                    }
                }
                // Ensure perPage is within valid range
                perPage = Math.Max(1, Math.Min(100, perPage));

                int page = 1; // Default value
                if (parameters.TryGetValue("page", out var pageObj))
                {
                    if (pageObj is long pageLong)
                    {
                        page = (int)pageLong;
                    }
                    else if (pageObj is int pageInt)
                    {
                        page = pageInt;
                    }
                }
                // Ensure page is positive
                page = Math.Max(1, page);

                string sort = null;
                if (parameters.TryGetValue("sort", out var sortObj) && sortObj is string sortStr && !string.IsNullOrWhiteSpace(sortStr))
                {
                    sort = sortStr.ToLowerInvariant();
                    // Validate sort value
                    if (sort != "indexed" && sort != "best-match")
                    {
                        return CreateResult(true, true, "Error: 'sort' parameter must be 'indexed' or 'best-match'.");
                    }
                }

                string order = null;
                if (parameters.TryGetValue("order", out var orderObj) && orderObj is string orderStr && !string.IsNullOrWhiteSpace(orderStr))
                {
                    order = orderStr.ToLowerInvariant();
                    // Validate order value
                    if (order != "desc" && order != "asc")
                    {
                        return CreateResult(true, true, "Error: 'order' parameter must be 'desc' or 'asc'.");
                    }
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
                return await SearchCodeAsync(query, perPage, page, sort, order);
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

        private async Task<BuiltinToolResult> SearchCodeAsync(string query, int perPage, int page, string sort, string order)
        {
            try
            {
                SendStatusUpdate($"Searching for code with query: {query}...");
                
                // Build the URL with query parameters
                var urlBuilder = new StringBuilder("https://api.github.com/search/code");
                urlBuilder.Append($"?q={Uri.EscapeDataString(query)}");
                urlBuilder.Append($"&per_page={perPage}");
                urlBuilder.Append($"&page={page}");
                
                if (!string.IsNullOrEmpty(sort))
                {
                    urlBuilder.Append($"&sort={sort}");
                }
                
                if (!string.IsNullOrEmpty(order))
                {
                    urlBuilder.Append($"&order={order}");
                }
                
                string url = urlBuilder.ToString();
                
                var response = await _httpClient.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorObj = JObject.Parse(content);
                    string errorMessage = errorObj["message"]?.ToString() ?? "Unknown error";
                    return CreateResult(true, true, $"GitHub API Error: {errorMessage} (Status code: {response.StatusCode})");
                }
                
                var formattedContent = FormatSearchResults(content, query, page, perPage);
                
                SendStatusUpdate("Successfully retrieved search results.");
                return CreateResult(true, true, formattedContent);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error searching code");
                return CreateResult(true, true, $"Error searching code: {ex.Message}");
            }
        }

        private string FormatSearchResults(string jsonContent, string query, int page, int perPage)
        {
            try
            {
                var searchResult = JObject.Parse(jsonContent);
                var sb = new StringBuilder();
                
                int totalCount = searchResult["total_count"]?.ToObject<int>() ?? 0;
                bool incompleteResults = searchResult["incomplete_results"]?.ToObject<bool>() ?? false;
                var items = searchResult["items"] as JArray;
                
                sb.AppendLine($"# GitHub Code Search Results");
                sb.AppendLine();
                sb.AppendLine($"**Query:** {query}");
                sb.AppendLine($"**Total Results:** {totalCount}");
                sb.AppendLine($"**Page:** {page}");
                sb.AppendLine($"**Results Per Page:** {perPage}");
                
                if (incompleteResults)
                {
                    sb.AppendLine();
                    sb.AppendLine("⚠️ **Note:** The search results might be incomplete due to timeout or other limitations.");
                }
                
                sb.AppendLine();
                
                if (items == null || items.Count == 0)
                {
                    sb.AppendLine("No results found for this query.");
                    return sb.ToString();
                }
                
                sb.AppendLine("## Results");
                sb.AppendLine();
                
                int resultNumber = (page - 1) * perPage + 1;
                
                foreach (var item in items)
                {
                    string name = item["name"]?.ToString() ?? "Unknown";
                    string path = item["path"]?.ToString() ?? "Unknown";
                    string htmlUrl = item["html_url"]?.ToString() ?? "#";
                    string repository = item["repository"]?["full_name"]?.ToString() ?? "Unknown";
                    
                    sb.AppendLine($"### {resultNumber}. {name}");
                    sb.AppendLine();
                    sb.AppendLine($"**Repository:** {repository}");
                    sb.AppendLine($"**Path:** {path}");
                    sb.AppendLine($"**URL:** {htmlUrl}");
                    sb.AppendLine();
                    
                    // Add code snippets if available
                    var textMatches = item["text_matches"] as JArray;
                    if (textMatches != null && textMatches.Count > 0)
                    {
                        sb.AppendLine("**Matches:**");
                        sb.AppendLine();
                        
                        foreach (var match in textMatches)
                        {
                            string fragment = match["fragment"]?.ToString();
                            if (!string.IsNullOrEmpty(fragment))
                            {
                                sb.AppendLine("```");
                                sb.AppendLine(fragment);
                                sb.AppendLine("```");
                                sb.AppendLine();
                            }
                        }
                    }
                    
                    resultNumber++;
                }
                
                // Add pagination info
                int totalPages = (int)Math.Ceiling((double)totalCount / perPage);
                sb.AppendLine($"Page {page} of {totalPages} (showing {items.Count} of {totalCount} results)");
                
                return sb.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error formatting search results");
                return $"Error formatting search results: {ex.Message}\n\nRaw JSON:\n{jsonContent}";
            }
        }
    }
}