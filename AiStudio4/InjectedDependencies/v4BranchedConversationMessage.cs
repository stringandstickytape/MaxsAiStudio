using AiStudio4.Core.Models;
using SharedClasses.Providers;
using System.Text.Json.Serialization;

namespace AiStudio4.InjectedDependencies
{
    public class v4BranchedConvMessage
    {
        public v4BranchedConvMessageRole Role { get; set; }

        // Removed Children collection for flat structure

        public string UserMessage { get; set; }
        public string Id { get; set; }

        // Add explicit parent reference
    public string ParentId { get; set; }

        public TokenCost CostInfo { get; set; }
    }
}