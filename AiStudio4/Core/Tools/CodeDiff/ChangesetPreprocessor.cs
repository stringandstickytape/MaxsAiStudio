// AiStudio4.Core\Tools\CodeDiff\ChangesetPreprocessor.cs
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace AiStudio4.Core.Tools.CodeDiff
{
    /// <summary>
    /// Preprocesses CodeDiff tool parameters to handle multiple changesets
    /// </summary>
    public class ChangesetPreprocessor
    {
        private readonly ILogger _logger;

        public ChangesetPreprocessor(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Preprocesses the input to handle multiple tool calls merged into one string.
        /// Detects and combines multiple JSON objects into a single changeset.
        /// </summary>
        /// <param name="toolParameters">The original tool parameters string which may contain multiple JSON objects</param>
        /// <returns>A preprocessed JSON string with a single combined changeset</returns>
        public string PreprocessMultipleChangesets(string toolParameters)
        {
            try
            {
                // Check if the input might contain multiple JSON objects
                if (!toolParameters.TrimStart().StartsWith("{") || !toolParameters.TrimEnd().EndsWith("}"))
                {
                    _logger.LogInformation("Input doesn't appear to be a standard JSON object, attempting to parse multiple objects");
                }
                
                // Try to parse as a single object first
                try
                {
                    JObject.Parse(toolParameters);
                    // If parsing succeeds, it's a valid single object, no preprocessing needed
                    return toolParameters;
                }
                catch (JsonException)
                {
                    // Not a single valid JSON object, continue with preprocessing
                    _logger.LogInformation("Input is not a single valid JSON object, attempting to extract multiple changesets");
                }
                
                // Look for patterns that might indicate multiple tool calls
                // Common pattern: {"name":"CodeDiff","args":{...}}{"name":"CodeDiff","args":{...}}
                List<JObject> allChangesets = new List<JObject>();
                Dictionary<string, List<JObject>> combinedFilesByPath = new Dictionary<string, List<JObject>>(StringComparer.OrdinalIgnoreCase);
                string combinedDescription = "Combined multiple changesets: ";
                bool foundAnyChangesets = false;
                
                // Use regex to find JSON objects with the CodeDiff pattern
                var matches = Regex.Matches(
                    toolParameters, 
                    @"\{\s*[""']name[""']\s*:\s*[""']CodeDiff[""']\s*,\s*[""'](?:args|parameters)[""']\s*:\s*(\{[^{}]*(?:\{[^{}]*(?:\{[^{}]*\}[^{}]*)*\}[^{}]*)*\})\s*\}");
                
                foreach (Match match in matches)
                {
                    if (match.Groups.Count < 2) continue;
                    
                    string argsJson = match.Groups[1].Value;
                    try
                    {
                        var args = JObject.Parse(argsJson);
                        var changeset = args["changeset"] as JObject;
                        if (changeset != null)
                        {
                            foundAnyChangesets = true;
                            string desc = changeset["description"]?.ToString() ?? "Unnamed changeset";
                            combinedDescription += desc + "; ";
                            
                            var files = changeset["files"] as JArray;
                            if (files != null)
                            {
                                foreach (JObject fileObj in files.OfType<JObject>())
                                {
                                    string path = fileObj["path"]?.ToString();
                                    if (string.IsNullOrEmpty(path)) continue;
                                    
                                    var changes = fileObj["changes"] as JArray;
                                    if (changes == null || !changes.Any()) continue;
                                    
                                    if (!combinedFilesByPath.TryGetValue(path, out var changesList))
                                    {
                                        changesList = new List<JObject>();
                                        combinedFilesByPath[path] = changesList;
                                    }
                                    
                                    // Add all changes for this file
                                    foreach (JObject change in changes.OfType<JObject>())
                                    {
                                        changesList.Add(change);
                                    }
                                }
                            }
                        }
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogWarning("Failed to parse a potential changeset: {Error}", ex.Message);
                    }
                }
                
                // If we found and processed any changesets, build a new combined one
                if (foundAnyChangesets)
                {
                    _logger.LogInformation("Successfully combined {Count} changesets", matches.Count);
                    
                    // Create combined files array
                    var combinedFiles = new JArray();
                    foreach (var kvp in combinedFilesByPath)
                    {
                        var fileObj = new JObject
                        {
                            ["path"] = kvp.Key,
                            ["changes"] = new JArray(kvp.Value)
                        };
                        combinedFiles.Add(fileObj);
                    }
                    
                    // Create the combined changeset
                    var combinedChangeset = new JObject
                    {
                        ["description"] = combinedDescription.TrimEnd(';', ' '),
                        ["files"] = combinedFiles
                    };
                    
                    // Create the final result
                    var combinedResult = new JObject
                    {
                        ["changeset"] = combinedChangeset
                    };
                    
                    return combinedResult.ToString(Formatting.None);
                }
                
                // If we couldn't find multiple changesets, return the original
                return toolParameters;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error preprocessing multiple changesets, will use original input");
                return toolParameters;
            }
        }
    }
}