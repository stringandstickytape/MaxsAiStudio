using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Ignore;

namespace SharedClasses.Git
{
    public class GitIgnoreFilterManager
    {
        private readonly string _projectRoot;
        private readonly Ignore.Ignore _ignoreList;

        public GitIgnoreFilterManager(string gitIgnoreContent, string projectRoot)
        {
            _projectRoot = projectRoot?.TrimEnd('\\', '/') ?? string.Empty;
            _ignoreList = new Ignore.Ignore();
            
            // Add the gitignore patterns to the ignore list
            if (!string.IsNullOrEmpty(gitIgnoreContent))
            {
                using (var reader = new StringReader(gitIgnoreContent))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        _ignoreList.Add(line);
                    }
                }
            }
        }

        public List<string> FilterNonIgnoredPaths(List<string> paths)
        {
            return paths.Where(path => !PathIsIgnored(path)).ToList();
        }

        public bool PathIsIgnored(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            // Convert path to be relative to project root if it's absolute
            string relativePath = GetRelativePath(path);
            
            // Normalize path separators to forward slashes for gitignore matching
            relativePath = relativePath.Replace('\\', '/');

            if (relativePath == ".git/")
                return true;

            // Check if the path is ignored
            return _ignoreList.IsIgnored(relativePath);
        }

        private string GetRelativePath(string path)
        {
            // If the path is already relative or project root is empty, return as is
            if (string.IsNullOrEmpty(_projectRoot) || !Path.IsPathRooted(path))
                return path;

            // If the path is rooted but doesn't start with the project root,
            // it might be using a different drive or format, so return it as is
            if (!path.StartsWith(_projectRoot, StringComparison.OrdinalIgnoreCase))
            {
                path = path.Replace('/', '\\');
                if (!path.StartsWith(_projectRoot, StringComparison.OrdinalIgnoreCase))
                {
                    return path;
                }
            }

            // Get the path relative to the project root
            string relativePath = path.Substring(_projectRoot.Length).TrimStart('\\', '/');
            return relativePath;
        }
    }
}