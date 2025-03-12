using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using static DiffLibrary.ChangesetProcessor;

namespace DiffLibrary
{
	/// <summary>
	/// Provides functionality to apply changesets to files, handling file modifications, additions, deletions, creation, renaming, and deletion.
	/// </summary>
	public class ChangesetProcessor
	{
		private readonly TextReplacer _textReplacer;
		private string _rootPath;

		public StringBuilder Log { get; set; } = new StringBuilder();

		/// <summary>
		/// Initializes a new instance of the ChangesetProcessor class.
		/// </summary>
		/// <param name="rootPath">The root directory path for all file operations.</param>
		public ChangesetProcessor(string rootPath)
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

            //var options = new JsonSerializerOptions
            //{
            //    AllowTrailingCommas = true,
            //    ReadCommentHandling = JsonCommentHandling.Skip,
            //    PropertyNameCaseInsensitive = true
            //};
            //
            //
            //options.Converters.Add(new NewlineHandlingJsonConverter());
            //
            //try
            //{
            //	changeset = JsonSerializer.Deserialize<ChangesetRoot>(jsonContent, options);
            //}
            //catch
            //{
            //    changeset = JsonSerializer.Deserialize<ChangesetRoot>(jsonContent.Replace("\r\n","\n"), options);
            //}

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
			{string fileContent = File.ReadAllText(resolvedPath);
				
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

		/// <summary>
		/// Provides functionality to replace text in a source file with new text, using a line number hint for efficient matching.
		/// </summary>
		private class TextReplacer
		{
			/// <summary>
			/// Replaces a specified text in a source file with new text, searching for matches around a given line number hint.
			/// </summary>
			/// <param name="sourceFile">The complete content of the source file to modify.</param>
			/// <param name="oldText">The text to be replaced.</param>
			/// <param name="newText">The text to replace with.</param>
			/// <param name="lineNumberHint">The approximate line number where the replacement should occur (1-based).</param>
			/// <returns>The modified file content if a match is found, or the original content if no match is found.</returns>
			public string ReplaceTextAtHint(string sourceFile, string oldText, string newText, int lineNumberHint)
			{
				if (string.IsNullOrEmpty(sourceFile) || string.IsNullOrEmpty(oldText))
					return sourceFile;

				// Properly split texts with newline preservation
				var sourceTextInfo = SplitTextPreservingNewlines(sourceFile);
				var oldTextInfo = SplitTextPreservingNewlines(oldText);
				var newTextInfo = SplitTextPreservingNewlines(newText);

				// Handle empty file case
				if (sourceTextInfo.Lines.Count == 0)
				{
					return sourceFile; // Can't match anything in an empty file
				}

				// Convert from 1-based (user) to 0-based (code) indexing with bounds checking
				int zeroBasedLineHint = Math.Max(0, lineNumberHint - 1);

				// Ensure valid line hint (capped at the last line)
				zeroBasedLineHint = Math.Min(zeroBasedLineHint, sourceTextInfo.Lines.Count - 1);

				// Find the match using bidirectional search
				int matchPosition = FindMatchPosition(sourceTextInfo.Lines, oldTextInfo.Lines, zeroBasedLineHint);

				// If no match found, return original content
				if (matchPosition < 0)
					return sourceFile;

				// Build the result with proper newline handling
				return BuildResult(
					sourceTextInfo.Lines,
					sourceTextInfo.LineEndings,
					newTextInfo.Lines,
					newTextInfo.LineEndings,
					matchPosition,
					oldTextInfo.Lines.Count,
					sourceTextInfo.EndsWithNewline
				);
			}

			/// <summary>
			/// Detailed text info including lines, their endings, and whether the text ends with a newline
			/// </summary>
			private class TextInfo
			{
				public List<string> Lines { get; } = new List<string>();
				public List<string> LineEndings { get; } = new List<string>();
				public bool EndsWithNewline { get; set; }
			}

			/// <summary>
			/// Splits text into lines while preserving the exact newline characters used for each line
			/// </summary>
			private TextInfo SplitTextPreservingNewlines(string text)
			{
				var result = new TextInfo();

				if (string.IsNullOrEmpty(text))
					return result;

				// Use regex to split while capturing the newline characters
				var matches = Regex.Matches(text, @"([^\r\n]*)(\r\n|\n|\r|$)");

				foreach (Match match in matches)
				{
					string line = match.Groups[1].Value;
					string ending = match.Groups[2].Value;

					// Skip the last empty match that doesn't represent a real line
					if (match.Index + match.Length == text.Length && string.IsNullOrEmpty(line) && string.IsNullOrEmpty(ending))
						break;

					result.Lines.Add(line);
					result.LineEndings.Add(ending);

					// Track if the text ends with a newline
					if (match.Index + match.Length == text.Length)
					{
						result.EndsWithNewline = !string.IsNullOrEmpty(ending) && ending != "$";
					}
				}

				return result;
			}

			/// <summary>
			/// Finds the position where the oldLines match in sourceLines, searching outward from the hint
			/// </summary>
			private int FindMatchPosition(List<string> sourceLines, List<string> oldLines, int lineHint)
			{
				if (oldLines.Count == 0 || sourceLines.Count == 0)
					return lineHint;

				// Handle special case - empty pattern matches at any position
				if (oldLines.Count == 1 && oldLines[0] == "")
					return Math.Min(lineHint, sourceLines.Count);

				int maxOffset = Math.Max(sourceLines.Count, lineHint);

				for (int offset = 0; offset <= maxOffset; offset++)
				{
					// Try line number + offset
					int upperLine = lineHint + offset;
					if (upperLine < sourceLines.Count && MatchesAtPosition(sourceLines, oldLines, upperLine))
					{
						return upperLine;
					}

					// Try line number - offset (avoid checking hint twice)
					int lowerLine = lineHint - offset;
					if (offset > 0 && lowerLine >= 0 && MatchesAtPosition(sourceLines, oldLines, lowerLine))
					{
						return lowerLine;
					}

					// Early exit if we've searched the entire file
					if ((upperLine >= sourceLines.Count) && (lowerLine < 0))
					{
						break;
					}
				}

				return -1; // No match found
			}

			/// <summary>
			/// Checks if the sourceLines match the oldLines starting at the given position
			/// </summary>
			private bool MatchesAtPosition(List<string> sourceLines, List<string> oldLines, int startLine)
			{
				if (startLine + oldLines.Count > sourceLines.Count)
					return false;

				for (int i = 0; i < oldLines.Count; i++)
				{
					if (sourceLines[startLine + i].Trim() != oldLines[i].Trim())
						return false;
				}

				return true;
			}

			/// <summary>
			/// Builds the result by combining parts before the match, the new text, and parts after the match
			/// </summary>
			private string BuildResult(
				List<string> sourceLines,
				List<string> sourceEndings,
				List<string> newLines,
				List<string> newEndings,
				int matchPosition,
				int oldLinesCount,
				bool sourceEndsWithNewline)
			{
				var result = new StringBuilder();

				// Add lines before match with their original endings
				for (int i = 0; i < matchPosition; i++)
				{
					result.Append(sourceLines[i]);
					result.Append(sourceEndings[i]);
				}

				// Add new text with appropriate endings
				for (int i = 0; i < newLines.Count; i++)
				{
					result.Append(newLines[i]);

					// For the last line of new text, we need to be careful with the ending
					if (i == newLines.Count - 1)
					{
						// If source ends with newline or we're not at the end of the file,
						// ensure we have a proper line ending
						if (matchPosition + oldLinesCount < sourceLines.Count || sourceEndsWithNewline)
						{
							// Prefer to use the ending from the last line of old text when available
							string lineEnding = (matchPosition + oldLinesCount - 1 < sourceEndings.Count)
								? sourceEndings[matchPosition + oldLinesCount - 1]
								: (sourceEndings.Count > 0 ? sourceEndings[0] : "\n");

							result.Append(lineEnding);
						}
					}
					else if (i < newEndings.Count)
					{
						// Use new text's line ending for non-last lines
						result.Append(newEndings[i]);
					}
				}

				// Add lines after match with their original endings
				for (int i = matchPosition + oldLinesCount; i < sourceLines.Count; i++)
				{
					result.Append(sourceLines[i]);

					// Add line ending if not the last line or if source ends with newline
					if (i < sourceLines.Count - 1 || sourceEndsWithNewline)
					{
						// Ensure we don't go out of bounds
						if (i < sourceEndings.Count)
						{
							result.Append(sourceEndings[i]);
						}
					}
				}

				return result.ToString();
			}
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