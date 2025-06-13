// AiStudio4.Core\Tools\Git\GitStatusTool.cs














namespace AiStudio4.Core.Tools.Git
{
    /// <summary>
    /// Implementation of the GitStatus tool for repository state information
    /// </summary>
    public class GitStatusTool : BaseToolImplementation
    {
        public GitStatusTool(ILogger<GitStatusTool> logger, IGeneralSettingsService generalSettingsService, IStatusMessageService statusMessageService)
            : base(logger, generalSettingsService, statusMessageService)
        {
        }

        public override Tool GetToolDefinition()
        {
            return new Tool
            {
                Guid = ToolGuids.GIT_STATUS_TOOL_GUID,
                Name = "GitStatus",
                Description = "Shows working directory status, current branch, and repository state information including ahead/behind remote status.",
                Schema = """
{
  "name": "GitStatus",
  "description": "Shows working directory status, current branch, and repository state information including ahead/behind remote status.",
  "input_schema": {
    "type": "object",
    "properties": {
      "include_branches": { "type": "boolean", "description": "Include list of all branches in the output.", "default": false },
      "include_remote_status": { "type": "boolean", "description": "Include ahead/behind status compared to remote tracking branch.", "default": true },
      "porcelain": { "type": "boolean", "description": "Use porcelain format for machine-readable output.", "default": false }
    }
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
            SendStatusUpdate("Starting GitStatus tool execution...");
            var resultSummary = new StringBuilder();
            bool overallSuccess = true;
            var errors = new List<string>();
            JObject parameters = null;
            bool includeBranches = false;
            bool includeRemoteStatus = true;
            bool porcelain = false;

            // --- 1. Parse and Validate Input ---
            try
            {
                parameters = JObject.Parse(toolParameters);
                
                var includeBranchesToken = parameters["include_branches"];
                if (includeBranchesToken != null)
                {
                    includeBranches = includeBranchesToken.Value<bool>();
                }

                var includeRemoteStatusToken = parameters["include_remote_status"];
                if (includeRemoteStatusToken != null)
                {
                    includeRemoteStatus = includeRemoteStatusToken.Value<bool>();
                }

                var porcelainToken = parameters["porcelain"];
                if (porcelainToken != null)
                {
                    porcelain = porcelainToken.Value<bool>();
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
                _logger.LogError("GitStatus validation failed: {Errors}", string.Join("; ", errors));
                return CreateResult(false, false, string.Join("\n", errors));
            }

            // --- 2. Gather Git Status Information ---
            var statusInfo = new JObject();
            try
            {
                // Get current branch
                SendStatusUpdate("Getting current branch...");
                var currentBranchResult = await GetCurrentBranch();
                if (currentBranchResult["success"]?.Value<bool>() == true)
                {
                    statusInfo["currentBranch"] = currentBranchResult["currentBranch"];
                }
                else
                {
                    errors.Add($"Failed to get current branch: {currentBranchResult["error"]}");
                }

                // Get working directory status
                SendStatusUpdate("Getting working directory status...");
                var workingDirResult = await GetWorkingDirectoryStatus(porcelain);
                if (workingDirResult["success"]?.Value<bool>() == true)
                {
                    statusInfo["workingDirectory"] = workingDirResult["status"];
                    statusInfo["hasChanges"] = workingDirResult["hasChanges"];
                    statusInfo["fileCount"] = workingDirResult["fileCount"];
                }
                else
                {
                    errors.Add($"Failed to get working directory status: {workingDirResult["error"]}");
                }

                // Get remote tracking status if requested
                if (includeRemoteStatus)
                {
                    SendStatusUpdate("Getting remote tracking status...");
                    var remoteStatusResult = await GetRemoteTrackingStatus();
                    if (remoteStatusResult["success"]?.Value<bool>() == true)
                    {
                        statusInfo["remoteTracking"] = remoteStatusResult["tracking"];
                    }
                    else
                    {
                        // Remote tracking status is optional, don't fail if it's not available
                        statusInfo["remoteTracking"] = new JObject
                        {
                            ["available"] = false,
                            ["reason"] = remoteStatusResult["error"]
                        };
                    }
                }

                // Get branch list if requested
                if (includeBranches)
                {
                    SendStatusUpdate("Getting branch list...");
                    var branchListResult = await GetBranchList();
                    if (branchListResult["success"]?.Value<bool>() == true)
                    {
                        statusInfo["branches"] = branchListResult["branches"];
                    }
                    else
                    {
                        errors.Add($"Failed to get branch list: {branchListResult["error"]}");
                    }
                }

                resultSummary.AppendLine($"Repository status retrieved successfully");
                if (statusInfo["currentBranch"] != null)
                {
                    resultSummary.AppendLine($"Current branch: {statusInfo["currentBranch"]}");
                }
                if (statusInfo["hasChanges"]?.Value<bool>() == true)
                {
                    resultSummary.AppendLine($"Working directory has {statusInfo["fileCount"]} changed files");
                }
                else
                {
                    resultSummary.AppendLine("Working directory is clean");
                }
            }
            catch (Exception ex)
            {
                errors.Add($"Unexpected error during status gathering: {ex.Message}");
                overallSuccess = false;
            }

            // --- 3. Report Result ---
            var resultJson = new JObject
            {
                ["overallSuccess"] = overallSuccess,
                ["statusInfo"] = statusInfo,
                ["options"] = new JObject
                {
                    ["includeBranches"] = includeBranches,
                    ["includeRemoteStatus"] = includeRemoteStatus,
                    ["porcelain"] = porcelain
                },
                ["errors"] = new JArray(errors),
                ["summary"] = resultSummary.ToString().Trim()
            };

            if (overallSuccess)
            {
                SendStatusUpdate("GitStatus completed successfully.");
            }
            else
            {
                SendStatusUpdate("GitStatus completed with errors. See details.");
            }

            string resultMessage = overallSuccess 
                ? "Repository status retrieved successfully."
                : "Failed to retrieve repository status.";
            return CreateResult(true, continueProcessing: overallSuccess, resultJson.ToString(), resultMessage);
        }

        private async Task<JObject> GetCurrentBranch()
        {
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
                ["currentBranch"] = currentBranch
            };
        }

        private async Task<JObject> GetWorkingDirectoryStatus(bool porcelain)
        {
            string gitArgs = porcelain ? "status --porcelain" : "status --short";
            var result = await RunGitCommand(gitArgs);
            
            if (!result.Success)
            {
                return new JObject
                {
                    ["success"] = false,
                    ["error"] = result.Error
                };
            }

            var files = new List<JObject>();
            var lines = result.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                
                // Parse git status short format: XY filename
                if (line.Length >= 3)
                {
                    string indexStatus = line.Substring(0, 1);
                    string workTreeStatus = line.Substring(1, 1);
                    string fileName = line.Substring(3);
                    
                    files.Add(new JObject
                    {
                        ["file"] = fileName,
                        ["indexStatus"] = indexStatus,
                        ["workTreeStatus"] = workTreeStatus,
                        ["statusDescription"] = GetStatusDescription(indexStatus, workTreeStatus)
                    });
                }
            }

            return new JObject
            {
                ["success"] = true,
                ["status"] = new JArray(files),
                ["hasChanges"] = files.Count > 0,
                ["fileCount"] = files.Count,
                ["rawOutput"] = result.Output
            };
        }

        private async Task<JObject> GetRemoteTrackingStatus()
        {
            // Get the upstream branch
            var upstreamResult = await RunGitCommand("rev-parse --abbrev-ref @{upstream}");
            if (!upstreamResult.Success)
            {
                return new JObject
                {
                    ["success"] = false,
                    ["error"] = "No upstream branch configured"
                };
            }

            string upstream = upstreamResult.Output.Trim();
            
            // Get ahead/behind counts
            var countResult = await RunGitCommand($"rev-list --left-right --count HEAD...{upstream}");
            if (!countResult.Success)
            {
                return new JObject
                {
                    ["success"] = false,
                    ["error"] = countResult.Error
                };
            }

            var counts = countResult.Output.Trim().Split('\t');
            if (counts.Length == 2 && int.TryParse(counts[0], out int ahead) && int.TryParse(counts[1], out int behind))
            {
                return new JObject
                {
                    ["success"] = true,
                    ["tracking"] = new JObject
                    {
                        ["upstream"] = upstream,
                        ["ahead"] = ahead,
                        ["behind"] = behind,
                        ["upToDate"] = ahead == 0 && behind == 0
                    }
                };
            }

            return new JObject
            {
                ["success"] = false,
                ["error"] = "Failed to parse ahead/behind counts"
            };
        }

        private async Task<JObject> GetBranchList()
        {
            var result = await RunGitCommand("branch -a");
            
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
                ["branches"] = new JArray(branches)
            };
        }

        private string GetStatusDescription(string indexStatus, string workTreeStatus)
        {
            var descriptions = new List<string>();
            
            // Index status
            switch (indexStatus)
            {
                case "A": descriptions.Add("Added to index"); break;
                case "M": descriptions.Add("Modified in index"); break;
                case "D": descriptions.Add("Deleted from index"); break;
                case "R": descriptions.Add("Renamed in index"); break;
                case "C": descriptions.Add("Copied in index"); break;
                case "U": descriptions.Add("Unmerged in index"); break;
            }
            
            // Work tree status
            switch (workTreeStatus)
            {
                case "M": descriptions.Add("Modified in working tree"); break;
                case "D": descriptions.Add("Deleted in working tree"); break;
                case "?": descriptions.Add("Untracked"); break;
                case "!": descriptions.Add("Ignored"); break;
                case "U": descriptions.Add("Unmerged in working tree"); break;
            }
            
            return descriptions.Count > 0 ? string.Join(", ", descriptions) : "Unknown status";
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
