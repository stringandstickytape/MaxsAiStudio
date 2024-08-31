using Newtonsoft.Json;
using SharedClasses.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace AiTool3.Tools
{
    public class ToolManager
    {
        public List<Tool> Tools { get; private set; }

        public ToolManager()
        {
            Tools = new List<Tool>();
            LoadTools();
        }

        private void LoadTools()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            string namespacePrefix = "AiTool3.Tools.";

            var resourceNames = assembly.GetManifestResourceNames()
                .Where(name => name.StartsWith(namespacePrefix) && name.EndsWith(".json"));

            foreach (string resourceName in resourceNames)
            {
                string json = AssemblyHelper.GetEmbeddedResource(assembly, resourceName);
                if (string.IsNullOrEmpty(json))
                {
                    Console.WriteLine($"Failed to load resource: {resourceName}");
                    continue;
                }

                try
                {
                    var lines = json.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
                    string firstLine = CleanLine(lines[0]);
                    string secondLine = CleanLine(lines[1]);

                    Tool tool = JsonConvert.DeserializeObject<Tool>(json);
                    if (tool == null) continue;

                    tool.InternalName = firstLine;
                    tool.OutputFilename = secondLine;
                    tool.FullText = json;

                    Tools.Add(tool);
                    Console.WriteLine($"Loaded tool: {tool.Name}");
                }
                catch (JsonException ex)
                {
                    Console.WriteLine($"Error parsing JSON for {resourceName}: {ex.Message}");
                }
            }
        }

        private string CleanLine(string line)
        {
            return line.Replace("//", "").Replace(" ", "").Replace("\r", "").Replace("\n", "").Trim();
        }

        public Tool GetToolByLabel(string label)
        {
            return Tools.FirstOrDefault(t => t.Name == label);
        }
    }

    public class Tool
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string FullText { get; set; }
        public string InternalName { get; set; }
        public string OutputFilename { get; set; }
        // Add other properties as needed
    }
}