using AiStudio4.Core.Models;
using SharedClasses.Providers;
using System.Text.Json.Serialization;

namespace AiStudio4.InjectedDependencies
{
    public class v4BranchedConvMessage
    {
        public v4BranchedConvMessageRole Role { get; set; }

        [JsonIgnore] // Ignore for serialization to avoid circular references
        public List<v4BranchedConvMessage> Children { get; set; } = new List<v4BranchedConvMessage>();

        public string UserMessage { get; set; }
        public string Id { get; set; }

        // Add explicit parent reference
    public string ParentId { get; set; }

        public TokenUsage TokenUsage { get; set; }
        public TokenCost CostInfo { get; set; }
    }
}