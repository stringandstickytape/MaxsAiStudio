// AiStudio4/Core/Services/ProjectPackager.cs
using AiStudio4.InjectedDependencies;
using Microsoft.Extensions.Logging;
using SharedClasses.Git;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace AiStudio4.Core.Services
{
    /// <summary>
    /// Service for packing project source code into a single XML file
    /// </summary>
    public interface IProjectPackager
    {
        /// <summary>
        /// Creates a package of the project source code
        /// </summary>
        /// <param name="projectRootPath">The root path of the project</param>
        /// <param name="includeExtensions">Extensions to include in the package</param>
        /// <param name="binaryFileExtensionsToExclude">Extensions to always exclude as binary</param>
        /// <returns>XML content as a string</returns>
        Task<string> CreatePackageAsync(string projectRootPath, IEnumerable<string> includeExtensions, IEnumerable<string> binaryFileExtensionsToExclude);
    }

    /// <summary>
    /// Implementation of the ProjectPackager service
    /// </summary>
    public class ProjectPackager : IProjectPackager
    {
        private readonly ILogger<ProjectPackager> _logger;
        private readonly IGeneralSettingsService _generalSettingsService;

        public ProjectPackager(ILogger<ProjectPackager> logger, IGeneralSettingsService generalSettingsService)
        {
            _logger = logger;
            _generalSettingsService = generalSettingsService;
        }

        /// <summary>
        /// Creates a package of the project source code
        /// </summary>
        /// <param name="projectRootPath">The root path of the project</param>
        /// <param name="includeExtensions">Extensions to consider as text files</param>
        /// <param name="binaryFileExtensionsToExclude">Extensions to always exclude as binary</param>
        /// <returns>XML content as a string</returns>
        public async Task<string> CreatePackageAsync(string projectRootPath, IEnumerable<string> includeExtensions, IEnumerable<string> binaryFileExtensionsToExclude)
        {
            try
            {
                _logger.LogInformation($"Starting project packaging from {projectRootPath}");

                // Validate project root path
                if (string.IsNullOrEmpty(projectRootPath) || !Directory.Exists(projectRootPath))
                {
                    throw new DirectoryNotFoundException($"Project root directory not found: {projectRootPath}");
                }

                // Initialize GitIgnoreFilterManager
                GitIgnoreFilterManager gitIgnoreFilterManager = null;
                var gitIgnorePath = Path.Combine(projectRootPath, ".gitignore");
                if (File.Exists(gitIgnorePath))
                {
                    var gitignoreContent = await File.ReadAllTextAsync(gitIgnorePath);
                    gitIgnoreFilterManager = new GitIgnoreFilterManager(gitignoreContent, projectRootPath);
                    _logger.LogInformation("Initialized GitIgnoreFilterManager with .gitignore file");
                }
                else
                {
                    _logger.LogWarning(".gitignore file not found, no filtering will be applied");
                }

                // Create XML document
                var xmlDoc = new XDocument(
                    new XElement("projectPackage",
                        new XElement("projectRoot_path", projectRootPath),
                        new XElement("creationTimestamp", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")),
                        new XElement("directoryStructure"),
                        new XElement("files")
                    )
                );

                // Get all directories
                var directories = GetAllDirectories(projectRootPath, gitIgnoreFilterManager);
                var directoryStructureElement = xmlDoc.Root.Element("directoryStructure");

                // Add root directory
                directoryStructureElement.Add(new XElement("dir", "/"));

                // Add all subdirectories (relative paths)
                foreach (var dir in directories)
                {
                    var relativePath = GetRelativePath(dir, projectRootPath);
                    if (!string.IsNullOrEmpty(relativePath))
                    {
                        directoryStructureElement.Add(new XElement("dir", relativePath + "/"));
                    }
                }

                // Get all files
                var files = GetAllFiles(projectRootPath, directories, gitIgnoreFilterManager, includeExtensions, binaryFileExtensionsToExclude);
                var filesElement = xmlDoc.Root.Element("files");

                // Add file contents
                foreach (var file in files)
                {
                    try
                    {
                        var relativePath = GetRelativePath(file, projectRootPath);
                        var content = await File.ReadAllTextAsync(file);
                        content = String.Concat((content.Where(x => x > 31 || x==10||x==13||x==9)));

                        var fileElement = new XElement("file",
                            new XAttribute("path", relativePath),
                            new XCData(content)
                        );

                        filesElement.Add(fileElement);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, $"Error reading file {file}: {ex.Message}");
                        // Continue with next file
                    }
                }

                _logger.LogInformation($"Project packaging completed. Included {directories.Count} directories and {files.Count} files.");

                // Return the XML as a string
                return xmlDoc.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating project package: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Gets all directories in the project, respecting .gitignore rules
        /// </summary>
        private List<string> GetAllDirectories(string rootPath, GitIgnoreFilterManager gitIgnoreFilter)
        {
            var directories = new List<string>();

            try
            {
                foreach (var dir in Directory.GetDirectories(rootPath, "*", SearchOption.AllDirectories))
                {
                    if (dir.EndsWith("\\.git") || dir.Contains("\\.git\\"))
                        continue;
                    // Check if directory is ignored by .gitignore
                    if (gitIgnoreFilter != null && gitIgnoreFilter.PathIsIgnored(dir + Path.DirectorySeparatorChar))
                    {
                        continue;
                    }

                    directories.Add(dir);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Error accessing directories: {ex.Message}");
            }

            return directories;
        }

        /// <summary>
        /// Gets all text files in the project, respecting .gitignore rules
        /// </summary>
        private List<string> GetAllFiles(string rootPath, List<string> directories, GitIgnoreFilterManager gitIgnoreFilter, 
            IEnumerable<string> includeExtensions, IEnumerable<string> binaryFileExtensionsToExclude)
        {
            var files = new List<string>();
            var textExtensions = includeExtensions.Select(ext => ext.ToLowerInvariant()).ToList();
            var binaryExtensions = binaryFileExtensionsToExclude.Select(ext => ext.ToLowerInvariant()).ToList();

            // Add files from root directory
            AddFilesFromDirectory(rootPath, files, gitIgnoreFilter, textExtensions, binaryExtensions);

            // Add files from all subdirectories
            foreach (var dir in directories)
            {
                AddFilesFromDirectory(dir, files, gitIgnoreFilter, textExtensions, binaryExtensions);
            }

            return files;
        }

        /// <summary>
        /// Adds files from a specific directory to the list
        /// </summary>
        private void AddFilesFromDirectory(string directory, List<string> files, GitIgnoreFilterManager gitIgnoreFilter,
            List<string> textExtensions, List<string> binaryExtensions)
        {
            try
            {
                foreach (var file in Directory.GetFiles(directory))
                {
                    // Check if file is ignored by .gitignore
                    if (gitIgnoreFilter != null && gitIgnoreFilter.PathIsIgnored(file))
                    {
                        continue;
                    }

                    var extension = Path.GetExtension(file).ToLowerInvariant();
                    var filename = Path.GetFileName(file);

                    // Skip binary files
                    if (binaryExtensions.Contains(extension))
                    {
                        continue;
                    }

                    // Check if the filename matches any exclude patterns
                    var excludePatterns = _generalSettingsService.CurrentSettings.PackerExcludeFilenames;
                    if (excludePatterns != null && excludePatterns.Any() && IsFilenameExcluded(filename, excludePatterns))
                    {
                        _logger.LogInformation($"Skipping file {file} due to exclude filename pattern.");
                        continue;
                    }

                    // Include if it's in the text extensions list or if the list is empty (include all)
                    if (textExtensions.Count == 0 || textExtensions.Contains(extension))
                    {
                        // Additional check to avoid binary files
                        if (IsTextFile(file))
                        {
                            files.Add(file);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Error accessing files in directory {directory}: {ex.Message}");
            }
        }

        /// <summary>
        /// Checks if a filename matches any of the exclude patterns
        /// </summary>
        private bool IsFilenameExcluded(string filename, List<string> excludePatterns)
        {
            if (excludePatterns == null || !excludePatterns.Any())
            {
                return false;
            }

            foreach (var pattern in excludePatterns)
            {
                if (string.IsNullOrWhiteSpace(pattern)) continue;

                // Convert wildcard pattern to regex
                // Escape regex special characters, then replace '*' with '.*'
                // Match the entire filename case-insensitively
                string regexPattern = "^" + Regex.Escape(pattern).Replace("\\*", ".*") + "$";
                if (Regex.IsMatch(filename, regexPattern, RegexOptions.IgnoreCase))
                {
                    return true; // File matches an exclusion pattern
                }
            }
            return false; // File does not match any exclusion patterns
        }

        /// <summary>
        /// Checks if a file is a text file by reading a sample
        /// </summary>
        private bool IsTextFile(string filePath)
        {
            try
            {
                // Read the first 8KB of the file
                const int sampleSize = 8 * 1024;
                byte[] sampleBytes = new byte[sampleSize];

                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    int bytesRead = stream.Read(sampleBytes, 0, sampleSize);
                    if (bytesRead == 0) return true; // Empty file, consider it text

                    // Resize array to actual bytes read
                    if (bytesRead < sampleSize)
                    {
                        Array.Resize(ref sampleBytes, bytesRead);
                    }
                }

                // Check for null bytes (common in binary files)
                int nullCount = 0;
                foreach (byte b in sampleBytes)
                {
                    if (b == 0) nullCount++;
                }

                // If more than 5% of the bytes are null, consider it binary
                return (nullCount * 100.0 / sampleBytes.Length) <= 5.0;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Error checking if file is text: {filePath}");
                return false; // Assume binary on error
            }
        }

        /// <summary>
        /// Gets a path relative to the project root
        /// </summary>
        private string GetRelativePath(string fullPath, string basePath)
        {
            // Ensure paths end with directory separator for proper substring operation
            if (!basePath.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                basePath += Path.DirectorySeparatorChar;
            }

            if (fullPath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase))
            {
                var relativePath = fullPath.Substring(basePath.Length);
                // Convert backslashes to forward slashes for XML consistency
                return relativePath.Replace("\\", "/");
            }

            return string.Empty;
        }
    }
}