using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text;

namespace AiTool3
{




    public class CSharpAnalyzer
    {
        public class MethodInfo
        {
            public string SourceCode { get; set; }
            public string SourceFilename { get; set; }
            public int StartLineNumber { get; set; }
            public string ClassName { get; set; }
            public string Namespace { get; set; }
            public List<string> RelatedMethodsFullName { get; set; }
            public string MethodSignature { get; set; }
        }

        private readonly List<MetadataReference> _references;

        public CSharpAnalyzer()
        {
            _references = new List<MetadataReference>
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location)
            };
        }

        public List<MethodInfo> AnalyzeFiles(List<string> filePaths)
        {
            var methodInfos = new List<MethodInfo>();
            var syntaxTrees = new List<SyntaxTree>();

            // First, parse all files into syntax trees
            foreach (var filePath in filePaths)
            {
                var sourceText = File.ReadAllText(filePath);
                var tree = CSharpSyntaxTree.ParseText(sourceText, path: filePath);
                syntaxTrees.Add(tree);
            }

            // Create compilation with all syntax trees
            var compilation = CreateCompilation(syntaxTrees);

            // Now analyze each syntax tree
            foreach (var tree in syntaxTrees)
            {
                var semanticModel = compilation.GetSemanticModel(tree);
                var root = tree.GetRoot();
                var filePath = tree.FilePath;

                var methods = root.DescendantNodes()
                    .Where(node => node is MethodDeclarationSyntax
                        || node is ConstructorDeclarationSyntax
                        || node is DestructorDeclarationSyntax
                        || node is OperatorDeclarationSyntax
                        || node is ConversionOperatorDeclarationSyntax
                        || node is IndexerDeclarationSyntax);

                foreach (var method in methods)
                {
                    var methodSymbol = semanticModel.GetDeclaredSymbol(method) as IMethodSymbol;
                    if (methodSymbol == null) continue;

                    var methodInfo = new MethodInfo
                    {
                        SourceCode = method.ToFullString(),
                        SourceFilename = Path.GetFileName(filePath),
                        StartLineNumber = method.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                        ClassName = GetClassName(method),
                        Namespace = GetNamespace(method),
                        RelatedMethodsFullName = GetRelatedMethods(method, semanticModel, compilation),
                        MethodSignature = methodSymbol.ToDisplayString()
                    };

                    methodInfos.Add(methodInfo);
                }
            }

            return methodInfos;
        }

        private Compilation CreateCompilation(List<SyntaxTree> syntaxTrees)
        {
            return CSharpCompilation.Create("TempAssembly",
                syntaxTrees,
                _references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        }

        private string GetClassName(SyntaxNode node)
        {
            var classDeclaration = node.Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault();
            return classDeclaration?.Identifier.Text ?? string.Empty;
        }

        private string GetNamespace(SyntaxNode node)
        {
            var namespaceDeclaration = node.Ancestors().OfType<NamespaceDeclarationSyntax>().FirstOrDefault();
            return namespaceDeclaration?.Name.ToString() ?? string.Empty;
        }

        private List<string> GetRelatedMethods(SyntaxNode method, SemanticModel semanticModel, Compilation compilation)
        {
            var relatedMethods = new HashSet<string>();

            foreach (var node in method.DescendantNodes())
            {
                ISymbol symbol = null;

                switch (node)
                {
                    case InvocationExpressionSyntax invocation:
                        symbol = semanticModel.GetSymbolInfo(invocation).Symbol;
                        break;
                    case ObjectCreationExpressionSyntax objectCreation:
                        symbol = semanticModel.GetSymbolInfo(objectCreation).Symbol;
                        break;
                    case MemberAccessExpressionSyntax memberAccess:
                        symbol = semanticModel.GetSymbolInfo(memberAccess).Symbol;
                        break;
                    case IdentifierNameSyntax identifier:
                        symbol = semanticModel.GetSymbolInfo(identifier).Symbol;
                        break;
                }

                if (symbol is IMethodSymbol methodSymbol)
                {
                    var containingType = methodSymbol.ContainingType;
                    var fullName = $"{containingType.ContainingNamespace}.{containingType.Name}.{methodSymbol.Name}";
                    relatedMethods.Add(fullName);
                }
            }

            return relatedMethods.ToList();
        }

        public string GenerateMermaidDiagram(List<MethodInfo> methodInfos)
        {
            // Clone the input list (same as before)
            var copyInfos = new List<MethodInfo>();
            foreach (var info in methodInfos)
            {
                var methodSignatureWithoutParams = info.MethodSignature.Split('(')[0];
                copyInfos.Add(new MethodInfo
                {
                    SourceCode = info.SourceCode,
                    SourceFilename = info.SourceFilename,
                    StartLineNumber = info.StartLineNumber,
                    ClassName = info.ClassName,
                    Namespace = string.IsNullOrEmpty(info.Namespace) ? "NoNamespace" : info.Namespace,
                    RelatedMethodsFullName = new List<string>(info.RelatedMethodsFullName),
                    MethodSignature = methodSignatureWithoutParams
                });
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("graph TD");

            Dictionary<string, string> methodIds = new Dictionary<string, string>();
            Dictionary<string, string> namespaceColors = new Dictionary<string, string>();
            string[] colors = { "#ff0000", "#00ff00", "#0000ff", "#ffa500", "#800080", "#00ffff", "#ff00ff", "#32cd32", "#ffc0cb", "#008080" };
            int colorIndex = 0;

            // Generate unique IDs for each method and assign colors to namespaces
            for (int i = 0; i < copyInfos.Count; i++)
            {
                var method = copyInfos[i];
                string fullName = $"{method.MethodSignature}";
                methodIds[fullName] = $"M{i}";

                if (!namespaceColors.ContainsKey(method.Namespace))
                {
                    namespaceColors[method.Namespace] = colors[colorIndex % colors.Length];
                    colorIndex++;
                }
            }

            // Add nodes for each method
            foreach (var method in copyInfos)
            {
                string fullName = $"{method.MethodSignature}";
                string id = methodIds[fullName];
                sb.AppendLine($"    {id}[\"{method.MethodSignature}\"]");
            }

            // Add links between methods
            foreach (var method in copyInfos)
            {
                string sourceId = methodIds[$"{method.MethodSignature}"];

                foreach (var relatedMethod in method.RelatedMethodsFullName)
                {
                    if (methodIds.TryGetValue(relatedMethod, out string targetId))
                    {
                        sb.AppendLine($"    {sourceId} --> {targetId}");
                    }
                }
            }

            // Add style definitions
            sb.AppendLine("    %% Node styles");
            foreach (var kvp in namespaceColors)
            {
                string safeNamespace = kvp.Key.Replace(".", "_");
                sb.AppendLine($"    style {safeNamespace} fill:{kvp.Value}");
            }

            // Apply styles to nodes
            foreach (var method in copyInfos)
            {
                string id = methodIds[$"{method.MethodSignature}"];
                string safeNamespace = method.Namespace.Replace(".", "_");
                sb.AppendLine($"    class {id} {safeNamespace}");
            }

            // Add link style
            sb.AppendLine("    linkStyle default stroke:#ffff00,stroke-width:2px");

            // Add key
            sb.AppendLine("    subgraph Key");
            foreach (var kvp in namespaceColors)
            {
                string safeNamespace = kvp.Key.Replace(".", "_");
                sb.AppendLine($"        {safeNamespace}[{kvp.Key}]");
                sb.AppendLine($"        style {safeNamespace} fill:{kvp.Value}");
            }
            sb.AppendLine("    end");

            return sb.ToString();
        }
    }
}