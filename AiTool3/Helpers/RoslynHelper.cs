using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

internal static class RoslynHelper
{

    public class MethodDetail
    {
        public string SourceCode { get; set; }
        public string SourceFileName { get; set; }
        public int StartLineNumber { get; set; }
        public string ClassName { get; set; }
        public string Namespace { get; set; }



    }

    public static List<MethodDetail> ExtractMethodsUsingRoslyn(string sourceCode, string sourceFileName)
    {
        var tree = CSharpSyntaxTree.ParseText(sourceCode, path: sourceFileName);
        var root = tree.GetCompilationUnitRoot();

        var methodLikeMembers = root.DescendantNodes()
            .Where(node => node is MethodDeclarationSyntax
                        || node is ConstructorDeclarationSyntax
                        || node is DestructorDeclarationSyntax
                        || node is OperatorDeclarationSyntax
                        || node is ConversionOperatorDeclarationSyntax
                        || node is IndexerDeclarationSyntax);

        var properties = root.DescendantNodes()
            .OfType<PropertyDeclarationSyntax>()
            .ToList();

        var result = methodLikeMembers.Select(m => new MethodDetail
        {
            SourceCode = m.ToFullString(),
            SourceFileName = sourceFileName,
            StartLineNumber = tree.GetLineSpan(m.Span).StartLinePosition.Line + 1,
            ClassName = GetClassName(m),
            Namespace = GetNamespace(m)
        }).ToList();

        if (properties.Any())
        {
            var propertiesDetail = new MethodDetail
            {
                SourceCode = string.Join(Environment.NewLine, properties.Select(p => p.ToFullString())),
                SourceFileName = sourceFileName,
                StartLineNumber = tree.GetLineSpan(properties.First().Span).StartLinePosition.Line + 1,
                ClassName = GetClassName(properties.First()),
                Namespace = GetNamespace(properties.First())
            };
            result.Add(propertiesDetail);
        }

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
}