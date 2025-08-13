using System;
using System.Linq;
using System.Text;

namespace AiStudio4.Tools.Services.SmartFileEditor
{
    /// <summary>
    /// Helper class for visualizing text differences and whitespace
    /// </summary>
    public static class TextVisualization
    {
        /// <summary>
        /// Makes whitespace visible using special characters
        /// </summary>
        public static string ShowWhitespace(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            return text
                .Replace(" ", "·")      // Middle dot for space
                .Replace("\t", "→")     // Arrow for tab
                .Replace("\r", "␍")     // CR symbol
                .Replace("\n", "␊");    // LF symbol
        }

        /// <summary>
        /// Shows a preview of text, truncated if necessary
        /// </summary>
        public static string Preview(string text, int maxLength = 80)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            if (text.Length <= maxLength)
                return text;

            // Try to break at a natural boundary (space, newline)
            var truncated = text.Substring(0, maxLength);
            var lastSpace = truncated.LastIndexOfAny(new[] { ' ', '\n', '\r' });
            
            if (lastSpace > maxLength / 2) // Only break at space if it's not too early
            {
                truncated = truncated.Substring(0, lastSpace);
            }

            return truncated + "...";
        }

        /// <summary>
        /// Creates a detailed comparison of two strings
        /// </summary>
        public static string CreateComparison(string expected, string actual, bool showWhitespace = true)
        {
            var sb = new StringBuilder();
            
            if (showWhitespace)
            {
                sb.AppendLine("Expected (with whitespace):");
                sb.AppendLine($"  {ShowWhitespace(Preview(expected, 120))}");
                sb.AppendLine();
                sb.AppendLine("Found (with whitespace):");
                sb.AppendLine($"  {ShowWhitespace(Preview(actual, 120))}");
            }
            else
            {
                sb.AppendLine("Expected:");
                sb.AppendLine($"  {Preview(expected, 120)}");
                sb.AppendLine();
                sb.AppendLine("Found:");
                sb.AppendLine($"  {Preview(actual, 120)}");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Identifies specific differences between two strings
        /// </summary>
        public static string IdentifyDifferences(string expected, string actual)
        {
            if (expected == actual)
                return "Strings are identical";

            var differences = new StringBuilder();

            // Check length difference
            if (expected.Length != actual.Length)
            {
                differences.AppendLine($"Length difference: expected {expected.Length} characters, found {actual.Length}");
            }

            // Check whitespace differences
            if (NormalizeWhitespace(expected) == NormalizeWhitespace(actual))
            {
                differences.AppendLine("Content matches when whitespace is normalized");
                
                // Identify specific whitespace differences
                var expectedSpaces = expected.Count(c => c == ' ');
                var actualSpaces = actual.Count(c => c == ' ');
                var expectedTabs = expected.Count(c => c == '\t');
                var actualTabs = actual.Count(c => c == '\t');
                var expectedNewlines = expected.Count(c => c == '\n');
                var actualNewlines = actual.Count(c => c == '\n');

                if (expectedSpaces != actualSpaces)
                    differences.AppendLine($"  - Space count: expected {expectedSpaces}, found {actualSpaces}");
                if (expectedTabs != actualTabs)
                    differences.AppendLine($"  - Tab count: expected {expectedTabs}, found {actualTabs}");
                if (expectedNewlines != actualNewlines)
                    differences.AppendLine($"  - Newline count: expected {expectedNewlines}, found {actualNewlines}");
            }

            // Check case differences
            if (expected.Equals(actual, StringComparison.OrdinalIgnoreCase))
            {
                differences.AppendLine("Content matches when case is ignored");
                
                // Find first case difference
                for (int i = 0; i < Math.Min(expected.Length, actual.Length); i++)
                {
                    if (expected[i] != actual[i] && 
                        char.ToLowerInvariant(expected[i]) == char.ToLowerInvariant(actual[i]))
                    {
                        differences.AppendLine($"  - First case difference at position {i}: '{expected[i]}' vs '{actual[i]}'");
                        break;
                    }
                }
            }

            // Find first difference position
            int firstDiff = -1;
            for (int i = 0; i < Math.Min(expected.Length, actual.Length); i++)
            {
                if (expected[i] != actual[i])
                {
                    firstDiff = i;
                    break;
                }
            }

            if (firstDiff >= 0)
            {
                var line = GetLineNumber(expected, firstDiff);
                differences.AppendLine($"First difference at position {firstDiff} (line {line}):");
                
                // Show context around the difference
                var contextStart = Math.Max(0, firstDiff - 20);
                var contextEnd = Math.Min(Math.Min(expected.Length, actual.Length), firstDiff + 20);
                
                var expectedContext = expected.Substring(contextStart, contextEnd - contextStart);
                var actualContext = actual.Substring(contextStart, contextEnd - contextStart);
                
                differences.AppendLine($"  Expected: ...{ShowWhitespace(expectedContext)}...");
                differences.AppendLine($"  Found:    ...{ShowWhitespace(actualContext)}...");
                differences.AppendLine($"  {new string(' ', firstDiff - contextStart + 14)}^");
            }

            return differences.ToString();
        }

        private static string NormalizeWhitespace(string text)
        {
            // Replace all consecutive whitespace with single space
            var normalized = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ");
            return normalized.Trim();
        }

        private static int GetLineNumber(string text, int position)
        {
            if (position >= text.Length)
                return -1;

            int line = 1;
            for (int i = 0; i < position; i++)
            {
                if (text[i] == '\n')
                    line++;
            }
            return line;
        }

        /// <summary>
        /// Gets line and column number for a position in text
        /// </summary>
        public static (int line, int column) GetLineAndColumn(string text, int position)
        {
            if (position >= text.Length)
                return (-1, -1);

            int line = 1;
            int lastNewline = -1;
            
            for (int i = 0; i < position; i++)
            {
                if (text[i] == '\n')
                {
                    line++;
                    lastNewline = i;
                }
            }
            
            int column = position - lastNewline;
            return (line, column);
        }
    }
}