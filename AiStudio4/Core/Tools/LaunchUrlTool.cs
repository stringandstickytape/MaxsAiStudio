// C:\Users\maxhe\source\repos\CloneTest\MaxsAiTool\AiStudio4\Core\Tools\LaunchUrlTool.cs
using AiStudio4.Core.Interfaces;
using AiStudio4.Core.Models;
using AiStudio4.InjectedDependencies;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace AiStudio4.Core.Tools
{
    /// <summary>
    /// Implementation of the LaunchUrl tool
    /// </summary>
    public class LaunchUrlTool : BaseToolImplementation
    {
        public LaunchUrlTool(ILogger<CodeDiffTool> logger, IGeneralSettingsService generalSettingsService, IStatusMessageService statusMessageService) : base(logger, generalSettingsService, statusMessageService)
        {
        }

        /// <summary>
        /// Gets the LaunchUrl tool definition
        /// </summary>
        public override Tool GetToolDefinition()
        {
            return new Tool
            {
                Guid = "a1b2c3d4-e5f6-7890-1234-56789abcdef04", // New unique GUID
                Name = "LaunchUrl",
                Description = "Launches one or more URLs in the default web browser.",
                Schema = @"{
  ""name"": ""LaunchUrl"",
  ""description"": ""Launches one or more URLs in the default web browser."",
  ""input_schema"": {
                ""properties"": {
                ""urls"": {
                    ""title"": ""URLs"",
                    ""description"": ""An array of URLs to launch."",
                    ""type"": ""array"",
                    ""items"": {
                        ""type"": ""string""
                    }
                }
            },
            ""required"": [""urls""],
            ""title"": ""launchUrlArguments"",
            ""type"": ""object""
  }
}",
                Categories = new List<string> { "Development" },
                OutputFileType = "",
                Filetype = string.Empty,
                LastModified = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Processes a LaunchUrl tool call
        /// </summary>
        public override Task<BuiltinToolResult> ProcessAsync(string toolParameters, Dictionary<string, string> extraProperties)
        {
            var parameters = Newtonsoft.Json.JsonConvert.DeserializeObject<JObject>(toolParameters);
            var urls = parameters?["urls"]?.ToObject<List<string>>() ?? new List<string>();
            var results = new StringBuilder();
            bool overallSuccess = true;

            if (!urls.Any())
            {
                _logger.LogWarning("LaunchUrlTool called with no URLs.");
                return Task.FromResult(CreateResult(false, false, "No URLs provided."));
            }

            foreach (var url in urls)
            {
                if (string.IsNullOrWhiteSpace(url))
                {
                    _logger.LogWarning("Attempted to launch an empty or whitespace URL.");
                    results.AppendLine($"Skipped empty URL.");
                    overallSuccess = false; // Consider this a partial failure
                    continue;
                }

                try
                {
                    _logger.LogInformation("Launching URL: {Url}", url);
                    // Use Process.Start with UseShellExecute = true for cross-platform compatibility
                    // and to open in the default browser.
                    Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                    results.AppendLine($"Successfully launched: {url}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error launching URL: {Url}", url);
                    results.AppendLine($"Failed to launch: {url}. Error: {ex.Message}");
                    overallSuccess = false;
                }
            }

            _logger.LogInformation("LaunchUrl tool finished processing {Count} URLs.", urls.Count);
            return Task.FromResult(CreateResult(overallSuccess, false, results.ToString()));
        }
    }
}