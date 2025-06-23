// C:\Users\maxhe\source\repos\MaxsAiStudio\AiStudio4\Core\Models\PathHelper.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AiStudio4.Core.Models
{
    public static class PathHelper
    {
        public static bool IsTestingProfile { get; set; } = false;

        public static string ProfileRootPath
        {
            get
            {
                var folder = IsTestingProfile ? "AiStudio4_Test" : "AiStudio4";
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
