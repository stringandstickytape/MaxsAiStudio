using Microsoft.Win32;
using System.Diagnostics;


namespace AiTool3.Helpers
{
    public static class PythonHelper
    {
        public static void LaunchPythonScript(string scriptContent)
        {
            string pythonPath = GetPythonPath();
            if (string.IsNullOrEmpty(pythonPath))
            {
                MessageBox.Show("Python executable not found.");
                return;
            }

            string tempScriptPath = Path.GetTempFileName() + ".py";
            File.WriteAllText(tempScriptPath, $"{scriptContent}{Environment.NewLine}{Environment.NewLine}input(\"Script completed, press Enter to exit...\")");

            try
            {
                Process process = new Process();
                process.StartInfo.FileName = pythonPath;
                process.StartInfo.Arguments = tempScriptPath;
                process.StartInfo.UseShellExecute = true;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.CreateNoWindow = false;

                process.Start();
                process.WaitForExit();
                string output = process.StandardOutput.ReadToEnd();

                // display in an mb
                MessageBox.Show(output, "Python Script Output", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error executing Python script: {ex.Message}");
            }
            finally
            {
                File.Delete(tempScriptPath);
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
