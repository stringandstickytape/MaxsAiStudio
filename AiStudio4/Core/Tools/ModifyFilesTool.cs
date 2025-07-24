


using AiStudio4.Core.Tools.CodeDiff;
using AiStudio4.Core.Tools.CodeDiff.FileOperationHandlers;
using AiStudio4.Core.Tools.CodeDiff.Models;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using System.ComponentModel;












namespace AiStudio4.Core.Tools
{
    
    
    
    
    [McpServerToolType]
    public class ModifyFilesTool : BaseToolImplementation
    {

        private readonly StringBuilder _validationErrorMessages;
        private readonly ISecondaryAiService _secondaryAiService;
        private  PathSecurityManager _pathSecurityManager;


        public ModifyFilesTool(ILogger<ModifyFilesTool> logger, IGeneralSettingsService generalSettingsService, 
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
                Guid = ToolGuids.MODIFY_FILES_TOOL_GUID, 
                Description = "Modifies content within one or more existing files.",
                Name = "ModifyFiles",
                Schema = """
{
  "name": "ModifyFiles",
  "description": "Modifies content within one or more existing files. Supports multiple changes across multiple files in a single call.",
  "input_schema": {
    "type": "object",
    "properties": {
      "modifications": {
        "type": "array",
        "description": "Array of file modifications to perform",
        "items": {
          "type": "object",
          "properties": {
            "path": { "type": "string", "description": "The absolute path to the file to modify" },
            "changes": {
              "type": "array",
              "description": "Array of changes to make to this file",
              "items": {
                "type": "object",
                "properties": {
                  "lineNumber": { "type": "integer", "description": "The approximate line number where the modification starts" },
                  "oldContent": { "type": "string", "description": "The content to be replaced. Should include significant context (at least 5 lines)." },
                  "newContent": { "type": "string", "description": "The content to replace the old content with" },
                  "description": { "type": "string", "description": "A human-readable explanation of this specific change" }
                },
                "required": ["lineNumber", "oldContent", "newContent", "description"]
              }
            }
          },
          "required": ["path", "changes"]
        }
      }
    },
    "required": ["modifications"]
  }
}
""",
                Categories = new List<string> { "MaxCode" },
                OutputFileType = "modifyfiles",
                Filetype = string.Empty,
                LastModified = DateTime.UtcNow
            };
        }

