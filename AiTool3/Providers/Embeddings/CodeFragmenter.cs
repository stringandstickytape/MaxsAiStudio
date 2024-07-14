
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

public class CodeFragmenter
{
    public List<CodeFragment> FragmentCode(string fileContent, string filePath)
    {
        var fragments = new List<CodeFragment>();
        var lines = fileContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

        var currentMethod = new List<string>();
        var currentBlock = new List<string>();
        var bracketCount = 0;
        var inMethod = false;
        var currentClassName = "";


        int startline = 0;

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();

            // Skip empty lines and comments
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("//"))
                continue;

            // Check for class definition

            // Check for method start
            if (Regex.IsMatch(line, @"^\s*(public|private|protected|internal|static)?\s*\w+(\s+\w+)?\s*\(.*\)\s*{?\s*$"))
            {
                if (currentMethod.Count > 0)
                {
                    AddFragment(fragments, string.Join("\n", currentMethod), "Method", filePath, startline);
                    currentMethod.Clear();
                    currentBlock.Clear();
                    startline = i;
                }
                inMethod = true;
            }

            currentMethod.Add(lines[i]); // Use original line to preserve indentation
            currentBlock.Add(lines[i]); // Use original line to preserve indentation

            // Count brackets to track blocks
            bracketCount += line.Count(c => c == '{') - line.Count(c => c == '}');

            // Check for end of block or statement
            if ((bracketCount == 0 && line.EndsWith("}")))
            {
                if (currentBlock.Count > 1)
                {
                    AddFragment(fragments, string.Join("\n", currentBlock), "Block", filePath, startline);
                }
                else   AddFragment(fragments, lines[i], "Statement", filePath, i); // Use original line
                currentBlock.Clear();
                currentMethod.Clear();
                startline = i;
                inMethod = false;
            }

            // Check for end of method
            if (inMethod && bracketCount == 0 && line.EndsWith("}"))
            {
                AddFragment(fragments, string.Join("\n", currentMethod), "Method", filePath, startline);

                currentBlock.Clear();
                currentMethod.Clear();
                startline = i;
                inMethod = false;
            }
        }

        // Add any remaining method
        if (currentMethod.Count > 0)
        {
            AddFragment(fragments, string.Join("\n", currentMethod), "Method", filePath, startline);
        }

        return fragments;
    }

    private void AddFragment(List<CodeFragment> fragments, string content, string type, string filePath, int lineNumber)
    {
        // strip path from filename
        fragments.Add(new CodeFragment
        {
            Content = content,
            Type = type,
            FilePath = filePath.Split('\\').Last(),
            LineNumber = lineNumber
        });
    }

}

public class CodeFragment
{
    public string Content { get; set; }
    public string Type { get; set; }
    public string FilePath { get; set; }
    public int LineNumber { get; set; }
}