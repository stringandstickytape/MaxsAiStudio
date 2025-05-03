// AiStudio4.Core\Tools\ModifyFileTool.cs
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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace AiStudio4.Core.Tools
{
    /// <summary>
    /// Implementation of the ModifyFile tool that modifies existing files.
    /// Uses a secondary AI for content modifications when needed.
    /// </summary>
    public class ModifyFileTool : BaseToolImplementation
    {
        private readonly StringBuilder _validationErrorMessages;
        private readonly ISecondaryAiService _secondaryAiService;
        private  PathSecurityManager _pathSecurityManager;

        public ModifyFileTool(ILogger<ModifyFileTool> logger, IGeneralSettingsService generalSettingsService, 
            ISecondaryAiService secondaryAiService, IStatusMessageService statusMessageService) 
            : base(logger, generalSettingsService, statusMessageService)
        {
            _validationErrorMessages = new StringBuilder();
            _secondaryAiService = secondaryAiService ?? throw new ArgumentNullException(nameof(secondaryAiService));
            
        }

        public override Tool GetToolDefinition()
        {
            return new Tool
            {
                Guid = "a1b2c3d4-e5f6-7890-1234-567890abcd03", // Fixed GUID for ModifyFile
                Description = "Modifies content within an existing file.",
                Name = "ModifyFile",
                Schema = @"{
                  ""name"": ""ModifyFile"",
                  ""description"": ""Modifies content within an existing file. Requires the file path, line number, old content to replace, and new content to insert."",
                  ""input_schema"": {
                    ""type"": ""object"",
                    ""properties"": {
                      ""path"": {
                        ""type"": ""string"",
                        ""description"": ""The absolute path to the file to modify""
                      },
                      ""lineNumber"": {
                        ""type"": ""integer"",
                        ""description"": ""The approximate line number where the modification starts""
                      },
                      ""oldContent"": {
                        ""type"": ""string"",
                        ""description"": ""The content to be replaced. Should include significant context (at least 5 lines).""
                      },
                      ""newContent"": {
                        ""type"": ""string"",
                        ""description"": ""The content to replace the old content with""
                      },
                      ""description"": {
                        ""type"": ""string"",
                        ""description"": ""A human-readable explanation of this modification""
                      }
                    },
                    ""required"": [
                      ""path"",
                      ""oldContent"",
                      ""newContent"",
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
            SendStatusUpdate("Starting ModifyFile tool execution...");
            
            JObject parameters;
            string filePath = null;
            int lineNumber = 0;
            string oldContent = null;
            string newContent = null;
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
                
                // Line number is optional but useful for context
                if (parameters["lineNumber"] != null)
                {
                    lineNumber = parameters["lineNumber"].Value<int>();
                }
                
                oldContent = parameters["oldContent"]?.ToString();
                if (string.IsNullOrEmpty(oldContent))
                {
                    _validationErrorMessages.AppendLine("Error: 'oldContent' is missing or empty.");
                    overallSuccess = false;
                }
                
                newContent = parameters["newContent"]?.ToString();
                if (newContent == null) // Allow empty string for newContent
                {
                    _validationErrorMessages.AppendLine("Error: 'newContent' is missing.");
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
                _logger.LogError(ex, "Unexpected error during ModifyFile initial parsing/validation.");
                overallSuccess = false;
            }

            // --- Stop if Validation Failed ---
            if (!overallSuccess)
            {
                _logger.LogError("ModifyFile request validation failed:\n{Errors}", _validationErrorMessages.ToString());
                SendStatusUpdate("Validation failed. See error details.");
                MessageBox.Show(_validationErrorMessages.ToString(), "ModifyFile Validation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return CreateResult(false, false, $"Validation failed: {_validationErrorMessages.ToString()}");
            }

            // --- Process the Modification ---
            try
            {
                SendStatusUpdate($"Modifying file: {Path.GetFileName(filePath)}");
                
                // Create a change object for the ModifyFileHandler
                var change = new JObject
                {
                    ["lineNumber"] = lineNumber,
                    ["oldContent"] = oldContent,
                    ["newContent"] = newContent,
                    ["description"] = description
                };
                
                // Use the existing ModifyFileHandler to process the change
                var handler = new ModifyFileHandler(_logger, _statusMessageService, _clientId, _secondaryAiService);
                var result = await handler.HandleAsync(filePath, change);
                
                if (result.Success)
                {
                    SendStatusUpdate("ModifyFile completed successfully.");
                    return CreateResult(true, true, toolParameters, "File modified successfully.");
                }
                else
                {
                    SendStatusUpdate("ModifyFile completed with errors. See details.");
                    MessageBox.Show(result.Message, "ModifyFile Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return CreateResult(true, false, toolParameters, $"Failed to modify file: {result.Message}");
                }
            }
            catch (Exception ex)
            {
                string errorMessage = $"Unexpected error during file modification: {ex.Message}";
                _logger.LogError(ex, "Unexpected error during ModifyFile execution.");
                SendStatusUpdate("ModifyFile failed with an unexpected error.");
                MessageBox.Show(errorMessage, "ModifyFile Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return CreateResult(true, false, toolParameters, errorMessage);
            }
        }
    }
}