using System;
using System.IO;

namespace AiTool3.Helpers
{

    public static class DirectoryHelper
    {
        public static void CreateSubdirectories()
        {
            // Get the directory of the executable
            string exeDirectory = AppDomain.CurrentDomain.BaseDirectory;

            // Define the subdirectories to create
            string[] subdirectories = { "Conversations", "Settings", "Templates", "Embeddings", "UsageStats" };

            foreach (var subdirectory in subdirectories)
            {
                // Combine the exe directory with the name of the subdirectory
                string subdirectoryPath = Path.Combine(exeDirectory, subdirectory);

                // Check if the directory exists, if not, create it
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
