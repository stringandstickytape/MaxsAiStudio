using SharedClasses.Providers;
using System.Text.Json.Serialization;

namespace AiStudio4.InjectedDependencies
{
    public class v4BranchedConversationMessage
    {
        public v4BranchedConversationMessageRole Role { get; set; }

        [JsonIgnore] // Ignore for serialization to avoid circular references
        public List<v4BranchedConversationMessage> Children { get; set; } = new List<v4BranchedConversationMessage>();

        public string UserMessage { get; set; }
        public string Id { get; set; }

        // Add explicit parent reference
    public string ParentId { get; set; }

        public TokenUsage TokenUsage { get; set; }
    }
}