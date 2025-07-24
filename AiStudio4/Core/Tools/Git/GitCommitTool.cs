// AiStudio4.Core\Tools\GitCommitTool.cs

using ModelContextProtocol;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace AiStudio4.Core.Tools.Git
{
    /// <summary>
    /// Implementation of the GitCommit tool
    /// </summary>
    [McpServerToolType]
    public class GitCommitTool : BaseToolImplementation
    {
        public GitCommitTool(ILogger<GitCommitTool> logger, IGeneralSettingsService generalSettingsService, IStatusMessageService statusMessageService)
            : base(logger, generalSettingsService, statusMessageService)
        {
        }

        public override Tool GetToolDefinition()
        {
            return new Tool
            {
                Guid = ToolGuids.GIT_COMMIT_TOOL_GUID,
                Name = "GitCommit",
                Description = "Commits a specified set of files to the git repository with a provided commit message and pushes changes by default. Only files within the project root may be committed.",
                Schema = """
{
  "name": "GitCommit",
  "description": "Commits a specified set of files to the git repository with a provided commit message and pushes changes by default. Only files within the project root may be committed.",
  "input_schema": {
    "type": "object",
    "properties": {
      "commit": {
        "type": "object",
        "description": "The commit operation parameters.",
        "properties": {
          "message": { "type": "string", "description": "The commit message to use. Must be non-empty." },
          "files": { "type": "array", "description": "An array of absolute file paths to commit. Each must be within the project root.", "items": { "type": "string" }, "minItems": 1 },
          "push": { "type": "boolean", "description": "Whether to push changes after committing. Defaults to true.", "default": true },
          "push_new_branch": { "type": "boolean", "description": "Whether to push a new branch to remote if it doesn't exist. Only applies when push is true.", "default": true }
        },
        "required": ["message", "files"]
      }
    },
    "required": ["commit"]
  }
}
""",
                Categories = new List<string> { "MaxCode" },
                OutputFileType = "gitcommit", 
                Filetype = string.Empty,
                LastModified = DateTime.UtcNow
            };
        }

        public override async Task<BuiltinToolResult> ProcessAsync(string toolParameters, Dictionary<string, string> extraProperties)
        {
            SendStatusUpdate("Starting GitCommit tool execution...");
            var resultSummary = new StringBuilder();
            bool overallSuccess = true;
            var errors = new List<string>();
            JObject parameters = null;
            JObject commitObj = null;
            string commitMessage = null;
            JArray filesArray = null;
            bool shouldPush = true; // Default to true
            bool pushNewBranch = true; // Default to true
            List<string> filesToCommit = new List<string>();

            // --- 1. Parse and Validate Input ---
            try
            {
                parameters = JObject.Parse(toolParameters);
                commitObj = parameters["commit"] as JObject;
                if (commitObj == null)
                {
                    errors.Add("Missing or invalid 'commit' object in parameters.");
                    overallSuccess = false;
                }
                else
                {
                    commitMessage = commitObj["message"]?.ToString();
                    filesArray = commitObj["files"] as JArray;
                    // Parse push parameter, default to true if not specified
                    var pushToken = commitObj["push"];
                    if (pushToken != null)
                    {
                        shouldPush = pushToken.Value<bool>();
                    }
                    // Parse push_new_branch parameter, default to true if not specified
                    var pushNewBranchToken = commitObj["push_new_branch"];
                    if (pushNewBranchToken != null)
                    {
                        pushNewBranch = pushNewBranchToken.Value<bool>();
                    }
                    if (string.IsNullOrWhiteSpace(commitMessage))
                    {
                        errors.Add("Commit message must be a non-empty string.");
                        overallSuccess = false;
                    }
                    if (filesArray == null || !filesArray.Any())
                    {
                        errors.Add("'files' array is missing or empty.");
                        overallSuccess = false;
                    }
                    else
                    {
                        foreach (var fileToken in filesArray)
                        {
                            string filePath = fileToken?.ToString();
                            if (string.IsNullOrWhiteSpace(filePath))
                            {
                                errors.Add("A file path in 'files' is null or empty.");
                                overallSuccess = false;
                                continue;
                            }
                            string normalizedPath;
                            try
                            {
                                normalizedPath = Path.Combine(_projectRoot, filePath);
                            }
                            catch (Exception ex)
                            {
                                errors.Add($"Invalid file path '{filePath}': {ex.Message}");
                                overallSuccess = false;
                                continue;
                            }
                            if (!IsPathWithinProjectRoot(normalizedPath, out string rootError))
                            {
                                errors.Add($"File '{filePath}' is outside the project root. {rootError}");
                                overallSuccess = false;
                                continue;
                            }

                            // Don't check the file exists.  It might be a delete we want to commit.
                            //if (!File.Exists(normalizedPath))...
                            
                            filesToCommit.Add(normalizedPath);
                        }
                    }
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
                _logger.LogError("GitCommit validation failed: {Errors}", string.Join("; ", errors));
                return CreateResult(false, false, string.Join("\n", errors));
            }

            // --- 2. Prepare and Run Commit ---
            try
            {
                SendStatusUpdate($"Staging {filesToCommit.Count} files for commit...");
                foreach (var file in filesToCommit)
                {
                    var addResult = await RunGitCommand($"add -- \"{file}\"");
                    if (!addResult.Success)
                    {
                        errors.Add($"Failed to stage file '{file}': {addResult.Error}");
                        overallSuccess = false;
                    }
                }
                if (!overallSuccess)
                {
                    SendStatusUpdate("Failed to stage one or more files. Aborting commit.");
                    return CreateResult(false, false, string.Join("\n", errors));
                }

                SendStatusUpdate("Running git commit...");
                string filesArg = string.Join(" ", filesToCommit.Select(f => $"\"{f}\""));
                var commitResult = await RunGitCommand($"commit -m \"{EscapeForCmd(commitMessage)}\" -- {filesArg}");
                if (!commitResult.Success)
                {
                    errors.Add($"Git commit failed: {commitResult.Error}");
                    overallSuccess = false;
                }
                else
                {
                    resultSummary.AppendLine($"Committed files:\n{string.Join("\n", filesToCommit)}");
                    resultSummary.AppendLine($"Commit message: {commitMessage}");
                    resultSummary.AppendLine($"Git commit output: {commitResult.Output}");
                    
                    // Push changes if requested (default behavior)
                    if (shouldPush)
                    {
                        SendStatusUpdate("Pushing changes to remote repository...");
                        
                        // Try regular push first
                        var pushResult = await RunGitCommand("push");
                        
                        // If push fails and we should push new branches, try push with upstream
                        if (!pushResult.Success && pushNewBranch && pushResult.Error.Contains("no upstream branch"))
                        {
                            SendStatusUpdate("Setting upstream and pushing new branch...");
                            var currentBranchResult = await RunGitCommand("branch --show-current");
                            if (currentBranchResult.Success)
                            {
                                string currentBranch = currentBranchResult.Output.Trim();
                                pushResult = await RunGitCommand($"push --set-upstream origin {currentBranch}");
                            }
                        }
                        
                        if (!pushResult.Success)
                        {
                            errors.Add($"Git push failed: {pushResult.Error}");
                            overallSuccess = false;
                            resultSummary.AppendLine($"Push failed: {pushResult.Error}");
                        }
                        else
                        {
                            resultSummary.AppendLine($"Git push output: {pushResult.Output}");
                            resultSummary.AppendLine("Changes pushed successfully.");
                        }
                    }
                    else
                    {
                        resultSummary.AppendLine("Push skipped as requested.");
                    }
                }
            }
            catch (Exception ex)
            {
                errors.Add($"Unexpected error during git commit: {ex.Message}");
                overallSuccess = false;
            }

            // --- 3. Report Result ---
            var resultJson = new JObject
            {
                ["overallSuccess"] = overallSuccess,
                ["committedFiles"] = new JArray(filesToCommit),
                ["commitMessage"] = commitMessage,
                ["pushRequested"] = shouldPush,
                ["pushNewBranch"] = pushNewBranch,
                ["errors"] = new JArray(errors),
                ["summary"] = resultSummary.ToString().Trim()
            };
            if (overallSuccess)
            {
                string successMessage = shouldPush ? "GitCommit and push completed successfully." : "GitCommit completed successfully (push skipped).";
                SendStatusUpdate(successMessage);
            }
            else
            {
                SendStatusUpdate("GitCommit completed with errors. See details.");
            }

            string resultMessage = overallSuccess 
                ? (shouldPush ? "Commit and push successful." : "Commit successful (push skipped).")
                : "Commit failed.";
            return CreateResult(true, continueProcessing: false, resultJson.ToString(), resultMessage);
        }

        /// <summary>
        /// Checks if a path is within the project root directory.
        /// </summary>
        private bool IsPathWithinProjectRoot(string normalizedPath, out string error)
        {
            error = null;
            if (string.IsNullOrEmpty(_projectRoot))
            {
                error = "Project root path is not set.";
                return false;
            }
            try
            {
                string normalizedRoot = Path.GetFullPath(_projectRoot);
                normalizedPath = normalizedPath.Replace("/", "\\");
                string pathWithSep = normalizedPath.EndsWith(Path.DirectorySeparatorChar.ToString()) ? normalizedPath : normalizedPath + Path.DirectorySeparatorChar;
                string rootWithSep = normalizedRoot.EndsWith(Path.DirectorySeparatorChar.ToString()) ? normalizedRoot : normalizedRoot + Path.DirectorySeparatorChar;
                bool isWithin = pathWithSep.StartsWith(rootWithSep, StringComparison.OrdinalIgnoreCase);
                if (!isWithin)
                {
                    error = $"Path '{normalizedPath}' is outside project root '{normalizedRoot}'.";
                }
                return isWithin;
            }
            catch (Exception ex)
            {
                error = $"Error validating path: {ex.Message}";
                return false;
            }
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
                    var tcs = new TaskCompletionSource<bool>();

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

        /// <summary>
        /// Escapes double quotes for command line arguments.
        /// </summary>
        private string EscapeForCmd(string input)
        {
            return input?.Replace("\"", "\\\"");
        }

        [McpServerTool, Description("Commits a specified set of files to the git repository with a provided commit message and pushes changes by default. Only files within the project root may be committed.")]
        public async Task<string> GitCommit([Description("JSON parameters for GitCommit")] string parameters = "{}")
        {
            return await ExecuteWithExtraProperties(parameters);
        }
    }
}
