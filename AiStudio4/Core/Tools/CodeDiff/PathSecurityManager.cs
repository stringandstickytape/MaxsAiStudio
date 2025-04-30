// AiStudio4.Core\Tools\CodeDiff\PathSecurityManager.cs
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Text;

namespace AiStudio4.Core.Tools.CodeDiff
{
    /// <summary>
    /// Handles path normalization and security checks for CodeDiff operations
    /// </summary>
    public class PathSecurityManager
    {
        private readonly ILogger _logger;
        private readonly string _projectRoot;

        public PathSecurityManager(ILogger logger, string projectRoot)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _projectRoot = projectRoot ?? throw new ArgumentNullException(nameof(projectRoot));
        }

        /// <summary>
        /// Normalizes a file path and checks if it's within the project root directory.
        /// </summary>
        /// <param name="rawFilePath">The raw file path to normalize and check</param>
        /// <param name="errorMessages">StringBuilder to append errors to</param>
        /// <returns>The normalized path if valid, null otherwise</returns>
        public string NormalizeAndValidatePath(string rawFilePath, StringBuilder errorMessages)
        {
            if (string.IsNullOrEmpty(rawFilePath))
            {
                errorMessages.AppendLine($"Error: Received an empty or invalid path for validation.");
                _logger.LogWarning("Received an empty path for validation in NormalizeAndValidatePath.");
                return null;
            }

            try
            {
                string normalizedPath = Path.GetFullPath(rawFilePath);
                
                if (!IsPathWithinProjectRoot(normalizedPath, errorMessages, rawFilePath))
                {
                    return null;
                }
                
                return normalizedPath;
            }
            catch (Exception ex) when (ex is ArgumentException || ex is NotSupportedException || ex is PathTooLongException || ex is System.Security.SecurityException)
            {
                errorMessages.AppendLine($"Error: Invalid or disallowed file path '{rawFilePath}'. {ex.Message}");
                _logger.LogWarning(ex, "Path normalization failed for input path '{RawPath}'", rawFilePath);
                return null;
            }
        }

        /// <summary>
        /// Check if a path is within the project root directory.
        /// Appends error messages to the provided StringBuilder if validation fails.
        /// </summary>
        /// <param name="normalizedPath">The fully normalized path to check.</param>
        /// <param name="errorMessages">StringBuilder to append errors to.</param>
        /// <param name="originalPathForErrorMsg">The original path string provided by the user, for clearer error messages.</param>
        /// <returns>True if path is within root, false otherwise.</returns>
        public bool IsPathWithinProjectRoot(string normalizedPath, StringBuilder errorMessages, string originalPathForErrorMsg)
        {
            // Check if project root itself is set
            if (string.IsNullOrEmpty(_projectRoot))
            {
                if (!errorMessages.ToString().Contains("Project root path is not set")) // Avoid duplicate messages
                {
                    errorMessages.AppendLine("Error: Project root path is not set. Cannot validate file paths.");
                    _logger.LogError("Project root path is not set in PathSecurityManager.");
                }
                return false;
            }

            // Should not happen if called after normalization, but check anyway.
            if (string.IsNullOrEmpty(normalizedPath))
            {
                if (!errorMessages.ToString().Contains("Received an empty path"))
                {
                    errorMessages.AppendLine($"Error: Received an empty or invalid path ('{originalPathForErrorMsg}') for validation.");
                    _logger.LogWarning("Received an empty path ('{OriginalPath}') for validation in IsPathWithinProjectRoot.", originalPathForErrorMsg);
                }
                return false;
            }

            try
            {
                // Normalize root path once (could be done in constructor)
                string normalizedRoot = Path.GetFullPath(_projectRoot);

                // Ensure paths end with separator for accurate StartsWith comparison
                string pathWithSeparator = normalizedPath.EndsWith(Path.DirectorySeparatorChar.ToString()) || normalizedPath.EndsWith(Path.AltDirectorySeparatorChar.ToString())
                   ? normalizedPath
                   : normalizedPath + Path.DirectorySeparatorChar;
                string rootWithSeparator = normalizedRoot.EndsWith(Path.DirectorySeparatorChar.ToString()) || normalizedRoot.EndsWith(Path.AltDirectorySeparatorChar.ToString())
                    ? normalizedRoot
                    : normalizedRoot + Path.DirectorySeparatorChar;

                // Perform case-insensitive comparison
                bool isWithin = pathWithSeparator.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase);

                if (!isWithin)
                {
                    errorMessages.AppendLine($"Security Error: The path '{originalPathForErrorMsg}' (resolves to '{normalizedPath}') is outside the allowed project root '{normalizedRoot}'. Access denied.");
                    _logger.LogWarning("Path validation failed: '{NormalizedPath}' is outside project root '{NormalizedRoot}'. Original input: '{OriginalPath}'", normalizedPath, normalizedRoot, originalPathForErrorMsg);
                }

                return isWithin;
            }
            catch (Exception ex) // Catch unexpected errors during the check itself
            {
                errorMessages.AppendLine($"Error validating path '{originalPathForErrorMsg}': {ex.Message}");
                _logger.LogError(ex, "Unexpected error during IsPathWithinProjectRoot check for path '{OriginalPath}' (Normalized: '{NormalizedPath}')", originalPathForErrorMsg, normalizedPath);
                return false;
            }
        }
        
        /// <summary>
        /// Simplified method to check if a path is safe (within the project root).
        /// This is a convenience wrapper around IsPathWithinProjectRoot.
        /// </summary>
        /// <param name="path">The path to check</param>
        /// <returns>True if the path is within the project root, false otherwise</returns>
        public bool IsPathSafe(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                _logger.LogWarning("Received an empty path for IsPathSafe check");
                return false;
            }
            
            try
            {
                string normalizedPath = Path.GetFullPath(path);
                var errorMessages = new StringBuilder(); // Temporary StringBuilder for errors
                return IsPathWithinProjectRoot(normalizedPath, errorMessages, path);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in IsPathSafe check for path '{Path}'", path);
                return false;
            }
        }
    }
}