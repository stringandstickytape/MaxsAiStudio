using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AiTool3.Topics
{
    public class TopicSet
    {
        [JsonPropertyName(name: "topics")]
        public List<Topic> Topics { get; set; }


        public TopicSet()
        {
            Topics = new List<Topic>();
        }
        public Topic GetDefaultTopic()
        {
            return Topics.Single(x => x.Guid == "00000000-0000-0000-0000-000000000000");
        }
        internal void Save()
        {
            string jsonString = JsonSerializer.Serialize(this, new JsonSerializerOptions
            {
                WriteIndented = true,
                IncludeFields = true
            });

            // Write the JSON to a file
            string fileName = $"topics.json";
            string filePath = Path.Combine(Environment.CurrentDirectory, fileName);
            File.WriteAllText(filePath, jsonString);

            return;
        }

        internal static TopicSet Load()
        {
            TopicSet t;
            if (File.Exists(Path.Combine(Environment.CurrentDirectory, "topics.json")))
            {

                string jsonString = File.ReadAllText(Path.Combine(Environment.CurrentDirectory, "topics.json"));
                t = JsonSerializer.Deserialize<TopicSet>(jsonString, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    IncludeFields = true
                });
            }
            else
            {
                t = new TopicSet();
                t.Topics.Add(new Topic("00000000-0000-0000-0000-000000000000", "<uncategorised>") { Templates = new List<ConversationTemplate>() });
            }

            return t;
        }
    }
}