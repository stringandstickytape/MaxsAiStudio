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
                Description = "Purely programmatic and relatively simple approach.  Applies multiple line-based changes to a single existing file atomically. Each change contains oldContent, newContent, and description. IMPORTANT: Matching and replacement are performed on entire lines only; partial line edits are not supported.",
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
      "applyAllOccurrences": { "type": "boolean", "description": "If true, replaces all occurrences of oldContent blocks; otherwise only the first match is replaced.", "default": false },
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
                return CreateResult(false, true, $"Invalid JSON parameters: {ex.Message}");
            }

            var path = parameters["path"]?.ToString();
            var changesArray = parameters["changes"] as JArray;
            bool whitespaceTolerant = parameters["whitespaceTolerant"]?.ToObject<bool?>() ?? true;
            bool strictMultipleMatches = parameters["strictMultipleMatches"]?.ToObject<bool?>() ?? false;
            bool applyAllOccurrences = parameters["applyAllOccurrences"]?.ToObject<bool?>() ?? false;

            var validation = new StringBuilder();
            if (string.IsNullOrWhiteSpace(path)) validation.AppendLine("Error: 'path' is required.");
            if (changesArray == null || changesArray.Count == 0) validation.AppendLine("Error: 'changes' must be a non-empty array.");

            if (!string.IsNullOrEmpty(path))
            {
                if (!Path.IsPathRooted(path)) validation.AppendLine($"Error: Path '{path}' must be absolute.");
                else if (!_pathSecurityManager.IsPathSafe(path)) validation.AppendLine($"Error: Path '{path}' is outside the allowed project directory.");
                else if (!File.Exists(path)) validation.AppendLine($"Error: File '{path}' does not exist.");
            }

            if (validation.Length > 0)
            {
                return CreateResult(false, true, validation.ToString());
            }

            // Concurrency guard per file
            var fileLock = FileLockProvider.GetLockForPath(path!);
            lock (fileLock)
            {
                return ProcessWithLock(path!, changesArray!, whitespaceTolerant, strictMultipleMatches, applyAllOccurrences);
            }
        }

        private BuiltinToolResult ProcessWithLock(string path, JArray changesArray, bool whitespaceTolerant, bool strictMultipleMatches, bool applyAllOccurrences)
        {
            // Load original content and detect encoding/BOM
            string originalContent;
            Encoding encodingUsed;
            try
            {
                using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                (originalContent, encodingUsed) = ReadAllTextWithBom(fs);
            }
            catch (Exception ex)
            {
                return CreateResult(false, true, $"Failed to read file: {ex.Message}");
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
                    var details = BuildErrorObject($"Change {index} failed: oldContent is empty.", index, description, 0, null, oldContent);
                    return RevertWithError(path, originalContent, details, encodingUsed).GetAwaiter().GetResult();
                }

                // Prepare blocks: match may be whitespace tolerant; insertion should preserve provided whitespace
                var oldBlock = SplitLines(oldContent).Select(l => whitespaceTolerant ? l.TrimEnd() : l).ToList();
                var newBlock = SplitLines(newContent); // do NOT TrimEnd to preserve author's trailing spaces

                var matchIndices = FindWholeLineMatches(current, oldBlock, whitespaceTolerant);
                int matchedCount = matchIndices.Count;

                if (matchedCount == 0)
                {
                    var fuzzy = BuildFuzzyNoMatchDiagnostics(current, oldBlock, whitespaceTolerant);
                    var details = BuildErrorObject("Change failed: oldContent not found as whole-line block.", index, description, 0, null, oldContent, fuzzy);
                    return RevertWithError(path, originalContent, details, encodingUsed).GetAwaiter().GetResult();
                }

                if (!applyAllOccurrences)
                {
                    if (matchedCount > 1 && strictMultipleMatches)
                    {
                        var details = BuildErrorObject($"Change {index} failed: oldContent matches {matchedCount} times and strictMultipleMatches=true.", index, description, matchedCount, null, oldContent);
                        return RevertWithError(path, originalContent, details, encodingUsed).GetAwaiter().GetResult();
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
                    if (oldBlock.Count < 3)
                    {
                        result["warningOldContentTooSmall"] = "oldContent block is small (<3 lines). Provide 3-5 lines of context before and after to uniquely identify the change.";
                    }
                    if (matchedCount > 1 && !strictMultipleMatches)
                    {
                        result["warning"] = "Multiple matches found; applied to first occurrence.";
                    }
                    changeResults.Add(result);
                }
                else
                {
                    // applyAllOccurrences: replace all matches; recompute indices as we mutate
                    var appliedIndices = new JArray();
                    // Work from last to first to avoid shifting indices
                    matchIndices.Reverse();
                    foreach (var idx in matchIndices)
                    {
                        ApplyReplacement(current, idx, oldBlock.Count, newBlock);
                        appliedIndices.Add(idx);
                    }
                    var result = new JObject
                    {
                        ["index"] = index,
                        ["description"] = description,
                        ["matchedCount"] = matchedCount,
                        ["appliedAtIndices"] = appliedIndices,
                        ["replacedLineCountPerOccurrence"] = oldBlock.Count,
                    };
                    if (oldBlock.Count < 3)
                    {
                        result["warningOldContentTooSmall"] = "oldContent block is small (<3 lines). Provide 3-5 lines of context before and after to uniquely identify the change.";
                    }
                    changeResults.Add(result);
                }
            }

            // All changes applied successfully; write back (preserving trailing newline if present)
            try
            {
                var finalContent = string.Join(newline, current);
                if (hadTrailingNewline && (finalContent.Length == 0 || !finalContent.EndsWith(newline)))
                {
                    finalContent += newline;
                }
                using var fsOut = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
                WriteAllTextWithBom(fsOut, finalContent, encodingUsed);
            }
            catch (Exception ex)
            {
                var details = BuildErrorObject($"Failed to write modified file: {ex.Message}", null, null, null, null, null);
                return RevertWithError(path, originalContent, details, encodingUsed).GetAwaiter().GetResult();
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
                    ["applyAllOccurrences"] = applyAllOccurrences,
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

        private async Task<BuiltinToolResult> RevertWithError(string path, string originalContent, JObject errorDetails, Encoding? encoding = null)
        {
            try
            {
                // Preserve original encoding if provided
                if (encoding != null)
                {
                    using var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
                    WriteAllTextWithBom(fs, originalContent, encoding);
                }
                else
                {
                    await File.WriteAllTextAsync(path, originalContent, Encoding.UTF8);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while reverting file after failure: {Path}", path);
            }

            var output = new JObject
            {
                ["success"] = false,
                ["continueProcessing"] = true,
                ["error"] = errorDetails["message"] ?? "Error",
                ["errorDetails"] = errorDetails
            };

            return CreateResult(true, true, output.ToString(Formatting.Indented), (string?)errorDetails["message"] ?? "Change failed");
        }

        private static JObject BuildErrorObject(string message, int? index, string? description, int? matchedCount, int? appliedAtIndex, string? oldContentSnippet, JObject? fuzzy = null)
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
            if (fuzzy != null) obj["noMatchDiagnostics"] = fuzzy;
            return obj;
        }

        private static List<string> SplitLines(string content)
        {
            // Normalize CRLF and LF to logical lines without retaining line ending markers
            return content.Replace("\r\n", "\n").Split('\n').ToList();
        }

        private static JObject BuildFuzzyNoMatchDiagnostics(List<string> current, List<string> oldBlock, bool whitespaceTolerant)
        {
            // Provide limited fuzzy hints: compare first line of oldBlock against file lines and return top few closest with indices
            var result = new JObject();
            if (oldBlock.Count == 0) return result;
            string target = whitespaceTolerant ? oldBlock[0].TrimEnd() : oldBlock[0];
            var candidates = new List<(int index, int distance, string line)>();
            for (int i = 0; i < current.Count; i++)
            {
                var line = whitespaceTolerant ? current[i].TrimEnd() : current[i];
                int dist = LevenshteinDistance(target, line, maxThreshold: 80);
                candidates.Add((i, dist, line));
            }
            var top = candidates.OrderBy(t => t.distance).Take(5).ToList();
            var arr = new JArray();
            foreach (var t in top)
            {
                arr.Add(new JObject
                {
                    ["index"] = t.index,
                    ["distance"] = t.distance,
                    ["linePreview"] = t.line.Length > 200 ? t.line.Substring(0,200) : t.line
                });
            }
            result["closestFirstLineMatches"] = arr;
            result["note"] = "No exact block match. Showing closest single-line matches for the first oldContent line using Levenshtein distance (lower is closer).";
            return result;
        }

        private static int LevenshteinDistance(string a, string b, int maxThreshold = int.MaxValue)
        {
            if (a == b) return 0;
            if (a.Length == 0) return b.Length;
            if (b.Length == 0) return a.Length;
            var prev = new int[b.Length + 1];
            var curr = new int[b.Length + 1];
            for (int j = 0; j <= b.Length; j++) prev[j] = j;
            for (int i = 1; i <= a.Length; i++)
            {
                curr[0] = i;
                int best = curr[0];
                for (int j = 1; j <= b.Length; j++)
                {
                    int cost = a[i - 1] == b[j - 1] ? 0 : 1;
                    curr[j] = Math.Min(Math.Min(curr[j - 1] + 1, prev[j] + 1), prev[j - 1] + cost);
                    if (curr[j] < best) best = curr[j];
                }
                if (best > maxThreshold) return best; // early out
                var tmp = prev; prev = curr; curr = tmp;
            }
            return prev[b.Length];
        }

        private static (string content, Encoding encoding) ReadAllTextWithBom(FileStream fs)
        {
            // Detect common BOMs: UTF8 BOM, UTF16 LE/BE
            fs.Seek(0, SeekOrigin.Begin);
            using var ms = new MemoryStream();
            fs.CopyTo(ms);
            var bytes = ms.ToArray();
            Encoding encoding = new UTF8Encoding(false);
            int offset = 0;
            if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
            {
                encoding = new UTF8Encoding(true);
                offset = 3;
            }
            else if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
            {
                encoding = new UnicodeEncoding(false, true); // UTF-16 LE with BOM
                offset = 2;
            }
            else if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
            {
                encoding = new UnicodeEncoding(true, true); // UTF-16 BE with BOM
                offset = 2;
            }
            var content = encoding.GetString(bytes, offset, bytes.Length - offset);
            return (content, encoding);
        }

        private static void WriteAllTextWithBom(FileStream fs, string content, Encoding encoding)
        {
            fs.Seek(0, SeekOrigin.Begin);
            if (encoding is UTF8Encoding utf8)
            {
                if (utf8.GetPreamble().Length == 3)
                {
                    fs.Write(utf8.GetPreamble(), 0, 3);
                }
                var bytes = new UTF8Encoding(false).GetBytes(content);
                fs.Write(bytes, 0, bytes.Length);
            }
            else if (encoding is UnicodeEncoding uni)
            {
                var preamble = uni.GetPreamble();
                if (preamble.Length > 0) fs.Write(preamble, 0, preamble.Length);
                var bytes = uni.GetBytes(content);
                fs.Write(bytes, 0, bytes.Length);
            }
            else
            {
                // Fallback to provided encoding
                var preamble = encoding.GetPreamble();
                if (preamble.Length > 0) fs.Write(preamble, 0, preamble.Length);
                var bytes = encoding.GetBytes(content);
                fs.Write(bytes, 0, bytes.Length);
            }
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

        private static class FileLockProvider
        {
            private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, object> Locks = new();
            public static object GetLockForPath(string path)
            {
                return Locks.GetOrAdd(path, _ => new object());
            }
        }
    }
}