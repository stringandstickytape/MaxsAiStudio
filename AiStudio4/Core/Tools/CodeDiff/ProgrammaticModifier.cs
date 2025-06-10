using AiStudio4.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AiStudio4.Core.Tools.CodeDiff
{
    /// <summary>
    /// Applies AI-generated code changes to a source file using
    /// whitespace-tolerant pattern matching.
    /// </summary>
    public class ProgrammaticModifier
    {
        private readonly ILogger _logger;
        private readonly IStatusMessageService _statusMessageService;
        private readonly string _clientId;

        // ???????????????????????????????????????????????????????????????? ctor
        public ProgrammaticModifier(
            ILogger logger,
            IStatusMessageService statusMessageService,
            string clientId)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _statusMessageService = statusMessageService;
            _clientId = clientId;
        }

        // ????????????????????????????????????????????????????? public API
        /// <summary>
        /// Attempts to apply a list of JSON-encoded changes to the file
        /// content supplied in <paramref name="originalContent"/>.
        /// </summary>
        public bool TryApplyModifications(
            string filePath,
            string originalContent,
            List<JObject> changes,
            out string modifiedContent,
            out string failureReason)
        {
            failureReason = null;
            modifiedContent = originalContent;   // we mutate this as we go

            foreach (var change in changes)
            {
                string oldContent = change["oldContent"]?.ToString();
                string newContent = change["newContent"]?.ToString();

                if (string.IsNullOrWhiteSpace(oldContent))
                {
                    _logger.LogWarning(
                        "Skipping change with empty oldContent for file '{FilePath}'.",
                        filePath);
                    continue;
                }

                // ---- Build whitespace-agnostic regex pattern
                string pattern = BuildWhitespaceAgnosticRegexPattern(oldContent);

                // Search the *current* state of the text
                var matches = Regex.Matches(
                    modifiedContent,
                    pattern,
                    RegexOptions.Singleline);     // '.' matches newlines

                if (matches.Count == 0)
                {
                    failureReason =
                        $"Cannot find oldContent in file. Change index: {changes.IndexOf(change)}";
                    _logger.LogWarning(
                        "Cannot find oldContent in file '{FilePath}'. Falling back to AI.",
                        filePath);
                    return false;
                }

                if (matches.Count > 1)
                {
                    failureReason =
                        $"Multiple matches ({matches.Count}) for oldContent in file. Change index: {changes.IndexOf(change)}";
                    _logger.LogWarning(
                        "Multiple matches ({Count}) for oldContent in file '{FilePath}'. Falling back to AI.",
                        matches.Count,
                        filePath);
                    return false;
                }

                // Exactly one match – replace it with newContent verbatim
                var match = matches[0];
                modifiedContent = modifiedContent.Remove(match.Index, match.Length)
                                                 .Insert(match.Index, newContent);

                _logger.LogInformation(
                    "Successfully applied programmatic change to file '{FilePath}'.",
                    filePath);
            }

            return true;
        }

        // ????????????????????????????????????????????????????? helpers
        /// <summary>
        /// Converts an arbitrary text snippet into a regular-expression
        /// pattern that treats every *run* of whitespace characters
        /// (space, tab, CR, LF) as <c>\s+</c>.
        /// </summary>
        private static string BuildWhitespaceAgnosticRegexPattern(string text)
        {
            var sb = new StringBuilder();
            bool inWhitespace = false;

            foreach (char c in NormalizeLineEndings(text))
            {
                if (char.IsWhiteSpace(c))
                {
                    if (!inWhitespace)
                    {
                        sb.Append(@"\s+");   // first char in a run
                        inWhitespace = true;
                    }
                }
                else
                {
                    sb.Append(Regex.Escape(c.ToString()));
                    inWhitespace = false;
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Normalises all line endings in <paramref name="text"/> to LF.
        /// Does *not* touch spaces or tabs – that is left to the regex.
        /// </summary>
        private static string NormalizeLineEndings(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            return text.Replace("\r\n", "\n").Replace('\r', '\n');
        }

        // ????????????????????????????????????? debug-dump helpers

        public void SaveMergeDebugInfo(
            string filePath,
            string originalContent,
            List<JObject> changes,
            string failureReason)
        {
            try
            {
                string debugDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "AiStudio4",
                    "DebugLogs",
                    "MergeFailures");
                Directory.CreateDirectory(debugDir);

                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
                string filename = Path.GetFileName(filePath);
                string debugFilePath = Path.Combine(
                    debugDir,
                    $"merge_failure_{timestamp}_{filename}.json");

                var debugData = new
                {
                    Timestamp = DateTime.Now,
                    FilePath = filePath,
                    FailureReason = failureReason,
                    Changes = changes,
                    OriginalContent = originalContent
                };

                string json = JsonConvert.SerializeObject(debugData, Formatting.Indented);
                File.WriteAllText(debugFilePath, json, Encoding.UTF8);

                _logger.LogInformation(
                    "Saved merge failure debug info to {DebugFilePath}",
                    debugFilePath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to save merge failure debug info for {FilePath}",
                    filePath);
            }
        }

        // ????????????????????????????????????? status-message helper
        private async void SendStatusUpdate(string statusMessage)
        {
            try
            {
                if (_statusMessageService != null && !string.IsNullOrEmpty(_clientId))
                {
                    await _statusMessageService.SendStatusMessageAsync(_clientId, statusMessage);
                }
                else
                {
                    _logger.LogDebug(
                        "Status update not sent - missing StatusMessageService or clientId: {Message}",
                        statusMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send status update: {Message}", statusMessage);
            }
        }
    }
}