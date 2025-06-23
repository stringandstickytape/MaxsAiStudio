using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AiStudio4.Core.Models
{
    public static class PathHelper
    {
        public static string ProfileRootPath
        {
            get
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AiStudio4");
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
