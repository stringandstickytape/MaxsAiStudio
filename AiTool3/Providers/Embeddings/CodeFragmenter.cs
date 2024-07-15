
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
public class CodeFragmenter
{
    public List<CodeFragment> FragmentCode(string fileContent, string filePath)
    {

        var a = RoslynHelper.ExtractMethodsUsingRoslyn(fileContent, filePath);

        return a.Select(x => new CodeFragment
        {
            Content = x.SourceCode,
            Type = "Method",
            FilePath = x.SourceFileName,
            LineNumber = x.StartLineNumber,
            Class = x.ClassName,
            Namespace = x.Namespace
        }).ToList();

    }

    private void AddFragment(List<CodeFragment> fragments, string content, string type, string filePath, int lineNumber, string className, string namespaceName)
    {
        // strip path from filename
        fragments.Add(new CodeFragment
        {
            Content = content,
            Type = type,
            FilePath = filePath.Split('\\').Last(),
            LineNumber = lineNumber,
            Class = className,
            Namespace = namespaceName
        });
    }
}

public class CodeFragment
{
    public string Content { get; set; }
    public string Type { get; set; }
    public string FilePath { get; set; }
    public int LineNumber { get; set; }

    public string Class { get; set; }
    public string Namespace { get; set; }
}