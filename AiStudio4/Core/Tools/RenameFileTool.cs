// AiStudio4.Core\Tools\RenameFileTool.cs
﻿

using AiStudio4.Core.Tools.CodeDiff;
using AiStudio4.Core.Tools.CodeDiff.FileOperationHandlers;
using AiStudio4.Core.Tools.CodeDiff.Models;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using System.ComponentModel;











namespace AiStudio4.Core.Tools
{
    /// <summary>
    /// Implementation of the RenameFile tool that renames existing files.
    /// </summary>
    [McpServerToolType]
    public class RenameFileTool : BaseToolImplementation
    {
        private readonly StringBuilder _validationErrorMessages;
        private  PathSecurityManager _pathSecurityManager;

        public RenameFileTool(ILogger<RenameFileTool> logger, IGeneralSettingsService generalSettingsService, 
            IStatusMessageService statusMessageService) 
            : base(logger, generalSettingsService, statusMessageService)
        {
            _validationErrorMessages = new StringBuilder();
            
        }

        public override Tool GetToolDefinition()
        {
            return new Tool
            {
                Guid = ToolGuids.RENAME_FILE_TOOL_GUID,
                Description = "Renames an existing file to a new path.",
                Name = "RenameFile",
                Schema = """
{
  "name": "RenameFile",
  "description": "Renames an existing file to a new path. Requires the original file path and the new file path.",
  "input_schema": {
    "type": "object",
    "properties": {
      "path": { "type": "string", "description": "The absolute path to the file to rename" },
      "newPath": { "type": "string", "description": "The new absolute path for the file" },
      "description": { "type": "string", "description": "A human-readable explanation of this file rename" }
    },
    "required": ["path", "newPath", "description"]
  }
}
""",
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
            SendStatusUpdate("Starting RenameFile tool execution...");
            
            JObject parameters;
            string filePath = null;
            string newPath = null;
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
                
                newPath = parameters["newPath"]?.ToString();
                if (string.IsNullOrEmpty(newPath))
                {
                    _validationErrorMessages.AppendLine("Error: 'newPath' is missing or empty.");
                    overallSuccess = false;
                }
                
                description = parameters["description"]?.ToString() ?? "No description provided";
                
                // Validate file paths security
                if (!string.IsNullOrEmpty(filePath) && !_pathSecurityManager.IsPathSafe(filePath))
                {
                    _validationErrorMessages.AppendLine($"Error: Path '{filePath}' is outside the allowed project directory.");
                    overallSuccess = false;
                }
                
                if (!string.IsNullOrEmpty(newPath) && !_pathSecurityManager.IsPathSafe(newPath))
                {
                    _validationErrorMessages.AppendLine($"Error: New path '{newPath}' is outside the allowed project directory.");
                    overallSuccess = false;
                }
                
                // Validate source file exists
                if (!string.IsNullOrEmpty(filePath) && !File.Exists(filePath))
                {
                    _validationErrorMessages.AppendLine($"Error: Source file '{filePath}' does not exist.");
                    overallSuccess = false;
                }
                
                // Validate target file doesn't exist
                if (!string.IsNullOrEmpty(newPath) && File.Exists(newPath))
                {
                    _validationErrorMessages.AppendLine($"Error: Target file '{newPath}' already exists.");
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
                _logger.LogError(ex, "Unexpected error during RenameFile initial parsing/validation.");
                overallSuccess = false;
            }

            // --- Stop if Validation Failed ---
            if (!overallSuccess)
            {
                _logger.LogError("RenameFile request validation failed:\n{Errors}", _validationErrorMessages.ToString());
                SendStatusUpdate("Validation failed. See error details.");
                MessageBox.Show(_validationErrorMessages.ToString(), "RenameFile Validation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return CreateResult(false, false, $"Validation failed: {_validationErrorMessages.ToString()}");
            }

            // --- Process the File Rename ---
            try
            {
                SendStatusUpdate($"Renaming file: {Path.GetFileName(filePath)} to {Path.GetFileName(newPath)}");
                
                // Create a change object for the RenameFileHandler
                var change = new JObject
                {
                    ["newContent"] = newPath, // In the original implementation, newContent holds the new path for rename operations
                    ["description"] = description
                };
                
                // Use the existing RenameFileHandler to process the change
                var handler = new RenameFileHandler(_logger, _statusMessageService, _clientId);
                var result = await handler.HandleAsync(filePath, change);
                
                if (result.Success)
                {
                    SendStatusUpdate("RenameFile completed successfully.");
                    return CreateResult(true, true, toolParameters, "File renamed successfully.");
                }
                else
                {
                    SendStatusUpdate("RenameFile completed with errors. See details.");
                    MessageBox.Show(result.Message, "RenameFile Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return CreateResult(true, false, toolParameters, $"Failed to rename file: {result.Message}");
                }
            }
            catch (Exception ex)
            {
                string errorMessage = $"Unexpected error during file rename: {ex.Message}";
                _logger.LogError(ex, "Unexpected error during RenameFile execution.");
                SendStatusUpdate("RenameFile failed with an unexpected error.");
                MessageBox.Show(errorMessage, "RenameFile Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return CreateResult(true, false, toolParameters, errorMessage);
            }
        }

        [McpServerTool, Description("Renames an existing file to a new path.")]
        public async Task<string> RenameFile([Description("JSON parameters for RenameFile")] string parameters = "{}")
        {
            return await ExecuteWithExtraProperties(parameters);
        }
    }
}
