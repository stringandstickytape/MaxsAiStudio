// AiStudio4.Core\Tools\DeleteFileTool.cs
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
    /// Implementation of the DeleteFile tool that deletes existing files.
    /// </summary>
    public class DeleteFileTool : BaseToolImplementation
    {
        private readonly StringBuilder _validationErrorMessages;

        public DeleteFileTool(ILogger<DeleteFileTool> logger, IGeneralSettingsService generalSettingsService, 
            IStatusMessageService statusMessageService) 
            : base(logger, generalSettingsService, statusMessageService)
        {
            _validationErrorMessages = new StringBuilder();
            
        }

        public override Tool GetToolDefinition()
        {
            return new Tool
            {
                Guid = "a1b2c3d4-e5f6-7890-1234-567890abcd02", // Fixed GUID for DeleteFile
                Description = "Deletes an existing file.",
                Name = "DeleteFile",
                Schema = @"{
                  ""name"": ""DeleteFile"",
                  ""description"": ""Deletes an existing file. Requires the file path."",
                  ""input_schema"": {
                    ""type"": ""object"",
                    ""properties"": {
                      ""path"": {
                        ""type"": ""string"",
                        ""description"": ""The absolute path to the file to delete""
                      },
                      ""description"": {
                        ""type"": ""string"",
                        ""description"": ""A human-readable explanation of this file deletion""
                      }
                    },
                    ""required"": [
                      ""path"",
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

        public override async Task<BuiltinToolResult> ProcessAsync(string toolParameters, Dictionary<string, string> extraProperties, string projectRootPathOverride = null)
        {
            var activeRoot = GetActiveProjectRoot(projectRootPathOverride);
            var pathSecurityManager = new PathSecurityManager(_logger, activeRoot); // Initialize with activeRoot

            _validationErrorMessages.Clear();
            var overallSuccess = true;
            
            // Send initial status update
            SendStatusUpdate("Starting DeleteFile tool execution...");
            
            JObject parameters;
            string filePath = null;
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
                
                description = parameters["description"]?.ToString() ?? "No description provided";
                
                // Validate file path security
                if (!string.IsNullOrEmpty(filePath) && !pathSecurityManager.IsPathSafe(filePath))
                {
                    _validationErrorMessages.AppendLine($"Error: Path '{filePath}' is outside the allowed project directory ({activeRoot}).");
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
                _logger.LogError(ex, "Unexpected error during DeleteFile initial parsing/validation.");
                overallSuccess = false;
            }

            // --- Stop if Validation Failed ---
            if (!overallSuccess)
            {
                _logger.LogError("DeleteFile request validation failed:\n{Errors}", _validationErrorMessages.ToString());
                SendStatusUpdate("Validation failed. See error details.");
                MessageBox.Show(_validationErrorMessages.ToString(), "DeleteFile Validation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return CreateResult(false, false, $"Validation failed: {_validationErrorMessages.ToString()}");
            }

            // --- Process the File Deletion ---
            try
            {
                SendStatusUpdate($"Deleting file: {Path.GetFileName(filePath)}");
                
                // Create a change object for the DeleteFileHandler
                var change = new JObject
                {
                    ["description"] = description
                };
                
                // Use the existing DeleteFileHandler to process the change
                var handler = new DeleteFileHandler(_logger, _statusMessageService, _clientId);
                var result = await handler.HandleAsync(filePath, change);
                
                if (result.Success)
                {
                    SendStatusUpdate("DeleteFile completed successfully.");
                    return CreateResult(true, true, toolParameters, "File deleted successfully.");
                }
                else
                {
                    SendStatusUpdate("DeleteFile completed with errors. See details.");
                    MessageBox.Show(result.Message, "DeleteFile Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return CreateResult(true, false, toolParameters, $"Failed to delete file: {result.Message}");
                }
            }
            catch (Exception ex)
            {
                string errorMessage = $"Unexpected error during file deletion: {ex.Message}";
                _logger.LogError(ex, "Unexpected error during DeleteFile execution.");
                SendStatusUpdate("DeleteFile failed with an unexpected error.");
                MessageBox.Show(errorMessage, "DeleteFile Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return CreateResult(true, false, toolParameters, errorMessage);
            }
        }
    }
}