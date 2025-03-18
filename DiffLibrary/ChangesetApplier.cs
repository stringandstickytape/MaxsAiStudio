using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace DiffLibrary
{
    /// <summary>
    /// Provides functionality to apply changesets to files, handling file modifications, additions, deletions, creation, renaming, and deletion.
    /// </summary>
    public class ChangesetApplier
    {
        private readonly TextReplacer _textReplacer;
        private string _rootPath;

        public StringBuilder Log { get; set; } = new StringBuilder();

        /// <summary>
        /// Initializes a new instance of the ChangesetApplier class.
        /// </summary>
        /// <param name="rootPath">The root directory path for all file operations.</param>
        public ChangesetApplier(string rootPath)
        {
            _textReplacer = new TextReplacer();
            _rootPath = rootPath?.Trim() ?? string.Empty;
        }

        /// <summary>
        /// Sets or updates the root path for file operations.
        /// </summary>
        /// <param name="rootPath">The new root path.</param>
        public void SetRootPath(string rootPath)
        {
            _rootPath = rootPath?.Trim() ?? string.Empty;
        }

        /// <summary>
        /// Processes a JSON changeset string and applies all changes.
        /// </summary>
        /// <param name="jsonContent">The JSON representation of the changeset.</param>
        /// <returns>True if the changeset was applied successfully, false otherwise.</returns>
        /// <exception cref="ArgumentException">Thrown when the input is invalid.</exception>
        /// <exception cref="UnauthorizedAccessException">Thrown when attempting to access files outside the root path.</exception>
        /// <exception cref="FileNotFoundException">Thrown when a required file is not found.</exception>
        /// <exception cref="Exception">Thrown when any other error occurs during processing.</exception>
        public bool ProcessChangeset(string jsonContent)
        {
            if (string.IsNullOrEmpty(_rootPath))
            {
                Log.AppendLine("Root path not specified");
            }

            if (string.IsNullOrEmpty(jsonContent))
            {
                Log.AppendLine("Changeset JSON content is empty");
            }

            ChangesetRoot changeset;

            // Configure Json.NET to be more lenient
            var settings = new JsonSerializerSettings
            {
                // Allow special characters in strings
                StringEscapeHandling = StringEscapeHandling.Default
            };

            // Deserialize with Newtonsoft.Json
            changeset = JsonConvert.DeserializeObject<ChangesetRoot>(jsonContent.Replace("\r\n","\n"), settings);

            if (changeset == null || changeset.changeset == null || changeset.changeset.files == null)
            {
                Log.AppendLine("Invalid changeset format");
            }

            ApplyChangeset(changeset.changeset);
            return true;
        }

        /// <summary>
        /// Applies all changes in a changeset.
        /// </summary>
        /// <param name="changeset">The changeset to apply.</param>
        private void ApplyChangeset(Changeset changeset)
        {
            foreach (var fileChange in changeset.files)
            {
                foreach (var change in fileChange.changes)
                {
                    // Attach the file path from the parent fileChange object
                    ApplyChange(fileChange.path, change);
                }
            }
        }

        /// <summary>
        /// Applies a single change to a file.
        /// </summary>
        /// <param name="path">The path of the file to modify.</param>
        /// <param name="change">The change to apply.</param>
        private void ApplyChange(string path, ChangeItem change)
        {
            switch (change.change_type)
            {
                case "modifyFile":
                    ApplyModification(path, change);
                    break;
                case "addToFile":
                    ApplyAddition(path, change);
                    break;
                case "deleteFromFile":
                    ApplyDeletion(path, change);
                    break;
                case "createnewFile":
                    CreateNewFile(path, change);
                    break;
                case "replaceFile":
                    ReplaceFile(path, change);
                    break;
                case "renameFile":
                    RenameFile(path, change);
                    break;
                case "deleteFile":
                    DeleteFile(path, change);
                    break;
                default:
                    Log.AppendLine($"Change type {change.change_type} is not supported");
                    break;
            }
        }

        /// <summary>
        /// Resolves the full path based on the root path if the provided path is relative.
        /// </summary>
        /// <param name="path">The path to resolve.</param>
        /// <returns>The resolved full path.</returns>
        private string ResolvePath(string path)
        {
            if (path.StartsWith("/"))
                path = path.Substring(1);

            if (Path.IsPathRooted(path))
            {
                return path;
            }
            return Path.Combine(_rootPath, path.Replace('/','\\')); 
        }

        /// <summary>
        /// Applies a modification change to a file.
        /// </summary>
        /// <param name="path">The file path.</param>
        /// <param name="change">The change to apply.</param>
        private void ApplyModification(string path, ChangeItem change)
        {
            if (change.oldContent == change.newContent) return;

            string resolvedPath = ResolvePath(path);
            ValidateAndPrepareFilePath(resolvedPath);

            try
            {
                string fileContent = File.ReadAllText(resolvedPath);
                string updatedContent = _textReplacer.ReplaceTextAtHint(fileContent, change.oldContent, change.newContent, change.lineNumber);

                if (fileContent == updatedContent)
                {
                    Log.AppendLine($"Failed to find matching content at line {change.lineNumber} in file {resolvedPath}");
                }
                else File.WriteAllText(resolvedPath, updatedContent);
            }
            catch(Exception ex)
            {
                Log.AppendLine($"Errored for \r\n\r\n{change.oldContent}\r\n\r\n{change.newContent}");
            }
        }

        /// <summary>
        /// Applies an addition change to a file.
        /// </summary>
        /// <param name="path">The file path.</param>
        /// <param name="change">The change to apply.</param>
        private void ApplyAddition(string path, ChangeItem change)
        {
            string resolvedPath = ResolvePath(path);
            ValidateAndPrepareFilePath(resolvedPath);

            string fileContent = File.ReadAllText(resolvedPath);
            string updatedContent = _textReplacer.ReplaceTextAtHint(fileContent, change.oldContent ?? "", change.oldContent + change.newContent, change.lineNumber);

            if (fileContent == updatedContent)
            {
                Log.AppendLine($"Failed to find matching content at line {change.lineNumber} in file {resolvedPath}");
            }

            File.WriteAllText(resolvedPath, updatedContent);
        }

        /// <summary>
        /// Applies a deletion change to a file.
        /// </summary>
        /// <param name="path">The file path.</param>
        /// <param name="change">The change to apply.</param>
        private void ApplyDeletion(string path, ChangeItem change)
        {
            string resolvedPath = ResolvePath(path);
            ValidateAndPrepareFilePath(resolvedPath);

            string fileContent = File.ReadAllText(resolvedPath);
            string updatedContent = _textReplacer.ReplaceTextAtHint(fileContent, change.oldContent, string.Empty, change.lineNumber);

            if (fileContent == updatedContent)
            {
                Log.AppendLine($"Failed to find matching content at line {change.lineNumber} in file {resolvedPath}");
            }

            File.WriteAllText(resolvedPath, updatedContent);
        }

        /// <summary>
        /// Creates a new file with the specified content.
        /// </summary>
        /// <param name="path">The file path.</param>
        /// <param name="change">The change containing the file details.</param>
        private void CreateNewFile(string path, ChangeItem change)
        {
            string resolvedPath = ResolvePath(path);
            ValidateAndPrepareDirectoryPath(resolvedPath);
            File.WriteAllText(resolvedPath, change.newContent);
        }

        /// <summary>
        /// Renames a file from the old path to a new path.
        /// </summary>
        /// <param name="path">The source file path.</param>
        /// <param name="change">The change containing the file details.</param>
        private void RenameFile(string path, ChangeItem change)
        {
            string resolvedOldPath = ResolvePath(path);
            string resolvedNewPath = ResolvePath(change.newContent);
            
            ValidateAndPrepareFilePath(resolvedOldPath);
            ValidateAndPrepareDirectoryPath(resolvedNewPath);
            
            if (File.Exists(resolvedNewPath))
            {
                Log.AppendLine($"Target file already exists: {resolvedNewPath}");
                File.Delete(resolvedNewPath);
            
            }
            
            File.Move(resolvedOldPath, resolvedNewPath);
        }
        
        /// <summary>
        /// Deletes a file from the filesystem.
        /// </summary>
        /// <param name="path">The file path.</param>
        /// <param name="change">The change containing the file details.</param>
        private void DeleteFile(string path, ChangeItem change)
        {
            string resolvedPath = ResolvePath(path);
            ValidateAndPrepareFilePath(resolvedPath);
            File.Delete(resolvedPath);
        }

        /// <summary>
        /// Replaces an existing file with new content.
        /// </summary>
        /// <param name="path">The file path.</param>
        /// <param name="change">The change containing the file details.</param>
        private void ReplaceFile(string path, ChangeItem change)
        {
            string resolvedPath = ResolvePath(path);
            ValidateAndPrepareDirectoryPath(resolvedPath);
            File.WriteAllText(resolvedPath, change.newContent);
        }

        /// <summary>
        /// Validates a file path and checks if it exists.
        /// </summary>
        /// <param name="filePath">The file path to validate.</param>
        /// <exception cref="UnauthorizedAccessException">Thrown when the path is outside the root directory.</exception>
        /// <exception cref="FileNotFoundException">Thrown when the file doesn't exist.</exception>
        private void ValidateAndPrepareFilePath(string filePath)
        {
            if (!IsPathSafe(filePath))
            {
                Log.AppendLine($"Access denied: Path is outside the root directory: {filePath}");
            }

            if (!File.Exists(filePath))
            {
                Log.AppendLine($"File not found: {filePath}");
            }
        }

        /// <summary>
        /// Validates a path and creates the directory if it doesn't exist.
        /// </summary>
        /// <param name="filePath">The file path to validate.</param>
        /// <exception cref="UnauthorizedAccessException">Thrown when the path is outside the root directory.</exception>
        private void ValidateAndPrepareDirectoryPath(string filePath)
        {
            if (!IsPathSafe(filePath))
            {
                Log.AppendLine($"Access denied: Path is outside the root directory: {filePath}");
            }

            string directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory))
            {
                if (!IsPathSafe(directory))
                {
                    Log.AppendLine($"Access denied: Directory path is outside the root directory: {directory}");
                }

                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
            }
        }

        /// <summary>
        /// Checks if a path is within the root directory.
        /// </summary>
        /// <param name="path">The path to check.</param>
        /// <returns>True if the path is within the root directory, false otherwise.</returns>
        private bool IsPathSafe(string path)
        {
            if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(_rootPath))
                return false;

            // Normalize paths to ensure consistent comparison
            string normalizedFilePath = Path.GetFullPath(path);
            string normalizedRootPath = Path.GetFullPath(_rootPath);

            // Ensure root path ends with directory separator
            if (!normalizedRootPath.EndsWith(Path.DirectorySeparatorChar.ToString()))
                normalizedRootPath += Path.DirectorySeparatorChar;

            // Check if the file path starts with the root path
            return normalizedFilePath.StartsWith(normalizedRootPath, StringComparison.OrdinalIgnoreCase);
        }

        // Classes to deserialize JSON
        public class ChangesetRoot
        {
            public Changeset changeset { get; set; }
        }

        public class Changeset
        {
            public string description { get; set; }
            public List<FileChange> files { get; set; }
        }

        public class FileChange
        {
            public string path { get; set; }
            public List<ChangeItem> changes { get; set; }
        }

        public class ChangeItem
        {
            public string change_type { get; set; }
            public int lineNumber { get; set; }
            public string oldContent { get; set; }
            public string newContent { get; set; }
            public string description { get; set; }
        }
    }
}