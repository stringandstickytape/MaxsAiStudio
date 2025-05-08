using AiStudio4.Core.Interfaces;
using AiStudio4.Core.Models;
using AiStudio4.InjectedDependencies;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AiStudio4.Core.Tools
{
    /// <summary>
    /// Implementation of the RetrieveTextFromUrl tool that fetches text content from URLs
    /// and strips out HTML tags
    /// </summary>
    public class RetrieveTextFromUrlTool : BaseToolImplementation
    {
        private readonly HttpClient _httpClient;

        public RetrieveTextFromUrlTool(ILogger<RetrieveTextFromUrlTool> logger, IGeneralSettingsService generalSettingsService, IStatusMessageService statusMessageService) : base(logger, generalSettingsService, statusMessageService)
        {
            _httpClient = new HttpClient();
            // Set reasonable default timeout
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
            // Set a user agent to avoid being blocked by some websites
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "AiStudio4/1.0 TextRetrievalTool");
        }

        /// <summary>
        /// Gets the RetrieveTextFromUrl tool definition
        /// </summary>
        public override Tool GetToolDefinition()
        {
            return new Tool
            {
                Guid = "c3d4e5f6-a7b8-9012-3456-7890abcdef08", // Fixed GUID for RetrieveTextFromUrl
                Name = "RetrieveTextFromUrl",
                Description = "Fetches text content from URLs by removing HTML tags.",
                Schema = @"{
  ""name"": ""RetrieveTextFromUrl"",
  ""description"": ""Retrieves the text content from one or more URLs by removing HTML tags. Returns only the textual content without markup. Useful for extracting readable content from web pages."",
  ""input_schema"": {
                ""properties"": {
                ""urls"": {
                    ""anyOf"": [
                        {""items"": {""type"": ""string""}, ""type"": ""array""},
                        {""type"": ""string""}
                    ],
                    ""description"": ""URL or array of URLs to retrieve text content from""
                },
                ""timeout"": {
                    ""type"": ""integer"",
                    ""description"": ""Timeout in seconds for each request (default: 30)"",
                    ""default"": 30
                }
            },
            ""required"": [""urls""],
            ""type"": ""object""
  }
}",
                Categories = new List<string> { "Development"},
                OutputFileType = "txt",
                Filetype = string.Empty,
                LastModified = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Processes a RetrieveTextFromUrl tool call
        /// </summary>
        public override async Task<BuiltinToolResult> ProcessAsync(string toolParameters, Dictionary<string, string> extraProperties)
        {
            _logger.LogInformation("RetrieveTextFromUrl tool called");
            SendStatusUpdate("Starting RetrieveTextFromUrl tool execution...");
            var resultBuilder = new StringBuilder();

            try
            {
                var parameters = JsonConvert.DeserializeObject<Dictionary<string, object>>(toolParameters);
                var urlsObject = parameters["urls"];
                List<string> urlsToRetrieve = new List<string>();

                // Set timeout if provided
                if (parameters.TryGetValue("timeout", out var timeoutObj) && timeoutObj is int timeout)
                {
                    _httpClient.Timeout = TimeSpan.FromSeconds(timeout);
                    SendStatusUpdate($"Setting timeout to {timeout} seconds.");
                }

                // Handle both single URL and array of URLs
                if (urlsObject is string singleUrl)
                {
                    urlsToRetrieve.Add(singleUrl);
                }
                else if (urlsObject is JArray urlArray)
                {
                    urlsToRetrieve.AddRange(urlArray.Select(u => (string)u));
                }
                else
                {
                    throw new ArgumentException("Invalid format for 'urls' parameter. Expected string or array of strings.");
                }

                // Process each URL
                SendStatusUpdate($"Processing {urlsToRetrieve.Count} URL(s)...");
                foreach (var url in urlsToRetrieve)
                {
                    if (string.IsNullOrWhiteSpace(url))
                    {
                        SendStatusUpdate("Error: Empty URL provided.");
                        resultBuilder.AppendLine("---Error: Empty URL provided---");
                        continue;
                    }

                    if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
                        (uri.Scheme != "http" && uri.Scheme != "https"))
                    {
                        SendStatusUpdate($"Error: Invalid URL format: {url}");
                        resultBuilder.AppendLine($"---Error retrieving {url}: Invalid URL format. Only HTTP and HTTPS are supported.---");
                        continue;
                    }

                    try
                    {
                        // Fetch the content
                        SendStatusUpdate($"Fetching content from: {url}");
                        string htmlContent = await _httpClient.GetStringAsync(url);

                        // Extract text from HTML
                        SendStatusUpdate("Extracting text from HTML content...");
                        string textContent = ExtractTextFromHtml(htmlContent);

                        // Add to results
                        resultBuilder.AppendLine($"--- Content from: {url} ---");
                        resultBuilder.AppendLine(textContent);
                    }
                    catch (HttpRequestException ex)
                    {
                        _logger.LogError(ex, $"HTTP request error for URL: {url}");
                        SendStatusUpdate($"HTTP request error for URL: {url}");
                        resultBuilder.AppendLine($"---Error retrieving {url}: {ex.Message}---");
                    }
                    catch (TaskCanceledException ex)
                    {
                        _logger.LogError(ex, $"Request timed out for URL: {url}");
                        SendStatusUpdate($"Request timed out for URL: {url}");
                        resultBuilder.AppendLine($"---Error retrieving {url}: Request timed out---");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error processing URL: {url}");
                        SendStatusUpdate($"Error processing URL: {url}");
                        resultBuilder.AppendLine($"---Error retrieving {url}: {ex.Message}---");
                    }

                    resultBuilder.AppendLine(); // Add a separator between URLs
                }

                SendStatusUpdate("Text retrieval completed successfully.");
                return CreateResult(true, true, resultBuilder.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing RetrieveTextFromUrl tool");
                SendStatusUpdate($"Error processing RetrieveTextFromUrl tool: {ex.Message}");
                return CreateResult(true, true, $"Error processing RetrieveTextFromUrl tool: {ex.Message}");
            }
        }

        /// <summary>
        /// Extracts plain text from HTML content
        /// </summary>
        private string ExtractTextFromHtml(string html)
        {
            if (string.IsNullOrWhiteSpace(html))
                return string.Empty;

            try
            {
                // Remove script tags and their content
                html = Regex.Replace(html, @"<script[^>]*>.*?</script>", "", RegexOptions.Singleline);

                // Remove style tags and their content
                html = Regex.Replace(html, @"<style[^>]*>.*?</style>", "", RegexOptions.Singleline);

                // Remove all HTML comments
                html = Regex.Replace(html, @"<!--.*?-->", "", RegexOptions.Singleline);

                // Replace common HTML entities
                html = html.Replace("&nbsp;", " ")
                           .Replace("&amp;", "&")
                           .Replace("&lt;", "<")
                           .Replace("&gt;", ">")
                           .Replace("&quot;", "\"")
                           .Replace("&apos;", "'");

                // Replace block-level elements with newlines to preserve structure
                html = Regex.Replace(html, @"<(h[1-6]|p|div|section|article|header|footer|br|li)[^>]*>", "\n", RegexOptions.IgnoreCase);
                html = Regex.Replace(html, @"</(h[1-6]|p|div|section|article|header|footer|li)[^>]*>", "\n", RegexOptions.IgnoreCase);

                // Remove all remaining HTML tags
                html = Regex.Replace(html, @"<[^>]*>", "");

                // Decode HTML entities
                html = System.Net.WebUtility.HtmlDecode(html);

                // Normalize whitespace
                html = Regex.Replace(html, @"\s+", " ");
                html = Regex.Replace(html, @"\n+", "\n");
                html = Regex.Replace(html, @"^\s+|\s+$", "", RegexOptions.Multiline);

                return html.Trim();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting text from HTML");
                // Fall back to a simpler approach
                return Regex.Replace(html, @"<[^>]*>", "").Trim();
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