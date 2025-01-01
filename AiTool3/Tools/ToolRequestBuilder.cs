using AiTool3.Tools;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;

namespace AiTool3.Tools
{
    public class ToolRequestBuilder
    {
        private readonly ToolManager toolManager;

        public ToolRequestBuilder(ToolManager toolManager)
        {
            this.toolManager = toolManager;
        }

        public void AddToolToRequest(JObject request, string toolId, ToolFormat format)
        {
            var toolObj = toolManager.Tools.First(x => x.Name == toolId);
            var firstLine = toolObj.FullText.Split("\n")[0]
                .Replace("//", "")
                .Replace(" ", "")
                .Replace("\r", "")
                .Replace("\n", "");

            var tool = toolManager.Tools.First(x => x.InternalName == firstLine);
            var toolText = Regex.Replace(tool.FullText, @"^//.*\n", "", RegexOptions.Multiline);
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
            }
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

    public enum ToolFormat
    {
        OpenAI,
        Gemini,
        Ollama
    }
}