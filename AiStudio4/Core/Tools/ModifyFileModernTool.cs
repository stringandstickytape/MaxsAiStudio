// AiStudio4/Core/Tools/ModifyFileModernTool.cs

using AiStudio4.Core.Tools.CodeDiff;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text;

namespace AiStudio4.Core.Tools
{
    [McpServerToolType]
    public class ModifyFileModernTool : BaseToolImplementation
    {
        private PathSecurityManager _pathSecurityManager = null!;

        public ModifyFileModernTool(
            ILogger<ModifyFileModernTool> logger,
            IGeneralSettingsService generalSettingsService,
            IStatusMessageService statusMessageService)
            : base(logger, generalSettingsService, statusMessageService)
        {
        }

        public override Tool GetToolDefinition()
        {
            return new Tool
            {
                Guid = ToolGuids.MODIFY_FILE_MODERN_TOOL_GUID,
                Name = "ModifyFileModern",
                Description = "Applies multiple line-based changes to a single existing file atomically. Each change contains oldContent, newContent, and description. IMPORTANT: Matching and replacement are performed on entire lines only; partial line edits are not supported.",
                Schema = """
{
  "name": "ModifyFileModern",
  "description": "Modifies a single file atomically using an array of line-based changes. If any change fails, the file is reverted and a detailed error is returned with continueProcessing=true.",
  "input_schema": {
    "type": "object",
    "properties": {
      "path": { "type": "string", "description": "Absolute path to the file to modify (must be inside the project root)" },
      "whitespaceTolerant": { "type": "boolean", "description": "If true, match compares lines using TrimEnd() (ignore trailing whitespace). Default: true.", "default": true },
      "strictMultipleMatches": { "type": "boolean", "description": "If true, fail (and revert) when oldContent matches more than once. If false, apply to first and warn. Default: false.", "default": false },
      "changes": {
        "type": "array",
        "description": "Array of line-based changes to apply sequentially to this single file.",
        "items": {
          "type": "object",
          "properties": {
            "oldContent": { "type": "string", "description": "Original content to match on whole-line boundaries only. Should include the full lines to be replaced, plus at least three lines of context either side." },
            "newContent": { "type": "string", "description": "Replacement content written on whole-line boundaries only." },
            "description": { "type": "string", "description": "Human-readable explanation of this change." }
          },
          "required": ["oldContent", "newContent", "description"]
        }
      }
    },
    "required": ["path", "changes"]
  }
}
""",
                Categories = new List<string> { "MaxCode" },
                OutputFileType = "modifyfilemodern",
                Filetype = string.Empty,
                LastModified = DateTime.UtcNow
            };
        }

        [McpServerTool, Description("Applies multiple line-based changes to a single file atomically. Fails and reverts if any change can't be applied.")]
        public async Task<string> ModifyFileModern([Description("JSON parameters for ModifyFileModern")] string parameters = "{}")
        {
            return await ExecuteWithExtraProperties(parameters);
        }

