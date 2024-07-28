
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;

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

            string[] resourceNames = assembly.GetManifestResourceNames()
                .Where(name => name.StartsWith(namespacePrefix) && name.EndsWith(".json"))
                .ToArray();

            foreach (string resourceName in resourceNames)
            {
                string json = AssemblyHelper.GetEmbeddedAssembly(resourceName);
                if (!string.IsNullOrEmpty(json))
                {
                    try
                    {
                        Tool tool = JsonConvert.DeserializeObject<Tool>(json);
                        if (tool != null)
                        {
                            tool.FullText = json;
                            if (tool != null)
                            {
                                Tools.Add(tool);
                                Console.WriteLine($"Loaded tool: {tool.Name}");
                            }
                        }
                    }
                    catch (JsonException ex)
                    {
                        Console.WriteLine($"Error parsing JSON for {resourceName}: {ex.Message}");
                    }
                }
                else
                {
                    Console.WriteLine($"Failed to load resource: {resourceName}");
                }
            }
        }
    }

    public class Tool
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string FullText { get; set; }
        // Add other properties as needed
    }

    public static class AssemblyHelper
    {
        public static string GetEmbeddedAssembly(string resourceName)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                    return null;

                using (StreamReader reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }
    }
}