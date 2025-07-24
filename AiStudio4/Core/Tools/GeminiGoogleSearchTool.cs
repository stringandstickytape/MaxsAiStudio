// AiStudio4/Core/Tools/GeminiGoogleSearchTool.cs
using AiStudio4.AiServices;


using AiStudio4.Convs;
using AiStudio4.DataModels;

using AiStudio4.Services.Interfaces;


using SharedClasses.Models;
using SharedClasses.Providers;




using System.Threading;

using Azure;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace AiStudio4.Core.Tools
{
    /// <summary>
    /// Tool that performs Google Search using a configured Gemini model that has built-in search capabilities.
    /// This allows other AIs to leverage Gemini's Google Search functionality indirectly.
    /// </summary>
    [McpServerToolType]
    public class GeminiGoogleSearchTool : BaseToolImplementation
    {
        public GeminiGoogleSearchTool(
            ILogger<GeminiGoogleSearchTool> logger, 
            IGeneralSettingsService generalSettingsService, 
            IStatusMessageService statusMessageService) 
            : base(logger, generalSettingsService, statusMessageService)
        {
        }

        public override Tool GetToolDefinition()
        {
            return new Tool
            {
                Guid = ToolGuids.GEMINI_GOOGLE_SEARCH_TOOL_GUID,
                Name = "GeminiGoogleSearch",
                Description = "Performs a Google Search using a configured Gemini model that has built-in search capabilities. Returns a summary of the search results and key information. Useful for finding up-to-date information or specific web content when other search tools are not appropriate or when Gemini's specific search integration is desired.",
                Schema = """
{
  "name": "GeminiGoogleSearch",
  "description": "Performs a Google Search using a Gemini model that has built-in search capabilities. Returns a summary of the search results and key information.",
  "input_schema": {
    "type": "object",
    "properties": {
      "query": {
        "type": "string",
        "description": "The search query to be executed by the Gemini model."
      },
      "custom_instructions": {
        "type": "string",
        "description": "Optional. Specific instructions for the Gemini model on how to process or summarize the search results (e.g., 'Focus on academic papers published in the last year', 'Provide a bulleted list of the top 3 findings related to AI in healthcare'). If not provided, Gemini will provide a general summary and list of results."
      }
    },
    "required": ["query"]
  }
}
""",
                Categories = new List<string> { "Search" },
                OutputFileType = "txt",
                ExtraProperties = new Dictionary<string, string>
                {
                    { "geminiModelFriendlyNameToUse (Optional)", "" }
                },
                IsBuiltIn = true,
                LastModified = DateTime.UtcNow
            };
        }

        public override async Task<BuiltinToolResult> ProcessAsync(string toolParameters, Dictionary<string, string> extraProperties)
        {
            SendStatusUpdate("Initializing GeminiGoogleSearch tool...");
            var parameters = JsonConvert.DeserializeObject<Dictionary<string, object>>(toolParameters);

            string query = parameters?["query"]?.ToString();
            if (string.IsNullOrWhiteSpace(query))
            {
                return CreateResult(false, false, "Error: 'query' parameter is required for GeminiGoogleSearch.");
            }
            string customInstructions = parameters?.GetValueOrDefault("custom_instructions")?.ToString();

            // --- 1. Identify the Gemini Model to use ---
            string specifiedGeminiFriendlyName = extraProperties?.GetValueOrDefault("geminiModelFriendlyNameToUse (Optional)");
            Model geminiModelInfo = null;

            var allModels = _generalSettingsService.CurrentSettings.ModelList;
            var allProviders = _generalSettingsService.CurrentSettings.ServiceProviders;

            // Helper function to check if a model is a valid Gemini model for search
            Func<Model, bool> isValidGeminiSearchModel = (m) =>
            {
                if (m == null) return false;
                var provider = allProviders.FirstOrDefault(p => p.Guid == m.ProviderGuid);
                // Crucial: Check if the model name contains "gemini" (API name)
                // AND its provider's ServiceName is "Gemini" (our IAiService implementation)
                // AND it's likely a search-capable model (pro/flash heuristic).
                return provider?.ServiceName == "Gemini" &&
                       m.ModelName.Contains("gemini", StringComparison.OrdinalIgnoreCase) &&
                       (m.ModelName.Contains("pro", StringComparison.OrdinalIgnoreCase) || m.ModelName.Contains("flash", StringComparison.OrdinalIgnoreCase));
            };

            if (!string.IsNullOrWhiteSpace(specifiedGeminiFriendlyName))
            {
                // Attempt to find by partial match on FriendlyName first, ensuring it's a valid Gemini model for search
                geminiModelInfo = allModels
                    .Where(m => m.FriendlyName.Contains(specifiedGeminiFriendlyName, StringComparison.OrdinalIgnoreCase) &&
                                  isValidGeminiSearchModel(m))
                    .OrderByDescending(m => m.FriendlyName.Equals(specifiedGeminiFriendlyName, StringComparison.OrdinalIgnoreCase)) // Prefer exact friendly name
                    .ThenByDescending(m => m.ModelName.Contains("latest", StringComparison.OrdinalIgnoreCase))
                    .ThenByDescending(m => m.ModelName.Contains("pro", StringComparison.OrdinalIgnoreCase))
                    .FirstOrDefault();

                if (geminiModelInfo == null)
                {
                    // If no match on FriendlyName, try partial match on ModelName as a fallback, still ensuring it's a valid Gemini model for search
                    geminiModelInfo = allModels
                        .Where(m => m.ModelName.Contains(specifiedGeminiFriendlyName, StringComparison.OrdinalIgnoreCase) &&
                                      isValidGeminiSearchModel(m))
                        .OrderByDescending(m => m.ModelName.Equals(specifiedGeminiFriendlyName, StringComparison.OrdinalIgnoreCase)) // Prefer exact model name
                        .ThenByDescending(m => m.ModelName.Contains("latest", StringComparison.OrdinalIgnoreCase))
                        .ThenByDescending(m => m.ModelName.Contains("pro", StringComparison.OrdinalIgnoreCase))
                        .FirstOrDefault();
                }

                if (geminiModelInfo == null)
                {
                    return CreateResult(false, false, $"Error: No suitable Gemini model found matching friendly or model name '{specifiedGeminiFriendlyName}'. Ensure it is configured with ServiceName 'Gemini' and is a Pro/Flash variant.");
                }
                _logger.LogInformation("Using Gemini model '{ModelFriendlyName}' (API: {ModelName}) specified by user (matched '{SpecifiedName}').", geminiModelInfo.FriendlyName, geminiModelInfo.ModelName, specifiedGeminiFriendlyName);
            }
            else // Auto-detection if no specific friendly name is provided
            {
                geminiModelInfo = allModels
                    .Where(isValidGeminiSearchModel)
                    .OrderByDescending(m => m.ModelName.Contains("latest", StringComparison.OrdinalIgnoreCase))
                    .ThenByDescending(m => m.ModelName.Contains("pro", StringComparison.OrdinalIgnoreCase))
                    // Give a slight preference if the FriendlyName indicates it's a default or primary for general use
                    .ThenByDescending(m => m.FriendlyName.Contains("Default", StringComparison.OrdinalIgnoreCase) ||
                                            m.FriendlyName.Contains("Primary", StringComparison.OrdinalIgnoreCase))
                    .FirstOrDefault();

                if (geminiModelInfo != null)
                {
                    _logger.LogInformation("Auto-selected Gemini model '{ModelFriendlyName}' (API: {ModelName}) for search.", geminiModelInfo.FriendlyName, geminiModelInfo.ModelName);
                }
            }

            if (geminiModelInfo == null)
            {
                return CreateResult(false, false, "Error: No suitable Gemini model (e.g., Gemini Pro/Flash using the 'Gemini' service type) found for search. Please configure one or specify its friendly name in this tool's ExtraProperties.");
            }

            var geminiServiceProvider = allProviders.FirstOrDefault(p => p.Guid == geminiModelInfo.ProviderGuid);
            // This check is mostly redundant due to isValidGeminiSearchModel, but good for explicit error
            if (geminiServiceProvider == null || geminiServiceProvider.ServiceName != "Gemini")
            {
                return CreateResult(false, false, $"Error: Service provider for the selected Gemini model '{geminiModelInfo.FriendlyName}' is not configured as a 'Gemini' service type.");
            }

            // --- 2. Construct the prompt for the *internal* Gemini call ---
            var geminiSubPrompt = new StringBuilder();
            geminiSubPrompt.AppendLine($"Perform a Google Search for the query: \"{query}\"");
            if (!string.IsNullOrWhiteSpace(customInstructions))
            {
                geminiSubPrompt.AppendLine($"Follow these additional instructions when processing and summarizing the search results: {customInstructions}");
            }
            else
            {
                geminiSubPrompt.AppendLine("Provide raw search results, with no processing, interpretation or filtering.");
            }
            geminiSubPrompt.AppendLine("Respond directly with a list of URLs. Do not ask for confirmation to perform the search.");

            // --- 3. Make the call to the Gemini IAiService ---
            SendStatusUpdate($"Sending search query to Gemini model '{geminiModelInfo.FriendlyName}'...");

            var geminiAiService = AiServiceResolver.GetAiService(geminiServiceProvider.ServiceName, null, null);
            if (geminiAiService == null)
            {
                // This should not happen if ServiceName was "Gemini" and Gemini.cs is registered.
                return CreateResult(false, false, "Error: Could not resolve the internal Gemini AI service implementation.");
            }

            var tempLinearConv = new LinearConv(DateTime.UtcNow)
            {
                systemprompt = "You are an AI assistant whose sole task is to perform Google searches based on user queries and return the results in a structured and informative way. You have direct access to Google Search.",
                messages = new List<LinearConvMessage>
                {
                    new LinearConvMessage { role = "user", contentBlocks = new List<ContentBlock> { new ContentBlock { ContentType = ContentType.Text, Content = geminiSubPrompt.ToString() } } }
                }
            };

            var requestOptions = new AiRequestOptions
            {
                ServiceProvider = geminiServiceProvider,
                Model = geminiModelInfo,
                Conv = tempLinearConv,
                CancellationToken = CancellationToken.None,
                ApiSettings = _generalSettingsService.CurrentSettings.ToApiSettings(),
                ToolIds = new List<string> { "GEMINI_INTERNAL_GOOGLE_SEARCH" } // Special directive for Gemini.cs
            };

            AiResponse geminiApiResponse;
            try
            {
                // Don't force no tools since we want to enable Google Search
                geminiApiResponse = await geminiAiService.FetchResponse(requestOptions, forceNoTools: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Gemini service for search query '{Query}'.", query);
                return CreateResult(false, false, $"Error communicating with Gemini for search: {ex.Message}");
            }

            if (!geminiApiResponse.Success)
            {
                return CreateResult(false, false, $"Gemini search execution failed: {string.Join("\n\n", geminiApiResponse.ContentBlocks.Where(x => x.ContentType == ContentType.Text).Select(x => x.Content))}");
            }

            // --- 4. Format and return the result ---
            SendStatusUpdate("Gemini search completed. Returning results.");
            return CreateResult(true, true, string.Join("\n\n", geminiApiResponse.ContentBlocks.Where(x => x.ContentType == ContentType.Text).Select(x => x.Content)));
        }

        [McpServerTool, Description("Performs a Google Search using a configured Gemini model that has built-in search capabilities. Returns a summary of the search results and key information. Useful for finding up-to-date information or specific web content when other search tools are not appropriate or when Gemini's specific search integration is desired.")]
        public async Task<string> GeminiGoogleSearch([Description("JSON parameters for GeminiGoogleSearch")] string parameters = "{}")
        {
            return await ExecuteWithExtraProperties(parameters);
        }
    }
}
