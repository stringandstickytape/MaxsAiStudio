// AiStudio4/Core/Tools/PresentResultsAndAwaitUserInputTool.cs









namespace AiStudio4.Core.Tools
{
    /// <summary>
    /// Implementation of the PresentResultsAndAwaitUserInput tool
    /// </summary>
    public class PresentResultsAndAwaitUserInputTool : BaseToolImplementation
    {
        public PresentResultsAndAwaitUserInputTool(ILogger<PresentResultsAndAwaitUserInputTool> logger, IGeneralSettingsService generalSettingsService, IStatusMessageService statusMessageService) : base(logger, generalSettingsService, statusMessageService)
        {
        }

        public string OutputFileType { get; } = "md";

        /// <summary>
        /// Gets the PresentResultsAndAwaitUserInput tool definition
        /// </summary>
        public override Tool GetToolDefinition()
        {
            return new Tool
            {
                Guid = ToolGuids.PRESENT_RESULTS_AND_AWAIT_USER_INPUT_TOOL_GUID,
                Name = "PresentResultsAndAwaitUserInput",
                Description = "Use the tool to present results, findings, or completed work to the user and then explicitly await their input. This tool is for delivering final outputs, summaries, or conclusions and then pausing to wait for user feedback, approval, or next instructions. This tool's operation will stop AI processing to wait for the user.",
                Schema = """
{
  "name": "PresentResultsAndAwaitUserInput",
  "description": "Use the tool to present results, findings, or completed work to the user and then explicitly await their input.\n\nThis tool is for delivering final outputs, summaries, or conclusions and then pausing to wait for user feedback, approval, or next instructions. This tool's operation will stop AI processing to wait for the user.",
  "input_schema": {
    "properties": {
      "results": { "title": "Results", "description": "The results, findings, or completed work to present to the user", "type": "string" }
    },
    "required": ["results"],
    "title": "presentResultsAndAwaitUserInputArguments",
    "type": "object"
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
        /// Processes a PresentResultsAndAwaitUserInput tool call
        /// </summary>
        public override Task<BuiltinToolResult> ProcessAsync(string toolParameters, Dictionary<string, string> extraProperties)
        {
            // PresentResultsAndAwaitUserInput tool stops processing and waits for user input
            SendStatusUpdate("Processing PresentResultsAndAwaitUserInput tool...");
            var parameters = Newtonsoft.Json.JsonConvert.DeserializeObject<JObject>(toolParameters);

            var results = parameters?["results"]?.ToString() ?? "";
            var continueProcessing = false; // Always stop processing to await user input

            _logger.LogInformation("PresentResultsAndAwaitUserInput tool called with parameters: {Parameters}, continueProcessing: {ContinueProcessing}", results, continueProcessing);
            SendStatusUpdate("PresentResultsAndAwaitUserInput tool completed - awaiting user input.");
            return Task.FromResult(CreateResult(true, continueProcessing, results));
        }
    }
}
