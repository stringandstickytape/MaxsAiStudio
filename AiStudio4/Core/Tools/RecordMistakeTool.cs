using AiStudio4.Core.Interfaces;
using AiStudio4.Core.Models;
using AiStudio4.InjectedDependencies;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace AiStudio4.Core.Tools
{
    /// <summary>
    /// Implementation of the RecordMistake tool for logging AI mistakes
    /// </summary>
    public class RecordMistakeTool : BaseToolImplementation
    {
        private const string MISTAKES_FILE_PATH = "CommonAiMistakes.md";

        public RecordMistakeTool(ILogger<RecordMistakeTool> logger, IGeneralSettingsService generalSettingsService, IStatusMessageService statusMessageService) : base(logger, generalSettingsService, statusMessageService)
        {
        }

        /// <summary>
        /// Gets the RecordMistake tool definition
        /// </summary>
        public override Tool GetToolDefinition()
        {
            return new Tool
            {
                Guid = "a1b2c3d4-e5f6-7890-1234-567890abcdef",
                Name = "RecordMistake",
                Description = "Records AI mistakes for future reference",
                Schema = @"{
  ""name"": ""RecordMistake"",
  ""description"": ""Records a mistake made by the AI to help avoid similar issues in the future. The mistake will be logged in CommonAiMistakes.md in the project root."",
  ""input_schema"": {
                ""properties"": {
""mistake_title"": {
                    ""title"": ""Mistake Title"",
                    ""type"": ""string"",
                    ""description"":""A brief title describing the mistake""
                },
""mistake_description"": {
                    ""title"": ""Mistake Description"",
                    ""type"": ""string"",
                    ""description"":""Succinct description of what went wrong""
                },
""root_cause"": {
                    ""title"": ""Root Cause"",
                    ""type"": ""string"",
                    ""description"":""Succinct analysis of why the mistake occurred""
                },
""prevention_strategy"": {
                    ""title"": ""Prevention Strategy"",
                    ""type"": ""string"",
                    ""description"":""How to avoid making this mistake in the future""
                }
            },
           ""required"": [""mistake_title"", ""mistake_description"", ""root_cause"", ""prevention_strategy""],
            ""title"": ""RecordMistakeArguments"",
            ""type"": ""object""
  }
}",
                Categories = new List<string> { "MaxCode" },
                OutputFileType = "txt",
                Filetype = string.Empty,
                LastModified = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Processes a RecordMistake tool call
        /// </summary>
        public override Task<BuiltinToolResult> ProcessAsync(string toolParameters, Dictionary<string, string> extraProperties)
        {
            try
            {
                SendStatusUpdate("Starting RecordMistake tool execution...");
                var parameters = JsonConvert.DeserializeObject<Dictionary<string, object>>(toolParameters);

                // Extract parameters
                var mistakeTitle = parameters.ContainsKey("mistake_title") ? parameters["mistake_title"].ToString() : "Untitled Mistake";
                var mistakeDescription = parameters.ContainsKey("mistake_description") ? parameters["mistake_description"].ToString() : "No description provided";
                var rootCause = parameters.ContainsKey("root_cause") ? parameters["root_cause"].ToString() : "No root cause analysis provided";
                var preventionStrategy = parameters.ContainsKey("prevention_strategy") ? parameters["prevention_strategy"].ToString() : "No prevention strategy provided";

                // Format the mistake entry
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                var mistakeEntry = new StringBuilder();
                mistakeEntry.AppendLine($"## {mistakeTitle}");
                mistakeEntry.AppendLine($"*Recorded: {timestamp}*");
                mistakeEntry.AppendLine();
                mistakeEntry.AppendLine("### Description");
                mistakeEntry.AppendLine(mistakeDescription);
                mistakeEntry.AppendLine();
                mistakeEntry.AppendLine("### Root Cause");
                mistakeEntry.AppendLine(rootCause);
                mistakeEntry.AppendLine();
                mistakeEntry.AppendLine("### Prevention Strategy");
                mistakeEntry.AppendLine(preventionStrategy);
                mistakeEntry.AppendLine();
                mistakeEntry.AppendLine("---");
                mistakeEntry.AppendLine();

                // Get the full path to the mistakes file
                var mistakesFilePath = Path.Combine(_projectRoot, MISTAKES_FILE_PATH);

                // Create or append to the mistakes file
                bool fileExists = File.Exists(mistakesFilePath);
                using (var fileStream = new FileStream(mistakesFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite))
                {
                    using (var streamWriter = new StreamWriter(fileStream))
                    {
                        // If file is new, add a header
                        if (!fileExists || fileStream.Length == 0)
                        {
                            streamWriter.WriteLine("# Common AI Mistakes");
                            streamWriter.WriteLine("This document records mistakes made by the AI to help avoid similar issues in the future.");
                            streamWriter.WriteLine();
                        }
                        else
                        {
                            // Move to the end of the file for appending
                            fileStream.Seek(0, SeekOrigin.End);
                        }

                        // Write the new mistake entry
                        streamWriter.Write(mistakeEntry.ToString());
                    }
                }

                SendStatusUpdate("Mistake recorded successfully.");
                return Task.FromResult(CreateResult(true, true, $"Mistake '{mistakeTitle}' has been recorded in {MISTAKES_FILE_PATH}"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing RecordMistake tool");
                SendStatusUpdate($"Error processing RecordMistake tool: {ex.Message}");
                return Task.FromResult(CreateResult(true, true, $"Error processing RecordMistake tool: {ex.Message}"));
            }
        }
    }
}