using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.IO;
using SharedClasses;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;
using System.Linq;
using static RoslynHelper;

namespace VSIXTest
{
    public class ShortcutManager
    {
        private readonly DTE2 _dte;

        public ShortcutManager(DTE2 dte)
        {
            _dte = dte;
        }

        public List<string> GetShortcuts(string token)
        {
            var shortcuts = new List<string> { BacktickHelper.PrependHash(":all-open:"), BacktickHelper.PrependHash(":selection:"), BacktickHelper.PrependHash(":diff:") };
            var files = GetAllFilesInSolution();

            foreach (var file in files)
            {
                string fileName = Path.GetFileName(file).ToLower();
                if (fileName.Contains(token.ToLower()))
                {
                    shortcuts.Add($"#{Path.GetFileName(file)}");
                }
            }

            return shortcuts;
        }

        public List<FileWithMembers> GetAllFilesInSolutionWithMembers()
        {
            var filesWithMembers = new List<FileWithMembers>();
            ThreadHelper.ThrowIfNotOnUIThread();

            if (_dte.Solution != null)
            {
                foreach (EnvDTE.Project project in _dte.Solution.Projects)
                {
                    GetProjectFilesWithMembers(project, filesWithMembers);
                }
            }

            return filesWithMembers;
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

        private void GetProjectFilesWithMembers(EnvDTE.Project project, List<FileWithMembers> filesWithMembers)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (project == null)
                return;

            if (project.Kind == ProjectKinds.vsProjectKindSolutionFolder)
            {
                if (project.ProjectItems != null)
                    foreach (ProjectItem item in project.ProjectItems)
                    {
                        if (item.SubProject != null)
                        {
                            GetProjectFilesWithMembers(item.SubProject, filesWithMembers);
                        }
                        else
                        {
                            ProcessProjectItemWithMembers(item, filesWithMembers);
                        }
                    }
            }
            else
            {
                if (project.ProjectItems != null)
                    foreach (ProjectItem item in project.ProjectItems)
                    {
                        ProcessProjectItemWithMembers(item, filesWithMembers);
                    }
            }
        }

        private void ProcessProjectItemWithMembers(ProjectItem item, List<FileWithMembers> filesWithMembers)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (item == null)
                return;

            if (item.Kind == EnvDTE.Constants.vsProjectItemKindPhysicalFolder)
            {
                foreach (ProjectItem subItem in item.ProjectItems)
                {
                    ProcessProjectItemWithMembers(subItem, filesWithMembers);
                }
            }
            else
            {
                if (item.Properties != null)
                {
                    try
                    {
                        string filePath = item.Properties.Item("FullPath").Value.ToString();
                        if (File.Exists(filePath))
                        {
                            string sourceCode = File.ReadAllText(filePath);
                            string fileName = Path.GetFileName(filePath);

                            List<MethodDetail> methods = RoslynHelper.ExtractMethodsUsingRoslyn(sourceCode, fileName);

                            List<Member> members = methods.Select(m => new Member(m.ClassName, m.Namespace)).ToList();

                            filesWithMembers.Add(new FileWithMembers(filePath, members));
                        }
                    }
                    catch (Exception ex)
                    {
                        // Handle or log the exception
                        System.Diagnostics.Debug.WriteLine($"Error processing item: {ex.Message}");
                    }
                }
            }
        }


        public List<string> GetAllFilesInSolution()
        {
            var files = new List<string>();
            ThreadHelper.ThrowIfNotOnUIThread();

            if (_dte.Solution != null)
            {
                foreach (EnvDTE.Project project in _dte.Solution.Projects)
                {
                    GetProjectFiles(project, files);
                }
            }

            return files;
        }

        private void GetProjectFiles(EnvDTE.Project project, List<string> files)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (project == null)
                return;

            if (project.Kind == ProjectKinds.vsProjectKindSolutionFolder)
            {
                if(project.ProjectItems != null)
                    foreach (ProjectItem item in project.ProjectItems)
                    {
                        if (item.SubProject != null)
                        {
                            GetProjectFiles(item.SubProject, files);
                        }
                        else
                        {
                            ProcessProjectItem(item, files);
                        }
                    }
            }
            else
            {
                if (project.ProjectItems != null)
                    foreach (ProjectItem item in project.ProjectItems)
                {
                    ProcessProjectItem(item, files);
                }
            }
        }

        private void ProcessProjectItem(ProjectItem item, List<string> files)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (item == null)
                return;

            if (item.Kind == EnvDTE.Constants.vsProjectItemKindPhysicalFolder)
            {
                foreach (ProjectItem subItem in item.ProjectItems)
                {
                    ProcessProjectItem(subItem, files);
                }
            }
            else
            {
                if (item.Properties != null)
                {
                    try
                    {
                        string filePath = item.Properties.Item("FullPath").Value.ToString();
                        if (File.Exists(filePath))
                        {
                            files.Add(filePath);
                        }
                    }
                    catch (Exception ex)
                    {
                        // Handle or log the exception
                        System.Diagnostics.Debug.WriteLine($"Error processing item: {ex.Message}");
                    }
                }
            }
        }
    }

    public class FileWithMembers
    {
        public string FilePath { get; set; }
        public List<Member> Members { get; set; }

        public FileWithMembers(string filePath, List<Member> members)
        {
            FilePath = filePath;
            Members = members;
        }
    }

    public class Member
    {
        public string Name { get; set; }
        public string Kind { get; set; }

        public Member(string name, string kind)
        {
            Name = name;
            Kind = kind;
        }
    }
}