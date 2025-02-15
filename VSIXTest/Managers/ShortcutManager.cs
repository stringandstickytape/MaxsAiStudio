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
using SharedClasses.Git;
using System.Windows.Forms;

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
                        if (File.Exists(filePath) && IsValidFile(filePath))
                        {
                            string sourceCode = File.ReadAllText(filePath);
                            string fileName = Path.GetFileName(filePath);

                            List<MemberDetail> methods = RoslynHelper.ExtractMembersUsingRoslyn(sourceCode, fileName);

                            List<Member> members = methods.Select(m => new Member(m.ItemName, m.MemberType, m.SourceCode)).ToList();

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

       

        private string GetActiveFolderPath()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            // Try to get the active folder from Solution Explorer
            if (_dte.ToolWindows.SolutionExplorer.SelectedItems is Array selectedItems && selectedItems.Length > 0)
            {
                dynamic selected = selectedItems.GetValue(0);
                if (selected?.Object is ProjectItem item)
                {
                    try
                    {
                        return item.Properties.Item("FullPath").Value.ToString();
                    }
                    catch { }
                }
            }

            // Fallback to active document's folder
            if (_dte.ActiveDocument != null)
            {
                return Path.GetDirectoryName(_dte.ActiveDocument.FullName);
            }

            return null;
        }


        private void GetProjectFiles(EnvDTE.Project project, List<string> files)
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

            // ProjectKinds.vsProjectKindSolutionFolder

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
                        if (File.Exists(filePath) && IsValidFile(filePath))
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

        public List<string> GetAllFilesInSolution()
        {
            var files = new List<string>();
            ThreadHelper.ThrowIfNotOnUIThread();

            if (_dte.Solution != null && _dte.Solution.IsOpen)
            {


                // Check if it's a folder-based solution
                if (IsFolderBasedSolution())
                {
                    // If it's a folder-based solution
                    string rootFolder = GetRootFolder();
                    if (!string.IsNullOrEmpty(rootFolder))
                    {
                        files.AddRange(Directory.GetFiles(rootFolder, "*.*", SearchOption.AllDirectories)
                            .Where(f => IsValidFile(f)));

                        // check for a .gitignore
                        if (File.Exists(Path.Combine(rootFolder, ".gitignore")))
                        {
                            var gitignore = File.ReadAllText(Path.Combine(rootFolder, ".gitignore"));
                            var gitIgnoreFilterManager = new GitIgnoreFilterManager(gitignore);

                            if (gitignore != null)
                            {
                                files = gitIgnoreFilterManager.FilterNonIgnoredPaths(files).ToList();
                            }
                        }

                    }
                }
                else
                {
                    // If it's a normal solution - use existing approach
                    foreach (EnvDTE.Project project in _dte.Solution.Projects)
                    {
                        GetProjectFiles(project, files);
                    }

                    var solutionRoot = Path.GetDirectoryName(_dte.Solution.FullName);

                    if (File.Exists(Path.Combine(solutionRoot, ".gitignore")))
                    {
                        var gitignore = File.ReadAllText(Path.Combine(solutionRoot, ".gitignore"));
                        var gitIgnoreFilterManager = new GitIgnoreFilterManager(gitignore);

                        if (gitignore != null)
                        {
                            files = gitIgnoreFilterManager.FilterNonIgnoredPaths(files).ToList();
                        }
                    }
                }
            }

            return files;
        }

        private string GetRootFolder()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                // Attempt to get the solution path, which will be the root folder in the "open folder" case
                if (_dte.Solution != null && !string.IsNullOrEmpty(_dte.Solution.FullName))
                {
                    string solutionDir = _dte.Solution.FullName;
                    if (!string.IsNullOrEmpty(solutionDir) && Directory.Exists(solutionDir))
                    {
                        return solutionDir;
                    }
                }

                // Fallback if solution directory is not found
                if (_dte.ActiveDocument != null)
                {
                    return Path.GetDirectoryName(_dte.ActiveDocument.FullName);
                }

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting root folder: {ex.Message}");
            }

            return null;
        }

        private bool IsFolderBasedSolution()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            // Check if the solution file is an "empty" project file or a folder
            if (_dte.Solution != null && !string.IsNullOrEmpty(_dte.Solution.FullName))
            {
                string solutionPath = _dte.Solution.FullName;
                // If it's a folder-based solution, the solution file has no extension or an "empty" extension.
                return string.IsNullOrEmpty(Path.GetExtension(solutionPath));
            }

            return false;
        }

        private bool IsValidFile(string filePath)
        {
            // Add your file extension filters here
            string[] validExtensions = new string[0] { /* ".cs", ".vb", ".ts", ".js" */ }; // modify as needed

            // Check if it's a valid extension
            //if (!validExtensions.Contains(Path.GetExtension(filePath).ToLower()))
            //    return false;

            // Exclude binary folders and hidden folders
            string[] excludeFolders = new[] {
        "\\bin\\",
        "\\obj\\",
        "\\.git\\",
        "\\.vs\\",
        "\\node_modules\\"
    };

            if (excludeFolders.Any(f => filePath.IndexOf(f, StringComparison.OrdinalIgnoreCase) >= 0))
                return false;

            return true;
        }
        // Repeat for GetAllFilesInSolutionWithMembers
        public List<FileWithMembers> GetAllFilesInSolutionWithMembers()
        {
            var filesWithMembers = new List<FileWithMembers>();
            ThreadHelper.ThrowIfNotOnUIThread();

            if (_dte.Solution != null && _dte.Solution.IsOpen)
            {
                // Check if it's a folder-based solution
                if (IsFolderBasedSolution())
                {
                    string rootFolder = GetRootFolder();
                    if (!string.IsNullOrEmpty(rootFolder))
                    {
                        var csFiles = Directory.GetFiles(rootFolder, "*.*", SearchOption.AllDirectories);
                            csFiles = csFiles.Where(f => IsValidFile(f)).ToArray();

                        foreach (string filePath in csFiles)
                        {
                            try
                            {
                                string sourceCode = File.ReadAllText(filePath);
                                string fileName = Path.GetFileName(filePath);

                                List<MemberDetail> methods = RoslynHelper.ExtractMembersUsingRoslyn(sourceCode, fileName);
                                List<Member> members = methods.Select(m =>
                                    new Member(m.ItemName, m.MemberType, m.SourceCode)).ToList();

                                filesWithMembers.Add(new FileWithMembers(filePath, members));
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"Error processing file {filePath}: {ex.Message}");
                            }
                        }
                    }
                }
                else
                {
                    // Solution is open - use existing approach
                    foreach (EnvDTE.Project project in _dte.Solution.Projects)
                    {
                        GetProjectFilesWithMembers(project, filesWithMembers);
                    }
                }
            }

            return filesWithMembers;
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

        public string SourceCode { get; set; }

        public Member(string name, string kind, string sourceCode)
        {
            Name = name;
            Kind = kind;
            SourceCode = sourceCode;
        }
    }
}