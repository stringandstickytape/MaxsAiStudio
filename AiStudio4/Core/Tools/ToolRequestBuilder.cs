using AiStudio4.AiServices;

using Microsoft.AspNetCore.Authentication;


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
                var allToolsForServer = await mcpService.ListToolsAsync(serverDefinition.Id);
                IEnumerable<ModelContextProtocol.Protocol.Tool> toolsToConsider;

                // If SelectedTools is configured and not empty, filter by it
                if (serverDefinition.SelectedTools != null && serverDefinition.SelectedTools.Any())
                {
                    toolsToConsider = allToolsForServer.Where(t => serverDefinition.SelectedTools.Contains(t.Name));
                }
                // NEW BEHAVIOR: If SelectedTools is null or empty, ALL tools from this server will be exposed
                else
                {
                    toolsToConsider = allToolsForServer;
                }

                foreach (var tool in toolsToConsider)
                {
                    var obj = new JObject();
                    // Use the server ID as a prefix to ensure tool name uniqueness across different MCP servers
                    obj["name"] = $"{tool.Name.Replace(" ", "")}";
                    obj["description"] = tool.Description?.ToString() ?? "No description provided."; // Handle null description
                    
                    // Ensure InputSchema is not null before trying to parse
                    //if (tool.InputSchema != null)
                    {
                        try
                        {
                            obj["input_schema"] = JToken.Parse(tool.InputSchema.ToString());
                        }
                        catch (JsonReaderException ex)
                        {
                            // Log the error and potentially skip this tool or use a default schema
                            System.Diagnostics.Debug.WriteLine($"Error parsing input schema for tool {tool.Name} on server {serverDefinition.Id}: {ex.Message}");
                            obj["input_schema"] = new JObject(new JProperty("type", "object"), new JProperty("properties", new JObject())); // Default empty schema
                        }
                    }


                    switch (format)
                    {
                        case ToolFormat.OpenAI:


                            ConfigureOpenAIFormat(request, obj);
                            break;
                        case ToolFormat.Gemini:
                            ConfigureGeminiFormat(request, obj);
                            break;
                        case ToolFormat.Claude:
                            ConfigureClaudeFormat(request, obj);
                            break;
                    }

                    //ctr++;
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
            // Ensure the tools array exists
            if (request["tools"] == null)
            {
                request["tools"] = new JArray();
            }

            // Get the parameters schema
            var parametersSchema = toolConfig["input_schema"] as JObject;
            if (parametersSchema != null)
            {
                // Recursively add "additionalProperties": false
                SetAdditionalPropertiesFalse(parametersSchema);
            }
            else
            {
                // Handle cases where input_schema might be missing or not an object
                // For now, we'll create an empty object schema
                parametersSchema = new JObject { ["type"] = "object", ["properties"] = new JObject() };
                SetAdditionalPropertiesFalse(parametersSchema); // Add additionalProperties: false even to empty schema
            }

            // Create the correctly formatted tool object for OpenAI
            var openAiTool = new JObject
            {
                ["type"] = "function",
                ["function"] = new JObject
                {
                    ["name"] = toolConfig["name"],
                    ["description"] = toolConfig["description"],
                    ["parameters"] = parametersSchema
                }
            };

            // Add the tool to the tools array
            ((JArray)request["tools"]).Add(openAiTool);

            // Set tool_choice to "auto" which is OpenAI's default when tools are present
            request["tool_choice"] = "auto";
        }

        // Helper function to recursively add "additionalProperties": false
        private void SetAdditionalPropertiesFalse(JObject schemaObject)
        {
            if (schemaObject == null) return;

            // Check if it's an object type with properties
            if (schemaObject["type"]?.ToString() == "object" && schemaObject["properties"] is JObject properties)
            {
                // Add additionalProperties: false to this object
                schemaObject["additionalProperties"] = false;

                // Recursively process properties
                foreach (var property in properties.Properties())
                {
                    if (property.Value is JObject subSchema)
                    {
                        SetAdditionalPropertiesFalse(subSchema);
                    }
                    else if (property.Value is JObject itemSchemaContainer && itemSchemaContainer["items"] is JObject itemSchema)
                    { // Handle array items schema
                         if(itemSchemaContainer["type"]?.ToString() == "array")
                         {
                            SetAdditionalPropertiesFalse(itemSchema);
                         }
                    }
                     else if (property.Value is JObject itemSchemaContainerComplex && itemSchemaContainerComplex["type"]?.ToString() == "array" && itemSchemaContainerComplex["items"] is JObject itemSchemaComplex)
                    { // Handle array items schema explicitly
                        SetAdditionalPropertiesFalse(itemSchemaComplex);
                    }
                }
            }
             // Handle array items directly if the top level is an array schema
            else if (schemaObject["type"]?.ToString() == "array" && schemaObject["items"] is JObject itemSchema)
            {
                 SetAdditionalPropertiesFalse(itemSchema);
            }
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
