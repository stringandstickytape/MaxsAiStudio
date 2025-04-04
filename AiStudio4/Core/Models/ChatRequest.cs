using SharedClasses.Providers;
using System.Collections.Generic;

namespace AiStudio4.Core.Models
{
    public class ChatRequest
    {
        public string ClientId { get; set; }
        public string ConvId { get; set; }
        public string MessageId { get; set; }
        public string ParentMessageId { get; set; }
        public string Message { get; set; }
        public string Model { get; set; }
        public List<MessageHistoryItem> MessageHistory { get; set; } = new List<MessageHistoryItem>();
        public List<string> ToolIds { get; set; } = new List<string>();
        public string SystemPromptId { get; set; }
        public string SystemPromptContent { get; set; }
        public CancellationToken CancellationToken { get; set; } = CancellationToken.None;

        // Callbacks for streaming updates
        public Action<string> OnStreamingUpdate { get; set; }
        public Action OnStreamingComplete { get; set; }
    }
}