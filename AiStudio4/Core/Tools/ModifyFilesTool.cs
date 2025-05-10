// AiStudio4.Core\Tools\ModifyFilesTool.cs
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
                Guid = "a1b2c3d4-e5f6-7890-1234-567890abcd43", // Fixed GUID for ModifyFiles
                Description = "Modifies content within one or more existing files.",
                Name = "ModifyFiles",
                Schema = @"{
                  ""name"": ""ModifyFiles"",
                  ""description"": ""Modifies content within one or more existing files. Supports multiple changes across multiple files in a single call."",
                  ""input_schema"": {
                    ""type"": ""object"",
                    ""properties"": {
                      ""modifications"": {
                        ""type"": ""array"",
                        ""description"": ""Array of file modifications to perform"",
                        ""items"": {
                          ""type"": ""object"",
                          ""properties"": {
                            ""path"": {
                              ""type"": ""string"",
                              ""description"": ""The absolute path to the file to modify""
                            },
                            ""changes"": {
                              ""type"": ""array"",
                              ""description"": ""Array of changes to make to this file"",
                              ""items"": {
                                ""type"": ""object"",
                                ""properties"": {
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
                                    ""description"": ""A human-readable explanation of this specific change""
                                  }
                                },
                                ""required"": [
                                  ""lineNumber"",
                                  ""oldContent"",
                                  ""newContent"",
                                  ""description""
                                ]
                              }
                            }
                          },
                          ""required"": [
                            ""path"",
                            ""changes""
                          ]
                        }
                      }
                    },
                    ""required"": [
                      ""modifications""
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
            SendStatusUpdate("Starting ModifyFiles tool execution...");
            
            JObject parameters;
            JArray modifications = null;

            // --- Parse and Validate Input Structure ---
            try
            {
                parameters = JObject.Parse(toolParameters);
                
                // Get the modifications array
                modifications = parameters["modifications"] as JArray;
                if (modifications == null || modifications.Count == 0)
                {
                    _validationErrorMessages.AppendLine("Error: 'modifications' array is missing or empty.");
                    overallSuccess = false;
                }
                else
                {
                    // Validate each modification
                    foreach (JObject modification in modifications)
                    {
                        string filePath = modification["path"]?.ToString();
                        if (string.IsNullOrEmpty(filePath))
                        {
                            _validationErrorMessages.AppendLine("Error: 'path' is missing or empty in a modification.");
                            overallSuccess = false;
                            continue;
                        }
                        
                        // Validate file path security
                        if (!_pathSecurityManager.IsPathSafe(filePath))
                        {
                            _validationErrorMessages.AppendLine($"Error: Path '{filePath}' is outside the allowed project directory.");
                            overallSuccess = false;
                            continue;
                        }
                        
                        // Validate file exists
                        if (!File.Exists(filePath))
                        {
                            _validationErrorMessages.AppendLine($"Error: File '{filePath}' does not exist.");
                            overallSuccess = false;
                            continue;
                        }
                        
                        // Validate changes array
                        var changes = modification["changes"] as JArray;
                        if (changes == null || changes.Count == 0)
                        {
                            _validationErrorMessages.AppendLine($"Error: 'changes' array is missing or empty for file '{filePath}'.");
                            overallSuccess = false;
                            continue;
                        }
                        
                        // Validate each change
                        foreach (JObject change in changes)
                        {
                            // Validate required fields
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
                            if (newContent == null) // Allow empty string for newContent
                            {
                                _validationErrorMessages.AppendLine($"Error: 'newContent' is missing in a change for file '{filePath}'.");
                                overallSuccess = false;
                            }
                            
                            if (change["description"] == null)
                            {
                                _validationErrorMessages.AppendLine($"Error: 'description' is missing in a change for file '{filePath}'.");
                                overallSuccess = false;
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

            // --- Stop if Validation Failed ---
            if (!overallSuccess)
            {
                _logger.LogError("ModifyFiles request validation failed:\n{Errors}", _validationErrorMessages.ToString());
                SendStatusUpdate("Validation failed. See error details.");
                MessageBox.Show(_validationErrorMessages.ToString(), "ModifyFiles Validation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return CreateResult(false, false, $"Validation failed: {_validationErrorMessages.ToString()}");
            }
            
            // --- Preprocess Modifications to Consolidate Changes by File Path ---
            if (modifications != null && modifications.Count > 0)
            {
                // Group changes by file path
                var changesByFilePath = new Dictionary<string, List<JObject>>();
                
                foreach (JObject modification in modifications)
                {
                    string filePath = modification["path"].ToString();
                    var changes = modification["changes"] as JArray;
                    
                    if (!changesByFilePath.ContainsKey(filePath))
                    {
                        changesByFilePath[filePath] = new List<JObject>();
                    }
                    
                    // Add all changes for this file path
                    foreach (JObject change in changes)
                    {
                        changesByFilePath[filePath].Add(change);
                    }
                }
                
                // Rebuild modifications array with consolidated changes
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
                
                // Replace the original modifications array
                modifications = consolidatedModifications;
                SendStatusUpdate($"Consolidated changes for {changesByFilePath.Count} file(s)...");
            }

            // --- Process the Modifications ---
            try
            {
                SendStatusUpdate($"Processing {modifications.Count} file modification(s)...");
                
                var handler = new ModifyFileHandler(_logger, _statusMessageService, _clientId, _secondaryAiService);
                var results = new List<(string FilePath, bool Success, string Message)>();
                var processedFiles = new HashSet<string>();
                
                // Process each file modification
                foreach (JObject modification in modifications)
                {
                    string filePath = modification["path"].ToString();
                    var changesArray = modification["changes"] as JArray;
                    
                    processedFiles.Add(filePath);
                    SendStatusUpdate($"Modifying file: {Path.GetFileName(filePath)} ({changesArray.Count} change(s))");
                    
                    // Convert JArray to List<JObject> for the handler
                    var changes = changesArray.Select(c => c as JObject).Where(c => c != null).ToList();
                    
                    // Use the HandleModifyFileAsync method which can process multiple changes at once
                    var result = await handler.HandleModifyFilesAsync(filePath, changes);
                    results.Add((filePath, result.Success, result.Message));
                    
                    if (!result.Success)
                    {
                        _logger.LogError($"Failed to apply changes to {filePath}: {result.Message}");
                    }
                }
                
                // Check if all modifications were successful
                bool allSuccessful = results.All(r => r.Success);
                
                if (allSuccessful)
                {
                    SendStatusUpdate("ModifyFiles completed successfully.");
                    return CreateResult(true, true, toolParameters, "All files modified successfully.");
                }
                else
                {
                    // Build error message with details of failed modifications
                    var failedResults = results.Where(r => !r.Success).ToList();
                    var errorMessage = new StringBuilder("Failed to modify some files:\n");
                    foreach (var failure in failedResults)
                    {
                        errorMessage.AppendLine($"- {Path.GetFileName(failure.FilePath)}: {failure.Message}");
                    }
                    
                    string errorSummary = errorMessage.ToString();
                    SendStatusUpdate("ModifyFiles completed with errors. See details.");
                    MessageBox.Show(errorSummary, "ModifyFiles Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return CreateResult(true, false, toolParameters, errorSummary);
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