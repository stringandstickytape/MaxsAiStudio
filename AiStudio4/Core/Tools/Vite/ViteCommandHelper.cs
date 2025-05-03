using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace AiStudio4.Core.Tools.Vite
{
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
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = useCmd ? "cmd.exe" : command,
                    Arguments = useCmd ? $"/c {command} {arguments}" : arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                
                // Ensure the process inherits environment variables, particularly PATH
                // Combine Process, Machine, and User PATH variables to ensure all locations are searched
                string processPath = Environment.GetEnvironmentVariable("PATH");
                string machinePath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Machine);
                string userPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User);
                
                string combinedPath = string.Join(";", 
                    new[] { processPath, machinePath, userPath }
                    .Where(p => !string.IsNullOrEmpty(p))
                    .Distinct());
                
                startInfo.EnvironmentVariables["PATH"] = combinedPath;
                
                // Log the PATH for debugging purposes
                logger?.LogDebug($"Using PATH: {combinedPath}");
                logger?.LogDebug($"Executing: {startInfo.FileName} {startInfo.Arguments}");
                
                var process = new Process { StartInfo = startInfo };

                process.Start();
                string output = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (process.ExitCode != 0)
                {
                    logger?.LogWarning($"Command exited with code {process.ExitCode}: {error}");
                    return string.Empty;
                }

                return output;
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, $"Error executing command {command} {arguments}");
                return string.Empty;
            }
        }
    }
}