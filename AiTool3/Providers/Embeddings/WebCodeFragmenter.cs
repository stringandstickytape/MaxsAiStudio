
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

namespace AiTool3.Providers.Embeddings
{


    public class WebCodeFragmenter
    {
        public static void ProcessHtml(string htmlContent)
        {
            List<string> styleTags = new List<string>();
            List<string> scriptTags = new List<string>();

            // Regex for style tags
            string stylePattern = @"<style\b[^>]*>(.*?)</style>";
            MatchCollection styleMatches = Regex.Matches(htmlContent, stylePattern, RegexOptions.Singleline);

            foreach (Match match in styleMatches)
            {
                if (match.Groups.Count > 1)
                {
                    styleTags.Add(match.Groups[1].Value);
                }
            }

            // Regex for script tags
            string scriptPattern = @"<script\b[^>]*>(.*?)</script>";
            MatchCollection scriptMatches = Regex.Matches(htmlContent, scriptPattern, RegexOptions.Singleline);

            foreach (Match match in scriptMatches)
            {
                if (match.Groups.Count > 1)
                {
                    scriptTags.Add(match.Groups[1].Value);
                }
            }

            // Remove style and script tags from the original HTML
            string remainingHtml = Regex.Replace(htmlContent, stylePattern, "", RegexOptions.Singleline);
            remainingHtml = Regex.Replace(remainingHtml, scriptPattern, "", RegexOptions.Singleline);

            return;
        }

        public List<CodeFragment> FragmentCode(string fileContent, string filePath)
        {

            ProcessHtml(fileContent);
            var fragments = new List<CodeFragment>();
            var lines = fileContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

            var currentBlock = new List<string>();
            var inScript = false;
            var inStyle = false;
            var inHtmlTag = false;

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();

                // Skip empty lines and HTML comments
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("<!--"))
                    continue;

                // Check for script start/end
                if (line.Contains("<script"))
                {
                    inScript = true;
                    AddFragment(fragments, string.Join("\n", currentBlock), "HTML", filePath);
                    currentBlock.Clear();
                }
                else if (line.Contains("</script>"))
                {
                    inScript = false;
                    AddFragment(fragments, string.Join("\n", currentBlock), "JavaScript", filePath);
                    currentBlock.Clear();
                }
                // Check for style start/end
                else if (line.Contains("<style"))
                {
                    inStyle = true;
                    AddFragment(fragments, string.Join("\n", currentBlock), "HTML", filePath);
                    currentBlock.Clear();
                }
                else if (line.Contains("</style>"))
                {
                    inStyle = false;
                    AddFragment(fragments, string.Join("\n", currentBlock), "CSS", filePath);
                    currentBlock.Clear();
                }
                // Check for HTML tag start/end
                else if (line.StartsWith("<") && !inHtmlTag && !inScript && !inStyle)
                {
                    if (currentBlock.Count > 0)
                    {
                        AddFragment(fragments, string.Join("\n", currentBlock), "HTML", filePath);
                        currentBlock.Clear();
                    }
                    inHtmlTag = true;
                }
                else if (line.EndsWith(">") && inHtmlTag)
                {
                    currentBlock.Add(lines[i]); // Use original line to preserve indentation
                    AddFragment(fragments, string.Join("\n", currentBlock), "HTML", filePath);
                    currentBlock.Clear();
                    inHtmlTag = false;
                    continue;
                }

                currentBlock.Add(lines[i]); // Use original line to preserve indentation

                // Add JavaScript statement
                if (inScript && line.EndsWith(";"))
                {
                    AddFragment(fragments, string.Join("\n", currentBlock), "JavaScript", filePath);
                    currentBlock.Clear();
                }
            }

            // Add any remaining block
            if (currentBlock.Count > 0)
            {
                var type = inScript ? "JavaScript" : (inStyle ? "CSS" : "HTML");
                AddFragment(fragments, string.Join("\n", currentBlock), type, filePath);
            }

            return fragments;
        }

        private void AddFragment(List<CodeFragment> fragments, string content, string type, string filePath)
        {
            if (!string.IsNullOrWhiteSpace(content))
            {
                fragments.Add(new CodeFragment
                {
                    Content = content,
                    Type = type,
                    FilePath = filePath,
                    LineNumber = 0
                });
            }
        }
    }
}
