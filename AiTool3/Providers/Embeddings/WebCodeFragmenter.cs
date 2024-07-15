using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace AiTool3.Providers.Embeddings
{
    public class WebCodeFragmenter
    {
        private const int MaxFragmentSize = 1000;
        private const int MinFragmentSize = 50;

        public List<CodeFragment> FragmentCode(string fileContent, string filePath)
        {
            var fragments = new List<CodeFragment>();
            var (html, css, js) = SeparateContent(fileContent);

            fragments.AddRange(ProcessHtml(html, filePath));
            fragments.AddRange(ProcessCss(css, filePath));
            fragments.AddRange(ProcessJavaScript(js, filePath));

            return fragments;
        }

        private (string html, List<string> css, List<string> js) SeparateContent(string fileContent)
        {
            var htmlDoc = new HtmlAgilityPack.HtmlDocument();
            htmlDoc.LoadHtml(fileContent);

            var css = new List<string>();
            var js = new List<string>();

            // Extract CSS
            var styleNodes = htmlDoc.DocumentNode.SelectNodes("//style");
            if (styleNodes != null)
            {
                foreach (var node in styleNodes)
                {
                    css.Add(node.InnerHtml);
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
                        js.Add(node.InnerHtml);
                        node.Remove();
                    }
                }
            }

            return (htmlDoc.DocumentNode.OuterHtml, css, js);
        }

        private List<CodeFragment> ProcessHtml(string html, string filePath)
        {
            var fragments = new List<CodeFragment>();
            var htmlDoc = new HtmlAgilityPack.HtmlDocument();
            htmlDoc.LoadHtml(html);

            ProcessHtmlNodes(htmlDoc.DocumentNode, fragments, filePath);

            return fragments;
        }

        private void ProcessHtmlNodes(HtmlNode node, List<CodeFragment> fragments, string filePath)
        {
            if (IsSignificantBlock(node))
            {
                var content = node.OuterHtml;
                if (content.Length > MaxFragmentSize)
                {
                    foreach (var childNode in node.ChildNodes)
                    {
                        ProcessHtmlNodes(childNode, fragments, filePath);
                    }
                }
                else if (content.Length >= MinFragmentSize)
                {
                    AddFragment(fragments, content, "HTML", filePath, node.Line);
                }
            }
            else
            {
                foreach (var childNode in node.ChildNodes)
                {
                    ProcessHtmlNodes(childNode, fragments, filePath);
                }
            }
        }

        private List<CodeFragment> ProcessCss(List<string> cssBlocks, string filePath)
        {
            var fragments = new List<CodeFragment>();
            foreach (var css in cssBlocks)
            {
                var rules = SplitCssRules(css);
                foreach (var rule in rules)
                {
                    if (rule.Length >= MinFragmentSize)
                    {
                        AddFragment(fragments, rule, "CSS", filePath, 0);
                    }
                }
            }
            return fragments;
        }

        private List<CodeFragment> ProcessJavaScript(List<string> jsBlocks, string filePath)
        {
            var fragments = new List<CodeFragment>();
            foreach (var js in jsBlocks)
            {
                var codeFragments = ExtractJavaScriptFragments(js);
                foreach (var fragment in codeFragments)
                {
                    if (fragment.Length >= MinFragmentSize)
                    {
                        AddFragment(fragments, fragment, "JavaScript", filePath, 0);
                    }
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

        private List<string> SplitJavaScript(string js)
        {
            // This is a simple split. For more accurate splitting, consider using a JS parser.
            return Regex.Split(js, @"(?<=;|\})")
                        .Where(statement => !string.IsNullOrWhiteSpace(statement))
                        .Select(statement => statement.Trim())
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