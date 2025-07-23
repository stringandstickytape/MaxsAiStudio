// AiStudio4/Core/Tools/ThinkAndAwaitUserInputTool.cs
using ModelContextProtocol;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace AiStudio4.Core.Tools
{
    /// <summary>
    /// Implementation of the ThinkAndAwaitUserInput tool
    /// </summary>
    [McpServerToolType]
    public class ThinkAndAwaitUserInputTool : BaseToolImplementation
    {
        public ThinkAndAwaitUserInputTool(ILogger<ThinkAndAwaitUserInputTool> logger, IGeneralSettingsService generalSettingsService, IStatusMessageService statusMessageService) : base(logger, generalSettingsService, statusMessageService)
        {
        }
        
        public string OutputFileType { get; } = "thinkandawaituserinput";

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
                OutputFileType = "thinkandawaituserinput",
                Filetype = "thinkandawaituserinput",
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

            // Format result as JSON for the rich renderer
            var resultJson = JsonConvert.SerializeObject(new { 
                thought = thought, 
                timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC"),
                status = "awaiting_user_input"
            });

            _logger.LogInformation("ThinkAndAwaitUserInput tool called with parameters: {Parameters}, continueProcessing: {ContinueProcessing}", thought, continueProcessing);
            SendStatusUpdate("ThinkAndAwaitUserInput tool completed - awaiting user input.");
            return Task.FromResult(CreateResult(true, continueProcessing, resultJson));
        }

        [McpServerTool, Description("Use the tool to think about something and then explicitly await user input. It will not obtain new information or make any changes to the repository, but just log the thought. Use it when complex reasoning or brainstorming is needed, and you require user feedback or confirmation before proceeding. This tool's operation will stop AI processing to wait for the user.")]
        public async Task<string> ThinkAndAwaitUserInput([Description("JSON parameters for ThinkAndAwaitUserInput")] string parameters = "{}")
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
