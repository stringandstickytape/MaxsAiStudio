using HtmlAgilityPack;
using System.Text;
using System.Text.RegularExpressions;

namespace AiTool3.Embeddings.Fragmenters
{
    public class WebCodeFragmenter
    {
        private const int MaxFragmentSize = 1000;
        private const int MinFragmentSize = 50;

        public class CodeBlock
        {
            public string Content { get; set; }
            public int LineNumber { get; set; }
        }

        public class SeparatedContent
        {
            public List<CodeBlock> Html { get; set; }
            public List<CodeBlock> Css { get; set; }
            public List<CodeBlock> Js { get; set; }
        }

        public List<CodeFragment> FragmentCode(string fileContent, string filePath)
        {
            var fragments = new List<CodeFragment>();
            var separatedContent = SeparateContent(fileContent);

            fragments.AddRange(ProcessHtml(separatedContent.Html, filePath));
            fragments.AddRange(ProcessCss(separatedContent.Css, filePath));
            fragments.AddRange(ProcessJavaScript(separatedContent.Js, filePath));

            return fragments;
        }

        // New method that only processes JavaScript
        public List<CodeFragment> FragmentJavaScriptCode(string fileContent, string filePath)
        {
            var fragments = new List<CodeFragment>();
            var jsBlock = new CodeBlock { Content = fileContent, LineNumber = 1 };
            var jsBlocks = new List<CodeBlock> { jsBlock };

            fragments.AddRange(ProcessJavaScript(jsBlocks, filePath));

            return fragments;
        }
        private SeparatedContent SeparateContent(string fileContent)
        {
            var htmlDoc = new HtmlAgilityPack.HtmlDocument();
            htmlDoc.LoadHtml(fileContent);

            var html = new List<CodeBlock>();
            var css = new List<CodeBlock>();
            var js = new List<CodeBlock>();

            // Extract CSS
            var styleNodes = htmlDoc.DocumentNode.SelectNodes("//style");
            if (styleNodes != null)
            {
                foreach (var node in styleNodes)
                {
                    css.Add(new CodeBlock { Content = node.InnerHtml, LineNumber = node.Line });
                    node.Remove();
                }
            }

            // Extract JavaScript
            var scriptNodes = htmlDoc.DocumentNode.SelectNodes("//script");
            if (scriptNodes != null)
            {
                foreach (var node in scriptNodes)
                {
                    if (string.IsNullOrEmpty(node.GetAttributeValue("src", null)))
                    {
                        js.Add(new CodeBlock { Content = node.InnerHtml, LineNumber = node.Line });
                        node.Remove();
                    }
                }
            }

            // Process remaining HTML
            ProcessHtmlNodesForSeparation(htmlDoc.DocumentNode, html);

            return new SeparatedContent { Html = html, Css = css, Js = js };
        }

        private void ProcessHtmlNodesForSeparation(HtmlNode node, List<CodeBlock> html)
        {
            if (IsSignificantBlock(node))
            {
                html.Add(new CodeBlock { Content = node.OuterHtml, LineNumber = node.Line });
            }
            else
            {
                foreach (var childNode in node.ChildNodes)
                {
                    ProcessHtmlNodesForSeparation(childNode, html);
                }
            }
        }

        private List<CodeFragment> ProcessHtml(List<CodeBlock> htmlBlocks, string filePath)
        {
            var fragments = new List<CodeFragment>();
            foreach (var block in htmlBlocks)
            {
                if (block.Content.Length > MaxFragmentSize)
                {
                    var htmlDoc = new HtmlAgilityPack.HtmlDocument();
                    htmlDoc.LoadHtml(block.Content);
                    ProcessHtmlNodes(htmlDoc.DocumentNode, fragments, filePath, block.LineNumber);
                }
                else if (block.Content.Length >= MinFragmentSize)
                {
                    AddFragment(fragments, block.Content, "HTML", filePath, block.LineNumber);
                }
            }
            return fragments;
        }

