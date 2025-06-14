// AiStudio4/Services/DotNetProjectAnalyzerService.cs




using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;

using AiStudio4.Services.Interfaces;

using static RoslynHelper;


namespace AiStudio4.Services
{
    public class DotNetProjectAnalyzerService : IDotNetProjectAnalyzerService
    {
        private readonly ILogger<DotNetProjectAnalyzerService> _logger;
        private readonly IProjectFileWatcherService _projectFileWatcherService;

        public DotNetProjectAnalyzerService(
            ILogger<DotNetProjectAnalyzerService> logger,
            IProjectFileWatcherService projectFileWatcherService)
        {
            _logger = logger;
            _projectFileWatcherService = projectFileWatcherService;
        }

        /// <summary>
        /// Analyzes all C# files in the project directory and extracts their members using Roslyn.
        /// </summary>
        /// <param name="projectPath">The directory path containing the project files</param>
        /// <returns>A list of files with their extracted members</returns>
        public List<FileWithMembers> AnalyzeProjectFiles(string projectPath)
        {
            var filesWithMembers = new List<FileWithMembers>();
            
            // Initialize the file watcher service with the project path if needed
            if (_projectFileWatcherService.ProjectPath != projectPath)
            {
                _projectFileWatcherService.Initialize(projectPath);
            }
            
            // Get all C# files from the file watcher service
            var csFiles = _projectFileWatcherService.Files
                .Where(f => f.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                .Where(f => IsValidFile(f))
                .ToArray();

            _logger.LogInformation("Found {FileCount} C# files to analyze", csFiles.Length);

            foreach (string filePath in csFiles)
            {
                try
                {
                    string sourceCode = File.ReadAllText(filePath);
                    string fileName = Path.GetFileName(filePath);

                    List<MemberDetail> methods = RoslynHelper.ExtractMembersUsingRoslyn(sourceCode, fileName);
                    List<Member> members = methods.Select(m =>
                        new Member(m.ItemName, m.MemberType, m.SourceCode, m.Namespace)).ToList();

                    filesWithMembers.Add(new FileWithMembers(filePath, members));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing file {FilePath}: {ErrorMessage}", filePath, ex.Message);
                }
            }

            return filesWithMembers;
        }

        /// <summary>
        /// Determines if a file should be included in the analysis.
        /// </summary>
        private bool IsValidFile(string filePath)
        {
            // Skip files in bin, obj, and other non-source directories
            string normalizedPath = filePath.Replace('\\', '/');
            return !normalizedPath.Contains("/bin/") && 
                   !normalizedPath.Contains("/obj/") && 
                   !normalizedPath.Contains("/node_modules/") &&
                   !normalizedPath.Contains("/packages/");
        }
    }

    /// <summary>
    /// Represents a file with its extracted members.
    /// </summary>
    public class FileWithMembers
    {
        public string FilePath { get; }
        public List<Member> Members { get; }

        public FileWithMembers(string filePath, List<Member> members)
        {
            FilePath = filePath;
            Members = members;
        }
    }
}
