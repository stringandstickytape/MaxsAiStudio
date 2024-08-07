using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace AiTool3.Helpers
{
    public static class StlHelper
    {
        public static void LaunchStlFile(string contents)
        {
            // save to temp file
            string tempFilePath = Path.GetTempPath() + Guid.NewGuid().ToString() + ".stl";
            File.WriteAllText(tempFilePath, contents);
            try
            {
                Process process = new Process();
                process.StartInfo.FileName = tempFilePath;
                process.StartInfo.UseShellExecute = true;
                process.StartInfo.Verb = "open";

                process.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error opening STL file: {ex.Message}");
                MessageBox.Show($"Error opening STL file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                File.Delete(tempFilePath);
            }
        }
    }
}