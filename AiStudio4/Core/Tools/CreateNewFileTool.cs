// AiStudio4.Core\Tools\CreateNewFileTool.cs


using AiStudio4.Core.Tools.CodeDiff;
using AiStudio4.Core.Tools.CodeDiff.FileOperationHandlers;
using AiStudio4.Core.Tools.CodeDiff.Models;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using System.ComponentModel;
using Newtonsoft.Json.Linq; // Added for JObject.Parse


namespace AiStudio4.Core.Tools
{
    /// <summary>
    /// Implementation of the CreateNewFile tool that creates new files.
    /// </summary>
    [McpServerToolType] // <- ADDED THIS
    public class CreateNewFileTool : BaseToolImplementation
    {
        private readonly StringBuilder _validationErrorMessages;
        private  PathSecurityManager _pathSecurityManager;

        public CreateNewFileTool(ILogger<CreateNewFileTool> logger, IGeneralSettingsService generalSettingsService, 
            IStatusMessageService statusMessageService) 
            : base(logger, generalSettingsService, statusMessageService)
        {
            _validationErrorMessages = new StringBuilder();
            
        }

        public override Tool GetToolDefinition()
        {
            return new Tool
            {
                Guid = ToolGuids.CREATE_NEW_FILE_TOOL_GUID,
                Description = "Creates a new file with the specified content.",
                Name = "CreateNewFile",
                Schema = """
{
  "name": "CreateNewFile",
  "description": "Creates a new file with the specified content. Requires the file path and content to create.",
  "input_schema": {
    "type": "object",
    "properties": {
      "path": { "type": "string", "description": "The absolute path where the new file should be created" },
      "content": { "type": "string", "description": "The content for the new file" },
      "description": { "type": "string", "description": "A human-readable explanation of this file creation" }
    },
    "required": ["path", "content", "description"]
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
            SendStatusUpdate("Starting CreateNewFile tool execution...");
            
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
                
                // If the file already exists, we'll take this as a replace
            }
            catch (JsonException jsonEx)
            {
                _validationErrorMessages.AppendLine($"Error parsing tool parameters JSON: {jsonEx.Message}");
                overallSuccess = false;
            }
            catch (Exception ex)
            {
                _validationErrorMessages.AppendLine($"Unexpected error during initial parsing or validation: {ex.Message}");
                _logger.LogError(ex, "Unexpected error during CreateNewFile initial parsing/validation.");
                overallSuccess = false;
            }

            // --- Stop if Validation Failed ---
            if (!overallSuccess)
            {
                _logger.LogError("CreateNewFile request validation failed:\n{Errors}", _validationErrorMessages.ToString());
                SendStatusUpdate("Validation failed. See error details.");
                MessageBox.Show(_validationErrorMessages.ToString(), "CreateNewFile Validation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return CreateResult(false, false, $"Validation failed: {_validationErrorMessages.ToString()}");
            }

            // --- Process the File Creation ---
            try
            {
                SendStatusUpdate($"Creating file: {Path.GetFileName(filePath)}");
                
                // Create a change object for the CreateFileHandler
                var change = new JObject
                {
                    ["newContent"] = content,
                    ["description"] = description
                };
                
                // Use the existing CreateFileHandler to process the change
                var handler = new CreateFileHandler(_logger, _statusMessageService, _clientId);
                var result = await handler.HandleAsync(filePath, change);
                
                if (result.Success)
                {
                    SendStatusUpdate("CreateNewFile completed successfully.");
                    return CreateResult(true, true, toolParameters, "File created successfully.");
                }
                else
                {
                    SendStatusUpdate("CreateNewFile completed with errors. See details.");
                    MessageBox.Show(result.Message, "CreateNewFile Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return CreateResult(true, false, toolParameters, $"Failed to create file: {result.Message}");
                }
            }
            catch (Exception ex)
            {
                string errorMessage = $"Unexpected error during file creation: {ex.Message}";
                _logger.LogError(ex, "Unexpected error during CreateNewFile execution.");
                SendStatusUpdate("CreateNewFile failed with an unexpected error.");
                MessageBox.Show(errorMessage, "CreateNewFile Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return CreateResult(true, false, toolParameters, errorMessage);
            }
        }

        [McpServerTool, Description("Creates a new file with the specified content.")]
        public async Task<string> CreateNewFile([Description("JSON parameters for CreateNewFile")] string parameters = "{}")
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