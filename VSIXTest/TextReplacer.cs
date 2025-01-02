using System;
using System.Linq;

namespace VSIXTest
{
    public class TextReplacer
    {
        public string ReplaceTextAtHint(string sourceFile, string oldText, string newText, int lineNumberHint)
        {
            // Determine dominant newlines and split texts
            var (sourceLines, sourceNewline) = SplitWithNewlineDetection(sourceFile);
            var (oldLines, _) = SplitWithNewlineDetection(oldText);
            var (newLines, _) = SplitWithNewlineDetection(newText);

            if (sourceLines.Length == 0 || oldLines.Length == 0)
                return sourceFile;

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
                    {
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

            // Split the text
            return (text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None), dominantNewline);
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