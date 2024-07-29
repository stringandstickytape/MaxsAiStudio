using System.Diagnostics;
using System.Text.RegularExpressions;

namespace AiTool3
{
    public class GitIgnoreFilterManager
    {
        string gitIgnoreContent { get; set; }

        List<(Regex Regex, bool IsNegation)> gitIgnorePatterns { get; set; }

        public GitIgnoreFilterManager(string gitIgnoreContent)
        {
            this.gitIgnoreContent = gitIgnoreContent;
            gitIgnorePatterns = ParseGitIgnore();
        }

        public List<string> FilterNonIgnoredPaths(List<string> paths)
        {
            return paths.Where(path => !IsIgnored(path)).ToList();
        }

        public bool PathIsIgnored(string path)
        {
            return FilterNonIgnoredPaths(new List<string> { path }).Count == 0;
        }

        private List<(Regex Regex, bool IsNegation)> ParseGitIgnore()
        {
            return gitIgnoreContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(line => !string.IsNullOrWhiteSpace(line) && !line.TrimStart().StartsWith("#"))
                .Select(pattern =>
                {
                    bool isNegation = pattern.StartsWith("!");
                    if (isNegation) pattern = pattern.Substring(1);

                    pattern = pattern.Trim('/').Replace(".", "\\.").Replace("**", ".*").Replace("*", "[^/]*").Replace("?", ".");
                    if (pattern.EndsWith("/")) pattern += ".*";

                    // Ensure the pattern matches anywhere in the path
                    if (!pattern.StartsWith(".*") && !pattern.StartsWith("^")) pattern = "(^|/)" + pattern;
                    if (!pattern.EndsWith(".*") && !pattern.EndsWith("$")) pattern += "($|/)";

                    return (new Regex(pattern, RegexOptions.IgnoreCase), isNegation);
                })
                .ToList();
        }

        private bool IsIgnored(string path)
        {
            path = path.Replace('\\', '/').Trim('/');
            bool ignored = false;

            foreach (var (pattern, isNegation) in gitIgnorePatterns)
            {
                if (pattern.IsMatch(path))
                {
                    ignored = !isNegation;
                }
            }
            if (ignored)
                Debug.WriteLine(path);
            return ignored;
        }
    }
}