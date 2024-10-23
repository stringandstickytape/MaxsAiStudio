using AiTool3.Tools;
using SharedClasses.Helpers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AiTool3.Topics
{
    public class TopicSet
    {
        [JsonPropertyName(name: "topics")]
        public List<Topic> Categories { get; set; }

        internal void Save()
        {
            string jsonString = JsonSerializer.Serialize(this, new JsonSerializerOptions
            {
                WriteIndented = true,
                IncludeFields = true
            });

            // Write the JSON to a file
            string fileName = $"Templates\\templates.json";
            string filePath = Path.Combine(Environment.CurrentDirectory, fileName);

            // Create the path if it doesn't exist
            if (!Directory.Exists(Path.GetDirectoryName(filePath)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            }
            File.WriteAllText(filePath, jsonString);

            return;
        }

        internal static TopicSet Load()
        {
            TopicSet t;

            string jsonText = "";

            if (File.Exists(Path.Combine(Environment.CurrentDirectory, "Templates\\templates.json")))
            {

                jsonText = File.ReadAllText(Path.Combine(Environment.CurrentDirectory, "Templates\\templates.json"));
            }
            else jsonText = AssemblyHelper.GetEmbeddedResource("SharedClasses.Defaults.templates.json");

            t = JsonSerializer.Deserialize<TopicSet>(jsonText, new JsonSerializerOptions
            {
                WriteIndented = true,
                IncludeFields = true,
                AllowTrailingCommas = true
            });

            if (!File.Exists(Path.Combine(Environment.CurrentDirectory, "Templates\\templates.json")))
                t.Save();

            return t;
        }
    }
}