// AiStudio4/Services/DotNetProjectAnalyzerService.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.Extensions.Logging;
using AiStudio4.Services.Interfaces;
using static RoslynHelper;
using System.IO;

namespace AiStudio4.Services
{
    public class DotNetProjectAnalyzerService : IDotNetProjectAnalyzerService
    {
        private readonly ILogger<DotNetProjectAnalyzerService> _logger;

        public DotNetProjectAnalyzerService(ILogger<DotNetProjectAnalyzerService> logger)
        {
            _logger = logger;
        }

        public async Task<Dictionary<string, Dictionary<string, List<string>>>> GetProjectStructureAsync(string projectPath)
        {
            if (!System.IO.File.Exists(projectPath))
            {
                _logger.LogError("Project file not found: {ProjectPath}", projectPath);
                throw new System.IO.FileNotFoundException("Project file not found.", projectPath);
            }



            _logger.LogInformation("Attempting to load project: {ProjectPath}", projectPath);

            // Create an MSBuildWorkspace with C# language support
            var properties = new Dictionary<string, string>
            {
                { "BuildingInsideVisualStudio", "true" },
                { "DesignTimeBuild", "true" }
            };

            var _ = typeof(Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions);

            using (var workspace = MSBuildWorkspace.Create(properties))
            {
                // Register the C# language
                workspace.LoadMetadataForReferencedProjects = true;
                {
                    Project project;
                    try
                    {
                        project = await workspace.OpenProjectAsync(projectPath);
                        _logger.LogInformation("Successfully loaded project: {ProjectName}, Found {DocumentCount} documents.", project.Name, project.Documents.Count());
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error opening project {ProjectPath}: {ErrorMessage}", projectPath, ex.Message);
                        return new Dictionary<string, Dictionary<string, List<string>>>(); // Return empty on failure
                    }

                    var structure = new Dictionary<string, Dictionary<string, List<string>>>();

                    foreach (var document in project.Documents)
                    {
                        if (!document.SupportsSyntaxTree || !document.FilePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                        {
                            _logger.LogInformation("Skipping document (unsupported or not C#): {DocumentName}", document.Name);
                            continue;
                        }

                        _logger.LogInformation("Processing document: {DocumentName}", document.Name);
                        var syntaxTree = await document.GetSyntaxTreeAsync();
                        if (syntaxTree == null)
                        {
                            _logger.LogWarning("Could not get syntax tree for: {DocumentName}", document.Name);
                            continue;
                        }

                        var root = await syntaxTree.GetRootAsync();
                        var classDeclarations = root.DescendantNodes().OfType<ClassDeclarationSyntax>();

                        foreach (var classDecl in classDeclarations)
                        {
                            string className = classDecl.Identifier.Text;
                            string namespaceName = "[Global Namespace]"; // Default

                            var namespaceDecl = classDecl.Parent as NamespaceDeclarationSyntax;
                            if (namespaceDecl != null)
                            {
                                namespaceName = namespaceDecl.Name.ToString();
                            }
                            else if (classDecl.Parent is FileScopedNamespaceDeclarationSyntax fileScopedNamespace)
                            {
                                namespaceName = fileScopedNamespace.Name.ToString();
                            }

                            if (!structure.ContainsKey(namespaceName))
                            {
                                structure[namespaceName] = new Dictionary<string, List<string>>();
                            }

                            if (!structure[namespaceName].ContainsKey(className))
                            {
                                structure[namespaceName][className] = new List<string>();
                            }

                            var methodDeclarations = classDecl.Members.OfType<MethodDeclarationSyntax>();
                            foreach (var methodDecl in methodDeclarations)
                            {
                                string methodName = methodDecl.Identifier.Text;
                                structure[namespaceName][className].Add(methodName);
                            }
                        }
                    }
                    _logger.LogInformation("Finished processing documents for project: {ProjectPath}", projectPath);
                    return structure;
                }
            }
        }
    }
}