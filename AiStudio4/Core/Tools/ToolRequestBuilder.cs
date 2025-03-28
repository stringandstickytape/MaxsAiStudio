using AiStudio4.AiServices;
using AiStudio4.Core.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Newtonsoft.Json.Linq;
using OpenAI.Chat;
using System.Diagnostics;
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

                int ctr = 0;

                foreach (var tool in tools)
                {
                    if (ctr == 2)
                    {
                        ctr++;
                        continue;
                    }

                    //if (ctr == 4)
                    //    continue;

                    var obj = new JObject();
                    obj["name"] = $"{serverDefinition.Id.Replace(" ", "")}_{tool.Name.Replace(" ", "")}";
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

                    ctr++;
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

        private JObject ConvertToolToGemini(JObject toolConfig)
        {
            var functionObject = toolConfig;
            var parameters = functionObject["parameters"];
            var properties = parameters["properties"];
            var required = parameters["required"];

            var convertedProperties = new JObject();
            foreach (var property in properties.Cast<JProperty>())
            {
                
                var prop = property.Value;
                if (prop["type"] != null)
                {
                    convertedProperties[property.Name] = new JObject
                    {
                        ["type"] = prop["type"].ToString().ToUpper(),
                        ["description"] = prop["description"]
                    };
                }
            }

            return new JObject
            {
                    ["name"] = functionObject["name"],
                    ["description"] = functionObject["description"],
                    ["parameters"] = new JObject
                    {
                        ["type"] = parameters["type"].ToString().ToUpper(),
                        ["properties"] = convertedProperties,
                        ["required"] = required
                    }
            };
        }

        public static JObject RemoveDefaultProperties(JObject jObject)
        {
            if (jObject == null)
                return null;

            // Find properties to remove at the current level
            var propertiesToRemove = jObject.Properties()
                .Where(p => p.Name == "default" ||
                           (p.Name == "type" && p.Value.Type == JTokenType.String && p.Value.ToString() == "null"))
                .ToList();

            // Remove the found properties
            foreach (var property in propertiesToRemove)
            {
                property.Remove();
            }



            // Process nested objects - using ToList() to create a copy since we may modify during iteration
            foreach (var property in jObject.Properties().ToList())
            {
                if (property.Value is JObject nestedObject)
                {
                    // Recursively process nested objects
                    RemoveDefaultProperties(nestedObject);

                    // Remove the property if the nested object is now empty
                    if (!nestedObject.HasValues)
                    {
                        property.Remove();
                    }
                }
                else if (property.Value is JArray jArray)
                {
                    // Process arrays of objects
                    ProcessJArray(jArray);

                    // Remove the property if the array is now empty
                    if (jArray.Count == 0)
                    {
                        property.Remove();
                    }
                }
            }

            // Rename additionalProperties to properties
            var additionalPropertiesProperty = jObject.Property("additionalProperties");
            if (additionalPropertiesProperty != null)
            {
                // Create a new property with the name "properties" and the value from "additionalProperties"
                jObject["properties"] = new JObject();
                jObject["properties"]["additionalProperties"] = additionalPropertiesProperty.Value;
                jObject["properties"]["type"] = "object";
                // Remove the old "additionalProperties" property
                additionalPropertiesProperty.Remove();
            }

            return jObject;
        }

        private static void ProcessJArray(JArray jArray)
        {
            // Process each item in the array - using ToList() to create a copy since we may modify during iteration
            for (int i = jArray.Count - 1; i >= 0; i--)
            {
                var item = jArray[i];

                if (item is JObject nestedObject)
                {
                    // Recursively process objects in the array
                    RemoveDefaultProperties(nestedObject);

                    // Remove the item if the object is now empty
                    if (!nestedObject.HasValues)
                    {
                        jArray.RemoveAt(i);
                    }
                }
                else if (item is JArray nestedArray)
                {
                    // Recursively process nested arrays
                    ProcessJArray(nestedArray);

                    // Remove the item if the nested array is now empty
                    if (nestedArray.Count == 0)
                    {
                        jArray.RemoveAt(i);
                    }
                }
            }
        }

        private void ConfigureGeminiFormat(JObject request, JObject toolConfig)
        {
            toolConfig["parameters"] = toolConfig["input_schema"];
            toolConfig.Remove("input_schema");
            RemoveDefaultProperties(toolConfig);

            if (request["tools"] == null)
            {
                request["tools"] = new JArray
                {
                    new JObject
                    {
                        ["function_declarations"] = new JArray( )
                    }
                };
            };

            ((JArray)request["tools"][0]["function_declarations"]).Add(toolConfig);
            

              /*  request["tools"] = new JArray
            {
                new JObject
                {
                    ["function_declarations"] = new JArray { toolConfig }
                }
            };*/



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

        private void RemoveDeletedAndAddditionalProps(JObject obj)
        {
            if (obj == null) return;


        }

    }
}