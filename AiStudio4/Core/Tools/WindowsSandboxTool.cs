// AiStudio4.Core\Tools\WindowsSandboxTool.cs

using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AiStudio4.Core.Tools
{
    /// <summary>
    /// Implementation of the Windows Sandbox tool that provides safe, isolated Windows environments for testing and execution.
    /// All file operations are constrained to %APPDATA%\WSBTool\ for security.
    /// </summary>
    public class WindowsSandboxTool : BaseToolImplementation
    {
        private readonly StringBuilder _validationErrorMessages;
        private readonly string _sandboxExchangeRoot;
        private readonly string _inputFolder;
        private readonly string _outputFolder;
        private readonly string _logsFolder;
        private readonly string _tempFolder;

        public WindowsSandboxTool(ILogger<WindowsSandboxTool> logger, IGeneralSettingsService generalSettingsService, 
            IStatusMessageService statusMessageService) 
            : base(logger, generalSettingsService, statusMessageService)
        {
            _validationErrorMessages = new StringBuilder();
            
            // Initialize the constrained folder structure
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            _sandboxExchangeRoot = Path.Combine(appDataPath, "WSBTool");
            _inputFolder = Path.Combine(_sandboxExchangeRoot, "input");
            _outputFolder = Path.Combine(_sandboxExchangeRoot, "output");
            _logsFolder = Path.Combine(_sandboxExchangeRoot, "logs");
            _tempFolder = Path.Combine(_sandboxExchangeRoot, "temp");
            
            // Ensure directories exist
            EnsureDirectoriesExist();
        }

        private void EnsureDirectoriesExist()
        {
            try
            {
                Directory.CreateDirectory(_sandboxExchangeRoot);
                Directory.CreateDirectory(_inputFolder);
                Directory.CreateDirectory(_outputFolder);
                Directory.CreateDirectory(_logsFolder);
                Directory.CreateDirectory(_tempFolder);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create WSBTool directories");
            }
        }

        public override Tool GetToolDefinition()
        {
            return new Tool
            {
                Guid = ToolGuids.WINDOWS_SANDBOX_TOOL_GUID,
                Description = "Creates and manages Windows Sandbox environments for safe testing and execution. All file operations are constrained to %APPDATA%\\WSBTool\\ for security.",
                Name = "WindowsSandbox",
                Schema = """
{
  "name": "WindowsSandbox",
  "description": "Creates and manages Windows Sandbox environments for safe testing and execution. Supports starting sandboxes, executing commands, sharing files, and managing sandbox lifecycle.",
  "input_schema": {
    "type": "object",
    "properties": {
      "action": {
        "type": "string",
        "enum": ["start", "exec", "stop", "list", "share", "connect", "ip", "workflow"],
        "description": "The action to perform: start (create sandbox), exec (run command), stop (terminate), list (show running), share (setup folder sharing), connect (remote desktop), ip (get sandbox IP), workflow (automated start->share->exec sequence)"
      },
      "sandboxId": {
        "type": "string",
        "description": "The ID of the sandbox to operate on (required for exec, stop, share, connect, ip actions)"
      },
      "command": {
        "type": "string",
        "description": "The command to execute in the sandbox (required for exec action)"
      },
      "runAs": {
        "type": "string",
        "enum": ["System", "ExistingLogin"],
        "default": "System",
        "description": "User context to run command in (for exec action)"
      },
      "workingDirectory": {
        "type": "string",
        "description": "Working directory for command execution (for exec action)"
      },
      "config": {
        "type": "string",
        "description": "Custom XML configuration for sandbox creation (for start action)"
      },
      "inputFiles": {
        "type": "array",
        "items": {
          "type": "object",
          "properties": {
            "filename": { "type": "string", "description": "Name of the file to create in input folder" },
            "content": { "type": "string", "description": "Content of the file" }
          },
          "required": ["filename", "content"]
        },
        "description": "Files to place in the input folder before sandbox operations"
      },
      "allowWrite": {
        "type": "boolean",
        "default": true,
        "description": "Whether to allow the sandbox to write to the shared folder (for share action)"
      },
      "description": {
        "type": "string",
        "description": "Human-readable description of what this sandbox operation is for"
      }
    },
    "required": ["action"]
  }
}
""",
                Categories = new List<string> { "MaxCode", "System" },
                OutputFileType = "json",
                Filetype = string.Empty,
                LastModified = DateTime.UtcNow
            };
        }

        public override async Task<BuiltinToolResult> ProcessAsync(string toolParameters, Dictionary<string, string> extraProperties)
        {
            _validationErrorMessages.Clear();
            var overallSuccess = true;
            
            SendStatusUpdate("Starting Windows Sandbox tool execution...");
            
            JObject parameters;
            string action = null;
            string sandboxId = null;
            string command = null;
            string runAs = "System";
            string workingDirectory = null;
            string config = null;
            JArray inputFiles = null;
            bool allowWrite = true;
            string description = null;

            // Parse and validate input
            try
            {
                parameters = JObject.Parse(toolParameters);
                
                action = parameters["action"]?.ToString();
                if (string.IsNullOrEmpty(action))
                {
                    _validationErrorMessages.AppendLine("Error: 'action' is required.");
                    overallSuccess = false;
                }
                
                sandboxId = parameters["sandboxId"]?.ToString();
                command = parameters["command"]?.ToString();
                runAs = parameters["runAs"]?.ToString() ?? "System";
                workingDirectory = parameters["workingDirectory"]?.ToString();
                config = parameters["config"]?.ToString();
                inputFiles = parameters["inputFiles"] as JArray;
                allowWrite = parameters["allowWrite"]?.ToObject<bool>() ?? true;
                description = parameters["description"]?.ToString() ?? "Windows Sandbox operation";
                
                // Validate required parameters based on action
                if (!ValidateActionParameters(action, sandboxId, command))
                {
                    overallSuccess = false;
                }
            }
            catch (Newtonsoft.Json.JsonException jsonEx)
            {
                _validationErrorMessages.AppendLine($"Error parsing tool parameters JSON: {jsonEx.Message}");
                overallSuccess = false;
            }
            catch (Exception ex)
            {
                _validationErrorMessages.AppendLine($"Unexpected error during parsing: {ex.Message}");
                _logger.LogError(ex, "Unexpected error during WindowsSandbox parsing");
                overallSuccess = false;
            }

            if (!overallSuccess)
            {
                _logger.LogError("WindowsSandbox validation failed:\n{Errors}", _validationErrorMessages.ToString());
                SendStatusUpdate("Validation failed. See error details.");
                return CreateResult(false, false, $"Validation failed: {_validationErrorMessages.ToString()}");
            }

            // Process input files if provided
            if (inputFiles != null && inputFiles.Count > 0)
            {
                await ProcessInputFiles(inputFiles);
            }

            // Execute the requested action
            try
            {
                var result = action.ToLower() switch
                {
                    "start" => await StartSandbox(config, description),
                    "exec" => await ExecuteCommand(sandboxId, command, runAs, workingDirectory),
                    "stop" => await StopSandbox(sandboxId),
                    "list" => await ListSandboxes(),
                    "share" => await ShareFolder(sandboxId, allowWrite),
                    "connect" => await ConnectToSandbox(sandboxId),
                    "ip" => await GetSandboxIP(sandboxId),
                    "workflow" => await ExecuteWorkflow(command, runAs, workingDirectory, config, description),
                    _ => CreateResult(false, false, $"Unknown action: {action}")
                };

                return result;
            }
            catch (Exception ex)
            {
                string errorMessage = $"Error executing {action}: {ex.Message}";
                _logger.LogError(ex, "Error executing WindowsSandbox action: {Action}", action);
                SendStatusUpdate($"WindowsSandbox {action} failed with error.");
                return CreateResult(true, false, CreateErrorOutput(action, errorMessage), errorMessage);
            }
        }

        private bool ValidateActionParameters(string action, string sandboxId, string command)
        {
            var requiresSandboxId = new[] { "exec", "stop", "share", "connect", "ip" };
            var requiresCommand = new[] { "exec", "workflow" };
            
            if (requiresSandboxId.Contains(action) && string.IsNullOrEmpty(sandboxId))
            {
                _validationErrorMessages.AppendLine($"Error: 'sandboxId' is required for action '{action}'.");
                return false;
            }
            
            if (requiresCommand.Contains(action) && string.IsNullOrEmpty(command))
            {
                _validationErrorMessages.AppendLine($"Error: 'command' is required for action '{action}'.");
                return false;
            }
            
            return true;
        }

        private async Task ProcessInputFiles(JArray inputFiles)
        {
            SendStatusUpdate($"Processing {inputFiles.Count} input files...");
            
            foreach (JObject fileObj in inputFiles)
            {
                var filename = fileObj["filename"]?.ToString();
                var content = fileObj["content"]?.ToString();
                
                if (!string.IsNullOrEmpty(filename) && content != null)
                {
                    var filePath = Path.Combine(_inputFolder, filename);
                    await File.WriteAllTextAsync(filePath, content);
                    _logger.LogInformation("Created input file: {FileName}", filename);
                }
            }
        }

        private async Task<BuiltinToolResult> StartSandbox(string config, string description)
        {
            SendStatusUpdate("Starting Windows Sandbox...");
            
            var args = "start";
            if (!string.IsNullOrEmpty(config))
            {
                args += $" --config \"{config}\"";
            }
            
            var result = await ExecuteWsbCommand(args);
            
            if (result.Success)
            {
                // Extract sandbox ID from output
                var sandboxId = ExtractSandboxId(result.Output);
                var output = CreateSuccessOutput("start", $"Sandbox started successfully. ID: {sandboxId}", new { sandboxId });
                LogOperation("start", description, result.Output);
                return CreateResult(true, true, output, "Sandbox started successfully.");
            }
            else
            {
                var output = CreateErrorOutput("start", result.ErrorMessage);
                LogOperation("start", description, result.ErrorMessage);
                return CreateResult(true, false, output, $"Failed to start sandbox: {result.ErrorMessage}");
            }
        }

        private async Task<BuiltinToolResult> ExecuteCommand(string sandboxId, string command, string runAs, string workingDirectory)
        {
            SendStatusUpdate($"Executing command in sandbox {sandboxId}...");
            return null;

            command = $"'{command.Substring(1,command.Length-2)}'";
            command = command.Replace("\\\"", "\"");

            command = "\"start \\\"\\\" \\\"C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe\\\"\"";

            var args = $"exec --id {sandboxId} -c {command} -r {runAs}";
            if (!string.IsNullOrEmpty(workingDirectory))
            {
                args += $" -d \"{workingDirectory}\"";
            }
            
            var result = await ExecuteWsbCommand(args);
            
            if (result.Success)
            {
                var output = CreateSuccessOutput("exec", "Command executed successfully", new { 
                    sandboxId, 
                    command, 
                    runAs, 
                    workingDirectory,
                    exitCode = result.ExitCode 
                });
                LogOperation("exec", $"Command: {command}", result.Output);
                return CreateResult(true, true, output, "Command executed successfully.");
            }
            else
            {
                var output = CreateErrorOutput("exec", result.ErrorMessage);
                LogOperation("exec", $"Command: {command}", result.ErrorMessage);
                return CreateResult(true, false, output, $"Failed to execute command: {result.ErrorMessage}");
            }
        }

        private async Task<BuiltinToolResult> StopSandbox(string sandboxId)
        {
            SendStatusUpdate($"Stopping sandbox {sandboxId}...");
            
            var result = await ExecuteWsbCommand($"stop --id {sandboxId}");
            
            if (result.Success)
            {
                var output = CreateSuccessOutput("stop", "Sandbox stopped successfully", new { sandboxId });
                LogOperation("stop", $"Sandbox ID: {sandboxId}", "Stopped successfully");
                return CreateResult(true, true, output, "Sandbox stopped successfully.");
            }
            else
            {
                var output = CreateErrorOutput("stop", result.ErrorMessage);
                LogOperation("stop", $"Sandbox ID: {sandboxId}", result.ErrorMessage);
                return CreateResult(true, false, output, $"Failed to stop sandbox: {result.ErrorMessage}");
            }
        }

        private async Task<BuiltinToolResult> ListSandboxes()
        {
            SendStatusUpdate("Listing running sandboxes...");
            
            var result = await ExecuteWsbCommand("list");
            
            if (result.Success)
            {
                var output = CreateSuccessOutput("list", "Sandboxes listed successfully", new { 
                    sandboxes = ParseSandboxList(result.Output) 
                });
                LogOperation("list", "List sandboxes", result.Output);
                return CreateResult(true, true, output, "Sandboxes listed successfully.");
            }
            else
            {
                var output = CreateErrorOutput("list", result.ErrorMessage);
                LogOperation("list", "List sandboxes", result.ErrorMessage);
                return CreateResult(true, false, output, $"Failed to list sandboxes: {result.ErrorMessage}");
            }
        }

        private async Task<BuiltinToolResult> ShareFolder(string sandboxId, bool allowWrite)
        {
            SendStatusUpdate($"Setting up folder sharing for sandbox {sandboxId}...");
            
            var args = $"share --id {sandboxId} -f \"{_sandboxExchangeRoot}\" -s \"C:\\exchange\"";
            if (allowWrite)
            {
                args += " --allow-write";
            }
            
            var result = await ExecuteWsbCommand(args);
            
            if (result.Success)
            {
                var output = CreateSuccessOutput("share", "Folder sharing configured successfully", new { 
                    sandboxId, 
                    hostPath = _sandboxExchangeRoot,
                    sandboxPath = "C:\\exchange",
                    allowWrite 
                });
                LogOperation("share", $"Sandbox ID: {sandboxId}", "Folder shared successfully");
                return CreateResult(true, true, output, "Folder sharing configured successfully.");
            }
            else
            {
                var output = CreateErrorOutput("share", result.ErrorMessage);
                LogOperation("share", $"Sandbox ID: {sandboxId}", result.ErrorMessage);
                return CreateResult(true, false, output, $"Failed to configure folder sharing: {result.ErrorMessage}");
            }
        }        private async Task<BuiltinToolResult> ConnectToSandbox(string sandboxId)
        {
            SendStatusUpdate($"Connecting to sandbox {sandboxId}...");
            
            var result = await ExecuteWsbCommandNonBlocking($"connect --id {sandboxId}");
            
            if (result.Success)
            {
                var output = CreateSuccessOutput("connect", "Remote desktop connection initiated", new { sandboxId });
                LogOperation("connect", $"Sandbox ID: {sandboxId}", "Connection initiated");
                return CreateResult(true, true, output, "Remote desktop connection initiated.");
            }
            else
            {
                var output = CreateErrorOutput("connect", result.ErrorMessage);
                LogOperation("connect", $"Sandbox ID: {sandboxId}", result.ErrorMessage);
                return CreateResult(true, false, output, $"Failed to connect to sandbox: {result.ErrorMessage}");
            }
        }

        private async Task<BuiltinToolResult> GetSandboxIP(string sandboxId)
        {
            SendStatusUpdate($"Getting IP address for sandbox {sandboxId}...");
            
            var result = await ExecuteWsbCommand($"ip --id {sandboxId}");
            
            if (result.Success)
            {
                var ipAddress = result.Output.Trim();
                var output = CreateSuccessOutput("ip", "IP address retrieved successfully", new { 
                    sandboxId, 
                    ipAddress 
                });
                LogOperation("ip", $"Sandbox ID: {sandboxId}", $"IP: {ipAddress}");
                return CreateResult(true, true, output, "IP address retrieved successfully.");
            }
            else
            {
                var output = CreateErrorOutput("ip", result.ErrorMessage);
                LogOperation("ip", $"Sandbox ID: {sandboxId}", result.ErrorMessage);
                return CreateResult(true, false, output, $"Failed to get IP address: {result.ErrorMessage}");
            }
        }

        private async Task<BuiltinToolResult> ExecuteWorkflow(string command, string runAs, string workingDirectory, string config, string description)
        {
            SendStatusUpdate("Executing automated sandbox workflow...");
            
            try
            {
                // Step 1: Start sandbox
                var startResult = await StartSandbox(config, description);
                if (!startResult.ContinueProcessing)
                {
                    return startResult;
                }
                
                // Extract sandbox ID from start result
                var startOutput = JObject.Parse(startResult.ResultMessage);
                var sandboxId = startOutput["data"]?["sandboxId"]?.ToString();
                
                if (string.IsNullOrEmpty(sandboxId))
                {
                    return CreateResult(true, false, CreateErrorOutput("workflow", "Failed to extract sandbox ID from start operation"));
                }
                
                // Step 2: Share folder
                var shareResult = await ShareFolder(sandboxId, true);
                if (!shareResult.ContinueProcessing)
                {
                    // Try to clean up
                    await StopSandbox(sandboxId);
                    return shareResult;
                }
                
                // Step 3: Execute command
                var execResult = await ExecuteCommand(sandboxId, command, runAs, workingDirectory);
                
                // Step 4: Create workflow summary
                var workflowOutput = CreateSuccessOutput("workflow", "Automated workflow completed", new {
                    sandboxId,
                    steps = new[] { "start", "share", "exec" },
                    command,
                    status = execResult.ContinueProcessing ? "success" : "partial_failure"
                });
                
                LogOperation("workflow", description, $"Workflow completed for sandbox {sandboxId}");
                
                return CreateResult(true, true, workflowOutput, "Automated workflow completed.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in sandbox workflow execution");
                var output = CreateErrorOutput("workflow", $"Workflow failed: {ex.Message}");
                return CreateResult(true, false, output, $"Workflow failed: {ex.Message}");
            }
        }        private async Task<WsbCommandResult> ExecuteWsbCommand(string arguments)
        {
            try
            {
                // works: wsb exec --id cd7b32ef-6de3-4494-87a0-4e1f679d80eb -c "C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe" -r ExistingLogin

                var processInfo = new ProcessStartInfo
                {
                    FileName = "wsb",
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = new Process { StartInfo = processInfo };
                var outputBuilder = new StringBuilder();
                var errorBuilder = new StringBuilder();

                process.OutputDataReceived += (sender, e) => {
                    if (!string.IsNullOrEmpty(e.Data))
                        outputBuilder.AppendLine(e.Data);
                };

                process.ErrorDataReceived += (sender, e) => {
                    if (!string.IsNullOrEmpty(e.Data))
                        errorBuilder.AppendLine(e.Data);
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                await process.WaitForExitAsync();

                var output = outputBuilder.ToString();
                var error = errorBuilder.ToString();
                var exitCode = process.ExitCode;

                return new WsbCommandResult
                {
                    Success = exitCode == 0,
                    Output = output,
                    ErrorMessage = string.IsNullOrEmpty(error) ? null : error,
                    ExitCode = exitCode
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute wsb command: {Arguments}", arguments);
                return new WsbCommandResult
                {
                    Success = false,
                    ErrorMessage = $"Failed to execute wsb command: {ex.Message}",
                    ExitCode = -1
                };
            }
        }

        private async Task<WsbCommandResult> ExecuteWsbCommandNonBlocking(string arguments)
        {
            try
            {
                var processInfo = new ProcessStartInfo
                {
                    FileName = "wsb",
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = new Process { StartInfo = processInfo };
                var outputBuilder = new StringBuilder();
                var errorBuilder = new StringBuilder();

                process.OutputDataReceived += (sender, e) => {
                    if (!string.IsNullOrEmpty(e.Data))
                        outputBuilder.AppendLine(e.Data);
                };

                process.ErrorDataReceived += (sender, e) => {
                    if (!string.IsNullOrEmpty(e.Data))
                        errorBuilder.AppendLine(e.Data);
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                // Give the process a moment to start and capture any immediate errors
                await Task.Delay(1000);

                // Check if the process has already exited (indicating an error)
                if (process.HasExited)
                {
                    var output = outputBuilder.ToString();
                    var error = errorBuilder.ToString();
                    var exitCode = process.ExitCode;

                    return new WsbCommandResult
                    {
                        Success = exitCode == 0,
                        Output = output,
                        ErrorMessage = string.IsNullOrEmpty(error) ? null : error,
                        ExitCode = exitCode
                    };
                }
                else
                {
                    // Process is still running, return success immediately
                    return new WsbCommandResult
                    {
                        Success = true,
                        Output = "Process started successfully and is running",
                        ErrorMessage = null,
                        ExitCode = 0
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute wsb command non-blocking: {Arguments}", arguments);
                return new WsbCommandResult
                {
                    Success = false,
                    ErrorMessage = $"Failed to execute wsb command: {ex.Message}",
                    ExitCode = -1
                };
            }
        }

        private string ExtractSandboxId(string output)
        {
            // Parse the output to extract sandbox ID
            // This would need to be adjusted based on actual wsb output format
            var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                if (line.Contains("ID:") || line.Contains("sandbox"))
                {
                    // Extract GUID-like pattern
                    var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    foreach (var part in parts)
                    {
                        if (Guid.TryParse(part, out _))
                        {
                            return part;
                        }
                    }
                }
            }
            return "unknown";
        }

        private object ParseSandboxList(string output)
        {
            // Parse the sandbox list output into structured data
            var sandboxes = new List<object>();
            output = output.Replace("\r\n", "\n");
            var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var line in lines)
            {
                if (Guid.TryParse(line, out var parsedGuid))
                {
                    sandboxes.Add(new
                    {
                        id = line
                    });
                }
            }
            
            return sandboxes;
        }

        private string CreateSuccessOutput(string action, string message, object data = null)
        {
            var output = new JObject
            {
                ["action"] = action,
                ["success"] = true,
                ["message"] = message,
                ["timestamp"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                ["exchangeFolder"] = _sandboxExchangeRoot
            };
            
            if (data != null)
            {
                output["data"] = JToken.FromObject(data);
            }
            
            return output.ToString(Formatting.Indented);
        }

        private string CreateErrorOutput(string action, string errorMessage)
        {
            var output = new JObject
            {
                ["action"] = action,
                ["success"] = false,
                ["error"] = errorMessage,
                ["timestamp"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                ["exchangeFolder"] = _sandboxExchangeRoot
            };
            
            return output.ToString(Formatting.Indented);
        }

        private void LogOperation(string action, string description, string result)
        {
            try
            {
                var logEntry = new JObject
                {
                    ["timestamp"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    ["action"] = action,
                    ["description"] = description,
                    ["result"] = result
                };
                
                var logFile = Path.Combine(_logsFolder, $"sandbox-{DateTime.UtcNow:yyyyMMdd}.log");
                File.AppendAllText(logFile, logEntry.ToString(Formatting.None) + Environment.NewLine);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to write sandbox operation log");
            }
        }

        private class WsbCommandResult
        {
            public bool Success { get; set; }
            public string Output { get; set; }
            public string ErrorMessage { get; set; }
            public int ExitCode { get; set; }
        }
    }
}