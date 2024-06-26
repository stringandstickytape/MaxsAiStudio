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
    }
}