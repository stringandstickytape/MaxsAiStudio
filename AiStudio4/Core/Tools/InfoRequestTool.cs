﻿using AiStudio4.Core.Interfaces;
using AiStudio4.Core.Models;
using AiStudio4.InjectedDependencies;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AiStudio4.Core.Tools
{
    /// <summary>
    /// Implementation of the infoRequest tool
    /// </summary>
    public class InfoRequestTool : BaseToolImplementation
    {
        public InfoRequestTool(ILogger<InfoRequestTool> logger, IGeneralSettingsService generalSettingsService, IStatusMessageService statusMessageService) : base(logger, generalSettingsService, statusMessageService)
        {
        }

        /// <summary>
        /// Gets the infoRequest tool definition
        /// </summary>
        public override Tool GetToolDefinition()
        {
            return new Tool
            {
                Guid = ToolGuids.INFO_REQUEST_TOOL_GUID,
                Name = "infoRequest",
                Description = "A tool which allows you to request more information from the user",
                Schema = """
{
  "name": "infoRequest",
  "description": "A tool which allows you to request more information from the user, be it further file contents or other information",
  "input_schema": {
    "properties": {
      "message": { "description": "The request for information that you want to send to the user", "type": "string" }
    },
    "required": ["message"],
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
        /// Processes an infoRequest tool call
        /// </summary>
        public override Task<BuiltinToolResult> ProcessAsync(string toolParameters, Dictionary<string, string> extraProperties)
        {
            try
            {
                _logger.LogInformation("infoRequest tool called with parameters: {Parameters}", toolParameters);
                SendStatusUpdate("Processing information request...");
                var parameters = JsonConvert.DeserializeObject<dynamic>(toolParameters);
                string message = parameters?.message;

                SendStatusUpdate("Information request sent to user.");
                return Task.FromResult(CreateResult(true, false, message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing infoRequest tool");
                SendStatusUpdate($"Error processing information request: {ex.Message}");
                return Task.FromResult(CreateResult(true, true, "Error processing information request: " + ex.Message));
            }
        }
    }
}