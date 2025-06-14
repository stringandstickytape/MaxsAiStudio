





namespace AiStudio4.Core.Tools.Vite
{
    
    
    
    public class CommandResult
    {
        
        
        
        public string Output { get; set; }
        
        
        
        
        public string Error { get; set; }
        
        
        
        
        public int ExitCode { get; set; }
        
        
        
        
        public bool Success => ExitCode == 0;
    }
    
    
    
    
    public static class ViteCommandHelper
    {
        
        
        
        
        
        
        
        
        public static async Task<string> GetCommandOutputAsync(string command, string arguments, bool useCmd, ILogger logger)
        {
            var result = await ExecuteCommandAsync(command, arguments, useCmd, null, logger, false);
            return result.Success ? result.Output : string.Empty;
        }
        
        
        
        
        
        
        
        
        
        
        
        public static async Task<CommandResult> ExecuteCommandAsync(string command, string arguments, bool useCmd, string workingDirectory = null, ILogger logger = null, bool showWindow = true)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = useCmd ? "cmd.exe" : command,
                    Arguments = useCmd
                        ? (showWindow
                            
                            ? $"/C {command} {arguments}"
                            
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
                
                
                
                string processPath = Environment.GetEnvironmentVariable("PATH");
                string machinePath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Machine);
                string userPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User);
                
                string combinedPath = string.Join(";", 
                    new[] { processPath, machinePath, userPath }
                    .Where(p => !string.IsNullOrEmpty(p))
                    .Distinct());
                
                
                
                
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
            
            
            string processPath = Environment.GetEnvironmentVariable("PATH");
            string machinePath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Machine);
            string userPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User);
            
            string combinedPath = string.Join(";", 
                new[] { processPath, machinePath, userPath }
                .Where(p => !string.IsNullOrEmpty(p))
                .Distinct());
            
            
            
            
            logger?.LogDebug($"Using PATH: {combinedPath}");
            logger?.LogDebug($"Configured process: {startInfo.FileName} {startInfo.Arguments}");
            
            return new Process { StartInfo = startInfo };
        }
    }
}
