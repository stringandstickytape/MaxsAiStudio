using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AiTool3.Snippets
{
    public static class HtmlHighlighter
    {
        private static readonly Font CodeFont = new Font("Consolas", 10);
        private static readonly Color HtmlTagColor = Color.DeepSkyBlue;
        private static readonly Color AttributeNameColor = Color.Orange;
        private static readonly Color AttributeValueColor = Color.LightGreen;
        private static readonly Color JsColor = Color.PaleGoldenrod;
        private static readonly Color CommentColor = Color.Gray;
        private static readonly Color StringColor = Color.LightCoral;
        private static readonly Color KeywordColor = Color.LightSkyBlue;
        private static readonly Color NumberColor = Color.MediumPurple;
        private static readonly Color FunctionColor = Color.YellowGreen;
        private static readonly Color OperatorColor = Color.LightPink;

        public static void HighlightHtml(RichTextBox richTextBox, int startIndex, int length)
        {
            string snippet = richTextBox.Text.Substring(startIndex, length);

            // HTML tag pattern
            string htmlTagPattern = @"</?[\w\s""=]+>";

            // Attribute pattern
            string attributePattern = @"(\w+)(\s*=\s*""[^""]*"")";

            // JavaScript pattern
            string jsPattern = @"(<script[^>]*>)([\s\S]*?)(</script>)";

            // Comment pattern
            string commentPattern = @"<!--[\s\S]*?-->";

            // Apply fixed-width font to the entire snippet
            ApplyCodeFont(richTextBox, startIndex, length);

            // Highlight HTML tags
            HighlightPattern(richTextBox, htmlTagPattern, HtmlTagColor, startIndex, length);

            // Highlight attributes
            MatchCollection attributeMatches = Regex.Matches(snippet, attributePattern);
            foreach (Match match in attributeMatches)
            {
                // Highlight attribute name
                HighlightText(richTextBox, startIndex + match.Groups[1].Index, match.Groups[1].Length, AttributeNameColor);

                // Highlight attribute value
                HighlightText(richTextBox, startIndex + match.Groups[2].Index, match.Groups[2].Length, AttributeValueColor);
            }

            // Highlight JavaScript
            MatchCollection jsMatches = Regex.Matches(snippet, jsPattern);
            foreach (Match match in jsMatches)
            {
                // Highlight script tags
                HighlightText(richTextBox, startIndex + match.Groups[1].Index, match.Groups[1].Length, HtmlTagColor);
                HighlightText(richTextBox, startIndex + match.Groups[3].Index, match.Groups[3].Length, HtmlTagColor);

                // Highlight JavaScript content
                int jsStartIndex = startIndex + match.Groups[2].Index;
                int jsLength = match.Groups[2].Length;
                HighlightJavaScript(richTextBox, jsStartIndex, jsLength);
            }

            // Highlight comments
            HighlightPattern(richTextBox, commentPattern, CommentColor, startIndex, length);
        }

        private static void HighlightJavaScript(RichTextBox richTextBox, int startIndex, int length)
        {
            string jsSnippet = richTextBox.Text.Substring(startIndex, length);

            // JavaScript patterns
            string keywordPattern = @"\b(var|let|const|function|return|if|else|for|while|do|switch|case|break|continue|try|catch|throw|new|typeof|instanceof)\b";
            string stringPattern = @"(""[^""\\]*(?:\\.[^""\\]*)*""|'[^'\\]*(?:\\.[^'\\]*)*')";
            string commentPattern = @"(//.*?$|/\*[\s\S]*?\*/)";
            string numberPattern = @"\b\d+(?:\.\d+)?\b";
            string functionPattern = @"\b[a-zA-Z_]\w*(?=\s*\()";
            string operatorPattern = @"[+\-*/=<>!&|^~?:]+";

            // Set the base color for JavaScript content
            HighlightText(richTextBox, startIndex, length, JsColor);

            // Highlight keywords
            HighlightPattern(richTextBox, keywordPattern, KeywordColor, startIndex, length);

            // Highlight strings
            HighlightPattern(richTextBox, stringPattern, StringColor, startIndex, length);

            // Highlight comments
            HighlightPattern(richTextBox, commentPattern, CommentColor, startIndex, length);

            // Highlight numbers
            HighlightPattern(richTextBox, numberPattern, NumberColor, startIndex, length);

            // Highlight functions
            HighlightPattern(richTextBox, functionPattern, FunctionColor, startIndex, length);

            // Highlight operators
            HighlightPattern(richTextBox, operatorPattern, OperatorColor, startIndex, length);
        }

        private static void HighlightPattern(RichTextBox richTextBox, string pattern, Color color, int startIndex, int length)
        {
            string snippet = richTextBox.Text.Substring(startIndex, length);
            MatchCollection matches = Regex.Matches(snippet, pattern, RegexOptions.Multiline);
            foreach (Match match in matches)
            {
                HighlightText(richTextBox, startIndex + match.Index, match.Length, color);
            }
        }

        private static void HighlightText(RichTextBox richTextBox, int start, int length, Color color)
        {
            richTextBox.SelectionStart = start;
            richTextBox.SelectionLength = length;
            richTextBox.SelectionColor = color;
        }

        private static void ApplyCodeFont(RichTextBox richTextBox, int start, int length)
        {
            richTextBox.SelectionStart = start;
            richTextBox.SelectionLength = length;
            richTextBox.SelectionFont = CodeFont;
        }
    }
}