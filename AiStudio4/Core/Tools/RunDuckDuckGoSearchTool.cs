







using System.Net.Http;

using System.Text.RegularExpressions;

using System.Web;

namespace AiStudio4.Core.Tools
{
    
    
    
    public class RunDuckDuckGoSearchTool : BaseToolImplementation
    {
        private readonly HttpClient _httpClient;

        public RunDuckDuckGoSearchTool(ILogger<RunDuckDuckGoSearchTool> logger, IGeneralSettingsService generalSettingsService, IStatusMessageService statusMessageService) : base(logger, generalSettingsService, statusMessageService)
        {
            _httpClient = new HttpClient();
            
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
            
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
        }

        
        
        
        public override Tool GetToolDefinition()
        {
            return new Tool
            {
                Guid = ToolGuids.RUN_DUCK_DUCK_GO_SEARCH_TOOL_GUID, 
                Name = "RunDuckDuckGoSearch",
                Description = "Searches DuckDuckGo and returns formatted search results.",
                Schema = """
{
  "name": "RunDuckDuckGoSearch",
  "description": "Searches DuckDuckGo and returns a formatted list of search results with titles, snippets, and URLs.",
  "input_schema": {
    "properties": {
      "query": { "type": "string", "description": "The search query to send to DuckDuckGo" },
      "maxResults": { "type": "integer", "description": "Maximum number of results to return (default: 10)", "default": 10 }
    },
    "required": ["query"],
    "type": "object"
  }
}
""",
                Categories = new List<string> { "Development"},
                OutputFileType = "txt",
                Filetype = string.Empty,
                LastModified = DateTime.UtcNow
            };
        }

        
        
        
        public override async Task<BuiltinToolResult> ProcessAsync(string toolParameters, Dictionary<string, string> extraProperties)
        {
            _logger.LogInformation("RunDuckDuckGoSearch tool called");
            SendStatusUpdate("Starting RunDuckDuckGoSearch tool execution...");
            var resultBuilder = new StringBuilder();

            try
            {
                var parameters = JsonConvert.DeserializeObject<Dictionary<string, object>>(toolParameters);
                string query = parameters["query"].ToString();
                int maxResults = 10; 

                if (parameters.TryGetValue("maxResults", out var maxResultsObj) && maxResultsObj is int maxResultsValue)
                {
                    maxResults = maxResultsValue;
                }
                
                SendStatusUpdate($"Searching DuckDuckGo for: {query} (max results: {maxResults})...");

                
                string encodedQuery = HttpUtility.UrlEncode(query);
                string searchUrl = $"https://html.duckduckgo.com/html?q={encodedQuery}";

                
                SendStatusUpdate("Fetching search results from DuckDuckGo...");
                string htmlContent = await _httpClient.GetStringAsync(searchUrl);

                
                SendStatusUpdate("Parsing search results...");
                string formattedResults = ParseDuckDuckGoResults(htmlContent, maxResults);

                resultBuilder.AppendLine($"--- DuckDuckGo Search Results for: {query} ---\n");
                resultBuilder.AppendLine(formattedResults);

                SendStatusUpdate("Search completed successfully.");
                return CreateResult(true, true, resultBuilder.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing RunDuckDuckGoSearch tool");
                SendStatusUpdate($"Error processing RunDuckDuckGoSearch tool: {ex.Message}");
                return CreateResult(true, true, $"Error processing RunDuckDuckGoSearch tool: {ex.Message}");
            }
        }

        
        
        
        private string ParseDuckDuckGoResults(string html, int maxResults)
        {
            var resultBuilder = new StringBuilder();
            int count = 0;

            try
            {
                
                
                var resultPattern = new Regex(@"<div class=""result results_links results_links_deep web-result[^""]*""[\s\S]*?<a rel=""nofollow"" class=""result__a"" href=""([^""]*)"">([\s\S]*?)</a>[\s\S]*?<a class=""result__url""[^>]*>([\s\S]*?)</a>[\s\S]*?<a class=""result__snippet""[^>]*>([\s\S]*?)</a>");
                var matches = resultPattern.Matches(html);

                foreach (Match match in matches)
                {
                    if (count >= maxResults) break;

                    
                    string fullUrl = match.Groups[1].Value;
                    string title = match.Groups[2].Value.Trim();
                    string displayUrl = match.Groups[3].Value.Trim();
                    string snippet = match.Groups[4].Value.Trim();

                    
                    snippet = Regex.Replace(snippet, @"<[^>]+>", "");
                    snippet = System.Net.WebUtility.HtmlDecode(snippet);

                    
                    string actualUrl = ExtractUrlFromDuckDuckGoRedirect(fullUrl);

                    
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

        
        
        
        private string ExtractUrlFromDuckDuckGoRedirect(string duckDuckGoUrl)
        {
            try
            {
                
                
                if (duckDuckGoUrl.Contains("uddg="))
                {
                    int startIndex = duckDuckGoUrl.IndexOf("uddg=") + 5;
                    int endIndex = duckDuckGoUrl.IndexOf("&", startIndex);

                    if (endIndex == -1) 
                        endIndex = duckDuckGoUrl.Length;

                    string encodedUrl = duckDuckGoUrl.Substring(startIndex, endIndex - startIndex);
                    return HttpUtility.UrlDecode(encodedUrl);
                }

                
                return duckDuckGoUrl;
            }
            catch
            {
                
                return duckDuckGoUrl;
            }
        }

        
        
        
        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}
