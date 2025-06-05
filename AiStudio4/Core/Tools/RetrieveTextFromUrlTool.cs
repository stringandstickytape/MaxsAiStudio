﻿using AiStudio4.Core.Interfaces;
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
    
    
    
    
    public class RetrieveTextFromUrlTool : BaseToolImplementation
    {
        private readonly HttpClient _httpClient;

        public RetrieveTextFromUrlTool(ILogger<RetrieveTextFromUrlTool> logger, IGeneralSettingsService generalSettingsService, IStatusMessageService statusMessageService) : base(logger, generalSettingsService, statusMessageService)
        {
            _httpClient = new HttpClient();
            
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
            
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "AiStudio4/1.0 TextRetrievalTool");
        }

        
        
        
        public override Tool GetToolDefinition()
        {
            return new Tool
            {
                Guid = ToolGuids.RETRIEVE_TEXT_FROM_URL_TOOL_GUID, 
                Name = "RetrieveTextFromUrl",
                Description = "Fetches text content from URLs by removing HTML tags.",
                Schema = """
{
  "name": "RetrieveTextFromUrl",
  "description": "Retrieves the text content from one or more URLs by removing HTML tags. Returns only the textual content without markup. Useful for extracting readable content from web pages.",
  "input_schema": {
    "properties": {
      "urls": { "anyOf": [{ "items": { "type": "string" }, "type": "array" }, { "type": "string" }], "description": "URL or array of URLs to retrieve text content from" },
      "timeout": { "type": "integer", "description": "Timeout in seconds for each request (default: 30)", "default": 30 }
    },
    "required": ["urls"],
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
            _logger.LogInformation("RetrieveTextFromUrl tool called");
            SendStatusUpdate("Starting RetrieveTextFromUrl tool execution...");
            var resultBuilder = new StringBuilder();

            try
            {
                var parameters = JsonConvert.DeserializeObject<Dictionary<string, object>>(toolParameters);
                var urlsObject = parameters["urls"];
                List<string> urlsToRetrieve = new List<string>();

                
                if (parameters.TryGetValue("timeout", out var timeoutObj) && timeoutObj is int timeout)
                {
                    _httpClient.Timeout = TimeSpan.FromSeconds(timeout);
                    SendStatusUpdate($"Setting timeout to {timeout} seconds.");
                }

                
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
                        
                        SendStatusUpdate($"Fetching content from: {url}");
                        string htmlContent = await _httpClient.GetStringAsync(url);

                        
                        SendStatusUpdate("Extracting text from HTML content...");
                        string textContent = ExtractTextFromHtml(htmlContent);

                        
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

                    resultBuilder.AppendLine(); 
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

        
        
        
        private string ExtractTextFromHtml(string html)
        {
            if (string.IsNullOrWhiteSpace(html))
                return string.Empty;

            try
            {
                
                html = Regex.Replace(html, @"<script[^>]*>.*?</script>", "", RegexOptions.Singleline);

                
                html = Regex.Replace(html, @"<style[^>]*>.*?</style>", "", RegexOptions.Singleline);

                
                html = Regex.Replace(html, @"<!--.*?-->", "", RegexOptions.Singleline);

                
                html = html.Replace("&nbsp;", " ")
                           .Replace("&amp;", "&")
                           .Replace("&lt;", "<")
                           .Replace("&gt;", ">")
                           .Replace("&quot;", "\"")
                           .Replace("&apos;", "'");

                
                html = Regex.Replace(html, @"<(h[1-6]|p|div|section|article|header|footer|br|li)[^>]*>", "\n", RegexOptions.IgnoreCase);
                html = Regex.Replace(html, @"</(h[1-6]|p|div|section|article|header|footer|li)[^>]*>", "\n", RegexOptions.IgnoreCase);

                
                html = Regex.Replace(html, @"<[^>]*>", "");

                
                html = System.Net.WebUtility.HtmlDecode(html);

                
                html = Regex.Replace(html, @"\s+", " ");
                html = Regex.Replace(html, @"\n+", "\n");
                html = Regex.Replace(html, @"^\s+|\s+$", "", RegexOptions.Multiline);

                return html.Trim();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting text from HTML");
                
                return Regex.Replace(html, @"<[^>]*>", "").Trim();
            }
        }

        
        
        
        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}
