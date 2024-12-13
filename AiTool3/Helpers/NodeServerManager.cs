using System.Diagnostics;
using System.Text.RegularExpressions;

namespace AiTool3.Helpers
{
    public class NodeServerManager : IDisposable
    {
        private Process _serverProcess;
        private readonly string _workingDirectory;

        public NodeServerManager()
        {
            _workingDirectory = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), $"node-server-{Guid.NewGuid()}");
            Directory.CreateDirectory(_workingDirectory);
        }

        public async Task StartServerFromScript(string scriptContent)
        {
            try
            {
                // Save script to working directory
                string scriptPath = Path.Combine(_workingDirectory, "server.js");
                await File.WriteAllTextAsync(scriptPath, scriptContent);

                // Extract and install dependencies
                //await InstallDependencies(scriptContent);

                // Start the server
                StartServer(scriptPath);

                // Wait briefly to ensure server starts
                await Task.Delay(2000);

                // Optional: Verify server is running
                if (_serverProcess?.HasExited ?? true)
                {
                    throw new Exception("Server failed to start");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to start server: {ex.Message}", ex);
            }
        }

        private async Task InstallDependencies(string scriptContent)
        {
            var dependencies = ExtractDependencies(scriptContent);

            // Create package.json
            var packageJson = new
            {
                name = "extracted-server",
                type = "module",
                dependencies = dependencies.ToDictionary(d => d, d => "latest")
            };

            await File.WriteAllTextAsync(
                Path.Combine(_workingDirectory, "package.json"),
                System.Text.Json.JsonSerializer.Serialize(packageJson, new System.Text.Json.JsonSerializerOptions { WriteIndented = true })
            );

            // Run npm install
            var npmProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "npm",
                    Arguments = "install",
                    WorkingDirectory = _workingDirectory,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };

            npmProcess.Start();
            await npmProcess.WaitForExitAsync();

            if (npmProcess.ExitCode != 0)
            {
                throw new Exception("Failed to install dependencies");
            }
        }

        private IEnumerable<string> ExtractDependencies(string scriptContent)
        {
            var dependencies = new HashSet<string>();

            var importRegex = new Regex(@"import.*from ['""](@[^/""']+/[^""']+|[^./""']+)[""']");
            var matches = importRegex.Matches(scriptContent);

            foreach (Match match in matches)
            {
                dependencies.Add(match.Groups[1].Value);
            }

            return dependencies;
        }

        private void StartServer(string scriptPath)
        {
            var nodePath = FindNodeExePath();

            _serverProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = nodePath,
                    Arguments = scriptPath,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    WorkingDirectory = _workingDirectory,
                    Environment = { ["PATH"] = Environment.GetEnvironmentVariable("PATH") }
                }
            };

            _serverProcess.OutputDataReceived += (s, e) =>
                Debug.WriteLine($"Server output: {e.Data}");
            _serverProcess.ErrorDataReceived += (s, e) =>
                Debug.WriteLine($"Server error: {e.Data}");

            _serverProcess.Start();
            _serverProcess.BeginOutputReadLine();
            _serverProcess.BeginErrorReadLine();
        }

        public void Dispose()
        {
            if (_serverProcess != null && !_serverProcess.HasExited)
            {
                _serverProcess.Kill();
                _serverProcess.Dispose();
            }

            // Cleanup working directory
            try
            {
                Directory.Delete(_workingDirectory, true);
            }
            catch { /* Ignore cleanup errors */ }
        }

        public static string FindNodeExePath()
        {
            try
            {
                // Get the PATH environment variable
                string pathVariable = Environment.GetEnvironmentVariable("PATH");

                // Split PATH into individual directories
                string[] pathDirectories = pathVariable.Split(Path.PathSeparator);

                // Search each directory for node.exe
                foreach (string directory in pathDirectories)
                {
                    if (string.IsNullOrWhiteSpace(directory))
                        continue;

                    try
                    {
                        string nodePath = Path.Combine(directory, "python.exe");
                        if (File.Exists(nodePath))
                        {
                            return nodePath;
                        }

                        // Check subdirectories recursively
                        string[] subdirectories = Directory.GetDirectories(directory, "*", SearchOption.AllDirectories);
                        foreach (string subdir in subdirectories)
                        {
                            try
                            {
                                nodePath = Path.Combine(subdir, "node.exe");
                                if (File.Exists(nodePath))
                                {
                                    return nodePath;
                                }
                            }
                            catch (UnauthorizedAccessException)
                            {
                                // Skip directories we don't have access to
                                continue;
                            }
                            catch (Exception)
                            {
                                // Skip any other directory-specific errors
                                continue;
                            }
                        }
                    }
                    catch (UnauthorizedAccessException)
                    {
                        // Skip directories we don't have access to
                        continue;
                    }
                    catch (Exception)
                    {
                        // Skip any other directory-specific errors
                        continue;
                    }
                }

                // If node.exe is not found, return null
                return null;
            }
            catch (Exception ex)
            {
                // Handle any unexpected errors
                Console.WriteLine($"Error searching for node.exe: {ex.Message}");
                return null;
            }
        }
    }
}
