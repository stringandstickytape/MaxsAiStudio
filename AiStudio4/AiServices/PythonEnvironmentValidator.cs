using System.Diagnostics;

namespace AiStudio4.AiServices
{
    public static class PythonEnvironmentValidator
    {
        public class ValidationResult
        {
            public bool IsValid { get; set; }
            public string ErrorMessage { get; set; }
            public string PythonVersion { get; set; }
            public string OpenAIVersion { get; set; }
            public List<string> MissingRequirements { get; set; } = new List<string>();
            public string SetupInstructions { get; set; }
        }

        public static ValidationResult ValidateEnvironment()
        {
            var result = new ValidationResult();
            var missingItems = new List<string>();

            try
            {
                // Check Python installation
                var pythonResult = CheckPython();
                if (!pythonResult.success)
                {
                    missingItems.Add("Python 3.8+");
                    result.ErrorMessage = pythonResult.error;
                }
                else
                {
                    result.PythonVersion = pythonResult.version;
                }

                // Check OpenAI package
                var openaiResult = CheckOpenAIPackage();
                if (!openaiResult.success)
                {
                    missingItems.Add("openai package");
                    result.ErrorMessage += (string.IsNullOrEmpty(result.ErrorMessage) ? "" : "\n") + openaiResult.error;
                }
                else
                {
                    result.OpenAIVersion = openaiResult.version;
                }

                result.MissingRequirements = missingItems;
                result.IsValid = !missingItems.Any();

                if (!result.IsValid)
                {
                    result.SetupInstructions = GenerateSetupInstructions(missingItems);
                }

                return result;
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.ErrorMessage = $"Environment validation failed: {ex.Message}";
                result.SetupInstructions = GenerateSetupInstructions(new[] { "Python 3.8+", "openai package" });
                return result;
            }
        }

        private static (bool success, string version, string error) CheckPython()
        {
            try
            {
                var processInfo = new ProcessStartInfo
                {
                    FileName = "python",
                    Arguments = "--version",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(processInfo);
                if (process == null)
                {
                    return (false, "", "Failed to start Python process");
                }

                process.WaitForExit(5000);
                
                if (process.ExitCode != 0)
                {
                    var error = process.StandardError.ReadToEnd();
                    return (false, "", $"Python check failed: {error}");
                }

                var output = process.StandardOutput.ReadToEnd().Trim();
                return (true, output, "");
            }
            catch (Exception ex)
            {
                return (false, "", $"Python not found: {ex.Message}");
            }
        }

        private static (bool success, string version, string error) CheckOpenAIPackage()
        {
            try
            {
                var processInfo = new ProcessStartInfo
                {
                    FileName = "python",
                    Arguments = "-c \"import openai; print(f'OpenAI {openai.__version__}')\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(processInfo);
                if (process == null)
                {
                    return (false, "", "Failed to start Python process");
                }

                process.WaitForExit(5000);
                
                if (process.ExitCode != 0)
                {
                    var error = process.StandardError.ReadToEnd();
                    return (false, "", "OpenAI package not installed");
                }

                var output = process.StandardOutput.ReadToEnd().Trim();
                return (true, output, "");
            }
            catch (Exception ex)
            {
                return (false, "", $"OpenAI package check failed: {ex.Message}");
            }
        }

        private static string GenerateSetupInstructions(IEnumerable<string> missingItems)
        {
            var instructions = new StringBuilder();
            instructions.AppendLine("To use the PythonOpenAI provider, please install the following:");
            instructions.AppendLine();

            if (missingItems.Contains("Python 3.8+"))
            {
                instructions.AppendLine("1. Install Python 3.8 or newer:");
                instructions.AppendLine("   • Windows: Download from https://python.org/downloads");
                instructions.AppendLine("   • macOS: Download from https://python.org/downloads or use Homebrew: brew install python");
                instructions.AppendLine("   • Linux: Use your package manager: sudo apt install python3 python3-pip");
                instructions.AppendLine();
            }

            if (missingItems.Contains("openai package"))
            {
                instructions.AppendLine("2. Install the OpenAI Python package:");
                instructions.AppendLine("   pip install openai");
                instructions.AppendLine();
            }

            instructions.AppendLine("After installation, restart AiStudio4.");
            instructions.AppendLine();
            instructions.AppendLine("Need help? Check the documentation or create an issue on GitHub.");

            return instructions.ToString();
        }

        public static async Task<bool> TestBridgeConnection()
        {
            try
            {
                var scriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Core", "Tools", "Python", "openai_python_bridge.py");
                if (!File.Exists(scriptPath))
                {
                    return false;
                }

                var processInfo = new ProcessStartInfo
                {
                    FileName = "python",
                    Arguments = $"-u \"{scriptPath}\"",
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(processInfo);
                if (process == null) return false;

                // Wait for ready signal
                var readyTask = Task.Run(async () =>
                {
                    var line = await process.StandardOutput.ReadLineAsync();
                    if (string.IsNullOrEmpty(line)) return false;
                    
                    var response = JsonConvert.DeserializeObject<JObject>(line);
                    return response["type"]?.ToString() == "ready";
                });

                var timeoutTask = Task.Delay(5000);
                var completedTask = await Task.WhenAny(readyTask, timeoutTask);

                if (completedTask == timeoutTask)
                {
                    process.Kill();
                    return false;
                }

                var isReady = await readyTask;
                process.Kill();
                return isReady;
            }
            catch
            {
                return false;
            }
        }
    }
}