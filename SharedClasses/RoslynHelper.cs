using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

public static class RoslynHelper
{
    public class Member
    {
        public string Name { get; set; }
        public string Kind { get; set; }

        public string Namespace { get; set; }

        public string SourceCode { get; set; }

        public Member(string name, string kind, string sourceCode, string namespaceStr)
        {
            Name = name;
            Kind = kind;
            SourceCode = sourceCode;
            Namespace = namespaceStr;
        }
    }

    public class MemberDetail
    {
        public string SourceCode { get; set; }
        public string SourceFileName { get; set; }
        public int StartLineNumber { get; set; }
        public string ClassName { get; set; }
        public string Namespace { get; set; }
        public string MemberType { get; set; }
        public string ItemName { get; set; }
    }

    public static List<MemberDetail> ExtractMembersUsingRoslyn(string sourceCode, string sourceFileName)
    {
        var tree = CSharpSyntaxTree.ParseText(sourceCode, path: sourceFileName);
        var root = tree.GetCompilationUnitRoot();

        var members = root.DescendantNodes()
            .Where(node => node is MemberDeclarationSyntax);

        var result = members.Select(m => new MemberDetail
        {
            SourceCode = m.ToFullString(),
            SourceFileName = sourceFileName,
            StartLineNumber = tree.GetLineSpan(m.Span).StartLinePosition.Line + 1,
            ClassName = GetClassName(m),
            Namespace = GetNamespace(m),
            MemberType = GetMemberType(m),
            ItemName = GetItemName(m)
        }).ToList();

        var unknown = result.Where(r => r.MemberType == "Unknown" && r.ClassName == "").ToList();

        result = result.Except(unknown).ToList();

        return result;
    }

    private static string GetClassName(SyntaxNode node)
    {
        var classDeclaration = node.Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault();
        return classDeclaration?.Identifier.Text ?? string.Empty;
    }

    private static string GetNamespace(SyntaxNode node)
    {
        var namespaceDeclaration = node.Ancestors().OfType<NamespaceDeclarationSyntax>().FirstOrDefault();
        return namespaceDeclaration?.Name.ToString() ?? string.Empty;
    }

    private static string GetMemberType(SyntaxNode node)
    {
        if (node is MethodDeclarationSyntax) return "Method";
        if (node is ConstructorDeclarationSyntax) return "Constructor";
        if (node is DestructorDeclarationSyntax) return "Destructor";
        if (node is OperatorDeclarationSyntax) return "Operator";
        if (node is ConversionOperatorDeclarationSyntax) return "ConversionOperator";
        if (node is IndexerDeclarationSyntax) return "Indexer";
        if (node is PropertyDeclarationSyntax) return "Property";
        if (node is FieldDeclarationSyntax) return "Field";
        if (node is EventDeclarationSyntax) return "Event";
        if (node is DelegateDeclarationSyntax) return "Delegate";
        if (node is ClassDeclarationSyntax) return "Class";
        return "Unknown";
    }

    private static string GetItemName(SyntaxNode node)
    {
        switch (node)
        {
            case MethodDeclarationSyntax method:
                return method.Identifier.Text;
            case ConstructorDeclarationSyntax constructor:
                return constructor.Identifier.Text;
            case DestructorDeclarationSyntax destructor:
                return "~" + destructor.Identifier.Text;
            case OperatorDeclarationSyntax op:
                return "operator " + op.OperatorToken.Text;
            case ConversionOperatorDeclarationSyntax convOp:
                return "implicit operator " + convOp.Type.ToString();
            case IndexerDeclarationSyntax indexer:
                return "this[]";
            case PropertyDeclarationSyntax property:
                return property.Identifier.Text;
            case FieldDeclarationSyntax field:
                return string.Join(", ", field.Declaration.Variables.Select(v => v.Identifier.Text));
            case EventDeclarationSyntax evt:
                return evt.Identifier.Text;
            case DelegateDeclarationSyntax del:
                return del.Identifier.Text;
            case ClassDeclarationSyntax cls:
                return cls.Identifier.Text;
            default:
                return string.Empty;
        }
    }
}