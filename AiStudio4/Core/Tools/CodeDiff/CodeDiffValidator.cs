
using AiStudio4.Core.Tools.CodeDiff.Models;







namespace AiStudio4.Core.Tools.CodeDiff
{
    
    
    
    public class CodeDiffValidator
    {
        private readonly ILogger _logger;
        private readonly PathSecurityManager _pathSecurityManager;

        public CodeDiffValidator(ILogger logger, PathSecurityManager pathSecurityManager)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _pathSecurityManager = pathSecurityManager ?? throw new ArgumentNullException(nameof(pathSecurityManager));
        }

        
        
        
        
        
        
        
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

                
                string normalizedPath = _pathSecurityManager.NormalizeAndValidatePath(rawFilePath, validationErrorMessages);
                if (normalizedPath == null)
                {
                    
                    validationSuccess = false;
                    continue; 
                }

                
                if (!pathDetails.TryGetValue(normalizedPath, out var details))
                {
                    details = new PathValidationDetails { FilePath = normalizedPath };
                    pathDetails[normalizedPath] = details;
                    changesByPath[normalizedPath] = new List<JObject>(); 
                }

                
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

                    
                    string oldContent = change["oldContent"]?.ToString();
                    string newContent = change["newContent"]?.ToString();

                    bool contentValid = ValidateChangeContent(changeType, oldContent, newContent, normalizedPath, validationErrorMessages);
                    if (!contentValid)
                    {
                        validationSuccess = false;
                        continue; 
                    }

                    
                    if (changeType == "renameFile")
                    {
                        string newPathRaw = newContent; 
                        string newPathNormalized = _pathSecurityManager.NormalizeAndValidatePath(newPathRaw, validationErrorMessages);
                        if (newPathNormalized == null)
                        {
                            validationSuccess = false;
                            continue;
                        }
                        
                        
                        if (pathDetails.Values.Any(pd => pd.RenameTargetPath == newPathNormalized))
                        {
                            validationErrorMessages.AppendLine($"Error: Multiple files are being renamed to the same target path '{newPathNormalized}'.");
                            validationSuccess = false;
                            
                        }
                        details.RenameTargetPath = newPathNormalized; 
                    }

                    
                    changesByPath[normalizedPath].Add(change);

                    
                    if (changeType == "deleteFile") details.HasDelete = true;
                    if (changeType == "renameFile") details.HasRename = true;
                    if (changeType == "replaceFile") details.HasReplace = true;
                    if (changeType == "createnewFile") details.HasCreate = true;
                    if (changeType == "modifyFile") details.HasModify = true;

                } 
            } 

            
            foreach (var kvp in pathDetails)
            {
                string path = kvp.Key;
                var details = kvp.Value;

                
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
                            
                            _logger.LogWarning("File '{OriginalPath}' is being renamed to '{TargetPath}', which is also marked for deletion.", path, details.RenameTargetPath);
                        }
                    }
                }
            }

            return validationSuccess;
        }

        
        
        
        private bool ValidateChangeContent(string changeType, string oldContent, string newContent, string filePath, StringBuilder validationErrorMessages)
        {
            bool isValid = true;
            switch (changeType)
            {
                case "modifyFile":
                    
                    
                    if (oldContent == null) 
                    {
                        validationErrorMessages.AppendLine($"Error: 'oldContent' is required for 'modifyFile' operation on '{filePath}'.");
                        isValid = false;
                    }
                    
                    break;
                case "createnewFile":
                case "replaceFile":
                    if (newContent == null)
                    {
                        validationErrorMessages.AppendLine($"Error: 'newContent' is required for '{changeType}' operation on '{filePath}'.");
                        isValid = false;
                    }
                    
                    break;
                case "renameFile":
                    if (string.IsNullOrEmpty(newContent)) 
                    {
                        validationErrorMessages.AppendLine($"Error: 'newContent' (the new path) is required and cannot be empty for 'renameFile' operation on '{filePath}'.");
                        isValid = false;
                    }
                    
                    break;
                case "deleteFile":
                    
                    break;
                default:
                    
                    validationErrorMessages.AppendLine($"Internal Error: Unexpected change type '{changeType}' encountered in ValidateChangeContent for '{filePath}'.");
                    isValid = false;
                    break;
            }
            return isValid;
        }

        
        
        
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
