// AiStudio4/Core/Tools/YouTubeSearchTool.cs
using AiStudio4.Core.Models;
using AiStudio4.InjectedDependencies;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

namespace AiStudio4.Core.Tools
{
    /// <summary>
    /// Tool to search YouTube using the Data API v3.
    /// </summary>
    public class YouTubeSearchTool : BaseToolImplementation, IDisposable
    {
        private string ApiKey = "insert_key_here"; // Hardcoded API Key
        private const string ApiBaseUrl = "https://www.googleapis.com/youtube/v3/search";
        private readonly HttpClient _httpClient;

        public YouTubeSearchTool(ILogger<YouTubeSearchTool> logger, ISettingsService settingsService) : base(logger, settingsService)
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(30); // Default timeout
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "AiStudio4/1.0 YouTubeSearchTool");
        }

        /// <summary>
        /// Gets the YouTubeSearch tool definition.
        /// </summary>
        public override Tool GetToolDefinition()
        {
            return new Tool
            {
                Guid = "d1e2f3a4-b5c6-7890-1234-567890abcdef10", // Generate a unique GUID for this tool
                Name = "YouTubeSearch",
                Description = "Searches YouTube for videos, channels, or playlists based on a query.",
                Schema = @"{ 
  ""name"": ""YouTubeSearch"",
  ""description"": ""Performs a search on YouTube using the specified query. Returns a list of videos, channels, and playlists matching the query."",
  ""input_schema"": {
    ""type"": ""object"",
    ""properties"": {
      ""query"": {
        ""type"": ""string"",
        ""description"": ""The search query term.""
      },
      ""maxResults"": {
        ""type"": ""integer"",
        ""description"": ""The maximum number of results to return (1-50)."",
        ""default"": 10,
        ""minimum"": 1,
        ""maximum"": 50
      },
      ""type"": {
        ""type"": ""string"",
        ""description"": ""Comma-separated list of resource types to search (e.g., video,channel,playlist)."",
        ""default"": ""video,channel,playlist""
      }
    },
    ""required"": [""query""]
  }
}",
                Categories = new List<string> { "Search", "Online Services" },
                OutputFileType = "", // Changed from json to text
                Filetype = string.Empty,
                LastModified = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Processes a YouTubeSearch tool call.
        /// </summary>
        public override async Task<BuiltinToolResult> ProcessAsync(string toolParameters)
        {
            _logger.LogInformation("YouTubeSearch tool called");

            try
            {
                var parameters = JsonConvert.DeserializeObject<Dictionary<string, object>>(toolParameters);

                if (!parameters.TryGetValue("query", out var queryObj) || string.IsNullOrWhiteSpace(queryObj as string))
                {
                    return CreateResult(true, false, "Error: 'query' parameter is required and cannot be empty.");
                }

                string query = queryObj.ToString();
                int maxResults = 10; // Default value
                string type = "video,channel,playlist"; // Default value

                if (parameters.TryGetValue("maxResults", out var maxResultsObj) && int.TryParse(maxResultsObj.ToString(), out int parsedMaxResults))
                {
                    maxResults = Math.Clamp(parsedMaxResults, 1, 50);
                }

                if (parameters.TryGetValue("type", out var typeObj) && !string.IsNullOrWhiteSpace(typeObj as string))
                {
                    type = typeObj.ToString();
                }

                var searchResult = await SearchYouTube(query, maxResults, type);
                //string jsonResult = JsonConvert.SerializeObject(searchResult, Formatting.Indented);

                // Format results as a Markdown list
                var outputBuilder = new System.Text.StringBuilder();
                outputBuilder.AppendLine("## YouTube Search Results:");
                outputBuilder.AppendLine(); // Add a blank line for spacing

                if (searchResult?.items != null && searchResult.items.Any(i => i.id?.kind == "youtube#video"))
                {
                    var videosFound = false;
                    foreach (var item in searchResult.items)
                    {
                        if (item.id?.kind == "youtube#video" && !string.IsNullOrEmpty(item.id.videoId))
                        {
                            videosFound = true;
                            string title = item.snippet?.title ?? "(No Title)";
                            // Escape Markdown characters in title if necessary (e.g., brackets)
                            title = title.Replace("[", "\\[").Replace("]", "\\]");
                            string url = $"https://www.youtube.com/watch?v={item.id.videoId}";
                            outputBuilder.AppendLine($"* [{title}]({url})");
                        }
                        // Optionally handle other types like channels or playlists here
                        // else if (item.id?.kind == "youtube#channel" && !string.IsNullOrEmpty(item.id.channelId))
                        // {
                        //     string title = item.snippet?.title ?? "(No Title)";
                        //     string url = $"https://www.youtube.com/channel/{item.id.channelId}";
                        //     outputBuilder.AppendLine($"* Channel: [{title}]({url})");
                        // }
                        // else if (item.id?.kind == "youtube#playlist" && !string.IsNullOrEmpty(item.id.playlistId))
                        // {
                        //     string title = item.snippet?.title ?? "(No Title)";
                        //     string url = $"https://www.youtube.com/playlist?list={item.id.playlistId}";
                        //     outputBuilder.AppendLine($"* Playlist: [{title}]({url})");
                        // }
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

                return CreateResult(true, true, outputBuilder.ToString());
            }
            catch (JsonSerializationException ex)
            {
                _logger.LogError(ex, "Error deserializing tool parameters: {ToolParameters}", toolParameters);
                return CreateResult(true, false, $"Error: Invalid JSON format in tool parameters. {ex.Message}");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request error while searching YouTube.");
                return CreateResult(true, false, $"Error: Failed to connect to YouTube API. {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while searching YouTube.");
                return CreateResult(true, false, $"Error: An unexpected error occurred. {ex.Message}");
            }
        }

        private async Task<YouTubeSearchResult> SearchYouTube(string query, int maxResults, string type)
        {
            var urlBuilder = new UriBuilder(ApiBaseUrl);
            var queryParams = HttpUtility.ParseQueryString(string.Empty);
            queryParams["part"] = "snippet";
            queryParams["q"] = query;
            queryParams["maxResults"] = maxResults.ToString();
            queryParams["type"] = type;
            queryParams["key"] = ApiKey; // Use the API Key

            urlBuilder.Query = queryParams.ToString();
            string requestUrl = urlBuilder.ToString();

            _logger.LogInformation("YouTube Search URL: {RequestUrl}", requestUrl);

            var response = await _httpClient.GetAsync(requestUrl);
            response.EnsureSuccessStatusCode(); // Throw exception if not a success code.

            string jsonResponse = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<YouTubeSearchResult>(jsonResponse);
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }

    // Define classes to deserialize the JSON response from the YouTube API
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