        public override async Task<BuiltinToolResult> ProcessAsync(string toolParameters, Dictionary<string, string> extraProperties)
        {
            _pathSecurityManager = new PathSecurityManager(_logger, _projectRoot);
            JObject parameters;
            try
            {
                parameters = JObject.Parse(toolParameters);
            }
            catch (Exception ex)
            {
                return CreateResult(false, false, $"Invalid JSON parameters: {ex.Message}");
            }

            var path = parameters["path"]?.ToString();
            var changesArray = parameters["changes"] as JArray;
            bool whitespaceTolerant = parameters["whitespaceTolerant"]?.ToObject<bool?>() ?? true;
            bool strictMultipleMatches = parameters["strictMultipleMatches"]?.ToObject<bool?>() ?? false;

            var validation = new StringBuilder();
            if (string.IsNullOrWhiteSpace(path)) validation.AppendLine("Error: 'path' is required.");
            if (changesArray == null || changesArray.Count == 0) validation.AppendLine("Error: 'changes' must be a non-empty array.");

            if (!string.IsNullOrEmpty(path))
            {
                if (!_pathSecurityManager.IsPathSafe(path)) validation.AppendLine($"Error: Path '{path}' is outside the allowed project directory.");
                else if (!File.Exists(path)) validation.AppendLine($"Error: File '{path}' does not exist.");
            }

            if (validation.Length > 0)
            {
                return CreateResult(false, false, validation.ToString());
            }

            // Load original content and make a backup for atomic revert
            string originalContent;
            try
            {
                originalContent = await File.ReadAllTextAsync(path, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                return CreateResult(false, false, $"Failed to read file: {ex.Message}");
            }

            // Detect newline style and whether the file ends with a trailing newline
            var usesCrLf = originalContent.Contains("\r\n");
            var newline = usesCrLf ? "\r\n" : "\n";
            bool hadTrailingNewline = originalContent.EndsWith(newline);

            // Normalize to line arrays without line ending markers
            var lines = SplitLines(originalContent);

            // We'll work on a mutable copy
            var current = new List<string>(lines);

            // Track per-change results
            var changeResults = new JArray();

            // Apply changes one-by-one; if any fails, revert and report with continueProcessing=true
            int index = 0;
            foreach (var changeToken in changesArray!)
            {
                index++;
                var change = (JObject)changeToken;
                var oldContent = change["oldContent"]?.ToString() ?? string.Empty;
                var newContent = change["newContent"]?.ToString() ?? string.Empty;
                var description = change["description"]?.ToString() ?? "(no description)";

                if (string.IsNullOrEmpty(oldContent))
                {
                    var err = $"Change {index} failed: oldContent is empty. Description: {description}";
                    return await RevertWithError(path, originalContent, BuildErrorPayload(err, index, description, 0, null, oldContent));
                }

                // Prepare blocks: match may be whitespace tolerant; insertion should preserve provided whitespace
                var oldBlock = SplitLines(oldContent).Select(l => whitespaceTolerant ? l.TrimEnd() : l).ToList();
                var newBlock = SplitLines(newContent); // do NOT TrimEnd to preserve author's trailing spaces

                var matchIndices = FindWholeLineMatches(current, oldBlock, whitespaceTolerant);
                int matchedCount = matchIndices.Count;

                if (matchedCount == 0)
                {
                    var err = $"Change {index} failed: oldContent not found as whole-line block.";
                    return await RevertWithError(path, originalContent, BuildErrorPayload(err, index, description, 0, null, oldContent));
                }

                if (matchedCount > 1 && strictMultipleMatches)
                {
                    var err = $"Change {index} failed: oldContent matches {matchedCount} times and strictMultipleMatches=true.";
                    return await RevertWithError(path, originalContent, BuildErrorPayload(err, index, description, matchedCount, null, oldContent));
                }

                int appliedAtIndex = matchIndices[0];
                ApplyReplacement(current, appliedAtIndex, oldBlock.Count, newBlock);

                var result = new JObject
                {
                    ["index"] = index,
                    ["description"] = description,
                    ["matchedCount"] = matchedCount,
                    ["appliedAtIndex"] = appliedAtIndex,
                    ["replacedLineCount"] = oldBlock.Count,
                };
                if (matchedCount > 1 && !strictMultipleMatches)
                {
                    result["warning"] = "Multiple matches found; applied to first occurrence.";
                }
                changeResults.Add(result);
            }

            // All changes applied successfully; write back (preserving trailing newline if present)
            try
            {
                var finalContent = string.Join(newline, current);
                if (hadTrailingNewline && (finalContent.Length == 0 || !finalContent.EndsWith(newline)))
                {
                    finalContent += newline;
                }
                await File.WriteAllTextAsync(path, finalContent, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                return await RevertWithError(path, originalContent, BuildErrorPayload($"Failed to write modified file: {ex.Message}", null, null, null, null, null));
            }

            var summary = new JObject
            {
                ["summary"] = new JObject
                {
                    ["path"] = path,
                    ["changeCount"] = changesArray!.Count,
                    ["success"] = true,
                    ["whitespaceTolerant"] = whitespaceTolerant,
                    ["strictMultipleMatches"] = strictMultipleMatches,
                    ["preservedTrailingNewline"] = hadTrailingNewline,
                    ["message"] = "All line-based changes applied successfully."
                },
                ["changes"] = changeResults
            };

            return CreateResult(true, true, summary.ToString(Formatting.Indented), "File modified successfully.");
        }

        private static void ApplyReplacement(List<string> current, int start, int oldCount, List<string> newBlock)
        {
            current.RemoveRange(start, oldCount);
            current.InsertRange(start, newBlock);
        }

        private async Task<BuiltinToolResult> RevertWithError(string path, string originalContent, string error)
        {
            try
            {
                await File.WriteAllTextAsync(path, originalContent, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while reverting file after failure: {Path}", path);
            }

            var output = new JObject
            {
                ["success"] = false,
                ["continueProcessing"] = true,
                ["error"] = error
            };

            return CreateResult(true, false, output.ToString(Formatting.Indented), error);
        }

        private static string BuildErrorPayload(string message, int? index, string? description, int? matchedCount, int? appliedAtIndex, string? oldContentSnippet)
        {
            var obj = new JObject
            {
                ["message"] = message
            };
            if (index.HasValue) obj["changeIndex"] = index.Value;
            if (!string.IsNullOrEmpty(description)) obj["description"] = description;
            if (matchedCount.HasValue) obj["matchedCount"] = matchedCount.Value;
            if (appliedAtIndex.HasValue) obj["appliedAtIndex"] = appliedAtIndex.Value;
            if (!string.IsNullOrEmpty(oldContentSnippet))
            {
                var lines = SplitLines(oldContentSnippet);
                var first = lines.Count > 0 ? lines[0] : string.Empty;
                if (first.Length > 120) first = first.Substring(0, 120);
                obj["oldContentFirstLine"] = first;
                obj["oldContentLineCount"] = lines.Count;
            }
            return obj.ToString(Formatting.None);
        }

        private static List<string> SplitLines(string content)
        {
            // Normalize CRLF and LF to logical lines without retaining line ending markers
            return content.Replace("\r\n", "\n").Split('\n').ToList();
        }

        private static List<int> FindWholeLineMatches(List<string> lines, List<string> block, bool whitespaceTolerant)
        {
            var indices = new List<int>();
            if (block.Count == 0) return indices;
            for (int i = 0; i <= lines.Count - block.Count; i++)
            {
                bool match = true;
                for (int j = 0; j < block.Count; j++)
                {
                    var a = whitespaceTolerant ? (lines[i + j] ?? string.Empty).TrimEnd() : (lines[i + j] ?? string.Empty);
                    var b = whitespaceTolerant ? (block[j] ?? string.Empty).TrimEnd() : (block[j] ?? string.Empty);
                    if (!string.Equals(a, b, StringComparison.Ordinal))
                    {
                        match = false; break;
                    }
                }
                if (match) indices.Add(i);
            }
            return indices;
        }
    }
}