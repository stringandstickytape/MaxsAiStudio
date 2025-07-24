// AiStudio4/Core/Tools/GoogleCustomSearchApiTool.cs









using System.Net.Http;
using System.Net.Http.Headers;
using System.Web; // For HttpUtility.UrlEncode
using ModelContextProtocol;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace AiStudio4.Core.Tools
{
    [McpServerToolType]
    public class GoogleCustomSearchApiTool : BaseToolImplementation
    {
        private readonly HttpClient _httpClient;

        public GoogleCustomSearchApiTool(ILogger<GoogleCustomSearchApiTool> logger, IGeneralSettingsService generalSettingsService, IStatusMessageService statusMessageService)
            : base(logger, generalSettingsService, statusMessageService)
        {
            _httpClient = new HttpClient();
            // Google APIs often use `application/json` for Accept.
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "AiStudio4-GoogleCustomSearch-Tool");
        }

        public override Tool GetToolDefinition()
        {
            return new Tool
            {
                Guid = ToolGuids.GOOGLE_CUSTOM_SEARCH_API_TOOL_GUID,
                Name = "GoogleCustomSearchApi", // Must match the "name" field in the schema
                Description = "Performs a search using Google Custom Search API. Requires a configured API Key and a Custom Search Engine ID (cx).",
                Schema = """
{
  "name": "GoogleCustomSearchApi",
  "description": "Performs a search using Google Custom Search API. Returns a list of search results including title, link, and snippet.",
  "input_schema": {
    "type": "object",
    "properties": {
      "query": {
        "type": "string",
        "description": "The search query."
      },
      "num_results": {
        "type": "integer",
        "description": "Number of search results to return. Default is 10, maximum is 50",
        "default": 10,
        "minimum": 1,
        "maximum": 50 
      },
      "start_index": {
        "type": "integer",
        "description": "The index of the first result to return (1-based). Used for pagination. Default is 1",
        "default": 1,
        "minimum": 1
      },
      "language_restrict": {
        "type": "string",
        "description": "Restricts search results to documents written in a particular language (e.g., 'lang_en'). (Optional)"
      },
      "country_restrict": {
        "type": "string",
        "description": "Restricts search results to documents originating in a particular country (e.g., 'countryUS', 'countryCA.countryMX'). (Optional)"
      },
      "site_search": {
        "type": "string",
        "description": "Restricts results to URLs from a specific site (e.g., 'example.com') or domain. (Optional)"
      },
      "exact_terms": {
        "type": "string",
        "description": "Identifies a phrase that all documents in the search results must contain. (Optional)"
      }
    },
    "required": ["query"]
  }
}
""",
                Categories = new List<string> { "Search", "APITools" },
                OutputFileType = "txt", // Or "json" if you prefer raw API output for AI
                Filetype = string.Empty,
                ExtraProperties = new Dictionary<string, string>
                {
                    { "customSearchEngineID", "" },
                },
                LastModified = DateTime.UtcNow
            };
        }

        public override async Task<BuiltinToolResult> ProcessAsync(string toolParameters, Dictionary<string, string> extraProperties)
        {
            SendStatusUpdate("Starting Google Custom Search API tool execution...");
            try
            {
                var parameters = JsonConvert.DeserializeObject<JObject>(toolParameters);
                if (parameters == null)
                {
                    return CreateResult(false, false, "Error: Invalid JSON parameters.");
                }

                string query = parameters["query"]?.ToString();
                string cseId = extraProperties?.GetValueOrDefault("customSearchEngineID");

                if (string.IsNullOrWhiteSpace(query) || string.IsNullOrWhiteSpace(cseId))
                {
                    return CreateResult(false, false, "Error: 'query' and 'customSearchEngineID' parameters are required.");
                }

                // Get API key from settings
                string apiKey = _generalSettingsService.GetDecryptedGoogleCustomSearchApiKey();
                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    return CreateResult(false, false, "Error: Google Custom Search API Key is not configured. Please set it via the application menu.");
                }

                // Optional parameters
                int numResults = parameters["num_results"]?.ToObject<int>() ?? 10;
                int startIndex = parameters["start_index"]?.ToObject<int>() ?? 1;
                string languageRestrict = parameters["language_restrict"]?.ToString();
                string countryRestrict = parameters["country_restrict"]?.ToString();
                string siteSearch = parameters["site_search"]?.ToString();
                string exactTerms = parameters["exact_terms"]?.ToString();

                // Construct API URL
                var queryParams = new Dictionary<string, string>
                {
                    { "key", apiKey },
                    { "cx", cseId },
                    { "q", query },
                    { "number", numResults.ToString() },
                    { "start", startIndex.ToString() }
                };

                if (!string.IsNullOrWhiteSpace(languageRestrict)) queryParams["lr"] = languageRestrict;
                if (!string.IsNullOrWhiteSpace(countryRestrict)) queryParams["cr"] = countryRestrict;
                if (!string.IsNullOrWhiteSpace(siteSearch)) queryParams["siteSearch"] = siteSearch;
                if (!string.IsNullOrWhiteSpace(exactTerms)) queryParams["exactTerms"] = exactTerms;

                string apiUrl = "https://www.googleapis.com/customsearch/v1";
                var uriBuilder = new UriBuilder(apiUrl);
                var httpValueCollection = HttpUtility.ParseQueryString(string.Empty);
                foreach (var kvp in queryParams)
                {
                    httpValueCollection[kvp.Key] = kvp.Value;
                }
                uriBuilder.Query = httpValueCollection.ToString();

                SendStatusUpdate($"Searching Google Custom Search API for: '{query}'...");
                var response = await _httpClient.GetAsync(uriBuilder.ToString());
                string responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Google Custom Search API Error: {StatusCode} - {Content}", response.StatusCode, responseContent);
                    var errorObj = JObject.Parse(responseContent);
                    string errorMessage = errorObj?["error"]?["message"]?.ToString() ?? responseContent;
                    return CreateResult(false, false, $"Google Custom Search API Error: {errorMessage} (Status: {response.StatusCode})");
                }

                var formattedResults = FormatSearchResults(responseContent);
                SendStatusUpdate("Google Custom Search API results retrieved successfully.");
                return CreateResult(true, true, formattedResults);

            }
            catch (JsonException jsonEx)
            {
                _logger.LogError(jsonEx, "Error parsing Google Custom Search tool parameters");
                return CreateResult(false, false, $"Error parsing parameters: {jsonEx.Message}");
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, "HTTP error in Google Custom Search tool");
                return CreateResult(false, false, $"Network error: {httpEx.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in Google Custom Search tool");
                return CreateResult(false, false, $"Unexpected error: {ex.Message}");
            }
        }

        private string FormatSearchResults(string jsonContent)
        {
            try
            {
                var searchData = JObject.Parse(jsonContent);
                var sb = new StringBuilder();

                sb.AppendLine("# Google Custom Search API Results");
                sb.AppendLine();

                var searchInfo = searchData["searchInformation"];
                if (searchInfo != null)
                {
                    sb.AppendLine($"**Total Results:** {searchInfo["totalResults"]}");
                    sb.AppendLine($"**Search Time:** {searchInfo["formattedSearchTime"]} seconds");
                    sb.AppendLine();
                }

                var items = searchData["items"] as JArray;
                if (items == null || !items.Any())
                {
                    sb.AppendLine("No results found.");
                    return sb.ToString();
                }

                int count = 1;
                foreach (var item in items)
                {
                    sb.AppendLine($"## {count}. {item["title"]}");
                    sb.AppendLine($"**Link:** {item["link"]}");
                    sb.AppendLine($"**Snippet:** {item["snippet"]?.ToString().Replace("\n", " ").Trim()}");
                    if (item["pagemap"]?["cse_image"]?[0]?["src"] != null)
                    {
                        sb.AppendLine($"**Image:** {item["pagemap"]["cse_image"][0]["src"]}");
                    }
                    sb.AppendLine();
                    count++;
                }

                return sb.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error formatting Google Custom Search results");
                return $"Error formatting search results: {ex.Message}\n\nRaw JSON:\n{jsonContent}";
            }
        }

        [McpServerTool, Description("Performs a search using Google Custom Search API. Requires a configured API Key and a Custom Search Engine ID (cx).")]
        public async Task<string> GoogleCustomSearchApi([Description("JSON parameters for GoogleCustomSearchApi")] string parameters = "{}")
        {
            return await ExecuteWithExtraProperties(parameters);
        }
    }
}
