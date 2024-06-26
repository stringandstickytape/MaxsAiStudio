using AiTool3.Snippets;
using Microsoft.CodeAnalysis.Scripting;
using System.Diagnostics;

namespace AiTool3.UI
{
    public static class LaunchHelpers
    {

        public static async void LaunchCSharp(string code)
        {
            try
            {
                var scriptOptions = ScriptOptions.Default.AddReferences(typeof(Console).Assembly);
                // evaluate c# in .net core 8
                var result = await Microsoft.CodeAnalysis.CSharp.Scripting.CSharpScript.EvaluateAsync(code, scriptOptions);

                MessageBox.Show(result.ToString());

            }
            catch (CompilationErrorException e)
            {
                Console.WriteLine("Compilation error: " + e.Message);
            }
            catch (Exception e)
            {
                Console.WriteLine("Execution error: " + e.Message);
            }
        }

        public static void LaunchHtml(object? s)
        {
            var snip = (Snippet)((Button)s).Tag;

            var code = snip.Code;

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
            var snip = (Snippet)((Button)s).Tag;

            var code = snip.Code;

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