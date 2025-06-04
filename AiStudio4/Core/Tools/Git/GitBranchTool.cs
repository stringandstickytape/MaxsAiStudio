// AiStudio4.Core\Tools\Git\GitBranchTool.cs
using AiStudio4.Core.Interfaces;
using AiStudio4.Core.Models;
using AiStudio4.InjectedDependencies;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AiStudio4.Core.Tools.Git
{
    /// <summary>
    /// Implementation of the GitBranch tool for branch management operations
    /// </summary>
    public class GitBranchTool : BaseToolImplementation
    {
        public GitBranchTool(ILogger<GitBranchTool> logger, IGeneralSettingsService generalSettingsService, IStatusMessageService statusMessageService)
            : base(logger, generalSettingsService, statusMessageService)
        {
        }

        public override Tool GetToolDefinition()
        {
            return new Tool
            {
                Guid = "e5f6a7b8-c9d0-1234-5678-90abcdef1234",
                Name = "GitBranch",
                Description = "Manages git branches including creating, switching, listing, and deleting branches within the project repository.",
                Schema = @"{
  ""name"": ""GitBranch"",
  ""description"": ""Manages git branches including creating, switching, listing, and deleting branches within the project repository."",
  ""input_schema"": {
    ""type"": ""object"",
    ""properties"": {
      ""operation"": {
        ""type"": ""string"",
        ""enum"": [""create"", ""switch"", ""list"", ""delete"", ""current""],
        ""description"": ""The branch operation to perform.""
      },
      ""branch_name"": {
        ""type"": ""string"",
        ""description"": ""Name of the branch (required for create, switch, delete operations).""
      },
      ""create_from"": {
        ""type"": ""string"",
        ""description"": ""Reference to create branch from (optional for create operation, defaults to current HEAD).""
      },
      ""force"": {
        ""type"": ""boolean"",
        ""description"": ""Force the operation (use with caution, for delete operations)."",
        ""default"": false
      },
      ""include_remote"": {
        ""type"": ""boolean"",
        ""description"": ""Include remote branches in list operation."",
        ""default"": false
      }
    },
    ""required"": [""operation""]
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
            SendStatusUpdate("Starting GitBranch tool execution...");
            var resultSummary = new StringBuilder();
            bool overallSuccess = true;
            var errors = new List<string>();
            JObject parameters = null;
            string operation = null;
            string branchName = null;
            string createFrom = null;
            bool force = false;
            bool includeRemote = false;

            // --- 1. Parse and Validate Input ---
            try
            {
                parameters = JObject.Parse(toolParameters);
                
                operation = parameters["operation"]?.ToString();
                if (string.IsNullOrWhiteSpace(operation))
                {
                    errors.Add("'operation' is required and must be one of: create, switch, list, delete, current.");
                    overallSuccess = false;
                }
                else if (!new[] { "create", "switch", "list", "delete", "current" }.Contains(operation))
                {
                    errors.Add("'operation' must be one of: create, switch, list, delete, current.");
                    overallSuccess = false;
                }

                branchName = parameters["branch_name"]?.ToString();
                if (new[] { "create", "switch", "delete" }.Contains(operation) && string.IsNullOrWhiteSpace(branchName))
                {
                    errors.Add($"'branch_name' is required for {operation} operation.");
                    overallSuccess = false;
                }

                createFrom = parameters["create_from"]?.ToString();
                
                var forceToken = parameters["force"];
                if (forceToken != null)
                {
                    force = forceToken.Value<bool>();
                }

                var includeRemoteToken = parameters["include_remote"];
                if (includeRemoteToken != null)
                {
                    includeRemote = includeRemoteToken.Value<bool>();
                }
            }
            catch (JsonException jsonEx)
            {
                errors.Add($"Error parsing tool parameters JSON: {jsonEx.Message}");
                overallSuccess = false;
            }
            catch (Exception ex)
            {
                errors.Add($"Unexpected error during parsing/validation: {ex.Message}");
                overallSuccess = false;
            }

            if (!overallSuccess)
            {
                SendStatusUpdate("Validation failed. See error details.");
                _logger.LogError("GitBranch validation failed: {Errors}", string.Join("; ", errors));
                return CreateResult(false, false, string.Join("\n", errors));
            }

            // --- 2. Execute Branch Operation ---
            var operationResult = new JObject();
            try
            {
                switch (operation)
                {
                    case "create":
                        operationResult = await CreateBranch(branchName, createFrom);
                        break;
                    case "switch":
                        operationResult = await SwitchBranch(branchName);
                        break;
                    case "list":
                        operationResult = await ListBranches(includeRemote);
                        break;
                    case "delete":
                        operationResult = await DeleteBranch(branchName, force);
                        break;
                    case "current":
                        operationResult = await GetCurrentBranch();
                        break;
                }

                if (operationResult["success"]?.Value<bool>() == true)
                {
                    resultSummary.AppendLine(operationResult["message"]?.ToString());
                }
                else
                {
                    errors.Add(operationResult["error"]?.ToString() ?? "Operation failed");
                    overallSuccess = false;
                }
            }
            catch (Exception ex)
            {
                errors.Add($"Unexpected error during {operation} operation: {ex.Message}");
                overallSuccess = false;
            }

            // --- 3. Report Result ---
            var resultJson = new JObject
            {
                ["overallSuccess"] = overallSuccess,
                ["operation"] = operation,
                ["branchName"] = branchName,
                ["operationResult"] = operationResult,
                ["errors"] = new JArray(errors),
                ["summary"] = resultSummary.ToString().Trim()
            };

            if (overallSuccess)
            {
                SendStatusUpdate($"GitBranch {operation} operation completed successfully.");
            }
            else
            {
                SendStatusUpdate($"GitBranch {operation} operation completed with errors. See details.");
            }

            string resultMessage = overallSuccess 
                ? $"Branch {operation} operation successful."
                : $"Branch {operation} operation failed.";
            return CreateResult(true, continueProcessing: overallSuccess, resultJson.ToString(), resultMessage);
        }

        private async Task<JObject> CreateBranch(string branchName, string createFrom)
        {
            SendStatusUpdate($"Creating branch '{branchName}'...");
            
            string gitArgs = string.IsNullOrWhiteSpace(createFrom) 
                ? $"checkout -b \"{branchName}\""
                : $"checkout -b \"{branchName}\" \"{createFrom}\"";

            var result = await RunGitCommand(gitArgs);
            
            return new JObject
            {
                ["success"] = result.Success,
                ["message"] = result.Success ? $"Created and switched to branch '{branchName}'" : null,
                ["error"] = result.Success ? null : result.Error,
                ["output"] = result.Output
            };
        }

        private async Task<JObject> SwitchBranch(string branchName)
        {
            SendStatusUpdate($"Switching to branch '{branchName}'...");
            
            var result = await RunGitCommand($"checkout \"{branchName}\"");
            
            return new JObject
            {
                ["success"] = result.Success,
                ["message"] = result.Success ? $"Switched to branch '{branchName}'" : null,
                ["error"] = result.Success ? null : result.Error,
                ["output"] = result.Output
            };
        }

        private async Task<JObject> ListBranches(bool includeRemote)
        {
            SendStatusUpdate("Listing branches...");
            
            string gitArgs = includeRemote ? "branch -a" : "branch";
            var result = await RunGitCommand(gitArgs);
            
            if (!result.Success)
            {
                return new JObject
                {
                    ["success"] = false,
                    ["error"] = result.Error
                };
            }

            var branches = new List<JObject>();
            var lines = result.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                
                var trimmedLine = line.Trim();
                bool isCurrent = trimmedLine.StartsWith("* ");
                string branchName = isCurrent ? trimmedLine.Substring(2) : trimmedLine;
                bool isRemote = branchName.StartsWith("remotes/");
                
                branches.Add(new JObject
                {
                    ["name"] = branchName,
                    ["current"] = isCurrent,
                    ["remote"] = isRemote
                });
            }

            return new JObject
            {
                ["success"] = true,
                ["message"] = $"Found {branches.Count} branches",
                ["branches"] = new JArray(branches),
                ["output"] = result.Output
            };
        }

        private async Task<JObject> DeleteBranch(string branchName, bool force)
        {
            SendStatusUpdate($"Deleting branch '{branchName}'...");
            
            string gitArgs = force 
                ? $"branch -D \"{branchName}\""
                : $"branch -d \"{branchName}\"";

            var result = await RunGitCommand(gitArgs);
            
            return new JObject
            {
                ["success"] = result.Success,
                ["message"] = result.Success ? $"Deleted branch '{branchName}'" : null,
                ["error"] = result.Success ? null : result.Error,
                ["output"] = result.Output
            };
        }

        private async Task<JObject> GetCurrentBranch()
        {
            SendStatusUpdate("Getting current branch...");
            
            var result = await RunGitCommand("branch --show-current");
            
            if (!result.Success)
            {
                return new JObject
                {
                    ["success"] = false,
                    ["error"] = result.Error
                };
            }

            string currentBranch = result.Output.Trim();
            
            return new JObject
            {
                ["success"] = true,
                ["message"] = $"Current branch: {currentBranch}",
                ["currentBranch"] = currentBranch,
                ["output"] = result.Output
            };
        }

        /// <summary>
        /// Runs a git command in the project root directory.
        /// </summary>
        private async Task<(bool Success, string Output, string Error)> RunGitCommand(string arguments)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = arguments,
                WorkingDirectory = _projectRoot,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            try
            {
                using (var process = new Process { StartInfo = psi })
                {
                    var outputBuilder = new StringBuilder();
                    var errorBuilder = new StringBuilder();

                    process.OutputDataReceived += (s, e) => { if (e.Data != null) outputBuilder.AppendLine(e.Data); };
                    process.ErrorDataReceived += (s, e) => { if (e.Data != null) errorBuilder.AppendLine(e.Data); };

                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                    await process.WaitForExitAsync();

                    string output = outputBuilder.ToString();
                    string error = errorBuilder.ToString();
                    bool success = process.ExitCode == 0;
                    return (success, output, error);
                }
            }
            catch (Exception ex)
            {
                return (false, null, $"Exception running git command: {ex.Message}");
            }
        }
    }
}