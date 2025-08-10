using System;
using System.IO;

namespace AiStudio4.McpStandalone.Helpers
{
    public static class PathHelper
    {
        public static bool IsTestingProfile { get; set; } = false;

        public static string ProfileRootPath
        {
            get
            {
                var folder = IsTestingProfile ? "AiStudio4.McpStandalone_Test" : "AiStudio4.McpStandalone";
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), folder);
            }
        }

        public static string GetProfileSubPath(params string[] subPaths)
        {
            if (subPaths == null || subPaths.Length == 0)
            {
                throw new ArgumentException("Sub paths cannot be null or empty.", nameof(subPaths));
            }
            return Path.Combine(ProfileRootPath, Path.Combine(subPaths));
        }
    }
}