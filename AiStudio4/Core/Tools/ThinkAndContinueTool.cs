using ModelContextProtocol;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace AiStudio4.Core.Tools
{
    /// <summary>
    /// Implementation of the Think tool
    /// </summary>
    [McpServerToolType]
    public class ThinkAndContinueTool : BaseToolImplementation
    {
        public ThinkAndContinueTool(ILogger<ThinkAndContinueTool> logger, IGeneralSettingsService generalSettingsService, IStatusMessageService statusMessageService) : base(logger, generalSettingsService, statusMessageService)
        {
        }

        public string OutputFileType { get; } = "md";

        /// <summary>
        /// Gets the Think tool definition
        /// </summary>
        public override Tool GetToolDefinition()
        {
            return new Tool
            {
                Guid = ToolGuids.THINK_TOOL_GUID,
                Name = "ThinkAndContinue",
                Description = "Use the tool to think about something without pausing for user input. It will not obtain new information or make any changes to the repository, but just log the thought. Use it when complex reasoning or brainstorming is needed. This tool's operation allows for continued processing by the AI after the thought is logged. For example, if you explore the repo and discover the source of a bug, call this tool to brainstorm several unique ways of fixing the bug, and assess which change(s) are likely to be simplest and most effective. Alternatively, if you receive some test results, call this tool to brainstorm ways to fix the failing tests.",
                Schema = """
{
  "name": "ThinkAndContinue",
  "description": "Use the tool to think about something without pausing for user input.\n\nIt will not obtain new information or make any changes to the repository, but just log the thought. Use it when complex reasoning or brainstorming is needed. This tool's operation allows for continued processing by the AI after the thought is logged. For example, if you explore the repo and discover the source of a bug, call this tool to brainstorm several unique ways of fixing the bug, and assess which change(s) are likely to be simplest and most effective. Similarly, if you receive some test results, call this tool to brainstorm ways to fix the failing tests.",
  "input_schema": {
    "properties": {
      "thought": { "title": "Thought", "type": "string" }
    },
    "required": ["thought"],
    "title": "thinkArguments",
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
        /// Processes a Think tool call
        /// </summary>
        public override Task<BuiltinToolResult> ProcessAsync(string toolParameters, Dictionary<string, string> extraProperties)
        {
            // Think tool doesn't need special processing beyond logging
            SendStatusUpdate("Processing ThinkAndContinue tool...");
            var parameters = Newtonsoft.Json.JsonConvert.DeserializeObject<JObject>(toolParameters);

            var thought = parameters?["thought"]?.ToString() ?? "";
            var continueProcessing = true; // Always continue processing for Think tool

            _logger.LogInformation("ThinkAndContinue tool called with parameters: {Parameters}, continueProcessing: {ContinueProcessing}", thought, continueProcessing);
            SendStatusUpdate("ThinkAndContinue tool completed.");
            return Task.FromResult(CreateResult(true, continueProcessing, thought));
        }

        [McpServerTool, Description("Use the tool to think about something without pausing for user input. It will not obtain new information or make any changes to the repository, but just log the thought. Use it when complex reasoning or brainstorming is needed. This tool's operation allows for continued processing by the AI after the thought is logged. For example, if you explore the repo and discover the source of a bug, call this tool to brainstorm several unique ways of fixing the bug, and assess which change(s) are likely to be simplest and most effective. Alternatively, if you receive some test results, call this tool to brainstorm ways to fix the failing tests.")]
        public async Task<string> ThinkAndContinue([Description("JSON parameters for ThinkAndContinue")] string parameters = "{}")
        {
            return await ExecuteWithExtraProperties(parameters);
        }
    }
}
