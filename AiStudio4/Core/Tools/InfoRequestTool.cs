using AiStudio4.Core.Models;
using AiStudio4.InjectedDependencies;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

namespace AiStudio4.Core.Tools
{
    /// <summary>
    /// Implementation of the infoRequest tool
    /// </summary>
    public class InfoRequestTool : BaseToolImplementation
    {
        public InfoRequestTool(ILogger<CodeDiffTool> logger, ISettingsService settingsService) : base(logger, settingsService)
        {
        }

        /// <summary>
        /// Gets the infoRequest tool definition
        /// </summary>
        public override Tool GetToolDefinition()
        {
            return new Tool
            {
                Guid = "b2c3d4e5-f6a7-8901-2345-67890abcdef36", // Fixed GUID for infoRequest
                Name = "infoRequest",
                Description = "A tool which allows you to request more information from the user",
                Schema = @"{
  ""name"": ""infoRequest"",
  ""description"": ""A tool which allows you to request more information from the user, be it further file contents or other information"",
  ""input_schema"": {
    ""properties"": {
      ""message"": {
        ""description"": ""The request for information that you want to send to the user"",
        ""type"": ""string""
      }
    },
    ""required"": [""message""],
    ""type"": ""object""
  }
}",
                Categories = new List<string> { "MaxCode" },
                OutputFileType = "infoReq",
                Filetype = string.Empty,
                LastModified = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Processes an infoRequest tool call
        /// </summary>
        public override Task<BuiltinToolResult> ProcessAsync(string toolParameters)
        {
            try
            {
                _logger.LogInformation("infoRequest tool called with parameters: {Parameters}", toolParameters);
                var parameters = JsonConvert.DeserializeObject<dynamic>(toolParameters);
                string message = parameters?.message;

                return Task.FromResult(CreateResult(true, false, message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing infoRequest tool");
                return Task.FromResult(CreateResult(true, true, "Error processing information request: " + ex.Message));
            }
        }
    }
}