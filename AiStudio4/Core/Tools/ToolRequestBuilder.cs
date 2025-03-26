using AiStudio4.AiServices;
using AiStudio4.Core.Interfaces;
using Newtonsoft.Json.Linq;
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

        private void RemoveDefaultProperties(JToken token)
        {
            if (token is JObject obj)
            {
                // Create a list of properties to remove to avoid collection modification issues
                List<string> propertiesToRemove = new List<string>();

                foreach (var property in obj.Properties())
                {
                    if (property.Name == "default")
                    {
                        propertiesToRemove.Add(property.Name);
                    }
                    else
                    {
                        // Recursively process nested objects
                        RemoveDefaultProperties(property.Value);
                    }
                }

                // Remove the identified properties
                foreach (var propName in propertiesToRemove)
                {
                    obj.Remove(propName);
                }
            }
            else if (token is JArray array)
            {
                // Process each item in the array
                foreach (var item in array)
                {
                    RemoveDefaultProperties(item);
                }
            }
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

                    // should be deserialized to a string first
                    obj["input_schema"] = JObject.Parse(tool.InputSchema.ToString());

                    switch (format)
                    {
                        case ToolFormat.OpenAI:
 
                            ConfigureOpenAIFormat(request, obj);
                            break;
                        case ToolFormat.Gemini:
                            JObject schema = JObject.Parse(tool.InputSchema.ToString());

                            // Recursively remove all "default" properties
                            RemoveDefaultProperties(schema);
                            FixEmptyObjectProperties(schema);
                            obj["input_schema"] = schema;
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

        private void FixEmptyObjectProperties(JObject schema)
        {
            // Find all objects with type "OBJECT" or "object" that have empty properties
            ProcessObjectWithEmptyProperties(schema);
        }

        private void ProcessObjectWithEmptyProperties(JToken token)
        {
            if (token is JObject obj)
            {
                // Check if this is a schema with type "object" and empty properties
                JToken typeToken = obj["type"];
                if (typeToken != null)
                {
                    string typeValue = typeToken.ToString().ToLowerInvariant();
                    if (typeValue == "object")
                    {
                        // Handle case where properties exists but is empty
                        JToken propertiesToken = obj["properties"];
                        if (propertiesToken != null && propertiesToken is JObject propertiesObj && !propertiesObj.HasValues)
                        {
                            // Add a dummy property
                            propertiesObj["_dummy"] = new JObject
                            {
                                ["type"] = "string",
                                ["description"] = "Placeholder property to satisfy schema requirements"
                            };
                        }
                    }
                }

                // Process all child properties recursively
                foreach (var property in obj.Properties().ToList())
                {
                    ProcessObjectWithEmptyProperties(property.Value);
                }
            }
            else if (token is JArray array)
            {
                // Process all array items recursively
                foreach (var item in array)
                {
                    ProcessObjectWithEmptyProperties(item);
                }
            }
        }


        public async void AddToolToRequest(JObject request, string toolId, ToolFormat format)
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
            //request["tool_choice"] = new JObject
            //{
            //    ["type"] = "auto"
            //};
        }

        private void ConfigureOpenAIFormat(JObject request, JObject toolConfig)
        {
            //toolConfig["schema"] = toolConfig["input_schema"];
            //toolConfig.Remove("input_schema");
            //
            //request["response_format"] = new JObject
            //{
            //    ["type"] = "json_schema",
            //    ["json_schema"] = toolConfig
            //};

            if (!(request["tools"] is JArray toolsArray))
            {
                toolsArray = new JArray();
                request["tools"] = toolsArray;
            }

            // 2. Construct the tool object and add it to the array
            toolsArray.Add(new JObject
            {
                ["type"] = "function",
                ["function"] = new JObject(
                    // Required: name
                    new JProperty("name", toolConfig["name"]),
                    // Optional: description (only add if present in toolConfig)
                    toolConfig.ContainsKey("description") ? new JProperty("description", toolConfig["description"]) : null,
                    // Required: parameters (renamed from input_schema)
                    new JProperty("parameters", toolConfig["input_schema"])
                ) // Note: JObject constructor conveniently ignores null JProperty values
            });

            // 3. Remove the old response_format if it exists
            request.Remove("response_format");
        }

 private void ConfigureGeminiFormat(JObject request, JObject toolConfig)
{
    // 1. Prepare the individual tool config
    if (toolConfig.ContainsKey("input_schema")) // Check before accessing
    {
        toolConfig["parameters"] = toolConfig["input_schema"];
        toolConfig.Remove("input_schema");
    }
    RemoveAllOfAnyOfOneOf(toolConfig); // Clean up schema

    // 2. Ensure the 'tools' structure exists ([ { "function_declarations": [] } ])
    if (request["tools"] is not JArray toolsArray || toolsArray.Count == 0 || toolsArray[0] is not JObject)
    {
        // If 'tools' is missing, not an array, empty, or first item isn't object, create/reset it
        request["tools"] = new JArray { new JObject { ["function_declarations"] = new JArray() } };
    }

    // 3. Ensure 'function_declarations' exists within the first tool object
    var toolDeclarationsObj = (JObject)request["tools"]![0]!; // Safe casts after check above
    if (toolDeclarationsObj["function_declarations"] is not JArray)
    {
        // If missing or not an array, create/reset it
        toolDeclarationsObj["function_declarations"] = new JArray();
    }

    // 4. Add the prepared tool to the declarations array
    var functionDeclarationsArray = (JArray)toolDeclarationsObj["function_declarations"]!;
    functionDeclarationsArray.Add(toolConfig);

    // 5. Set/overwrite the global tool config mode
    request["tool_config"] = new JObject
    {
        ["function_calling_config"] = new JObject { ["mode"] = "ANY" }
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