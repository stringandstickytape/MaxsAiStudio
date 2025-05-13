using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace AiStudio4.Core.Tools.Vite
{
    /// <summary>
    /// Result of a command execution
    /// </summary>
    public class CommandResult
    {
        /// <summary>
        /// Standard output from the command
        /// </summary>
        public string Output { get; set; }
        
        /// <summary>
        /// Standard error from the command
        /// </summary>
        public string Error { get; set; }
        
        /// <summary>
        /// Exit code of the process
        /// </summary>
        public int ExitCode { get; set; }
        
        /// <summary>
        /// Whether the command executed successfully (exit code 0)
        /// </summary>
        public bool Success => ExitCode == 0;
    }
    
    /// <summary>
    /// Helper class for executing commands in Vite tools
    /// </summary>
    public static class ViteCommandHelper
    {
        /// <summary>
        /// Helper method to get command output with option to use cmd.exe for execution
        /// </summary>
        /// <param name="command">The command to execute</param>
        /// <param name="arguments">Arguments for the command</param>
        /// <param name="useCmd">Whether to use cmd.exe to execute the command (needed for npm and other batch files)</param>
        /// <param name="logger">Logger instance</param>
        /// <returns>The command output or empty string on error</returns>
        public static async Task<string> GetCommandOutputAsync(string command, string arguments, bool useCmd, ILogger logger)
        {
            var result = await ExecuteCommandAsync(command, arguments, useCmd, null, logger, false);
            return result.Success ? result.Output : string.Empty;
        }
        
        /// <summary>
        /// Executes a command and returns detailed results
        /// </summary>
        /// <param name="command">The command to execute</param>
        /// <param name="arguments">Arguments for the command</param>
        /// <param name="useCmd">Whether to use cmd.exe to execute the command (needed for npm and other batch files)</param>
        /// <param name="workingDirectory">Optional working directory for the command</param>
        /// <param name="logger">Logger instance</param>
        /// <param name="showWindow">Whether to show the command window to allow user interaction (default: true)</param>
        /// <returns>CommandResult with output, error, and exit code</returns>
        public static async Task<CommandResult> ExecuteCommandAsync(string command, string arguments, bool useCmd, string workingDirectory = null, ILogger logger = null, bool showWindow = true)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = useCmd ? "cmd.exe" : command,
                    Arguments = useCmd
                        ? (showWindow
                            // When showing window, do NOT use 'start' or '/K', just run the command directly so we wait for it to finish
                            ? $"/C {command} {arguments}"
                            // When not showing window, keep current behavior
                            : $"/C {command} {arguments}")
                        : arguments,
                    RedirectStandardOutput = !showWindow,
                    RedirectStandardError = !showWindow,
                    UseShellExecute = showWindow,
                    CreateNoWindow = !showWindow
                };
                
                if (!string.IsNullOrEmpty(workingDirectory))
                {
                    startInfo.WorkingDirectory = workingDirectory;
                }
                
                // Ensure the process inherits environment variables, particularly PATH
                // Combine Process, Machine, and User PATH variables to ensure all locations are searched
                string processPath = Environment.GetEnvironmentVariable("PATH");
                string machinePath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Machine);
                string userPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User);
                
                string combinedPath = string.Join(";", 
                    new[] { processPath, machinePath, userPath }
                    .Where(p => !string.IsNullOrEmpty(p))
                    .Distinct());
                
                //startInfo.EnvironmentVariables["PATH"] = combinedPath;
                
                // Log the PATH for debugging purposes
                logger?.LogDebug($"Using PATH: {combinedPath}");
                logger?.LogDebug($"Executing: {startInfo.FileName} {startInfo.Arguments}");
                
                var process = new Process { StartInfo = startInfo };

                process.Start();
                string output = "";
                string error = "";
                
                if (!showWindow)
                {
                    output = await process.StandardOutput.ReadToEndAsync();
                    error = await process.StandardError.ReadToEndAsync();
                }
                
                await process.WaitForExitAsync();

                if (process.ExitCode != 0)
                {
                    logger?.LogWarning($"Command exited with code {process.ExitCode}: {error}");
                }

                return new CommandResult
                {
                    Output = output,
                    Error = error,
                    ExitCode = process.ExitCode
                };
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, $"Error executing command {command} {arguments}");
                return new CommandResult
                {
                    Output = string.Empty,
                    Error = ex.Message,
                    ExitCode = -1
                };
            }
        }
        
        /// <summary>
        /// Creates a Process object configured for executing a command
        /// </summary>
        /// <param name="command">The command to execute</param>
        /// <param name="arguments">Arguments for the command</param>
        /// <param name="useCmd">Whether to use cmd.exe to execute the command</param>
        /// <param name="workingDirectory">Optional working directory</param>
        /// <param name="logger">Logger instance</param>
        /// <param name="showWindow">Whether to show the command window to allow user interaction (default: true)</param>
        /// <returns>Configured Process object ready to start</returns>
        public static Process CreateProcess(string command, string arguments, bool useCmd, string workingDirectory = null, ILogger logger = null, bool showWindow = true)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = useCmd ? "cmd.exe" : command,
                Arguments = useCmd ? $"/C start \"Vite Dev Server\" /MIN cmd /K {command} {arguments}" : arguments,
                RedirectStandardOutput = !showWindow,
                RedirectStandardError = !showWindow,
                UseShellExecute = showWindow,
                CreateNoWindow = !showWindow,
                
            };
            
            if (!string.IsNullOrEmpty(workingDirectory))
            {
                startInfo.WorkingDirectory = workingDirectory;
            }
            
            // Ensure the process inherits environment variables, particularly PATH
            string processPath = Environment.GetEnvironmentVariable("PATH");
            string machinePath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Machine);
            string userPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User);
            
            string combinedPath = string.Join(";", 
                new[] { processPath, machinePath, userPath }
                .Where(p => !string.IsNullOrEmpty(p))
                .Distinct());
            
            //startInfo.EnvironmentVariables["PATH"] = combinedPath;
            
            // Log the PATH for debugging purposes
            logger?.LogDebug($"Using PATH: {combinedPath}");
            logger?.LogDebug($"Configured process: {startInfo.FileName} {startInfo.Arguments}");
            
            return new Process { StartInfo = startInfo };
        }
    }
}