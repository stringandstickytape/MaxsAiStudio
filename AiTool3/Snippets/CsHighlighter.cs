using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AiTool3.Snippets
{
    public static class CSharpHighlighter
    {
        private static readonly Font CodeFont = new Font("Consolas", 10);
        public static Color KeywordColor { get; private set; } = Color.FromArgb(86, 156, 214);
        public static Color TypeColor { get; private set; } = Color.FromArgb(78, 201, 176);
        public static Color StringColor { get; private set; } = Color.FromArgb(214, 157, 133);
        public static Color CommentColor { get; private set; } = Color.FromArgb(87, 166, 74);
        public static Color NumberColor { get; private set; } = Color.FromArgb(181, 206, 168);
        public static Color MethodColor { get; private set; } = Color.FromArgb(220, 220, 170);
        public static Color OperatorColor { get; private set; } = Color.FromArgb(180, 180, 180);
        public static Color PreprocessorColor { get; private set; } = Color.FromArgb(155, 155, 155);
        public static Color AttributeColor { get; private set; } = Color.FromArgb(156, 220, 254);
        public static Color NamespaceColor { get; private set; } = Color.FromArgb(220, 220, 220);
        public static Color FieldColor { get; private set; } = Color.FromArgb(156, 220, 254);
        public static Color ConstantColor { get; private set; } = Color.FromArgb(189, 99, 197);

        public static Dictionary<string, Color> ConfigureColors()
        {
            var colorProperties = typeof(CSharpHighlighter).GetProperties(BindingFlags.Public | BindingFlags.Static)
                .Where(p => p.PropertyType == typeof(Color)).ToList();

            var form = new Form
            {
                Text = "Configure C# Highlighter Colors",
                Size = new Size(400, 150 + colorProperties.Count * 40),
                StartPosition = FormStartPosition.CenterScreen,
                BackColor = Color.Black
            };

            int y = 10;
            foreach (var prop in colorProperties)
            {
                var label = new Label
                {
                    Text = prop.Name,
                    Location = new Point(10, y),
                    Size = new Size(200, 30),
                    ForeColor = (Color)prop.GetValue(null)!,
                    //consolas
                    Font = new Font("Consolas", 10F, FontStyle.Regular, GraphicsUnit.Point, 0)
                };
                form.Controls.Add(label);

                var button = new Button
                {
                    Text = "Choose Color",
                    Location = new Point(220, y - 2),
                    Size = new Size(100, 35),
                    BackColor = (Color)prop.GetValue(null)!
                };
                button.Click += (sender, e) =>
                {
                    var colorDialog = new ColorDialog();
                    if (colorDialog.ShowDialog() == DialogResult.OK)
                    {
                        prop.SetValue(null, colorDialog.Color);
                        button.BackColor = colorDialog.Color;
                        label.ForeColor = colorDialog.Color;
                    }
                };
                form.Controls.Add(button);

                y += 40;
            }

            var okButton = new Button
            {
                Text = "OK",
                Location = new Point(300, y + 20),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                DialogResult = DialogResult.OK,
                AutoSize = true,
                ForeColor = Color.White,

            };
            form.Controls.Add(okButton);
            form.AcceptButton = okButton;

            return colorProperties.ToDictionary(p => p.Name, p => (Color)p.GetValue(null)!);
        }

        public static void HighlightCSharp(RichTextBox richTextBox, int startIndex, int length)
        {
            // Apply fixed-width font to the entire snippet
            ApplyCodeFont(richTextBox, startIndex, length);

            // C# patterns
            string keywordPattern = @"\b(abstract|as|base|bool|break|byte|case|catch|char|checked|class|const|continue|decimal|default|delegate|do|double|else|enum|event|explicit|extern|false|finally|fixed|float|for|foreach|goto|if|implicit|in|int|interface|internal|is|lock|long|namespace|new|null|object|operator|out|override|params|private|protected|public|readonly|ref|return|sbyte|sealed|short|sizeof|stackalloc|static|string|struct|switch|this|throw|true|try|typeof|uint|ulong|unchecked|unsafe|ushort|using|virtual|void|volatile|while)\b";
            string typePattern = @"\b(int|string|bool|double|float|char|long|void|object|dynamic|var|byte|short|decimal|DateTime)\b";
            string stringPattern = @"(""[^""\\]*(?:\\.[^""\\]*)*""|@""[^""]*(?:""""[^""]*)*""|'[^'\\]*(?:\\.[^'\\]*)*')";
            string commentPattern = @"(//.*?$|/\*[\s\S]*?\*/)";
            string numberPattern = @"\b\d+(?:\.\d+)?(?:[eE][+-]?\d+)?\b";
            string methodPattern = @"\b[a-zA-Z_]\w*(?=\s*\()";
            string operatorPattern = @"[+\-*/=<>!&|^~?:]+";
            string preprocessorPattern = @"^#\w+.*?$";
            string attributePattern = @"\[[\w\s,()]+\]";
            string namespacePattern = @"\b(?:namespace|using)\s+[\w.]+\b";
            string fieldPattern = @"\b_\w+\b";
            string constantPattern = @"\b[A-Z_][A-Z0-9_]+\b";

            // Highlight patterns
            HighlightPattern(richTextBox, keywordPattern, KeywordColor, startIndex, length);
            HighlightPattern(richTextBox, typePattern, TypeColor, startIndex, length);
            HighlightPattern(richTextBox, stringPattern, StringColor, startIndex, length);
            HighlightPattern(richTextBox, commentPattern, CommentColor, startIndex, length);
            HighlightPattern(richTextBox, numberPattern, NumberColor, startIndex, length);
            HighlightPattern(richTextBox, methodPattern, MethodColor, startIndex, length);
            HighlightPattern(richTextBox, operatorPattern, OperatorColor, startIndex, length);
            HighlightPattern(richTextBox, preprocessorPattern, PreprocessorColor, startIndex, length, RegexOptions.Multiline);
            HighlightPattern(richTextBox, attributePattern, AttributeColor, startIndex, length);
            HighlightPattern(richTextBox, namespacePattern, NamespaceColor, startIndex, length);
            HighlightPattern(richTextBox, fieldPattern, FieldColor, startIndex, length);
            HighlightPattern(richTextBox, constantPattern, ConstantColor, startIndex, length);
        }

        private static void HighlightPattern(RichTextBox richTextBox, string pattern, Color color, int startIndex, int length, RegexOptions options = RegexOptions.None)
        {
            string snippet = richTextBox.Text.Substring(startIndex, length);
            MatchCollection matches = Regex.Matches(snippet, pattern, options);
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