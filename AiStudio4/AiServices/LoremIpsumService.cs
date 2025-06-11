using AiStudio4.Core.Interfaces;
using AiStudio4.Core.Models;
using AiStudio4.DataModels;
using AiStudio4.Convs;
using AiStudio4.Services.Interfaces;
using Newtonsoft.Json.Linq;
using SharedClasses.Providers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AiStudio4.AiServices
{
    public class LoremIpsumService : AiServiceBase
    {
        private const string LOREM_IPSUM = "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum. Sed ut perspiciatis unde omnis iste natus error sit voluptatem accusantium doloremque laudantium, totam rem aperiam, eaque ipsa quae ab illo inventore veritatis et quasi architecto beatae vitae dicta sunt explicabo. Nemo enim ipsam voluptatem quia voluptas sit aspernatur aut odit aut fugit, sed quia consequuntur magni dolores eos qui ratione voluptatem sequi nesciunt. Neque porro quisquam est, qui dolorem ipsum quia dolor sit amet, consectetur, adipisci velit, sed quia non numquam eius modi tempora incidunt ut labore et dolore magnam aliquam quaerat voluptatem. Ut enim ad minima veniam, quis nostrum exercitationem ullam corporis suscipit laboriosam, nisi ut aliquid ex ea commodi consequatur. Quis autem vel eum iure reprehenderit qui in ea voluptate velit esse quam nihil molestiae consequatur, vel illum qui dolorem eum fugiat quo voluptas nulla pariatur.";
        private readonly Random _random = new Random();

        protected override async Task<AiResponse> FetchResponseInternal(AiRequestOptions options, bool forceNoTools = false)
        {
            var response = new AiResponse
            {
                Success = true,
                TokenUsage = new TokenUsage("0", "0"),
                Duration = TimeSpan.FromMilliseconds(_random.Next(500, 2000))
            };

            // Decide randomly between tool use and text generation
            bool shouldUseTool = !forceNoTools && 
                                options.ToolIds != null && 
                                options.ToolIds.Any(); // 30% chance of tool use

            if (shouldUseTool)
            {
                await HandleToolUseMode(options, response);
            }
            else
            {
                await HandleTextGenerationMode(options, response);
            }

            return response;
        }

        private async Task HandleTextGenerationMode(AiRequestOptions options, AiResponse response)
        {
            // Determine random length (20 to 200 words)
            int wordCount = _random.Next(20, 201);
            
            // Split lorem ipsum into words and create response text
            var words = LOREM_IPSUM.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var responseWords = new List<string>();
            
            for (int i = 0; i < wordCount; i++)
            {
                responseWords.Add(words[i % words.Length]);
            }

            string fullText = string.Join(" ", responseWords);
            response.ResponseText = fullText;

            // Simulate streaming if callbacks are provided
            if (options.OnStreamingUpdate != null)
            {
                foreach (var word in responseWords)
                {
                    if (options.CancellationToken.IsCancellationRequested)
                    {
                        response.IsCancelled = true;
                        break;
                    }

                    // Simulate network delay
                    await Task.Delay(_random.Next(20, 51), options.CancellationToken);
                    
                    options.OnStreamingUpdate(word + " ");
                }

                options.OnStreamingComplete?.Invoke();
            }
        }

        private async Task HandleToolUseMode(AiRequestOptions options, AiResponse response)
        {
            // Select a random tool
            string selectedToolId = options.ToolIds[_random.Next(options.ToolIds.Count)];
            
            // Get tool definition
            var toolDefinition = await ToolService.GetToolByIdAsync(selectedToolId);

            if (toolDefinition == null)
            {
                // Fallback to text generation if tool not found
                await HandleTextGenerationMode(options, response);
                return;
            }

            // Generate parameters based on tool schema
            var generatedParams = GenerateToolParameters(JObject.Parse(toolDefinition.Schema));

            // Create tool response
            var toolResponse = new ToolResponse();
            toolResponse.Tools.Add(new ToolResponseItem
            {
                ToolName = toolDefinition.Name,
                ResponseText = generatedParams
            });

            response.ToolResponseSet = toolResponse;
            response.ResponseText = string.Empty;
            response.ChosenTool = selectedToolId;
        }

        private string GenerateToolParameters(JObject toolDefinition)
        {
            var parameters = new JObject();
            
            if (toolDefinition["input_schema"]?["properties"] is JObject properties)
            {
                var requiredFields = toolDefinition["input_schema"]?["required"]?.ToObject<string[]>() ?? new string[0];
                
                foreach (var property in properties)
                {
                    string fieldName = property.Key;
                    var fieldDefinition = property.Value as JObject;
                    string fieldType = fieldDefinition?["type"]?.ToString() ?? "string";

                    // Only generate required fields to keep it simple
                    if (requiredFields.Contains(fieldName))
                    {
                        parameters[fieldName] = GenerateValueForType(fieldType);
                    }
                }
            }

            return parameters.ToString();
        }

        private JToken GenerateValueForType(string type)
        {
            return type.ToLower() switch
            {
                "string" => GenerateLoremIpsumSnippet(3, 8),
                "integer" => _random.Next(1, 1000),
                "number" => Math.Round(_random.NextDouble() * 1000, 2),
                "boolean" => _random.NextDouble() < 0.5,
                "array" => new JArray(
                    GenerateLoremIpsumSnippet(2, 5),
                    GenerateLoremIpsumSnippet(2, 5),
                    GenerateLoremIpsumSnippet(2, 5)
                ),
                _ => GenerateLoremIpsumSnippet(3, 8)
            };
        }

        private string GenerateLoremIpsumSnippet(int minWords, int maxWords)
        {
            int wordCount = _random.Next(minWords, maxWords + 1);
            var words = LOREM_IPSUM.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var selectedWords = new List<string>();
            
            for (int i = 0; i < wordCount; i++)
            {
                selectedWords.Add(words[i % words.Length]);
            }
            
            return string.Join(" ", selectedWords);
        }

        // Required abstract method implementations with minimal functionality
        protected override JObject CreateRequestPayload(string modelName, LinearConv conv, ApiSettings apiSettings)
        {
            // Not used for Lorem Ipsum service
            return new JObject();
        }

        protected override async Task<AiResponse> HandleStreamingResponse(
            HttpContent content,
            CancellationToken cancellationToken,
            Action<string> onStreamingUpdate,
            Action onStreamingComplete)
        {
            // Not used for Lorem Ipsum service
            return await Task.FromResult(new AiResponse { Success = false });
        }
    }
}