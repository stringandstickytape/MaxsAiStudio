using AiTool3.Snippets;
using Microsoft.CodeAnalysis.Scripting;
using System.Diagnostics;

namespace AiTool3.UI
{
    public static class LaunchHelpers
    {
        public static async Task LaunchPowerShell(string script)
        {
            string tempScriptPath = Path.GetTempFileName() + ".ps1";
            File.WriteAllText(tempScriptPath, script);

            DialogResult result = MessageBox.Show("This can be SUPER-DANGEROUS. Only click Yes if you're absolutely sure this script is safe to run!", "Are you sure?", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.No) return;

            try
            {
                Process process = new Process();
                process.StartInfo.FileName = "powershell.exe";
                process.StartInfo.Arguments = $"-ExecutionPolicy Bypass -File \"{tempScriptPath}\"";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.CreateNoWindow = true;

                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                await process.WaitForExitAsync();

                DisplayOutputForm(output);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error executing PowerShell script: {ex.Message}");
                MessageBox.Show($"Error executing PowerShell script: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                File.Delete(tempScriptPath);
            }
        }

        private static void DisplayOutputForm(string output)
        {
            Form outputForm = new Form
            {
                Text = "PowerShell Script Output",
                Size = new Size(600, 600),
                StartPosition = FormStartPosition.CenterScreen,
                MinimizeBox = false,
                MaximizeBox = false
            };

            TextBox outputTextBox = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Both,
                Font = new Font("Consolas", 10),
                Dock = DockStyle.Fill,
                Text = output, 
                WordWrap = false
            };

            outputForm.Controls.Add(outputTextBox);
            outputForm.Show();
        }


        public static void LaunchHtml(object? s)
        {
            var code = s.ToString();

            if (code.StartsWith("html\n"))
                code = code.Substring(5);

            var tempFile = $"{Path.GetTempPath()}{Guid.NewGuid().ToString()}.html";
            File.WriteAllText(tempFile, code);

            // find chrome path from registry
            var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\chrome.exe");
            var chromePath = key.GetValue(null).ToString();

            // start chrome
            Process.Start(chromePath, tempFile);
        }

        public static void LaunchTxt(object? s)
        {
            var code = s.ToString();

            // Remove "txt\n" prefix if it exists
            if (code.StartsWith("txt\n"))
                code = code.Substring(4);

            var tempFile = $"{Path.GetTempPath()}{Guid.NewGuid().ToString()}.txt";
            File.WriteAllText(tempFile, code);

            // Launch the default text editor (usually Notepad) for .txt files
            Process.Start(new ProcessStartInfo(tempFile) { UseShellExecute = true });
        }
    }
}