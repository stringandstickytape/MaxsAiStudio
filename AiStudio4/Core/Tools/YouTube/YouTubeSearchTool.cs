﻿using AiStudio4.Core.Interfaces;
using AiStudio4.Core.Models;
using AiStudio4.InjectedDependencies;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace AiStudio4.Core.Tools.YouTube
{
    
    
    
    public class YouTubeSearchTool : BaseToolImplementation, IDisposable
    {
        
        private const string ApiBaseUrl = "https://www.googleapis.com/youtube/v3/search";
        private readonly HttpClient _httpClient;
        private readonly IGeneralSettingsService _generalSettingsService;

        public YouTubeSearchTool(ILogger<YouTubeSearchTool> logger, IGeneralSettingsService generalSettingsService, IStatusMessageService statusMessageService) : base(logger, generalSettingsService, statusMessageService)
        {
            _generalSettingsService = generalSettingsService;
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(30); 
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "AiStudio4/1.0 YouTubeSearchTool");
        }

        
        
        
        public override Tool GetToolDefinition()
        {
            return new Tool
            {
                Guid = ToolGuids.YOUTUBE_SEARCH_TOOL_GUID,
                Name = "YouTubeSearch",
                Description = "Searches YouTube for videos, channels, or playlists based on a query.",
                Schema = """
{
  "name": "YouTubeSearch",
  "description": "Performs a search on YouTube using the specified query. Returns a list of videos, channels, and playlists matching the query.",
  "input_schema": {
    "type": "object",
    "properties": {
      "query": { "type": "string", "description": "The search query term." },
      "maxResults": { "type": "integer", "description": "The maximum number of results to return (1-50).", "default": 10, "minimum": 1, "maximum": 50 },
      "type": { "type": "string", "description": "Comma-separated list of resource types to search (e.g., video,channel,playlist).", "default": "video,channel,playlist" }
    },
    "required": ["query"]
  }
}
""",
                Categories = new List<string> { "Development" },
                OutputFileType = "", 
                Filetype = string.Empty,
                LastModified = DateTime.UtcNow
            };
        }

        
        
        
        public override async Task<BuiltinToolResult> ProcessAsync(string toolParameters, Dictionary<string, string> extraProperties)
        {
            _logger.LogInformation("YouTubeSearch tool called with parameters: {Parameters}", toolParameters);
            SendStatusUpdate("Starting YouTubeSearch tool execution...");
            var overallResultBuilder = new StringBuilder();
            var serializer = new JsonSerializer();
            bool anySuccess = false;
            bool allSuccess = true;

            try
            {
                using (var stringReader = new StringReader(toolParameters))
                using (var jsonReader = new JsonTextReader(stringReader) { SupportMultipleContent = true })
                {
                    int requestIndex = 0;
                    while (await jsonReader.ReadAsync())
                    {
                        if (jsonReader.TokenType == JsonToken.StartObject)
                        {
                            requestIndex++;
                            _logger.LogInformation("Processing search request #{Index}", requestIndex);
                            SendStatusUpdate($"Processing search request #{requestIndex}...");
                            try
                            {
                                var parameters = serializer.Deserialize<Dictionary<string, object>>(jsonReader);
                                string singleResult = await ProcessSingleSearchRequestAsync(parameters);
                                overallResultBuilder.AppendLine(singleResult);
                                anySuccess = true; 
                            }
                            catch (JsonSerializationException jsonEx)
                            {
                                _logger.LogError(jsonEx, "Error deserializing or processing search request #{Index}", requestIndex);
                                overallResultBuilder.AppendLine($"## Error processing request #{requestIndex}: Invalid JSON format. {jsonEx.Message}\n");
                                allSuccess = false;
                            }
                            catch (ArgumentException argEx)
                            {
                                _logger.LogError(argEx, "Error processing search request #{Index}", requestIndex);
                                overallResultBuilder.AppendLine($"## Error processing request #{requestIndex}: {argEx.Message}\n");
                                allSuccess = false;
                            }
                            catch (HttpRequestException httpEx)
                            {
                                _logger.LogError(httpEx, "HTTP request error during search request #{Index}", requestIndex);
                                overallResultBuilder.AppendLine($"## Error processing request #{requestIndex}: Failed to connect to YouTube API. {httpEx.Message}\n");
                                allSuccess = false;
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "An unexpected error occurred during search request #{Index}", requestIndex);
                                overallResultBuilder.AppendLine($"## Error processing request #{requestIndex}: An unexpected error occurred. {ex.Message}\n");
                                allSuccess = false;
                            }
                        }
                    }

                    if (requestIndex == 0) 
                    {
                         _logger.LogWarning("No valid JSON objects found in the input parameters.");
                         SendStatusUpdate("Error: No valid JSON search requests found in input.");
                         return CreateResult(true, false, "Error: Input did not contain any valid JSON search requests.");
                    }
                }

                
                SendStatusUpdate(allSuccess ? "YouTube search completed successfully." : "YouTube search completed with some errors.");
                return CreateResult(true, allSuccess && anySuccess, overallResultBuilder.ToString().TrimEnd());
            }
            catch (Exception ex) 
            {
                _logger.LogError(ex, "An unexpected error occurred while processing multiple YouTube search requests.");
                SendStatusUpdate($"Error processing YouTubeSearch tool: {ex.Message}");
                return CreateResult(true, false, $"Error: An unexpected error occurred while parsing search requests. {ex.Message}");
            }
        }

        
        
        
        
        private async Task<string> ProcessSingleSearchRequestAsync(Dictionary<string, object> parameters)
        {
            
            string apiKey = _generalSettingsService.GetDecryptedYouTubeApiKey();
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                _logger.LogWarning("YouTube API Key is not configured.");
                SendStatusUpdate("Error: YouTube API Key is not configured.");
                return "## Error: YouTube API Key is not configured. Please set it in the File -> Secrets menu.";
            }
            

            if (!parameters.TryGetValue("query", out var queryObj) || string.IsNullOrWhiteSpace(queryObj as string))
            {
                throw new ArgumentException("Error: 'query' parameter is required and cannot be empty.");
            }

            string query = queryObj.ToString();
            int maxResults = 10; 
            string type = "video,channel,playlist"; 

            if (parameters.TryGetValue("maxResults", out var maxResultsObj))
            {
                if (int.TryParse(maxResultsObj.ToString(), out int parsedMaxResults))
                {
                    maxResults = Math.Clamp(parsedMaxResults, 1, 50);
                }
                else
                {
                    _logger.LogWarning("'maxResults' parameter was provided but could not be parsed as an integer: {Value}. Using default {Default}.", maxResultsObj, maxResults);
                }
            }

            if (parameters.TryGetValue("type", out var typeObj) && typeObj is string typeStr && !string.IsNullOrWhiteSpace(typeStr))
            {
                type = typeStr;
            }

            
            SendStatusUpdate($"Searching YouTube for: {query}...");
            var searchResult = await SearchYouTube(query, maxResults, type);

            
            var outputBuilder = new StringBuilder();
            outputBuilder.AppendLine($"## YouTube Search Results for \\\"{query}\\\":");
            outputBuilder.AppendLine(); 

            if (searchResult?.items != null && searchResult.items.Any(i => i.id?.kind == "youtube#video"))
            {
                var videosFound = false;
                foreach (var item in searchResult.items)
                {
                    if (item.id?.kind == "youtube#video" && !string.IsNullOrEmpty(item.id.videoId))
                    {
                        videosFound = true;
                        string title = item.snippet?.title ?? "(No Title)";
                        
                        title = title.Replace("[", "\\[").Replace("]", "\\]");
                        string url = $"https://www.youtube.com/watch?v={item.id.videoId}";
                        outputBuilder.AppendLine($"* [{title}]({url})");
                    }
                    
                    
                    
                    
                    
                    
                    
                    
                    
                    
                    
                    
                    
                }
                if (!videosFound)
                {
                    outputBuilder.AppendLine("No video results found.");
                }
            }
            else
            {
                outputBuilder.AppendLine("No video results found.");
            }

            return outputBuilder.ToString();
        }

        private async Task<YouTubeSearchResult> SearchYouTube(string query, int maxResults, string type)
        {
            string apiKey = _generalSettingsService.GetDecryptedYouTubeApiKey();
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                 
                 
                _logger.LogError("YouTube Search attempted without an API key.");
                throw new InvalidOperationException("YouTube API Key is missing or not configured.");
            }

            var urlBuilder = new UriBuilder(ApiBaseUrl);
            var queryParams = HttpUtility.ParseQueryString(string.Empty);
            queryParams["part"] = "snippet";
            queryParams["q"] = query;
            queryParams["maxResults"] = maxResults.ToString();
            queryParams["type"] = type;
            queryParams["key"] = apiKey; 

            urlBuilder.Query = queryParams.ToString();
            string requestUrl = urlBuilder.ToString();

            _logger.LogInformation("YouTube Search URL: {RequestUrl}", requestUrl);

            var response = await _httpClient.GetAsync(requestUrl);
            response.EnsureSuccessStatusCode(); 

            string jsonResponse = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<YouTubeSearchResult>(jsonResponse);
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }

    
    public class YouTubeSearchResult
    {
        public string kind { get; set; }
        public string etag { get; set; }
        public string nextPageToken { get; set; }
        public string regionCode { get; set; }
        public PageInfo pageInfo { get; set; }
        public List<Item> items { get; set; }
    }

    public class PageInfo
    {
        public int totalResults { get; set; }
        public int resultsPerPage { get; set; }
    }

    public class Item
    {
        public string kind { get; set; }
        public string etag { get; set; }
        public Id id { get; set; }
        public Snippet snippet { get; set; }
    }

    public class Id
    {
        public string kind { get; set; }
        public string videoId { get; set; }
        public string channelId { get; set; }
        public string playlistId { get; set; }
    }

    public class Snippet
    {
        public DateTime publishedAt { get; set; }
        public string channelId { get; set; }
        public string title { get; set; }
        public string description { get; set; }
        public Thumbnails thumbnails { get; set; }
        public string channelTitle { get; set; }
        public string liveBroadcastContent { get; set; }
        public string publishTime { get; set; }
    }

    public class Thumbnails
    {
        public Default Default { get; set; }
        public Medium Medium { get; set; }
        public High High { get; set; }
    }

    public class Default
    {
        public string url { get; set; }
        public int width { get; set; }
        public int height { get; set; }
    }

    public class Medium
    {
        public string url { get; set; }
        public int width { get; set; }
        public int height { get; set; }
    }

    public class High
    {
        public string url { get; set; }
        public int width { get; set; }
        public int height { get; set; }
    }
}
