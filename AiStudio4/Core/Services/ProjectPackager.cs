
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



    public interface IProjectPackager
    {

        Task<string> CreatePackageAsync(string projectRootPath, IEnumerable<string> includeExtensions, IEnumerable<string> binaryFileExtensionsToExclude);
    }




    public class ProjectPackager : IProjectPackager
    {
        private readonly ILogger<ProjectPackager> _logger;
        private readonly IGeneralSettingsService _generalSettingsService;

        public ProjectPackager(ILogger<ProjectPackager> logger, IGeneralSettingsService generalSettingsService)
        {
            _logger = logger;
            _generalSettingsService = generalSettingsService;
        }


        public async Task<string> CreatePackageAsync(string projectRootPath, IEnumerable<string> includeExtensions, IEnumerable<string> binaryFileExtensionsToExclude)
        {
            try
            {
                _logger.LogInformation($"Starting project packaging from {projectRootPath}");


                if (string.IsNullOrEmpty(projectRootPath) || !Directory.Exists(projectRootPath))
                {
                    throw new DirectoryNotFoundException($"Project root directory not found: {projectRootPath}");
                }


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


                var xmlDoc = new XDocument(
                    new XElement("projectPackage",
                        new XElement("projectRoot_path", projectRootPath),
                        new XElement("creationTimestamp", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")),
                        new XElement("directoryStructure"),
                        new XElement("files")
                    )
                );


                var directories = GetAllDirectories(projectRootPath, gitIgnoreFilterManager);
                var directoryStructureElement = xmlDoc.Root.Element("directoryStructure");


                directoryStructureElement.Add(new XElement("dir", "/"));


                foreach (var dir in directories)
                {
                    var relativePath = GetRelativePath(dir, projectRootPath);
                    if (!string.IsNullOrEmpty(relativePath))
                    {
                        directoryStructureElement.Add(new XElement("dir", relativePath + "/"));
                    }
                }


                var files = GetAllFiles(projectRootPath, directories, gitIgnoreFilterManager, includeExtensions, binaryFileExtensionsToExclude);
                var filesElement = xmlDoc.Root.Element("files");


                foreach (var file in files)
                {
                    try
                    {
                        var relativePath = GetRelativePath(file, projectRootPath);
                        var content = await File.ReadAllTextAsync(file);
                        content = String.Concat((content.Where(x => x > 31 || x == 10 || x == 13 || x == 9)));

                        var fileElement = new XElement("file",
                            new XAttribute("path", relativePath),
                            new XCData(content)
                        );

                        filesElement.Add(fileElement);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, $"Error reading file {file}: {ex.Message}");

                    }
                }

                _logger.LogInformation($"Project packaging completed. Included {directories.Count} directories and {files.Count} files.");


                return xmlDoc.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating project package: {ex.Message}");
                throw;
            }
        }




        private List<string> GetAllDirectories(string rootPath, GitIgnoreFilterManager gitIgnoreFilter)
        {
            var directories = new List<string>();

            try
            {
                foreach (var dir in Directory.GetDirectories(rootPath, "*", SearchOption.AllDirectories))
                {
                    if (dir.EndsWith("\\.git") || dir.Contains("\\.git\\"))
                        continue;

                    if (gitIgnoreFilter != null && gitIgnoreFilter.PathIsIgnored(dir + Path.DirectorySeparatorChar))
                    {
                        continue;
                    }

                    var lastPathToken = dir.Split("\\").Last();

                    if (_generalSettingsService.CurrentSettings.PackerExcludeFolderNames.Any(x => dir.Contains(x, StringComparison.InvariantCultureIgnoreCase)))
                        continue;

                    directories.Add(dir);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Error accessing directories: {ex.Message}");
            }

            return directories;
        }




        private List<string> GetAllFiles(string rootPath, List<string> directories, GitIgnoreFilterManager gitIgnoreFilter,
            IEnumerable<string> includeExtensions, IEnumerable<string> binaryFileExtensionsToExclude)
        {
            var files = new List<string>();
            var textExtensions = includeExtensions.Select(ext => ext.ToLowerInvariant()).ToList();
            var binaryExtensions = binaryFileExtensionsToExclude.Select(ext => ext.ToLowerInvariant()).ToList();


            AddFilesFromDirectory(rootPath, files, gitIgnoreFilter, textExtensions, binaryExtensions);


            foreach (var dir in directories)
            {
                AddFilesFromDirectory(dir, files, gitIgnoreFilter, textExtensions, binaryExtensions);
            }

            return files;
        }




        private void AddFilesFromDirectory(string directory, List<string> files, GitIgnoreFilterManager gitIgnoreFilter,
            List<string> textExtensions, List<string> binaryExtensions)
        {
            try
            {
                foreach (var file in Directory.GetFiles(directory))
                {

                    if (gitIgnoreFilter != null && gitIgnoreFilter.PathIsIgnored(file))
                    {
                        continue;
                    }

                    var extension = Path.GetExtension(file).ToLowerInvariant();
                    var filename = Path.GetFileName(file);


                    if (binaryExtensions.Contains(extension))
                    {
                        continue;
                    }


                    var excludePatterns = _generalSettingsService.CurrentSettings.PackerExcludeFilenames;
                    if (excludePatterns != null && excludePatterns.Any() && IsFilenameExcluded(filename, excludePatterns))
                    {
                        _logger.LogInformation($"Skipping file {file} due to exclude filename pattern.");
                        continue;
                    }


                    if (textExtensions.Count == 0 || textExtensions.Contains(extension))
                    {

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




        private bool IsFilenameExcluded(string filename, List<string> excludePatterns)
        {
            if (excludePatterns == null || !excludePatterns.Any())
            {
                return false;
            }

            foreach (var pattern in excludePatterns)
            {
                if (string.IsNullOrWhiteSpace(pattern)) continue;




                string regexPattern = "^" + Regex.Escape(pattern).Replace("\\*", ".*") + "$";
                if (Regex.IsMatch(filename, regexPattern, RegexOptions.IgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }




        private bool IsTextFile(string filePath)
        {
            try
            {

                const int sampleSize = 8 * 1024;
                byte[] sampleBytes = new byte[sampleSize];

                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    int bytesRead = stream.Read(sampleBytes, 0, sampleSize);
                    if (bytesRead == 0) return true;


                    if (bytesRead < sampleSize)
                    {
                        Array.Resize(ref sampleBytes, bytesRead);
                    }
                }


                int nullCount = 0;
                foreach (byte b in sampleBytes)
                {
                    if (b == 0) nullCount++;
                }


                return (nullCount * 100.0 / sampleBytes.Length) <= 5.0;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Error checking if file is text: {filePath}");
                return false;
            }
        }




        private string GetRelativePath(string fullPath, string basePath)
        {

            if (!basePath.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                basePath += Path.DirectorySeparatorChar;
            }

            if (fullPath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase))
            {
                var relativePath = fullPath.Substring(basePath.Length);

                return relativePath.Replace("\\", "/");
            }

            return string.Empty;
        }
    }
}
