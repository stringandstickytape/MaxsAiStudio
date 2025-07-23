





 // Added for Dictionary
using ModelContextProtocol;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace AiStudio4.Core.Tools
{
    /// <summary>
    /// Implementation of the Stop tool
    /// </summary>
    [McpServerToolType]
    public class StopTool : BaseToolImplementation
    {
        public StopTool(ILogger<StopTool> logger, IGeneralSettingsService generalSettingsService, IStatusMessageService statusMessageService) : base(logger, generalSettingsService, statusMessageService)
        {
        }

        /// <summary>
        /// Gets the Stop tool definition
        /// </summary>
        public override Tool GetToolDefinition()
        {
            return new Tool
            {
                Guid = ToolGuids.STOP_TOOL_GUID,
                Name = "Stop",
                Description = "A tool which allows you to indicate that all outstanding tasks are completed, or you cannot proceed any further",
                Schema = """
{
  "name": "Stop",
  "description": "A tool which allows you to indicate that all outstanding tasks are completed",
  "input_schema": {
    "type": "object",
    "properties": {
      "param": { "type": "string", "description": "Information to the user goes here" }
    }
  }
}
""",
                Categories = new List<string> { "MaxCode" },
                OutputFileType = "",
                Filetype = string.Empty,
                LastModified = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Processes a Stop tool call
        /// </summary>
        public override Task<BuiltinToolResult> ProcessAsync(string toolParameters, Dictionary<string, string> extraProperties)
        {
            _logger.LogInformation("'Stop' tool called, signalling processing termination");
            SendStatusUpdate("Processing Stop tool...");
            string resultMessage = "Stop tool called without parameters.";

            // Optionally parse the parameters to extract the message
            try
            {
                if (!string.IsNullOrEmpty(toolParameters))
                {
                    dynamic stopParams = JsonConvert.DeserializeObject(toolParameters);
                    if (stopParams?.param != null)
                    {
                        resultMessage = stopParams.param;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error parsing Stop tool parameters");
                resultMessage = "Stop tool called (parameters could not be parsed)";
            }

            SendStatusUpdate("Stop tool completed. Ending conversation.");
            return Task.FromResult(CreateResult(true, false, resultMessage));
        }

        [McpServerTool, Description("A tool which allows you to indicate that all outstanding tasks are completed, or you cannot proceed any further")]
        public async Task<string> Stop([Description("JSON parameters for Stop")] string parameters = "{}")
        {
            try
            {
                var result = await ProcessAsync(parameters, new Dictionary<string, string>());
                
                if (!result.WasProcessed)
                {
                    return $"Tool was not processed successfully.";
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
