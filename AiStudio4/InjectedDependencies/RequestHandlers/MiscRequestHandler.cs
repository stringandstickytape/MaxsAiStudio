// AiStudio4/InjectedDependencies/RequestHandlers/MiscRequestHandler.cs
using AiStudio4.Core.Interfaces;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace AiStudio4.InjectedDependencies.RequestHandlers
{
    /// <summary>
    /// Handles miscellaneous requests that don't fit into other categories
    /// </summary>
    public class MiscRequestHandler : BaseRequestHandler
    {
        private readonly IGeneralSettingsService _generalSettingsService;

        public MiscRequestHandler(IGeneralSettingsService generalSettingsService)
        {
            _generalSettingsService = generalSettingsService ?? throw new ArgumentNullException(nameof(generalSettingsService));
        }

        protected override IEnumerable<string> SupportedRequestTypes => new[]
        {
            "saveCodeBlockAsFile",
            "gitDiff",
            "exitApplication"
        };

        public override async Task<string> HandleAsync(string clientId, string requestType, JObject requestObject)
        {
            try
            {
                return requestType switch
                {
                    "saveCodeBlockAsFile" => await HandleSaveCodeBlockAsFileRequest(requestObject),
                    "gitDiff" => await HandleGitDiffRequest(),
                    "exitApplication" => HandleExitApplicationRequest(),
                    _ => SerializeError($"Unsupported request type: {requestType}")
                };
            }
            catch (Exception ex)
            {
                return SerializeError($"Error handling {requestType} request: {ex.Message}");
            }
        }

        private string HandleExitApplicationRequest()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Application.Current.Shutdown();
            });
            return "";
        }

        private async Task<string> HandleSaveCodeBlockAsFileRequest(JObject requestObject)
        {
            try
            {
                string content = requestObject["content"]?.ToString();
                string suggestedFilename = requestObject["suggestedFilename"]?.ToString() ?? "codeblock.txt";
                if (string.IsNullOrEmpty(content))
                    return SerializeError("Content cannot be empty");

                // Use SaveFileDialog to prompt user for save location
                var dialog = new SaveFileDialog
                {
                    FileName = suggestedFilename,
                    Filter = "All files (*.*)|*.*",
                    Title = "Save Code Block As File"
                };
                bool? result = dialog.ShowDialog();
                if (result == true)
                {
                    string filePath = dialog.FileName;
                    await System.IO.File.WriteAllTextAsync(filePath, content);
                    return JsonConvert.SerializeObject(new { success = true, filePath });
                }
                else
                {
                    return JsonConvert.SerializeObject(new { success = false, error = "Save cancelled by user" });
                }
            }
            catch (Exception ex)
            {
                return SerializeError($"Error saving code block as file: {ex.Message}");
            }
        }

        private async Task<string> HandleGitDiffRequest()
        {
            try
            {
                var projectPath = _generalSettingsService.CurrentSettings.ProjectPath;
                if (string.IsNullOrWhiteSpace(projectPath) || !Directory.Exists(projectPath))
                {
                    return SerializeError("Project path not set or does not exist.");
                }
                if (!Directory.Exists(Path.Combine(projectPath, ".git")))
                {
                    return SerializeError("Not a git repository.");
                }
                
                var diffOutput = RunGitCommand("diff HEAD", projectPath);
                var newFiles = RunGitCommand("ls-files --others --exclude-standard", projectPath);
                var sb = new StringBuilder();
                sb.AppendLine("=== GIT DIFF ===\n");
                sb.AppendLine(diffOutput);
                sb.AppendLine("\n=== NEW FILES ===\n");
                sb.AppendLine(newFiles);
                
                // Convert to base64 for transmission
                var bytes = Encoding.UTF8.GetBytes(sb.ToString());
                var base64Content = Convert.ToBase64String(bytes);
                
                // Return as attachment object
                return JsonConvert.SerializeObject(new { 
                    success = true, 
                    attachment = new {
                        id = Guid.NewGuid().ToString(),
                        name = "git-diff.txt",
                        type = "text/plain",
                        size = bytes.Length,
                        content = base64Content,
                        lastModified = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                    }
                });
            }
            catch (Exception ex)
            {
                return SerializeError($"Error generating git diff: {ex.Message}");
            }
        }
        
        private string RunGitCommand(string args, string workingDir)
        {
            var psi = new ProcessStartInfo("git", args)
            {
                WorkingDirectory = workingDir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var proc = Process.Start(psi);
            var output = proc.StandardOutput.ReadToEnd();
            var error = proc.StandardError.ReadToEnd();
            proc.WaitForExit();
            if (proc.ExitCode != 0)
                throw new Exception($"Git error: {error}");
            return output;
        }
    }
}