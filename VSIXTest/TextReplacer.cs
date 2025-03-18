using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

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
        /// <param name="lineNumberHint">The approximate line number where the replacement should occur (1-based).</param>
        /// <returns>The modified file content if a match is found, or the original content if no match is found.</returns>
        public string ReplaceTextAtHint(string sourceFile, string oldText, string newText, int lineNumberHint)
        {
            if (string.IsNullOrEmpty(sourceFile) || string.IsNullOrEmpty(oldText))
                return sourceFile;

            // Properly split texts with newline preservation
            var sourceTextInfo = SplitTextPreservingNewlines(sourceFile);
            var oldTextInfo = SplitTextPreservingNewlines(oldText);
            var newTextInfo = SplitTextPreservingNewlines(newText);
            
            // Add logging for debugging purposes
            System.Diagnostics.Debug.WriteLine($"Source text has {sourceTextInfo.Lines.Count} lines");
            System.Diagnostics.Debug.WriteLine($"Old text has {oldTextInfo.Lines.Count} lines");

            // Handle empty file case
            if (sourceTextInfo.Lines.Count == 0)
            {
                return sourceFile; // Can't match anything in an empty file
            }

            // Convert from 1-based (user) to 0-based (code) indexing with bounds checking
            int zeroBasedLineHint = Math.Max(0, lineNumberHint - 1);

            // Ensure valid line hint (capped at the last line)
            zeroBasedLineHint = Math.Min(zeroBasedLineHint, sourceTextInfo.Lines.Count - 1);

            // Find the match using bidirectional search
            int matchPosition = FindMatchPosition(sourceTextInfo.Lines, oldTextInfo.Lines, zeroBasedLineHint);

            // If no match found, return original content
            if (matchPosition < 0)
                return sourceFile;

            // Build the result with proper newline handling
            return BuildResult(
                sourceTextInfo.Lines,
                sourceTextInfo.LineEndings,
                newTextInfo.Lines,
                newTextInfo.LineEndings,
                matchPosition,
                oldTextInfo.Lines.Count,
                sourceTextInfo.EndsWithNewline
            );
        }

        /// <summary>
        /// Detailed text info including lines, their endings, and whether the text ends with a newline
        /// </summary>
        private class TextInfo
        {
            public List<string> Lines { get; } = new List<string>();
            public List<string> LineEndings { get; } = new List<string>();
            public bool EndsWithNewline { get; set; }
        }

        /// <summary>
        /// Splits text into lines while preserving the exact newline characters used for each line
        /// </summary>
        private TextInfo SplitTextPreservingNewlines(string text)
        {
            var result = new TextInfo();

            if (string.IsNullOrEmpty(text))
                return result;

            // Use regex to split while capturing the newline characters
            var matches = Regex.Matches(text, @"([^\r\n]*)(\r\n|\n|\r|$)");

            foreach (Match match in matches)
            {
                string line = match.Groups[1].Value;
                string ending = match.Groups[2].Value;

                // Skip the last empty match that doesn't represent a real line
                if (match.Index + match.Length == text.Length && string.IsNullOrEmpty(line) && string.IsNullOrEmpty(ending))
                    break;

                result.Lines.Add(line);
                result.LineEndings.Add(ending);

                // Track if the text ends with a newline
                if (match.Index + match.Length == text.Length)
                {
                    result.EndsWithNewline = !string.IsNullOrEmpty(ending) && ending != "$";
                }
            }

            return result;
        }

        /// <summary>
        /// Finds the position where the oldLines match in sourceLines, searching outward from the hint
        /// </summary>
        private int FindMatchPosition(List<string> sourceLines, List<string> oldLines, int lineHint)
        {
            if (oldLines.Count == 0 || sourceLines.Count == 0)
                return lineHint;

            // Handle special case - empty pattern matches at any position
            if (oldLines.Count == 1 && oldLines[0] == "")
                return Math.Min(lineHint, sourceLines.Count);

            int maxOffset = Math.Max(sourceLines.Count, lineHint);

            for (int offset = 0; offset <= maxOffset; offset++)
            {
                // Try line number + offset
                int upperLine = lineHint + offset;
                if (upperLine < sourceLines.Count && MatchesAtPosition(sourceLines, oldLines, upperLine))
                {
                    return upperLine;
                }

                // Try line number - offset (avoid checking hint twice)
                int lowerLine = lineHint - offset;
                if (offset > 0 && lowerLine >= 0 && MatchesAtPosition(sourceLines, oldLines, lowerLine))
                {
                    return lowerLine;
                }

                // Early exit if we've searched the entire file
                if ((upperLine >= sourceLines.Count) && (lowerLine < 0))
                {
                    break;
                }
            }

            return -1; // No match found
        }

        /// <summary>
        /// Checks if the sourceLines match the oldLines starting at the given position
        /// </summary>
        private bool MatchesAtPosition(List<string> sourceLines, List<string> oldLines, int startLine)
        {
            if (startLine + oldLines.Count > sourceLines.Count)
                return false;

            for (int i = 0; i < oldLines.Count; i++)
            {
                if (sourceLines[startLine + i].Trim() != oldLines[i].Trim())
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Builds the result by combining parts before the match, the new text, and parts after the match
        /// </summary>
        private string BuildResult(
            List<string> sourceLines,
            List<string> sourceEndings,
            List<string> newLines,
            List<string> newEndings,
            int matchPosition,
            int oldLinesCount,
            bool sourceEndsWithNewline)
        {
            var result = new StringBuilder();

            // Add lines before match with their original endings
            for (int i = 0; i < matchPosition; i++)
            {
                result.Append(sourceLines[i]);
                result.Append(sourceEndings[i]);
            }

            // Add new text with appropriate endings
            for (int i = 0; i < newLines.Count; i++)
            {
                result.Append(newLines[i]);

                // For the last line of new text, we need to be careful with the ending
                if (i == newLines.Count - 1)
                {
                    // If source ends with newline or we're not at the end of the file,
                    // ensure we have a proper line ending
                    if (matchPosition + oldLinesCount < sourceLines.Count || sourceEndsWithNewline)
                    {
                        // Prefer to use the ending from the last line of old text when available
                        string lineEnding = (matchPosition + oldLinesCount - 1 < sourceEndings.Count)
                            ? sourceEndings[matchPosition + oldLinesCount - 1]
                            : (sourceEndings.Count > 0 ? sourceEndings[0] : "\n");

                        result.Append(lineEnding);
                    }
                }
                else if (i < newEndings.Count)
                {
                    // Use new text's line ending for non-last lines
                    result.Append(newEndings[i]);
                }
            }

            // Add lines after match with their original endings
            for (int i = matchPosition + oldLinesCount; i < sourceLines.Count; i++)
            {
                result.Append(sourceLines[i]);

                // Add line ending if not the last line or if source ends with newline
                if (i < sourceLines.Count - 1 || sourceEndsWithNewline)
                {
                    // Ensure we don't go out of bounds
                    if (i < sourceEndings.Count)
                    {
                        result.Append(sourceEndings[i]);
                    }
                }
            }

            return result.ToString();
        }
        
        /// <summary>
        /// Creates a debug report of the text replacement operation
        /// </summary>
        /// <param name="sourceFile">The original file content</param>
        /// <param name="oldText">The text that was replaced</param>
        /// <param name="newText">The text that was inserted</param>
        /// <param name="lineNumberHint">The line number hint used</param>
        /// <returns>A string containing debug information</returns>
        public string CreateDebugReport(string sourceFile, string oldText, string newText, int lineNumberHint)
        {
            var report = new System.Text.StringBuilder();
            report.AppendLine("=== Text Replacement Debug Report ===");
            report.AppendLine($"Source file length: {sourceFile?.Length ?? 0} characters");
            report.AppendLine($"Old text length: {oldText?.Length ?? 0} characters");
            report.AppendLine($"New text length: {newText?.Length ?? 0} characters");
            report.AppendLine($"Line number hint: {lineNumberHint}");
            return report.ToString();
        }
    }
}