using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;


namespace AiTool3.Helpers
{
    public static class PythonHelper
    {
        public static async Task LaunchPythonScriptAsync(string scriptContent)
        {
            string pythonPath = GetPythonPath();
            if (string.IsNullOrEmpty(pythonPath))
            {
                MessageBox.Show("Python executable not found.");
                return;
            }

            string tempScriptPath = Path.GetTempFileName() + ".py";
            await File.WriteAllTextAsync(tempScriptPath, $"{scriptContent}{Environment.NewLine}{Environment.NewLine}input(\"Script completed, press Enter to exit...\")");

            try
            {
                using (Process process = new Process())
                {
                    process.StartInfo.FileName = pythonPath;
                    process.StartInfo.Arguments = tempScriptPath;
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.RedirectStandardError = true;
                    process.StartInfo.CreateNoWindow = false;

                    process.Start();
                    await process.WaitForExitAsync();
                    string error = await process.StandardError.ReadToEndAsync();

                    if (!string.IsNullOrWhiteSpace(error))
                    {
                        MessageBox.Show($"Error executing Python script: {error}");
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error executing Python script: {ex.Message}");
            }
            finally
            {
                await Task.Run(() => File.Delete(tempScriptPath));
            }
        }

    private static string GetPythonPath()
        {
            // Try to get Python path from registry
            string[] registryKeys = { @"SOFTWARE\Python\PythonCore\3.9\InstallPath",
                              @"SOFTWARE\Python\PythonCore\3.8\InstallPath",
                              @"SOFTWARE\Python\PythonCore\3.7\InstallPath" };

            foreach (string key in registryKeys)
            {
                string path = Registry.GetValue($@"HKEY_LOCAL_MACHINE\{key}", "ExecutablePath", null) as string;
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                {
                    return path;
                }

                path = Registry.GetValue($@"HKEY_CURRENT_USER\{key}", "ExecutablePath", null) as string;
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                {
                    return path;
                }
            }

            // If not found in registry, try environment variables
            string pythonPath = Environment.GetEnvironmentVariable("PYTHON_HOME");
            if (!string.IsNullOrEmpty(pythonPath))
            {
                string fullPath = Path.Combine(pythonPath, "python.exe");
                if (File.Exists(fullPath))
                {
                    return fullPath;
                }
            }

            // If still not found, try PATH
            string pathVariable = Environment.GetEnvironmentVariable("PATH");
            if (!string.IsNullOrEmpty(pathVariable))
            {
                foreach (string path in pathVariable.Split(Path.PathSeparator))
                {
                    string fullPath = Path.Combine(path, "python.exe");
                    if (File.Exists(fullPath))
                    {
                        return fullPath;
                    }
                }
            }

            return null;
        }
    }
}
