using System;
using System.Linq;

namespace VSIXTest
{
    /// <summary>
    /// Provides functionality to replace text in a source file with new text, using a line number hint for efficient matching.
    /// </summary>
    public class TextReplacer
    {
        /// <summary>
        /// Replaces a specified text in a source file with new text, searching for matches around a given line number hint.
        /// </summary>
        /// <param name="sourceFile">The complete content of the source file to modify.</param>
        /// <param name="oldText">The text to be replaced.</param>
        /// <param name="newText">The text to replace with.</param>
        /// <param name="lineNumberHint">The approximate line number where the replacement should occur.</param>
        /// <returns>The modified file content if a match is found, or the original content if no match is found.</returns>
        public string ReplaceTextAtHint(string sourceFile, string oldText, string newText, int lineNumberHint)
        {
            // our index here is zero, but LLMs will typically number lines from 1
            lineNumberHint--;

            // Determine dominant newlines and split texts
            var (sourceLines, sourceNewline) = SplitWithNewlineDetection(sourceFile);
            var (oldLines, _) = SplitWithNewlineDetection(oldText);
            var (newLines, _) = SplitWithNewlineDetection(newText);


            // Search outward from hint
            int maxOffset = Math.Max(sourceLines.Length - lineNumberHint, lineNumberHint);

            for (int offset = 0; offset <= maxOffset; offset++)
            {
                // Try line number + offset
                int upperLine = lineNumberHint + offset;
                if (upperLine < sourceLines.Length)
                {
                    if (MatchesAtPosition(sourceLines, oldLines, upperLine))
                    {
                        return BuildResult(sourceLines, newLines, upperLine, sourceNewline, oldLines.Length);
                    }
                }

                // Try line number - offset
                int lowerLine = lineNumberHint - offset;
                if (lowerLine >= 0 && offset > 0) // offset > 0 to avoid checking hint twice
                {
                    if (MatchesAtPosition(sourceLines, oldLines, lowerLine))
                    { // - 1 is a fail
                        return BuildResult(sourceLines, newLines, lowerLine, sourceNewline, oldLines.Length);
                    }
                }
            }

            // No match found
            return sourceFile;
        }

        private (string[] lines, string newline) SplitWithNewlineDetection(string text)
        {
            // Detect dominant newline
            int crlfCount = text.Count(c => c == '\r');
            int lfCount = text.Count(c => c == '\n') - crlfCount; // Subtract CRLF count to get pure LF count

            string dominantNewline = crlfCount >= lfCount ? "\r\n" : "\n";

            var split = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

            if (string.IsNullOrEmpty(split.Last()))
                split = split.Take(split.Length - 1).ToArray();

            // Split the text
            return (split, dominantNewline);
        }

        private bool MatchesAtPosition(string[] sourceLines, string[] oldLines, int startLine)
        {
            if (startLine + oldLines.Length > sourceLines.Length)
                return false;

            for (int i = 0; i < oldLines.Length; i++)
            {
                if (sourceLines[startLine + i] != oldLines[i])
                    return false;
            }

            return true;
        }

        private string BuildResult(string[] sourceLines, string[] newLines, int matchPosition, string newline, int oldLinesLength)
        {
            var result = new System.Text.StringBuilder();

            // Add lines before match
            for (int i = 0; i < matchPosition; i++)
            {
                result.Append(sourceLines[i]);
                result.Append(newline);
            }

            // Add new text
            for (int i = 0; i < newLines.Length; i++)
            {
                result.Append(newLines[i]);
                if (i < newLines.Length - 1)
                    result.Append(newline);
            }

            // Add lines after match
            for (int i = matchPosition + oldLinesLength; i < sourceLines.Length; i++)
            {
                result.Append(newline);
                result.Append(sourceLines[i]);
            }

            return result.ToString();
        }
    }
}