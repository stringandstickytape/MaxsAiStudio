using System.Text.RegularExpressions;


namespace AiTool3.Snippets
{
    public class SnippetManager
    {
        public List<Snippet> FindSnippets(string text)
        {
            string pattern = @"```(.*?)```";
            List<Snippet> snippets = new List<Snippet>();

            var matches = Regex.Matches(text, pattern, RegexOptions.Singleline);

            foreach (Match match in matches)
            {
                if (match.Captures.Count > 0)
                {
                    int startIndex = match.Captures[0].Index;
                    int length = match.Captures[0].Length;

                    string type = null;
                    string filename = null;

                    // Extract type and filename if available
                    var lineBeforeMatch = Regex.Match(text.Substring(0, startIndex), @"### (.*?) \((.*?)\)");
                    if (lineBeforeMatch.Success)
                    {
                        type = lineBeforeMatch.Groups[1].Value;
                        filename = lineBeforeMatch.Groups[2].Value;
                    }

                    var snippetText = text.Substring(startIndex, length);

                    // Remove language name if present at the start of the snippet
                    snippetText = Regex.Replace(snippetText, @"^\s*(\w+)\s*\n", "");

                    snippets.Add(new Snippet
                    {
                        Type = type,
                        Filename = filename,
                        Code = snippetText.Trim(),
                        StartIndex = startIndex
                    });
                }
            }

            return snippets;
        }

        public string ApplySnippetFormatting(string text)
        {
            string pattern = @"```(.*?)```";
            return Regex.Replace(text, pattern, match =>
            {
                if (match.Groups.Count > 1)
                {
                    string snippetText = match.Groups[1].Value;
                    // Remove language name if present at the start of the snippet
                    snippetText = Regex.Replace(snippetText, @"^\s*(\w+)\s*\n", "");
                    return $"<snippet>{snippetText}</snippet>";
                }
                return match.Value;
            }, RegexOptions.Singleline);
        }
    }


}
