// AiStudio4/Core/Tools/SecondAiOpinionTool.cs
using AiStudio4.AiServices;
using AiStudio4.Convs;
using AiStudio4.Core.Interfaces;
using AiStudio4.Core.Models;
using AiStudio4.DataModels;
using AiStudio4.InjectedDependencies;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SharedClasses.Providers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AiStudio4.Core.Tools
{
    public class SecondAiOpinionTool : BaseToolImplementation
    {


        public SecondAiOpinionTool(
            ILogger<SecondAiOpinionTool> logger,
            IGeneralSettingsService generalSettingsService,
            IStatusMessageService statusMessageService
            )
            : base(logger, generalSettingsService, statusMessageService)
        {

        }

        public override Tool GetToolDefinition()
        {
            return new Tool
            {
                Guid = ToolGuids.SECOND_AI_OPINION_TOOL_GUID,
                Name = "SecondAiOpinion",
                Description = "Gets a second opinion from another configured AI model on a given prompt. The secondary model will have no access to the current conversation history or any tools. All necessary context must be provided in the prompt.",
                Schema = """
                {
                  "name": "SecondAiOpinion",
                  "description": "Gets a second opinion from another configured AI model on a given prompt. The secondary model will have no access to the current conversation history or any tools. All necessary context must be provided in the prompt.",
                  "input_schema": {
                    "type": "object",
                    "properties": {
                      "prompt": {
                        "type": "string",
                        "description": "The full prompt or question to send to the secondary model. Must contain all necessary context."
                      },
                      "system_prompt": {
                        "type": "string",
                        "description": "An optional system prompt to guide the behavior of the secondary model for this specific query."
                      }
                    },
                    "required": ["prompt"]
                  }
                }
                """,
                Categories = new List<string> { "MaxCode-Alt" },
                OutputFileType = "txt",
                ExtraProperties = new Dictionary<string, string>
                {
                    { "model", "" }
                }
            };
        }

        public override async Task<BuiltinToolResult> ProcessAsync(string toolParameters, Dictionary<string, string> extraProperties)
        {
            SendStatusUpdate("Initializing SecondAiOpinion tool...");
            var parameters = JsonConvert.DeserializeObject<Dictionary<string, object>>(toolParameters);

            string prompt = parameters?["prompt"]?.ToString();
            string systemPrompt = parameters?.GetValueOrDefault("system_prompt")?.ToString() ?? "You are a helpful assistant.";

            if (string.IsNullOrWhiteSpace(prompt))
            {
                return CreateResult(true, true, "Error: 'prompt' parameter is required.");
            }

            if (!extraProperties.TryGetValue("model", out var modelGuid) || string.IsNullOrWhiteSpace(modelGuid))
            {
                return CreateResult(true, true, "Error: The 'model' to use must be configured in the tool's Extra Properties.");
            }
            SendStatusUpdate($"Looking for model: {modelGuid}...");

            var modelToUse = _generalSettingsService.CurrentSettings.ModelList.FirstOrDefault(m => m.Guid.Equals(modelGuid, StringComparison.OrdinalIgnoreCase));
            if (modelToUse == null)
            {
                return CreateResult(true, true, $"Error: Model with friendly name '{modelGuid}' not found in settings.");
            }

            var provider = ServiceProvider.GetProviderForGuid(_generalSettingsService.CurrentSettings.ServiceProviders, modelToUse.ProviderGuid);
            if (provider == null)
            {
                return CreateResult(true, true, $"Error: Service provider for model '{modelGuid}' not found.");
            }

            var aiService = AiServiceResolver.GetAiService(provider.ServiceName, null, null);
            if (aiService == null)
            {
                return CreateResult(true, true, $"Error: Could not resolve AI service for provider '{provider.FriendlyName}'.");
            }
            SendStatusUpdate($"Contacting {modelGuid}...");

            var singleShotConv = new LinearConv(DateTime.UtcNow)
            {
                systemprompt = systemPrompt,
                messages = new List<LinearConvMessage>
                {
                    new LinearConvMessage { role = "user", content = prompt }
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
                SendStatusUpdate($"Error response from {modelGuid}.");
                return CreateResult(true, true, $"Error from {modelGuid}: {response.ResponseText}");
            }

            var resultBuilder = new StringBuilder();
            resultBuilder.AppendLine($"### Response from [{modelToUse.FriendlyName}]");
            resultBuilder.AppendLine("---");
            resultBuilder.AppendLine(response.ResponseText);

            SendStatusUpdate($"Received response from {modelGuid}.");
            return CreateResult(true, true, resultBuilder.ToString());
        }
    }
}