namespace AiTool3.Helpers
{

    public static class DirectoryHelper
    {
        public static void CreateSubdirectories()
        {
            string exeDirectory = AppDomain.CurrentDomain.BaseDirectory;

            string[] subdirectories = { "Conversations", "Settings", "Templates", "Embeddings", "UsageStats", "TokenUsage" };

            foreach (var subdirectory in subdirectories)
            {
                string subdirectoryPath = Path.Combine(exeDirectory, subdirectory);

                if (!Directory.Exists(subdirectoryPath))
                {
                    Directory.CreateDirectory(subdirectoryPath);
                    Console.WriteLine($"Created directory: {subdirectoryPath}");
                }
                else
                {
                    Console.WriteLine($"Directory already exists: {subdirectoryPath}");
                }
            }
        }
    }
}
