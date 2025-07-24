// AiStudio4/Core/Tools/ModifyFileUsingMorph.cs
using AiStudio4.AiServices;
using AiStudio4.Convs;
using AiStudio4.DataModels;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using Newtonsoft.Json;
using SharedClasses;
using SharedClasses.Providers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace AiStudio4.Core.Tools
{
    [McpServerToolType]
    public class ModifyFileUsingMorphTool : BaseToolImplementation
    {
        public ModifyFileUsingMorphTool(
            ILogger<ModifyFileUsingMorphTool> logger,
            IGeneralSettingsService generalSettingsService,
            IStatusMessageService statusMessageService
            )
            : base(logger, generalSettingsService, statusMessageService)
        {
        }

        [McpServerTool, Description("Use this tool to propose an edit to an existing file. This will be read by a less intelligent model, which will quickly apply the edit. You should make it clear what the edit is, while also minimizing the unchanged code you write. You must not submit the original content, only its filename. When writing the edit, you should specify each edit in sequence, with the special comment // ... existing. You should specify the following arguments before the others: [target_file]")]
        public async Task<string> ModifyFileUsingMorph([Description("JSON parameters for ModifyFileUsingMorph")] string parameters = "{}")
        {
            return await ExecuteWithExtraProperties(parameters);
        }

        public override Tool GetToolDefinition()
        {
            return new Tool
            {
                Guid = "f4b3b3b3-f4b3-f4b3-f4b3-f4b3f4b3f4b3", // This will be replaced by the ToolManager
                Name = "ModifyFileUsingMorph",
                Description = @"Use this tool to propose an edit to an existing file.

This will be read by a less intelligent model, which will quickly apply the edit. You should make it clear what the edit is, while also minimizing the unchanged code you write.

You must not submit the original content, only its filename.

When writing the edit, you should specify each edit in sequence, with the special comment // ... existing
You should specify the following arguments before the others: [target_file]",
                Schema = """
                {
                  "name": "ModifyFileUsingMorph",
                  "description": "Use this tool to propose an edit to an existing file. This will be read by a less intelligent model, which will quickly apply the edit. You should make it clear what the edit is, while also minimizing the unchanged code you write. When writing the edit, you should specify each edit in sequence, with the special comment // ... existing. You should specify the following arguments before the others: [target_file]",
                  "input_schema": {
                    "type": "object",
                    "properties": {
                      "target_file": {
                        "type": "string",
                        "description": "The full path of the file to be modified."
                      },
                      "update_snippet": {
                        "type": "string",
                        "description": "The code edits in the special format, using '// ... existing' to denote unchanged code."
                      }
                    },
                    "required": ["target_file", "update_snippet"]
                  }
                }
                """,
                Categories = new List<string> { "MaxCode" },
                OutputFileType = "txt",
                ExtraProperties = new Dictionary<string, string>
                {
                    { "model", "" }
                }
            };
        }

        public override async Task<BuiltinToolResult> ProcessAsync(string toolParameters, Dictionary<string, string> extraProperties)
        {
            SendStatusUpdate("Initializing ModifyFileUsingMorph tool...");
            var parameters = JsonConvert.DeserializeObject<Dictionary<string, object>>(toolParameters);

            if (parameters == null || !parameters.TryGetValue("target_file", out var targetFileObj) || !(targetFileObj is string targetFile) || string.IsNullOrWhiteSpace(targetFile))
            {
                return CreateResult(true, true, "Error: 'target_file' parameter is required and must be a non-empty string.");
            }

            if (!parameters.TryGetValue("update_snippet", out var updateSnippetObj) || !(updateSnippetObj is string updateSnippet) || string.IsNullOrWhiteSpace(updateSnippet))
            {
                return CreateResult(true, true, "Error: 'update_snippet' parameter is required and must be a non-empty string.");
            }

            if (!extraProperties.TryGetValue("model", out var modelGuid) || string.IsNullOrWhiteSpace(modelGuid))
            {
                return CreateResult(true, true, "Error: The 'model' to use must be configured in the tool's Extra Properties.");
            }

            string originalContent;
            try
            {
                originalContent = await File.ReadAllTextAsync(targetFile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading file: {FilePath}", targetFile);
                return CreateResult(true, true, $"Error reading file: {ex.Message}");
            }

            SendStatusUpdate($"Looking for model: {modelGuid}...");
            var modelToUse = _generalSettingsService.CurrentSettings.ModelList.FirstOrDefault(m => m.Guid.Equals(modelGuid, StringComparison.OrdinalIgnoreCase));
            if (modelToUse == null)
            {
                return CreateResult(true, true, $"Error: Model with GUID '{modelGuid}' not found in settings.");
            }

            var provider = ServiceProvider.GetProviderForGuid(_generalSettingsService.CurrentSettings.ServiceProviders, modelToUse.ProviderGuid);
            if (provider == null)
            {
                return CreateResult(true, true, $"Error: Service provider for model '{modelToUse.FriendlyName}' not found.");
            }

            var aiService = AiServiceResolver.GetAiService(provider.ServiceName, null, null);
            if (aiService == null)
            {
                return CreateResult(true, true, $"Error: Could not resolve AI service for provider '{provider.FriendlyName}'.");
            }

            SendStatusUpdate($"Contacting {modelToUse.FriendlyName} to apply changes to {Path.GetFileName(targetFile)}...");

            var systemPrompt = "You are an AI assistant that helps with code changes. The user will provide a snippet of code with edits. Your task is to apply these edits to the original code and return the complete, updated code. The edit snippet will use the comment // ... existing code ... to indicate parts of the original code that should remain unchanged.";
            var userPrompt = $"<code>{originalContent}</code><update>{updateSnippet}</update>";

            var singleShotConv = new LinearConv(DateTime.UtcNow)
            {
                //systemprompt = systemPrompt,
                messages = new List<LinearConvMessage>
                {
                    new LinearConvMessage { role = "user", contentBlocks = new List<ContentBlock> { new ContentBlock { ContentType = ContentType.Text, Content = userPrompt } } }
                }
            };

            var requestOptions = new AiRequestOptions
            {
                ServiceProvider = provider,
                Model = modelToUse,
                Conv = singleShotConv,
                CancellationToken = CancellationToken.None,
                ApiSettings = _generalSettingsService.CurrentSettings.ToApiSettings()
            };

            var response = await aiService.FetchResponse(requestOptions, forceNoTools: true);

            if (!response.Success)
            {
                var errorMessage = $"Error from {modelToUse.FriendlyName}: {string.Join("\n\n", response.ContentBlocks.Where(x => x.ContentType == ContentType.Text).Select(x => x.Content))}";
                SendStatusUpdate(errorMessage);
                return CreateResult(true, true, errorMessage);
            }

            var responseText = string.Join("", response.ContentBlocks.Where(x => x.ContentType == ContentType.Text).Select(x => x.Content)).Trim();
            var modifiedContent = ExtractCodeFromMarkdown(responseText);

            try
            {
                await File.WriteAllTextAsync(targetFile, modifiedContent);
                var successMessage = $"Successfully modified file: {targetFile}";
                SendStatusUpdate(successMessage);
                return CreateResult(true, true, successMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error writing file: {FilePath}", targetFile);
                return CreateResult(true, false, $"Error writing file: {ex.Message}");
            }
        }

        private string ExtractCodeFromMarkdown(string text)
        {
            var match = Regex.Match(text, $"{BacktickHelper.ThreeTicks}(?:[a-zA-Z]+)?\\s*\n(.*?)\n{BacktickHelper.ThreeTicks}", RegexOptions.Singleline);
            if (match.Success)
            {
                return match.Groups[1].Value;
            }
            return text;
        }
    }
}