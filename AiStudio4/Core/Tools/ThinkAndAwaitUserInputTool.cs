// AiStudio4/Core/Tools/ThinkAndAwaitUserInputTool.cs
﻿








namespace AiStudio4.Core.Tools
{
    /// <summary>
    /// Implementation of the ThinkAndAwaitUserInput tool
    /// </summary>
    public class ThinkAndAwaitUserInputTool : BaseToolImplementation
    {
        public ThinkAndAwaitUserInputTool(ILogger<ThinkAndAwaitUserInputTool> logger, IGeneralSettingsService generalSettingsService, IStatusMessageService statusMessageService) : base(logger, generalSettingsService, statusMessageService)
        {
        }

        public string OutputFileType { get; } = "md";

        /// <summary>
        /// Gets the ThinkAndAwaitUserInput tool definition
        /// </summary>
        public override Tool GetToolDefinition()
        {
            return new Tool
            {
                Guid = ToolGuids.THINK_AND_AWAIT_USER_INPUT_TOOL_GUID,
                Name = "ThinkAndAwaitUserInput",
                Description = "Use the tool to think about something and then explicitly await user input. It will not obtain new information or make any changes to the repository, but just log the thought. Use it when complex reasoning or brainstorming is needed, and you require user feedback or confirmation before proceeding. This tool's operation will stop AI processing to wait for the user.",
                Schema = """
{
  "name": "ThinkAndAwaitUserInput",
  "description": "Use the tool to think about something and then explicitly await user input.\n\nIt will not obtain new information or make any changes to the repository, but just log the thought. Use it when complex reasoning or brainstorming is needed, and you require user feedback or confirmation before proceeding. This tool's operation will stop AI processing to wait for the user.",
  "input_schema": {
    "properties": {
      "thought": { "title": "Thought", "type": "string" }
    },
    "required": ["thought"],
    "title": "thinkAndAwaitUserInputArguments",
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
        /// Processes a ThinkAndAwaitUserInput tool call
        /// </summary>
        public override Task<BuiltinToolResult> ProcessAsync(string toolParameters, Dictionary<string, string> extraProperties)
        {
            // ThinkAndAwaitUserInput tool stops processing and waits for user input
            SendStatusUpdate("Processing ThinkAndAwaitUserInput tool...");
            var parameters = Newtonsoft.Json.JsonConvert.DeserializeObject<JObject>(toolParameters);

            var thought = parameters?["thought"]?.ToString() ?? "";
            var continueProcessing = false; // Always stop processing to await user input

            _logger.LogInformation("ThinkAndAwaitUserInput tool called with parameters: {Parameters}, continueProcessing: {ContinueProcessing}", thought, continueProcessing);
            SendStatusUpdate("ThinkAndAwaitUserInput tool completed - awaiting user input.");
            return Task.FromResult(CreateResult(true, continueProcessing, thought));
        }
    }
}
