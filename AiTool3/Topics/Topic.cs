using System.Text.Json.Serialization;

namespace AiTool3.Topics
{
    public class Topic
    {
        [JsonPropertyName(name: "name")]
        public string Name { get; set; }

        [JsonPropertyName(name: "guid")]
        public string Guid { get; set; }
        [JsonPropertyName(name: "templates")]
        public List<ConversationTemplate> Templates { get; set; }

        public Topic()
        {
            Templates = new List<ConversationTemplate>();
        }

        public Topic(string guid, string name)
        {
            Guid = guid;
            Name = name;
            Templates = new List<ConversationTemplate>();
        }
    }

    public class ConversationTemplate
    {
        public string SystemPrompt { get; set; }
        public string InitialPrompt { get; set; }

        public string TemplateName { get; set; }
        public ConversationTemplate(string systemPrompt, string initialPrompt)
        {
            SystemPrompt = systemPrompt;
            InitialPrompt = initialPrompt;
        }

        public override string ToString()
        {
            return $"{TemplateName}";
        }
    }
}
