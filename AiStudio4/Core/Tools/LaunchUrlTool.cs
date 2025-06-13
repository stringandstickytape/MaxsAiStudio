// \AiStudio4\Core\Tools\LaunchUrlTool.cs
﻿








using System.Runtime.InteropServices;



namespace AiStudio4.Core.Tools
{
    /// <summary>
    /// Implementation of the LaunchUrl tool
    /// </summary>
    public class LaunchUrlTool : BaseToolImplementation
    {
        public LaunchUrlTool(ILogger<LaunchUrlTool> logger, IGeneralSettingsService generalSettingsService, IStatusMessageService statusMessageService) : base(logger, generalSettingsService, statusMessageService)
        {
        }

        /// <summary>
        /// Gets the LaunchUrl tool definition
        /// </summary>
        public override Tool GetToolDefinition()
        {
            return new Tool
            {
                Guid = ToolGuids.LAUNCH_URL_TOOL_GUID,
                Name = "LaunchUrl",
                Description = "Launches one or more URLs in the default web browser.",
                Schema = """
{
  "name": "LaunchUrl",
  "description": "Launches one or more URLs in the default web browser.",
  "input_schema": {
    "properties": {
      "urls": { "title": "URLs", "description": "An array of URLs to launch.", "type": "array", "items": { "type": "string" } }
    },
    "required": ["urls"],
    "title": "launchUrlArguments",
    "type": "object"
  }
}
""",
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
            SendStatusUpdate("Starting LaunchUrl tool execution...");
            var parameters = Newtonsoft.Json.JsonConvert.DeserializeObject<JObject>(toolParameters);
            var urls = parameters?["urls"]?.ToObject<List<string>>() ?? new List<string>();
            var results = new StringBuilder();
            bool overallSuccess = true;

            if (!urls.Any())
            {
                _logger.LogWarning("LaunchUrlTool called with no URLs.");
                SendStatusUpdate("Error: No URLs provided.");
                return Task.FromResult(CreateResult(false, false, "No URLs provided."));
            }
            
            SendStatusUpdate($"Launching {urls.Count} URL(s)...");

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
                    SendStatusUpdate($"Launching URL: {url}");
                    // Use Process.Start with UseShellExecute = true for cross-platform compatibility
                    // and to open in the default browser.
                    Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                    results.AppendLine($"Successfully launched: {url}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error launching URL: {Url}", url);
                    SendStatusUpdate($"Error launching URL: {url}");
                    results.AppendLine($"Failed to launch: {url}. Error: {ex.Message}");
                    overallSuccess = false;
                }
            }

            _logger.LogInformation("LaunchUrl tool finished processing {Count} URLs.", urls.Count);
            SendStatusUpdate(overallSuccess ? "All URLs launched successfully." : "Completed with some errors. See details.");
            return Task.FromResult(CreateResult(overallSuccess, false, results.ToString()));
        }
    }
}
