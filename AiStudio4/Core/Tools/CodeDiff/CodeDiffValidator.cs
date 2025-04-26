// AiStudio4.Core\Tools\CodeDiff\CodeDiffValidator.cs
using AiStudio4.Core.Tools.CodeDiff.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AiStudio4.Core.Tools.CodeDiff
{
    /// <summary>
    /// Handles validation of CodeDiff changesets
    /// </summary>
    public class CodeDiffValidator
    {
        private readonly ILogger _logger;
        private readonly PathSecurityManager _pathSecurityManager;

        public CodeDiffValidator(ILogger logger, PathSecurityManager pathSecurityManager)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _pathSecurityManager = pathSecurityManager ?? throw new ArgumentNullException(nameof(pathSecurityManager));
        }

        /// <summary>
        /// Groups changes by normalized path and validates the overall changeset consistency.
        /// </summary>
        /// <param name="filesArray">The array of file entries from the changeset</param>
        /// <param name="changesByPath">Dictionary to populate with changes grouped by path</param>
        /// <param name="validationErrorMessages">StringBuilder to populate with validation errors</param>
        /// <returns>True if validation passed, false otherwise</returns>
        public bool GroupAndValidateChanges(JArray filesArray, Dictionary<string, List<JObject>> changesByPath, StringBuilder validationErrorMessages)
        {
            bool validationSuccess = true;
            var pathDetails = new Dictionary<string, PathValidationDetails>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < filesArray.Count; i++)
            {
                var fileObj = filesArray[i] as JObject;
                if (fileObj == null)
                {
                    validationErrorMessages.AppendLine($"Error: Item at index {i} in 'files' array is not a valid JSON object.");
                    validationSuccess = false;
                    continue;
                }

                string rawFilePath = fileObj["path"]?.ToString();
                if (string.IsNullOrEmpty(rawFilePath))
                {
                    validationErrorMessages.AppendLine($"Error: File path is missing or empty for file entry at index {i}.");
                    validationSuccess = false;
                    continue;
                }

                // --- Normalize and Security Check Path ---
                string normalizedPath = _pathSecurityManager.NormalizeAndValidatePath(rawFilePath, validationErrorMessages);
                if (normalizedPath == null)
                {
                    // Error message added within NormalizeAndValidatePath
                    validationSuccess = false;
                    continue; // Don't process changes for invalid paths
                }

                // --- Get or Create Path Details for Validation ---
                if (!pathDetails.TryGetValue(normalizedPath, out var details))
                {
                    details = new PathValidationDetails { FilePath = normalizedPath };
                    pathDetails[normalizedPath] = details;
                    changesByPath[normalizedPath] = new List<JObject>(); // Initialize change list for this path
                }

                // --- Validate and Add Changes for this File Entry ---
                var changes = fileObj["changes"] as JArray;
                if (changes == null || !changes.Any())
                {
                    validationErrorMessages.AppendLine($"Error: No changes specified for file entry {i} ('{rawFilePath}'). Each entry must have at least one change.");
                    validationSuccess = false;
                    continue;
                }

                foreach (var changeToken in changes)
                {
                    var change = changeToken as JObject;
                    if (change == null)
                    {
                        validationErrorMessages.AppendLine($"Error: Change entry is not a valid JSON object for file '{rawFilePath}' (entry {i}).");
                        validationSuccess = false;
                        continue;
                    }

                    string changeType = change["change_type"]?.ToString();
                    if (string.IsNullOrEmpty(changeType) || !IsValidChangeType(changeType))
                    {
                        validationErrorMessages.AppendLine($"Error: Missing or invalid change_type ('{changeType ?? "null"}') for a change in file '{rawFilePath}' (entry {i}).");
                        validationSuccess = false;
                        continue;
                    }

                    // Validate content presence based on type
                    string oldContent = change["oldContent"]?.ToString();
                    string newContent = change["newContent"]?.ToString();

                    bool contentValid = ValidateChangeContent(changeType, oldContent, newContent, normalizedPath, validationErrorMessages);
                    if (!contentValid)
                    {
                        validationSuccess = false;
                        continue; // Don't add invalid change
                    }

                    // If rename, validate the target path as well
                    if (changeType == "renameFile")
                    {
                        string newPathRaw = newContent; // newContent holds the new path for rename
                        string newPathNormalized = _pathSecurityManager.NormalizeAndValidatePath(newPathRaw, validationErrorMessages);
                        if (newPathNormalized == null)
                        {
                            validationSuccess = false;
                            continue;
                        }
                        
                        // Check for rename collision (another file being renamed TO the same target) - basic check here
                        if (pathDetails.Values.Any(pd => pd.RenameTargetPath == newPathNormalized))
                        {
                            validationErrorMessages.AppendLine($"Error: Multiple files are being renamed to the same target path '{newPathNormalized}'.");
                            validationSuccess = false;
                            // Note: More complex collision detection (e.g., file A->B, file C->A) is harder to validate upfront.
                        }
                        details.RenameTargetPath = newPathNormalized; // Store for collision check
                    }

                    // Add valid change to the grouped list
                    changesByPath[normalizedPath].Add(change);

                    // Update validation details for conflict checks
                    if (changeType == "deleteFile") details.HasDelete = true;
                    if (changeType == "renameFile") details.HasRename = true;
                    if (changeType == "replaceFile") details.HasReplace = true;
                    if (changeType == "createnewFile") details.HasCreate = true;
                    if (changeType == "modifyFile") details.HasModify = true;

                } // End foreach change in entry
            } // End for each file entry

            // --- Final Cross-Path Validation ---
            foreach (var kvp in pathDetails)
            {
                string path = kvp.Key;
                var details = kvp.Value;

                // Check for conflicting top-level actions on the SAME path
                int exclusiveActions = (details.HasDelete ? 1 : 0) + (details.HasRename ? 1 : 0) + (details.HasReplace ? 1 : 0) + (details.HasCreate ? 1 : 0);

                if (exclusiveActions > 1)
                {
                    validationErrorMessages.AppendLine($"Error: Conflicting exclusive operations (delete, rename, replace, create) requested for the same file path '{path}'.");
                    validationSuccess = false;
                }
                if (exclusiveActions > 0 && details.HasModify)
                {
                    validationErrorMessages.AppendLine($"Error: Cannot specify 'modifyFile' along with delete, rename, replace, or create for the same file path '{path}'.");
                    validationSuccess = false;
                }

                // Add more checks? E.g., check if a rename target path conflicts with a create/replace path?
                if (details.HasRename && !string.IsNullOrEmpty(details.RenameTargetPath))
                {
                    if (pathDetails.TryGetValue(details.RenameTargetPath, out var targetDetails))
                    {
                        if (targetDetails.HasCreate || targetDetails.HasReplace)
                        {
                            validationErrorMessages.AppendLine($"Error: Rename operation targets path '{details.RenameTargetPath}', which is also targeted by a create or replace operation.");
                            validationSuccess = false;
                        }
                        if (targetDetails.HasDelete)
                        {
                            // Renaming to a path that is also being deleted might be okay, but log warning?
                            _logger.LogWarning("File '{OriginalPath}' is being renamed to '{TargetPath}', which is also marked for deletion.", path, details.RenameTargetPath);
                        }
                    }
                }
            }

            return validationSuccess;
        }

        /// <summary>
        /// Validates required content fields based on change type.
        /// </summary>
        private bool ValidateChangeContent(string changeType, string oldContent, string newContent, string filePath, StringBuilder validationErrorMessages)
        {
            bool isValid = true;
            switch (changeType)
            {
                case "modifyFile":
                    // oldContent is technically required for context matching by the AI, though AI might handle missing.
                    // newContent can be null/empty if the intention is to delete the oldContent lines.
                    if (oldContent == null) // Make oldContent mandatory for modify
                    {
                        validationErrorMessages.AppendLine($"Error: 'oldContent' is required for 'modifyFile' operation on '{filePath}'.");
                        isValid = false;
                    }
                    // newContent can be null, so no check here.
                    break;
                case "createnewFile":
                case "replaceFile":
                    if (newContent == null)
                    {
                        validationErrorMessages.AppendLine($"Error: 'newContent' is required for '{changeType}' operation on '{filePath}'.");
                        isValid = false;
                    }
                    // oldContent is ignored
                    break;
                case "renameFile":
                    if (string.IsNullOrEmpty(newContent)) // newContent holds the new path here
                    {
                        validationErrorMessages.AppendLine($"Error: 'newContent' (the new path) is required and cannot be empty for 'renameFile' operation on '{filePath}'.");
                        isValid = false;
                    }
                    // oldContent is ignored
                    break;
                case "deleteFile":
                    // oldContent and newContent are ignored
                    break;
                default:
                    // Should be caught earlier, but defensive check
                    validationErrorMessages.AppendLine($"Internal Error: Unexpected change type '{changeType}' encountered in ValidateChangeContent for '{filePath}'.");
                    isValid = false;
                    break;
            }
            return isValid;
        }

        /// <summary>
        /// Checks if the change type is valid
        /// </summary>
        private bool IsValidChangeType(string changeType)
        {
            return changeType == "modifyFile" ||
                   changeType == "createnewFile" ||
                   changeType == "replaceFile" ||
                   changeType == "renameFile" ||
                   changeType == "deleteFile";
        }
    }
}