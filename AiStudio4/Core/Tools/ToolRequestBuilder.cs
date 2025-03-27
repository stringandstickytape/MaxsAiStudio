using AiStudio4.AiServices;
using AiStudio4.Core.Interfaces;
using Newtonsoft.Json.Linq;
using OpenAI.Chat;
using System.Text.RegularExpressions;

namespace AiStudio4.Core.Tools
{
    public class ToolRequestBuilder
    {
        private readonly IToolService toolService;
        private readonly IMcpService mcpService;

        public ToolRequestBuilder(IToolService toolService, IMcpService mcpService)
        {
            this.toolService = toolService;
            this.mcpService = mcpService;
        }

        public async Task AddMcpServiceToolsToRequestAsync(JObject request, ToolFormat format)
        {
            var serverDefinitions = await mcpService.GetAllServerDefinitionsAsync();

            foreach(var serverDefinition in serverDefinitions.Where(x => x.IsEnabled))
            {
                var tools = await mcpService.ListToolsAsync(serverDefinition.Id);

                foreach (var tool in tools)
                {
                    var obj = new JObject();
                    obj["name"] = tool.Name.ToString();
                    obj["description"] = tool.Description.ToString();
                    obj["input_schema"] =  JToken.Parse(tool.InputSchema.ToString());

                    switch (format)
                    {
                        case ToolFormat.OpenAI:


                            ConfigureOpenAIFormat(request, obj);
                            break;
                        case ToolFormat.Gemini:
                            ConfigureGeminiFormat(request, obj);
                            break;
                        case ToolFormat.Ollama:
                            ConfigureOllamaFormat(request, obj);
                            break;
                        case ToolFormat.Claude:
                            ConfigureClaudeFormat(request, obj);
                            break;
                    }
                }

            }

        }

        public IMcpService GetMcpService()
        {
            return mcpService;
        }



        public async Task AddToolToRequestAsync(JObject request, string toolId, ToolFormat format)
        {
            var tool = await toolService.GetToolByIdAsync(toolId);
            if (tool == null) return;

            var firstLine = tool.Schema.Split("\n")[0]
                .Replace("//", "")
                .Replace(" ", "")
                .Replace("\r", "")
                .Replace("\n", "");
            
            var toolText = Regex.Replace(tool.Schema, @"^//.*\n", "", RegexOptions.Multiline);
            var toolConfig = JObject.Parse(toolText);
            
            switch (format)
            {
                case ToolFormat.OpenAI:
                    ConfigureOpenAIFormat(request, toolConfig);
                    break;
                case ToolFormat.Gemini:
                    ConfigureGeminiFormat(request, toolConfig);
                    break;
                case ToolFormat.Ollama:
                    ConfigureOllamaFormat(request, toolConfig);
                    break;
                case ToolFormat.Claude:
                    ConfigureClaudeFormat(request, toolConfig);
                    break;
            }
        }

        private void ConfigureClaudeFormat(JObject request, JObject toolConfig)
        {
            // Check if tools array exists
            if (request["tools"] == null)
            {
                request["tools"] = new JArray();
            }

            // Add the tool to the array
            ((JArray)request["tools"]).Add(toolConfig);

            // Set tool_choice to "any" to force Claude to use one of the provided tools
            request["tool_choice"] = new JObject
            {
                ["type"] = "any"
            };
        }

        private void ConfigureOpenAIFormat(JObject request, JObject toolConfig)
        {
            toolConfig["schema"] = toolConfig["input_schema"];
            toolConfig.Remove("input_schema");

            request["response_format"] = new JObject
            {
                ["type"] = "json_schema",
                ["json_schema"] = toolConfig
            };
        }

        private void ConfigureGeminiFormat(JObject request, JObject toolConfig)
        {
            toolConfig["parameters"] = toolConfig["input_schema"];
            toolConfig.Remove("input_schema");
            RemoveAllOfAnyOfOneOf(toolConfig);

            request["tools"] = new JArray
            {
                new JObject
                {
                    ["function_declarations"] = new JArray { toolConfig }
                }
            };
            request["tool_config"] = new JObject
            {
                ["function_calling_config"] = new JObject
                {
                    ["mode"] = "ANY"
                }
            };
        }

        private void ConfigureOllamaFormat(JObject request, JObject toolConfig)
        {
            request["format"] = toolConfig;
        }

        private void RemoveAllOfAnyOfOneOf(JObject obj)
        {
            if (obj == null) return;

            var propertiesToRemove = new List<string>();
            foreach (var property in obj.Properties())
            {
                if (property.Name == "allOf" || property.Name == "anyOf" || property.Name == "oneOf")
                {
                    propertiesToRemove.Add(property.Name);
                }
                else if (property.Value is JObject)
                {
                    RemoveAllOfAnyOfOneOf((JObject)property.Value);
                }
                else if (property.Value is JArray)
                {
                    foreach (var item in (JArray)property.Value)
                    {
                        if (item is JObject)
                        {
                            RemoveAllOfAnyOfOneOf((JObject)item);
                        }
                    }
                }
            }

            foreach (var prop in propertiesToRemove)
            {
                obj.Remove(prop);
            }
        }

    }
}