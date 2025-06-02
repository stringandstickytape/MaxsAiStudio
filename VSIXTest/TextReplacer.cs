using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace VSIXTest
{
    
    
    
    public class TextReplacer
    {
        
        
        
        
        
        
        
        
        public string ReplaceTextAtHint(string sourceFile, string oldText, string newText, int lineNumberHint)
        {
            if (string.IsNullOrEmpty(sourceFile))
                return sourceFile;

            
            var sourceTextInfo = SplitTextPreservingNewlines(sourceFile);
            var oldTextInfo = SplitTextPreservingNewlines(oldText);
            var newTextInfo = SplitTextPreservingNewlines(newText);
            
            
            System.Diagnostics.Debug.WriteLine($"Source text has {sourceTextInfo.Lines.Count} lines");
            System.Diagnostics.Debug.WriteLine($"Old text has {oldTextInfo.Lines.Count} lines");

            if(string.IsNullOrEmpty(oldText))
            {
                sourceTextInfo.Lines.InsertRange(lineNumberHint-1, newTextInfo.Lines);
                return string.Join("\n", sourceTextInfo.Lines);
                
            }

            
            if (sourceTextInfo.Lines.Count == 0)
            {
                return sourceFile; 
            }

            
            int zeroBasedLineHint = Math.Max(0, lineNumberHint - 1);

            
            zeroBasedLineHint = Math.Min(zeroBasedLineHint, sourceTextInfo.Lines.Count - 1);

            
            int matchPosition = FindMatchPosition(sourceTextInfo.Lines, oldTextInfo.Lines, zeroBasedLineHint);

            
            if (matchPosition < 0)
                return sourceFile;

            
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

        
        
        
        private class TextInfo
        {
            public List<string> Lines { get; } = new List<string>();
            public List<string> LineEndings { get; } = new List<string>();
            public bool EndsWithNewline { get; set; }
        }

        
        
        
        private TextInfo SplitTextPreservingNewlines(string text)
        {
            var result = new TextInfo();

            if (string.IsNullOrEmpty(text))
                return result;

            
            var matches = Regex.Matches(text, @"([^\r\n]*)(\r\n|\n|\r|$)");

            foreach (Match match in matches)
            {
                string line = match.Groups[1].Value;
                string ending = match.Groups[2].Value;

                
                if (match.Index + match.Length == text.Length && string.IsNullOrEmpty(line) && string.IsNullOrEmpty(ending))
                    break;

                result.Lines.Add(line);
                result.LineEndings.Add(ending);

                
                if (match.Index + match.Length == text.Length)
                {
                    result.EndsWithNewline = !string.IsNullOrEmpty(ending) && ending != "$";
                }
            }

            return result;
        }

        
        
        
        private int FindMatchPosition(List<string> sourceLines, List<string> oldLines, int lineHint)
        {
            if (oldLines.Count == 0 || sourceLines.Count == 0)
                return lineHint;

            
            if (oldLines.Count == 1 && oldLines[0] == "")
                return Math.Min(lineHint, sourceLines.Count);

            int maxOffset = Math.Max(sourceLines.Count, lineHint);

            for (int offset = 0; offset <= maxOffset; offset++)
            {
                
                int upperLine = lineHint + offset;
                if (upperLine < sourceLines.Count && MatchesAtPosition(sourceLines, oldLines, upperLine))
                {
                    return upperLine;
                }

                
                int lowerLine = lineHint - offset;
                if (offset > 0 && lowerLine >= 0 && MatchesAtPosition(sourceLines, oldLines, lowerLine))
                {
                    return lowerLine;
                }

                
                if ((upperLine >= sourceLines.Count) && (lowerLine < 0))
                {
                    break;
                }
            }

            return -1; 
        }

        
        
        
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

            
            for (int i = 0; i < matchPosition; i++)
            {
                result.Append(sourceLines[i]);
                result.Append(sourceEndings[i]);
            }

            
            for (int i = 0; i < newLines.Count; i++)
            {
                result.Append(newLines[i]);

                
                if (i == newLines.Count - 1)
                {
                    
                    
                    if (matchPosition + oldLinesCount < sourceLines.Count || sourceEndsWithNewline)
                    {
                        
                        string lineEnding = (matchPosition + oldLinesCount - 1 < sourceEndings.Count)
                            ? sourceEndings[matchPosition + oldLinesCount - 1]
                            : (sourceEndings.Count > 0 ? sourceEndings[0] : "\n");

                        result.Append(lineEnding);
                    }
                }
                else if (i < newEndings.Count)
                {
                    
                    result.Append(newEndings[i]);
                }
            }

            
            for (int i = matchPosition + oldLinesCount; i < sourceLines.Count; i++)
            {
                result.Append(sourceLines[i]);

                
                if (i < sourceLines.Count - 1 || sourceEndsWithNewline)
                {
                    
                    if (i < sourceEndings.Count)
                    {
                        result.Append(sourceEndings[i]);
                    }
                }
            }

            return result.ToString();
        }
        
        
        
        
        
        
        
        
        
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