        public override async Task<BuiltinToolResult> ProcessAsync(string toolParameters, Dictionary<string, string> extraProperties)
        {
            _pathSecurityManager = new PathSecurityManager(_logger, _projectRoot);
            _validationErrorMessages.Clear();
            var overallSuccess = true;
            
            
            SendStatusUpdate("Starting ModifyFiles tool execution...");
            
            JObject parameters;
            JArray modifications = null;

            
            try
            {
                parameters = JObject.Parse(toolParameters);
                
                
                modifications = parameters["modifications"] as JArray;
                if (modifications == null || modifications.Count == 0)
                {
                    _validationErrorMessages.AppendLine("Error: 'modifications' array is missing or empty.");
                    overallSuccess = false;
                }
                else
                {
                    
                    foreach (JObject modification in modifications)
                    {
                        string filePath = modification["path"]?.ToString();
                        if (string.IsNullOrEmpty(filePath))
                        {
                            _validationErrorMessages.AppendLine("Error: 'path' is missing or empty in a modification.");
                            overallSuccess = false;
                            continue;
                        }
                        
                        
                        if (!_pathSecurityManager.IsPathSafe(filePath))
                        {
                            _validationErrorMessages.AppendLine($"Error: Path '{filePath}' is outside the allowed project directory.");
                            overallSuccess = false;
                            continue;
                        }
                        
                        
                        if (!File.Exists(filePath))
                        {
                            _validationErrorMessages.AppendLine($"Error: File '{filePath}' does not exist.");
                            overallSuccess = false;
                            continue;
                        }
                        
                        
                        var changes = modification["changes"] as JArray;
                        if (changes == null || changes.Count == 0)
                        {
                            _validationErrorMessages.AppendLine($"Error: 'changes' array is missing or empty for file '{filePath}'.");
                            overallSuccess = false;
                            continue;
                        }
                        
                        
                        foreach (JObject change in changes)
                        {
                            
                            if (change["lineNumber"] == null)
                            {
                                _validationErrorMessages.AppendLine($"Error: 'lineNumber' is missing in a change for file '{filePath}'.");
                                overallSuccess = false;
                            }
                            
                            string oldContent = change["oldContent"]?.ToString();
                            if (string.IsNullOrEmpty(oldContent))
                            {
                                _validationErrorMessages.AppendLine($"Error: 'oldContent' is missing or empty in a change for file '{filePath}'.");
                                overallSuccess = false;
                            }
                            
                            string newContent = change["newContent"]?.ToString();
                            if (newContent == null) 
                            {
                                _validationErrorMessages.AppendLine($"Error: 'newContent' is missing in a change for file '{filePath}'.");
                                overallSuccess = false;
                            }
                            
                            if (change["description"] == null)
                            {
                                change["description"] = "(no description)";
                                //_validationErrorMessages.AppendLine($"Error: 'description' is missing in a change for file '{filePath}'.");
                                //overallSuccess = false;
                            }
                        }
                    }
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

            
            if (!overallSuccess)
            {
                _logger.LogError("ModifyFiles request validation failed:\n{Errors}", _validationErrorMessages.ToString());
                SendStatusUpdate("Validation failed. See error details.");
                MessageBox.Show(_validationErrorMessages.ToString(), "ModifyFiles Validation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return CreateResult(false, false, $"Validation failed: {_validationErrorMessages.ToString()}");
            }
            
            
            if (modifications != null && modifications.Count > 0)
            {
                
                var changesByFilePath = new Dictionary<string, List<JObject>>();
                
                foreach (JObject modification in modifications)
                {
                    string filePath = modification["path"].ToString();
                    var changes = modification["changes"] as JArray;
                    
                    if (!changesByFilePath.ContainsKey(filePath))
                    {
                        changesByFilePath[filePath] = new List<JObject>();
                    }
                    
                    
                    foreach (JObject change in changes)
                    {
                        changesByFilePath[filePath].Add(change);
                    }
                }
                
                
                var consolidatedModifications = new JArray();
                foreach (var kvp in changesByFilePath)
                {
                    var newModification = new JObject
                    {
                        ["path"] = kvp.Key,
                        ["changes"] = new JArray(kvp.Value)
                    };
                    consolidatedModifications.Add(newModification);
                }
                
                
                modifications = consolidatedModifications;
                SendStatusUpdate($"Consolidated changes for {changesByFilePath.Count} file(s)...");
            }

            
            try
            {
                SendStatusUpdate($"Processing {modifications.Count} file modification(s)...");
                
                var handler = new ModifyFileHandler(_logger, _statusMessageService, _clientId, _secondaryAiService);
                var results = new List<(string FilePath, bool Success, string Message)>();
                var processedFiles = new HashSet<string>();
                
                
                foreach (JObject modification in modifications)
                {
                    string filePath = modification["path"].ToString();
                    var changesArray = modification["changes"] as JArray;
                    
                    processedFiles.Add(filePath);
                    SendStatusUpdate($"Modifying file: {Path.GetFileName(filePath)} ({changesArray.Count} change(s))");
                    
                    
                    var changes = changesArray.Select(c => c as JObject).Where(c => c != null).ToList();
                    
                    
                    var result = await handler.HandleModifyFilesAsync(filePath, changes);
                    results.Add((filePath, result.Success, result.Message));
                    
                    if (!result.Success)
                    {
                        _logger.LogError($"Failed to apply changes to {filePath}: {result.Message}");
                    }
                }
                
                
                bool allSuccessful = results.All(r => r.Success);
                
                if (allSuccessful)
                {
                    SendStatusUpdate("ModifyFiles completed successfully.");
                    
                    // Create enhanced output for custom renderer
                    var enhancedOutput = CreateEnhancedOutput(modifications, results, true);
                    return CreateResult(true, true, enhancedOutput, "All files modified successfully.");
                }
                else
                {
                    
                    var failedResults = results.Where(r => !r.Success).ToList();
                    var errorMessage = new StringBuilder("Failed to modify some files:\n");
                    foreach (var failure in failedResults)
                    {
                        errorMessage.AppendLine($"- {Path.GetFileName(failure.FilePath)}: {failure.Message}");
                    }
                    
                    string errorSummary = errorMessage.ToString();
                    SendStatusUpdate("ModifyFiles completed with errors. See details.");
                    MessageBox.Show(errorSummary, "ModifyFiles Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    
                    // Create enhanced output even for partial failures
                    var enhancedOutput = CreateEnhancedOutput(modifications, results, false);
                    return CreateResult(true, false, enhancedOutput, errorSummary);
                }
            }
            catch (Exception ex)
            {
                string errorMessage = $"Unexpected error during file modification: {ex.Message}";
                _logger.LogError(ex, "Unexpected error during ModifyFile execution.");
                SendStatusUpdate("ModifyFile failed with an unexpected error.");
                MessageBox.Show(errorMessage, "ModifyFile Error", MessageBoxButton.OK, MessageBoxImage.Error);
                
                // Create basic enhanced output for unexpected errors
                var enhancedOutput = CreateEnhancedOutputForError(modifications, errorMessage);
                return CreateResult(true, false, enhancedOutput, errorMessage);
            }
        }

        private string CreateEnhancedOutput(JArray modifications, List<(string FilePath, bool Success, string Message)> results, bool overallSuccess)
        {
            var output = new JObject
            {
                ["summary"] = new JObject
                {
                    ["totalFiles"] = results.Count,
                    ["totalChanges"] = modifications.Sum(m => ((JArray)m["changes"]).Count),
                    ["success"] = overallSuccess,
                    ["timestamp"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    ["successfulFiles"] = results.Count(r => r.Success),
                    ["failedFiles"] = results.Count(r => !r.Success)
                },
                ["files"] = new JArray()
            };

            var filesArray = (JArray)output["files"];

            foreach (JObject modification in modifications)
            {
                string filePath = modification["path"].ToString();
                var changesArray = modification["changes"] as JArray;
                var result = results.FirstOrDefault(r => r.FilePath == filePath);
                
                var fileObj = new JObject
                {
                    ["path"] = filePath,
                    //["relativePath"] = GetRelativePath(filePath),
                    //["fileType"] = GetFileExtension(filePath),
                    ["status"] = result.Success ? "modified" : "failed",
                    ["message"] = result.Message,
                    ["changeCount"] = changesArray.Count,
                    ["changes"] = new JArray()
                };

                var changesOutputArray = (JArray)fileObj["changes"];
                int changeIndex = 1;

                foreach (JObject change in changesArray)
                {
                    var changeObj = new JObject
                    {
                        //["id"] = $"change-{changeIndex++}",
                        ["description"] = change["description"]?.ToString() ?? "Code modification",
                        //["lineNumber"] = change["lineNumber"],
                        //["changeType"] = DetermineChangeType(change["oldContent"]?.ToString(), change["newContent"]?.ToString()),
                        //["oldContent"] = change["oldContent"]?.ToString() ?? "",
                        //["newContent"] = change["newContent"]?.ToString() ?? ""
                    };
                    changesOutputArray.Add(changeObj);
                }

                filesArray.Add(fileObj);
            }

            return output.ToString(Formatting.Indented);
        }

        private string CreateEnhancedOutputForError(JArray modifications, string errorMessage)
        {
            var output = new JObject
            {
                ["summary"] = new JObject
                {
                    ["totalFiles"] = modifications?.Count ?? 0,
                    ["totalChanges"] = modifications?.Sum(m => ((JArray)m["changes"]).Count) ?? 0,
                    ["success"] = false,
                    ["error"] = errorMessage
                },
                ["files"] = new JArray()
            };

            return output.ToString(Formatting.Indented);
        }

        private string GetRelativePath(string fullPath)
        {
            try
            {
                if (string.IsNullOrEmpty(_projectRoot) || !fullPath.StartsWith(_projectRoot))
                    return Path.GetFileName(fullPath);
                
                return Path.GetRelativePath(_projectRoot, fullPath).Replace('\\', '/');
            }
            catch
            {
                return Path.GetFileName(fullPath);
            }
        }

        private string GetFileExtension(string filePath)
        {
            var extension = Path.GetExtension(filePath);
            return string.IsNullOrEmpty(extension) ? "txt" : extension.TrimStart('.');
        }

        private string DetermineChangeType(string oldContent, string newContent)
        {
            if (string.IsNullOrEmpty(oldContent) && !string.IsNullOrEmpty(newContent))
                return "addition";
            if (!string.IsNullOrEmpty(oldContent) && string.IsNullOrEmpty(newContent))
                return "deletion";
            return "modification";
        }

        [McpServerTool, Description("Modifies content within one or more existing files.")]
        public async Task<string> ModifyFiles([Description("JSON parameters for ModifyFiles")] string parameters = "{}")
        {
            return await ExecuteWithExtraProperties(parameters);
        }
    }
}
