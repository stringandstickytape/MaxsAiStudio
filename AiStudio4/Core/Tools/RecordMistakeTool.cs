











namespace AiStudio4.Core.Tools
{
    /// <summary>
    /// Implementation of the RecordMistake tool for logging AI mistakes
    /// </summary>
    public class RecordMistakeTool : BaseToolImplementation
    {
        private const string MISTAKES_FILE_PATH = "CommonAiMistakes.md";
        private readonly ISecondaryAiService _secondaryAiService;

        public RecordMistakeTool(ILogger<RecordMistakeTool> logger, IGeneralSettingsService generalSettingsService, IStatusMessageService statusMessageService, ISecondaryAiService secondaryAiService) : base(logger, generalSettingsService, statusMessageService)
        {
            _secondaryAiService = secondaryAiService ?? throw new ArgumentNullException(nameof(secondaryAiService));
        }

        /// <summary>
        /// Gets the RecordMistake tool definition
        /// </summary>
        public override Tool GetToolDefinition()
        {
            return new Tool
            {
                Guid = ToolGuids.RECORD_MISTAKE_TOOL_GUID,
                Name = "RecordMistake",
                Description = "Records AI mistakes for future reference",
                Schema = """
{
  "name": "RecordMistake",
  "description": "Records a mistake made by the AI to help avoid similar issues in the future. The mistake will be logged in CommonAiMistakes.md in the project root.",
  "input_schema": {
    "properties": {
      "mistake_title": { "title": "Mistake Title", "type": "string", "description": "A brief title describing the mistake" },
      "mistake_description": { "title": "Mistake Description", "type": "string", "description": "Succinct description of what went wrong" },
      "root_cause": { "title": "Root Cause", "type": "string", "description": "Succinct analysis of why the mistake occurred" },
      "prevention_strategy": { "title": "Prevention Strategy", "type": "string", "description": "How to avoid making this mistake in the future" }
    },
    "required": ["mistake_title", "mistake_description", "root_cause", "prevention_strategy"],
    "title": "RecordMistakeArguments",
    "type": "object"
  }
}
""",
                Categories = new List<string> { "MaxCode" },
                OutputFileType = "txt",
                Filetype = string.Empty,
                LastModified = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Processes a RecordMistake tool call
        /// </summary>
        public override async Task<BuiltinToolResult> ProcessAsync(string toolParameters, Dictionary<string, string> extraProperties)
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

                SendStatusUpdate("Mistake recorded successfully. Starting consolidation process...");
                
                // Consolidate mistakes using the secondary AI and await the result
                await ConsolidateMistakesAsync(mistakeTitle, mistakeDescription, rootCause, preventionStrategy);
                
                return CreateResult(true, true, $"Mistake '{mistakeTitle}' has been recorded in {MISTAKES_FILE_PATH} and the file has been consolidated");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing RecordMistake tool");
                SendStatusUpdate($"Error processing RecordMistake tool: {ex.Message}");
                return CreateResult(true, true, $"Error processing RecordMistake tool: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Consolidates the mistakes file using the secondary AI
        /// </summary>
        private async Task ConsolidateMistakesAsync(string newMistakeTitle, string newMistakeDescription, string newRootCause, string newPreventionStrategy)
        {
            try
            {
                SendStatusUpdate("Starting mistake consolidation process...");
                var mistakesFilePath = Path.Combine(_projectRoot, MISTAKES_FILE_PATH);
                
                // Check if file exists
                if (!File.Exists(mistakesFilePath))
                {
                    _logger.LogWarning("Mistakes file not found for consolidation");
                    SendStatusUpdate("Mistakes file not found for consolidation");
                    return;
                }
                
                // Read the entire file content
                SendStatusUpdate("Reading mistakes file...");
                string fileContent = await File.ReadAllTextAsync(mistakesFilePath);
                
                // Create a record of the new mistake to preserve after consolidation
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                var newMistakeEntry = new StringBuilder(fileContent);
                newMistakeEntry.AppendLine($"## {newMistakeTitle}");
                newMistakeEntry.AppendLine($"*Recorded: {timestamp}*");
                newMistakeEntry.AppendLine();
                newMistakeEntry.AppendLine("### Description");
                newMistakeEntry.AppendLine(newMistakeDescription);
                newMistakeEntry.AppendLine();
                newMistakeEntry.AppendLine("### Root Cause");
                newMistakeEntry.AppendLine(newRootCause);
                newMistakeEntry.AppendLine();
                newMistakeEntry.AppendLine("### Prevention Strategy");
                newMistakeEntry.AppendLine(newPreventionStrategy);
                newMistakeEntry.AppendLine();
                newMistakeEntry.AppendLine("---");
                newMistakeEntry.AppendLine();
                
                // Create the prompt for the secondary AI
                SendStatusUpdate("Preparing consolidation request...");
                string prompt = $"Produce a single consolidated guide to error prevention based on this file. Do not add anything, merely consolidate.  Don't remove any detail.  When consolidating a newly logged mistake of an existing type, into an existing target, strengthen the language of that target a little each time.\n\n```\n{newMistakeEntry}\n```";
                
                // Process the request with the secondary AI
                SendStatusUpdate("Sending mistake consolidation request to secondary AI...");
                var response = await _secondaryAiService.ProcessRequestAsync(prompt);
                
                if (!response.Success)
                {
                    _logger.LogError("Secondary AI consolidation failed: {Error}", response.Error);
                    SendStatusUpdate($"Consolidation failed: {response.Error}");
                    return;
                }
                
                // Create the consolidated content with the new mistakes section
                SendStatusUpdate("Processing AI response...");
                var consolidatedContent = new StringBuilder();
                consolidatedContent.Append(response.Response.Trim());
                
                // Write the consolidated content back to the file
                SendStatusUpdate("Writing consolidated content to file...");
                await File.WriteAllTextAsync(mistakesFilePath, consolidatedContent.ToString());
                
                SendStatusUpdate("Mistake consolidation completed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error consolidating mistakes");
                SendStatusUpdate($"Error during mistake consolidation: {ex.Message}");
            }
        }
    }
}
