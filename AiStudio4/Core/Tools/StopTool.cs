using AiStudio4.Core.Models;
using AiStudio4.InjectedDependencies;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

namespace AiStudio4.Core.Tools
{
    /// <summary>
    /// Implementation of the Stop tool
    /// </summary>
    public class StopTool : BaseToolImplementation
    {
        public StopTool(ILogger<CodeDiffTool> logger, ISettingsService settingsService) : base(logger, settingsService)
        {
        }

        /// <summary>
        /// Gets the Stop tool definition
        /// </summary>
        public override Tool GetToolDefinition()
        {
            return new Tool
            {
                Guid = "b2c3d4e5-f6a7-8901-2345-67890abcdef01", // Fixed GUID for Stop
                Name = "Stop",
                Description = "A tool which allows you to indicate that all outstanding tasks are completed, or you cannot proceed any further",
                Schema = @"{
  ""name"": ""Stop"",
  ""description"": ""A tool which allows you to indicate that all outstanding tasks are completed"",
  ""input_schema"": {
    ""type"": ""object"",
    ""properties"": {
      ""param"": {
        ""type"": ""string"",
        ""description"": ""Information to the user goes here""
      }
    }
  }
}",
                Categories = new List<string> { "MaxCode" },
                OutputFileType = "",
                Filetype = string.Empty,
                LastModified = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Processes a Stop tool call
        /// </summary>
        public override Task<BuiltinToolResult> ProcessAsync(string toolParameters)
        {
            _logger.LogInformation("'Stop' tool called, signalling processing termination");
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

            return Task.FromResult(CreateResult(true, false, resultMessage));
        }
    }
}