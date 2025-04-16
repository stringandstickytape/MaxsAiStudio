using AiStudio4.Core.Models;
using AiStudio4.InjectedDependencies;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AiStudio4.Core.Tools
{
    /// <summary>
    /// Implementation of the Think tool
    /// </summary>
    public class ThinkTool : BaseToolImplementation
    {
        public ThinkTool(ILogger<CodeDiffTool> logger, IGeneralSettingsService generalSettingsService) : base(logger, generalSettingsService)
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
                Guid = "b2c3d4e5-f6a7-8901-2345-67890abcdef03",
                Name = "Think",
                Description = "Use the tool to think about something.\r\n\r\nIt will not obtain new information or make any changes to the repository, but just log the thought. Use it when complex reasoning or brainstorming is needed. For example, if you explore the repo and discover the source of a bug, call this tool to brainstorm several unique ways of fixing the bug, and assess which change(s) are likely to be simplest and most effective. Alternatively, if you receive some test results, call this tool to brainstorm ways to fix the failing tests.",
                Schema = @"{
  ""name"": ""Think"",
  ""description"": ""Use the tool to think about something.

It will not obtain new information or make any changes to the repository, but just log the thought. Use it when complex reasoning or brainstorming is needed. For example, if you explore the repo and discover the source of a bug, call this tool to brainstorm several unique ways of fixing the bug, and assess which change(s) are likely to be simplest and most effective. Alternatively, if you receive some test results, call this tool to brainstorm ways to fix the failing tests."",
  ""input_schema"": {
                ""properties"": {
                ""thought"": {
                    ""title"": ""Thought"",
                    ""type"": ""string""
                }
            },
            ""required"": [""thought""],
            ""title"": ""thinkArguments"",
            ""type"": ""object""
  }
}",
                Categories = new List<string> { "MaxCode" },
                OutputFileType = "",
                Filetype = string.Empty,
                LastModified = DateTime.UtcNow,
                ExtraProperties = new Dictionary<string, string> {
                    { "TestProperty", "TestValue" }
                }
            };
        }

        /// <summary>
        /// Processes a Think tool call
        /// </summary>
        public override Task<BuiltinToolResult> ProcessAsync(string toolParameters)
        {
            // Think tool doesn't need special processing beyond logging
            var parameters = Newtonsoft.Json.JsonConvert.DeserializeObject<JObject>(toolParameters);

            var thought = parameters?["thought"]?.ToString() ?? "";

            _logger.LogInformation("Think tool called with parameters: {Parameters}", thought);
            return Task.FromResult(CreateResult(true, false, thought));
        }
    }
}