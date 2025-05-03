// AiStudio4.Core\Tools\ReplaceFileTool.cs
using AiStudio4.Core.Interfaces;
using AiStudio4.Core.Models;
using AiStudio4.Core.Tools.CodeDiff;
using AiStudio4.Core.Tools.CodeDiff.FileOperationHandlers;
using AiStudio4.Core.Tools.CodeDiff.Models;
using AiStudio4.InjectedDependencies;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace AiStudio4.Core.Tools
{
    /// <summary>
    /// Implementation of the ReplaceFile tool that replaces existing files with new content.
    /// </summary>
    public class ReplaceFileTool : BaseToolImplementation
    {
        private readonly StringBuilder _validationErrorMessages;
        private PathSecurityManager _pathSecurityManager;

        public ReplaceFileTool(ILogger<ReplaceFileTool> logger, IGeneralSettingsService generalSettingsService, 
            IStatusMessageService statusMessageService) 
            : base(logger, generalSettingsService, statusMessageService)
        {
            _validationErrorMessages = new StringBuilder();
            
        }

        public override Tool GetToolDefinition()
        {
            return new Tool
            {
                Guid = "a1b2c3d4-e5f6-7890-1234-567890abcd05", // Fixed GUID for ReplaceFile
                Description = "Replaces an existing file with new content.",
                Name = "ReplaceFile",
                Schema = @"{
                  ""name"": ""ReplaceFile"",
                  ""description"": ""Replaces an existing file with new content. Requires the file path and new content."",
                  ""input_schema"": {
                    ""type"": ""object"",
                    ""properties"": {
                      ""path"": {
                        ""type"": ""string"",
                        ""description"": ""The absolute path to the file to replace""
                      },
                      ""content"": {
                        ""type"": ""string"",
                        ""description"": ""The new content to replace the file with""
                      },
                      ""description"": {
                        ""type"": ""string"",
                        ""description"": ""A human-readable explanation of this file replacement""
                      }
                    },
                    ""required"": [
                      ""path"",
                      ""content"",
                      ""description""
                    ]
                  }
                }",
                Categories = new List<string> { "MaxCode" },
                OutputFileType = "json",
                Filetype = string.Empty,
                LastModified = DateTime.UtcNow
            };
        }

        public override async Task<BuiltinToolResult> ProcessAsync(string toolParameters, Dictionary<string, string> extraProperties)
        {
            _pathSecurityManager = new PathSecurityManager(_logger, _projectRoot);
            _validationErrorMessages.Clear();
            var overallSuccess = true;
            
            // Send initial status update
            SendStatusUpdate("Starting ReplaceFile tool execution...");
            
            JObject parameters;
            string filePath = null;
            string content = null;
            string description = null;

            // --- Parse and Validate Input Structure ---
            try
            {
                parameters = JObject.Parse(toolParameters);
                
                filePath = parameters["path"]?.ToString();
                if (string.IsNullOrEmpty(filePath))
                {
                    _validationErrorMessages.AppendLine("Error: 'path' is missing or empty.");
                    overallSuccess = false;
                }
                
                content = parameters["content"]?.ToString();
                if (content == null) // Allow empty string for content
                {
                    _validationErrorMessages.AppendLine("Error: 'content' is missing.");
                    overallSuccess = false;
                }
                
                description = parameters["description"]?.ToString() ?? "No description provided";
                
                // Validate file path security
                if (!string.IsNullOrEmpty(filePath) && !_pathSecurityManager.IsPathSafe(filePath))
                {
                    _validationErrorMessages.AppendLine($"Error: Path '{filePath}' is outside the allowed project directory.");
                    overallSuccess = false;
                }
                
                // Validate file exists
                if (!string.IsNullOrEmpty(filePath) && !File.Exists(filePath))
                {
                    _validationErrorMessages.AppendLine($"Error: File '{filePath}' does not exist.");
                    overallSuccess = false;
                }
            }
            catch (JsonException jsonEx)
            {
                _validationErrorMessages.AppendLine($"Error parsing tool parameters JSON: {jsonEx.Message}");
                overallSuccess = false;
            }
            catch (Exception ex)
            {
                _validationErrorMessages.AppendLine($"Unexpected error during initial parsing or validation: {ex.Message}");
                _logger.LogError(ex, "Unexpected error during ReplaceFile initial parsing/validation.");
                overallSuccess = false;
            }

            // --- Stop if Validation Failed ---
            if (!overallSuccess)
            {
                _logger.LogError("ReplaceFile request validation failed:\n{Errors}", _validationErrorMessages.ToString());
                SendStatusUpdate("Validation failed. See error details.");
                MessageBox.Show(_validationErrorMessages.ToString(), "ReplaceFile Validation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return CreateResult(false, false, $"Validation failed: {_validationErrorMessages.ToString()}");
            }

            // --- Process the File Replacement ---
            try
            {
                SendStatusUpdate($"Replacing file: {Path.GetFileName(filePath)}");
                
                // Create a change object for the ReplaceFileHandler
                var change = new JObject
                {
                    ["newContent"] = content,
                    ["description"] = description
                };
                
                // Use the existing ReplaceFileHandler to process the change
                var handler = new ReplaceFileHandler(_logger, _statusMessageService, _clientId);
                var result = await handler.HandleAsync(filePath, change);
                
                if (result.Success)
                {
                    SendStatusUpdate("ReplaceFile completed successfully.");
                    return CreateResult(true, true, toolParameters, "File replaced successfully.");
                }
                else
                {
                    SendStatusUpdate("ReplaceFile completed with errors. See details.");
                    MessageBox.Show(result.Message, "ReplaceFile Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return CreateResult(true, false, toolParameters, $"Failed to replace file: {result.Message}");
                }
            }
            catch (Exception ex)
            {
                string errorMessage = $"Unexpected error during file replacement: {ex.Message}";
                _logger.LogError(ex, "Unexpected error during ReplaceFile execution.");
                SendStatusUpdate("ReplaceFile failed with an unexpected error.");
                MessageBox.Show(errorMessage, "ReplaceFile Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return CreateResult(true, false, toolParameters, errorMessage);
            }
        }
    }
}