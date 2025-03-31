using AiStudio4.Core.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace AiStudio4.Core.Tools
{
    /// <summary>
    /// Implementation of the RunDuckDuckGoSearch tool that searches DuckDuckGo and returns formatted results
    /// </summary>
    public class RunDuckDuckGoSearchTool : BaseToolImplementation
    {
        private readonly HttpClient _httpClient;

        public RunDuckDuckGoSearchTool(ILogger<RunDuckDuckGoSearchTool> logger) : base(logger)
        {
            _httpClient = new HttpClient();
            // Set reasonable default timeout
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
            // Set a user agent to avoid being blocked by DuckDuckGo
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
        }

        /// <summary>
        /// Gets the RunDuckDuckGoSearch tool definition
        /// </summary>
        public override Tool GetToolDefinition()
        {
            return new Tool
            {
                Guid = "d4e5f6g7-h8i9-j0k1-l2m3-n4o5p6q7r8s9", // Fixed GUID for RunDuckDuckGoSearch
                Name = "RunDuckDuckGoSearch",
                Description = "Searches DuckDuckGo and returns formatted search results.",
                Schema = @"{
  ""name"": ""RunDuckDuckGoSearch"",
  ""description"": ""Searches DuckDuckGo and returns a formatted list of search results with titles, snippets, and URLs."",
  ""input_schema"": {
                ""properties"": {
                ""query"": {
                    ""type"": ""string"",
                    ""description"": ""The search query to send to DuckDuckGo""
                },
                ""maxResults"": {
                    ""type"": ""integer"",
                    ""description"": ""Maximum number of results to return (default: 10)"",
                    ""default"": 10
                }
            },
            ""required"": [""query""],
            ""type"": ""object""
  }
}",
                Categories = new List<string> { "MaxCode"},
                Filetype = string.Empty,
                LastModified = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Processes a RunDuckDuckGoSearch tool call
        /// </summary>
        public override async Task<BuiltinToolResult> ProcessAsync(string toolParameters)
        {
            _logger.LogInformation("RunDuckDuckGoSearch tool called");
            var resultBuilder = new StringBuilder();

            try
            {
                var parameters = JsonConvert.DeserializeObject<Dictionary<string, object>>(toolParameters);
                string query = parameters["query"].ToString();
                int maxResults = 10; // Default

                if (parameters.TryGetValue("maxResults", out var maxResultsObj) && maxResultsObj is int maxResultsValue)
                {
                    maxResults = maxResultsValue;
                }

                // Encode the query for URL
                string encodedQuery = HttpUtility.UrlEncode(query);
                string searchUrl = $"https://html.duckduckgo.com/html?q={encodedQuery}";

                // Fetch the search results
                string htmlContent = await _httpClient.GetStringAsync(searchUrl);

                // Parse and format the results
                string formattedResults = ParseDuckDuckGoResults(htmlContent, maxResults);

                resultBuilder.AppendLine($"--- DuckDuckGo Search Results for: {query} ---\n");
                resultBuilder.AppendLine(formattedResults);

                return CreateResult(true, true, resultBuilder.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing RunDuckDuckGoSearch tool");
                return CreateResult(true, true, $"Error processing RunDuckDuckGoSearch tool: {ex.Message}");
            }
        }

        /// <summary>
        /// Parses DuckDuckGo HTML search results and formats them
        /// </summary>
        private string ParseDuckDuckGoResults(string html, int maxResults)
        {
            var resultBuilder = new StringBuilder();
            int count = 0;

            try
            {
                // Find all search result divs
                // This pattern looks for divs with the class "result results_links results_links_deep web-result"
                var resultPattern = new Regex(@"<div class=""result results_links results_links_deep web-result[^""]*""[\s\S]*?<a rel=""nofollow"" class=""result__a"" href=""([^""]*)"">([\s\S]*?)</a>[\s\S]*?<a class=""result__url""[^>]*>([\s\S]*?)</a>[\s\S]*?<a class=""result__snippet""[^>]*>([\s\S]*?)</a>");
                var matches = resultPattern.Matches(html);

                foreach (Match match in matches)
                {
                    if (count >= maxResults) break;

                    // Extract the URL, title, display URL, and snippet
                    string fullUrl = match.Groups[1].Value;
                    string title = match.Groups[2].Value.Trim();
                    string displayUrl = match.Groups[3].Value.Trim();
                    string snippet = match.Groups[4].Value.Trim();

                    // Clean up HTML entities and tags in the snippet
                    snippet = Regex.Replace(snippet, @"<[^>]+>", "");
                    snippet = System.Net.WebUtility.HtmlDecode(snippet);

                    // Extract the actual URL from DuckDuckGo's redirect URL
                    string actualUrl = ExtractUrlFromDuckDuckGoRedirect(fullUrl);

                    // Format the result
                    resultBuilder.AppendLine($"{count + 1}. {title}");
                    resultBuilder.AppendLine($"   URL: {actualUrl}");
                    resultBuilder.AppendLine($"   {snippet}");
                    resultBuilder.AppendLine();

                    count++;
                }

                if (count == 0)
                {
                    resultBuilder.AppendLine("No results found.");
                }
                else
                {
                    resultBuilder.AppendLine($"Total results: {count}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing DuckDuckGo results");
                resultBuilder.AppendLine($"Error parsing results: {ex.Message}");
            }

            return resultBuilder.ToString();
        }

        /// <summary>
        /// Extracts the actual URL from DuckDuckGo's redirect URL
        /// </summary>
        private string ExtractUrlFromDuckDuckGoRedirect(string duckDuckGoUrl)
        {
            try
            {
                // DuckDuckGo URLs are in the format //duckduckgo.com/l/?uddg=https%3A%2F%2Fwww.example.com%2F&rut=....
                // We want to extract the actual URL (uddg parameter)
                if (duckDuckGoUrl.Contains("uddg="))
                {
                    int startIndex = duckDuckGoUrl.IndexOf("uddg=") + 5;
                    int endIndex = duckDuckGoUrl.IndexOf("&", startIndex);
                    
                    if (endIndex == -1) // If there are no more parameters
                        endIndex = duckDuckGoUrl.Length;
                    
                    string encodedUrl = duckDuckGoUrl.Substring(startIndex, endIndex - startIndex);
                    return HttpUtility.UrlDecode(encodedUrl);
                }
                
                // If the URL doesn't follow the expected pattern, return it as is
                return duckDuckGoUrl;
            }
            catch
            {
                // If any error occurs during extraction, return the original URL
                return duckDuckGoUrl;
            }
        }

        /// <summary>
        /// Clean up resources when the tool is disposed
        /// </summary>
        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}
