namespace AiTool3
{
    internal static class Program
    { 
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();

            try
            {
                Application.Run(new MaxsAiStudio());
            }
            catch (Exception ex)
            {
                // write very-full exception details to temp path
                var tempPath = Path.GetTempPath();
                var tempFile = Path.Combine(tempPath, "MaxsAiStudio-Error.txt");

                File.WriteAllText(tempFile, ex.ToString());
                File.AppendAllLines(tempFile, new[] { "----------------------", "Inner Exception", "----------------------", ex.InnerException?.ToString() });
                File.AppendAllLines(tempFile, new[] { "----------------------", "StackTrace", "----------------------", ex.StackTrace });
                File.AppendAllLines(tempFile, new[] { "----------------------", "Source", "----------------------", ex.Source });
                File.AppendAllLines(tempFile, new[] { "----------------------", "TargetSite", "----------------------", ex.TargetSite?.ToString() });
                File.AppendAllLines(tempFile, new[] { "----------------------", "Message", "----------------------", ex.Message });
                File.AppendAllLines(tempFile, new[] { "----------------------", "HelpLink", "----------------------", ex.HelpLink });
                File.AppendAllLines(tempFile, new[] { "----------------------", "HResult", "----------------------", ex.HResult.ToString() });

                MessageBox.Show( $"Error has been written to {tempFile}", ex.Message, MessageBoxButtons.OK, MessageBoxIcon.Error);

            }

        }
    }
}