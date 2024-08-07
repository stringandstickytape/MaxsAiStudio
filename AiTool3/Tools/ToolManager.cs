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
                        // get first and second lines of json file
                        string[] lines = json.Split(new[] { Environment.NewLine }, StringSplitOptions.None);

                        string firstLine = lines[0].Replace("//", "").Replace(" ", "").Replace("\r","").Replace("\n","").Trim();
                        string secondLine = lines[1].Replace("//", "").Replace(" ", "").Replace("\r", "").Replace("\n", "").Trim();

                        Tool tool = JsonConvert.DeserializeObject<Tool>(json);

                        tool.InternalName = firstLine;
                        tool.OutputFilename = secondLine;

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

        internal Tool GetToolByLabel(string v)
        {
            return Tools.FirstOrDefault(t => t.Name == v);
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