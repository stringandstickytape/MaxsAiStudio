// AiStudio4.Core\Tools\Git\GitLogTool.cs

using ModelContextProtocol;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace AiStudio4.Core.Tools.Git
{
    /// <summary>
    /// Implementation of the GitLog tool
    /// </summary>
    [McpServerToolType]
    public class GitLogTool : BaseToolImplementation
    {
        private const int MAX_MESSAGE_LENGTH = 150;

        public GitLogTool(ILogger<GitLogTool> logger, IGeneralSettingsService generalSettingsService, IStatusMessageService statusMessageService)
            : base(logger, generalSettingsService, statusMessageService)
        {
        }

        public override Tool GetToolDefinition()
        {
            return new Tool
            {
                Guid = ToolGuids.GIT_LOG_TOOL_GUID,
                Name = "GitLog",
                Description = "Retrieves git commit history between two references (tags, branches, commits) without showing diffs. Commit messages are automatically truncated to prevent excessive output. Useful for understanding what changed between versions or getting an overview of recent development.",
                Schema = """
{
  "name": "GitLog",
  "description": "Retrieves git commit history between two references (tags, branches, commits) without showing diffs. Commit messages are automatically truncated to prevent excessive output. Useful for understanding what changed between versions or getting an overview of recent development.",
  "input_schema": {
    "type": "object",
    "properties": {
      "from_ref": { "type": "string", "description": "Starting git reference (tag, branch, or commit hash). Use format like 'v0.93', 'main', or commit hash." },
      "to_ref": { "type": "string", "description": "Ending git reference (tag, branch, or commit hash). Defaults to 'HEAD' if not specified.", "default": "HEAD" },
      "limit": { "type": "integer", "description": "Maximum number of commits to return. Defaults to 100 to prevent excessive output.", "default": 100, "minimum": 1, "maximum": 500 },
      "format": { "type": "string", "enum": ["oneline", "short", "full"], "description": "Output format. 'oneline' shows hash and message only, 'short' adds author and date, 'full' includes all metadata.", "default": "short" },
      "reverse": { "type": "boolean", "description": "Show commits in chronological order (oldest first) instead of reverse chronological order.", "default": false }
    },
    "required": ["from_ref"]
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
            SendStatusUpdate("Starting GitLog tool execution...");
            var resultSummary = new StringBuilder();
            bool overallSuccess = true;
            var errors = new List<string>();
            JObject parameters = null;
            string fromRef = null;
            string toRef = "HEAD";
            int limit = 100;
            string format = "short";
            bool reverse = false;

            // --- 1. Parse and Validate Input ---
            try
            {
                parameters = JObject.Parse(toolParameters);
                
                fromRef = parameters["from_ref"]?.ToString();
                if (string.IsNullOrWhiteSpace(fromRef))
                {
                    errors.Add("'from_ref' is required and must be a non-empty string.");
                    overallSuccess = false;
                }

                var toRefToken = parameters["to_ref"];
                if (toRefToken != null && !string.IsNullOrWhiteSpace(toRefToken.ToString()))
                {
                    toRef = toRefToken.ToString();
                }

                var limitToken = parameters["limit"];
                if (limitToken != null)
                {
                    if (!int.TryParse(limitToken.ToString(), out limit) || limit < 1 || limit > 500)
                    {
                        errors.Add("'limit' must be an integer between 1 and 500.");
                        overallSuccess = false;
                    }
                }

                var formatToken = parameters["format"];
                if (formatToken != null)
                {
                    format = formatToken.ToString();
                    if (!new[] { "oneline", "short", "full" }.Contains(format))
                    {
                        errors.Add("'format' must be one of: oneline, short, full.");
                        overallSuccess = false;
                    }
                }

                var reverseToken = parameters["reverse"];
                if (reverseToken != null)
                {
                    reverse = reverseToken.Value<bool>();
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
                _logger.LogError("GitLog validation failed: {Errors}", string.Join("; ", errors));
                return CreateResult(false, false, string.Join("\n", errors));
            }

            // --- 2. Build Git Log Command ---
            var commits = new List<JObject>();
            try
            {
                SendStatusUpdate($"Retrieving git log from {fromRef} to {toRef}...");
                
                string gitFormat;
                switch (format)
                {
                    case "oneline":
                        gitFormat = "--pretty=format:%H|%s";
                        break;
                    case "full":
                        gitFormat = "--pretty=format:%H|%an|%ae|%ad|%s|%b";
                        break;
                    default: // "short"
                        gitFormat = "--pretty=format:%H|%an|%ad|%s";
                        break;
                }

                string rangeArg = $"{fromRef}..{toRef}";
                string reverseArg = reverse ? "--reverse" : "";
                string gitArgs = $"log {gitFormat} --date=iso {reverseArg} -n {limit} {rangeArg}";

                var logResult = await RunGitCommand(gitArgs);
                if (!logResult.Success)
                {
                    errors.Add($"Git log failed: {logResult.Error}");
                    overallSuccess = false;
                }
                else
                {
                    // Parse git log output
                    var lines = logResult.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                    foreach (var line in lines)
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        
                        var parts = line.Split('|');
                        if (parts.Length < 2) continue;

                        var commit = new JObject();
                        commit["hash"] = parts[0].Trim();
                        
                        switch (format)
                        {
                            case "oneline":
                                commit["message"] = TruncateMessage(parts[1].Trim());
                                break;
                            case "full":
                                if (parts.Length >= 6)
                                {
                                    commit["author"] = parts[1].Trim();
                                    commit["email"] = parts[2].Trim();
                                    commit["date"] = parts[3].Trim();
                                    commit["message"] = TruncateMessage(parts[4].Trim());
                                    commit["body"] = TruncateMessage(parts[5].Trim());
                                }
                                break;
                            default: // "short"
                                if (parts.Length >= 4)
                                {
                                    commit["author"] = parts[1].Trim();
                                    commit["date"] = parts[2].Trim();
                                    commit["message"] = TruncateMessage(parts[3].Trim());
                                }
                                break;
                        }
                        
                        commits.Add(commit);
                    }

                    resultSummary.AppendLine($"Retrieved {commits.Count} commits from {fromRef} to {toRef}");
                    if (commits.Count == limit)
                    {
                        resultSummary.AppendLine($"Note: Output limited to {limit} commits. There may be more commits in this range.");
                    }
                }
            }
            catch (Exception ex)
            {
                errors.Add($"Unexpected error during git log: {ex.Message}");
                overallSuccess = false;
            }

            // --- 3. Report Result ---
            var resultJson = new JObject
            {
                ["overallSuccess"] = overallSuccess,
                ["fromRef"] = fromRef,
                ["toRef"] = toRef,
                ["format"] = format,
                ["limit"] = limit,
                ["reverse"] = reverse,
                ["commitCount"] = commits.Count,
                ["commits"] = new JArray(commits),
                ["errors"] = new JArray(errors),
                ["summary"] = resultSummary.ToString().Trim()
            };

            if (overallSuccess)
            {
                SendStatusUpdate($"GitLog completed successfully. Retrieved {commits.Count} commits.");
            }
            else
            {
                SendStatusUpdate("GitLog completed with errors. See details.");
            }

            string resultMessage = overallSuccess 
                ? $"Retrieved {commits.Count} commits from {fromRef} to {toRef}."
                : "Git log failed.";
            return CreateResult(true, continueProcessing: overallSuccess, resultJson.ToString(), resultMessage);
        }

        /// <summary>
        /// Truncates commit messages to prevent excessive output.
        /// </summary>
        private string TruncateMessage(string message)
        {
            if (string.IsNullOrEmpty(message)) return message;
            
            if (message.Length <= MAX_MESSAGE_LENGTH) return message;
            
            return message.Substring(0, MAX_MESSAGE_LENGTH - 3) + "...";
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

        [McpServerTool, Description("Retrieves git commit history between two references (tags, branches, commits) without showing diffs. Commit messages are automatically truncated to prevent excessive output. Useful for understanding what changed between versions or getting an overview of recent development.")]
        public async Task<string> GitLog([Description("JSON parameters for GitLog")] string parameters = "{}")
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