        private void ProcessHtmlNodes(HtmlNode node, List<CodeFragment> fragments, string filePath, int baseLineNumber)
        {
            if (IsSignificantBlock(node))
            {
                var content = node.OuterHtml;
                if (content.Length > MaxFragmentSize)
                {
                    foreach (var childNode in node.ChildNodes)
                    {
                        ProcessHtmlNodes(childNode, fragments, filePath, baseLineNumber + node.Line - 1);
                    }
                }
                else if (content.Length >= MinFragmentSize)
                {
                    AddFragment(fragments, content, "HTML", filePath, baseLineNumber + node.Line - 1);
                }
            }
            else
            {
                foreach (var childNode in node.ChildNodes)
                {
                    ProcessHtmlNodes(childNode, fragments, filePath, baseLineNumber);
                }
            }
        }

        private List<CodeFragment> ProcessCss(List<CodeBlock> cssBlocks, string filePath)
        {
            var fragments = new List<CodeFragment>();
            foreach (var block in cssBlocks)
            {
                var rules = SplitCssRules(block.Content);
                int currentLine = block.LineNumber;
                foreach (var rule in rules)
                {
                    if (rule.Length >= MinFragmentSize)
                    {
                        AddFragment(fragments, rule, "CSS", filePath, currentLine);
                    }
                    currentLine += rule.Split('\n').Length - 1;
                }
            }
            return fragments;
        }

        private List<CodeFragment> ProcessJavaScript(List<CodeBlock> jsBlocks, string filePath)
        {
            var fragments = new List<CodeFragment>();
            foreach (var block in jsBlocks)
            {
                var codeFragments = ExtractJavaScriptFragments(block.Content);
                int currentLine = block.LineNumber;
                foreach (var fragment in codeFragments)
                {
                    if (fragment.Length >= MinFragmentSize)
                    {
                        AddFragment(fragments, fragment, "JavaScript", filePath, currentLine);
                    }
                    currentLine += fragment.Split('\n').Length - 1;
                }
            }
            return fragments;
        }

        private List<string> ExtractJavaScriptFragments(string jsCode)
        {
            var fragments = new List<string>();
            var bracketStack = new Stack<int>();
            var currentFragment = new StringBuilder();
            var inString = false;
            var stringDelimiter = '\0';

            for (int i = 0; i < jsCode.Length; i++)
            {
                char c = jsCode[i];
                currentFragment.Append(c);

                if (inString)
                {
                    if (c == stringDelimiter && jsCode[i - 1] != '\\')
                    {
                        inString = false;
                    }
                }
                else
                {
                    if (c == '"' || c == '\'' || c == '`')
                    {
                        inString = true;
                        stringDelimiter = c;
                    }
                    else if (c == '{')
                    {
                        bracketStack.Push(i);
                    }
                    else if (c == '}')
                    {
                        if (bracketStack.Count > 0)
                        {
                            int openBracketIndex = bracketStack.Pop();
                            if (bracketStack.Count == 0)
                            {
                                string fragment = currentFragment.ToString().Trim();
                                if (!string.IsNullOrWhiteSpace(fragment))
                                {
                                    fragments.Add(fragment);
                                }
                                currentFragment.Clear();
                            }
                        }
                    }
                }
            }

            // Add any remaining code as a fragment
            string remainingFragment = currentFragment.ToString().Trim();
            if (!string.IsNullOrWhiteSpace(remainingFragment))
            {
                fragments.Add(remainingFragment);
            }

            return fragments;
        }

        private bool IsSignificantBlock(HtmlNode node)
        {
            string[] significantTags = { "div", "section", "article", "header", "footer", "nav", "aside", "main" };
            return significantTags.Contains(node.Name) ||
                   node.GetAttributeValue("class", "").Contains("significant") ||
                   node.GetAttributeValue("id", "").Contains("significant");
        }

        private List<string> SplitCssRules(string css)
        {
            return Regex.Split(css, @"(?<=\})")
                        .Where(rule => !string.IsNullOrWhiteSpace(rule))
                        .Select(rule => rule.Trim())
                        .ToList();
        }

        private void AddFragment(List<CodeFragment> fragments, string content, string type, string filePath, int lineNumber)
        {
            if (!string.IsNullOrWhiteSpace(content))
            {
                fragments.Add(new CodeFragment
                {
                    Content = content,
                    Type = type,
                    FilePath = filePath,
                    LineNumber = lineNumber
                });
            }
        }
    }
}