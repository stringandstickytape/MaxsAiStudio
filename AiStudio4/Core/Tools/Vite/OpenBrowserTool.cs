using ModelContextProtocol;
using ModelContextProtocol.Server;
using System.ComponentModel;









namespace AiStudio4.Core.Tools.Vite
{
    /// <summary>
    /// Implementation of the OpenBrowser tool
    /// </summary>
    [McpServerToolType]
    public class OpenBrowserTool : BaseToolImplementation
    {
        public OpenBrowserTool(ILogger<OpenBrowserTool> logger, IGeneralSettingsService generalSettingsService, IStatusMessageService statusMessageService) 
            : base(logger, generalSettingsService, statusMessageService)
        {
        }

        /// <summary>
        /// Gets the OpenBrowser tool definition
        /// </summary>
        public override Tool GetToolDefinition()
        {
            return new Tool
            {
                Guid = ToolGuids.OPEN_BROWSER_TOOL_GUID,
                Name = "OpenBrowser",
                Description = "Opens a URL in the default or specified web browser",
                Schema = """
{
  "name": "OpenBrowser",
  "description": "Opens a URL in the default or specified web browser.",
  "input_schema": {
    "properties": {
      "url": { "title": "URL", "type": "string", "description": "URL to open in the browser" },
      "browser": { "title": "Browser", "type": "string", "description": "Specific browser to use" }
    },
    "required": ["url"],
    "title": "OpenBrowserArguments",
    "type": "object"
  }
}
""",
                Categories = new List<string> { "Vite" },
                OutputFileType = "txt",
                Filetype = string.Empty,
                LastModified = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Processes an OpenBrowser tool call
        /// </summary>
        public override Task<BuiltinToolResult> ProcessAsync(string toolParameters, Dictionary<string, string> extraProperties)
        {
            try
            {
                SendStatusUpdate("Starting OpenBrowser tool execution...");
                var parameters = JsonConvert.DeserializeObject<Dictionary<string, object>>(toolParameters);

                // Extract parameters
                var url = parameters.ContainsKey("url") ? parameters["url"].ToString() : "";
                var browser = parameters.ContainsKey("browser") ? parameters["browser"].ToString() : "";

                if (string.IsNullOrEmpty(url))
                {
                    return Task.FromResult(CreateResult(false, true, "Error: URL is required."));
                }

                // Validate URL format
                if (!Uri.TryCreate(url, UriKind.Absolute, out Uri uri) || 
                    (uri.Scheme != "http" && uri.Scheme != "https"))
                {
                    SendStatusUpdate("Error: Invalid URL format. URL must start with http:// or https://");
                    return Task.FromResult(CreateResult(false, true, "Error: Invalid URL format. URL must start with http:// or https://"));
                }

                SendStatusUpdate($"Opening URL: {url}");

                // Open the URL in the browser
                if (string.IsNullOrEmpty(browser))
                {
                    // Use default browser
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = url,
                        UseShellExecute = true
                    });
                }
                else
                {
                    // Use specified browser
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = browser,
                        Arguments = url,
                        UseShellExecute = true
                    });
                }

                SendStatusUpdate("Browser opened successfully.");
                return Task.FromResult(CreateResult(true, true, $"URL '{url}' opened successfully in the browser."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing OpenBrowser tool");
                SendStatusUpdate($"Error processing OpenBrowser tool: {ex.Message}");
                return Task.FromResult(CreateResult(false, true, $"Error processing OpenBrowser tool: {ex.Message}"));
            }
        }

        [McpServerTool, Description("Opens a URL in the default or specified web browser")]
        public async Task<string> OpenBrowser([Description("JSON parameters for OpenBrowser")] string parameters = "{}")
        {
            try
            {
                var result = await ProcessAsync(parameters, new Dictionary<string, string>());
                
                if (!result.WasProcessed)
                {
                    return "Tool was not processed successfully.";
                }
                
                return result.ResultMessage ?? "Tool executed successfully with no output.";
            }
            catch (Exception ex)
            {
                return $"Error executing tool: {ex.Message}";
            }
        }
    }
}
