
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
namespace VSIXTest
{




    public class GitDiffHelper
    {
        public string GetGitDiff()
        {
            string solutionDirectory = FindSolutionDirectory();
            if (string.IsNullOrEmpty(solutionDirectory))
            {
                return "Could not find solution directory.";
            }

            string gitCommand = "diff HEAD";
            string gitOutput = ExecuteGitCommand(solutionDirectory, gitCommand);

            string gitLsFilesCommand = "ls-files --others --exclude-standard";
            string newFiles = ExecuteGitCommand(solutionDirectory, gitLsFilesCommand);

            if (!string.IsNullOrEmpty(newFiles))
            {
                string[] newFilesList = newFiles.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string newFile in newFilesList)
                {
                    string newFileContent = File.ReadAllText(Path.Combine(solutionDirectory, newFile));
                    gitOutput += $"\n\nNew file: {newFile}\n{newFileContent}";
                }
            }

            return gitOutput;
        }

        private string FindSolutionDirectory()
        {
            string currentDirectory = Directory.GetCurrentDirectory();
            while (!string.IsNullOrEmpty(currentDirectory))
            {
                if (Directory.GetFiles(currentDirectory, "*.sln").Any())
                {
                    return currentDirectory;
                }
                currentDirectory = Directory.GetParent(currentDirectory)?.FullName;
            }
            return null;
        }

        private string ExecuteGitCommand(string workingDirectory, string command)
        {
            using (var process = new System.Diagnostics.Process())
            {
                process.StartInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = command,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WorkingDirectory = workingDirectory
                };

                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (!string.IsNullOrEmpty(error))
                {
                    return $"Error: {error}";
                }

                return output;
            }
        }
    }
}




