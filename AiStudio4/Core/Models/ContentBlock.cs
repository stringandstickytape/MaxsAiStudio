// AiStudio4/Core/Models/ContentBlock.cs
using System.Text.Json.Serialization;

namespace AiStudio4.Core.Models
{
    /// <summary>
    /// Represents a single piece of message content. A message may consist of multiple
    /// content blocks of potentially different types (e.g. text, code, images).
    /// </summary>
    public class ContentBlock
    {
        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;

        [JsonPropertyName("contentType")]
        public ContentType ContentType { get; set; } = ContentType.Text;

        /// <summary>
        /// The actual tool call ID from the AI service API (e.g., OpenAI tool call ID).
        /// Used for proper conversation resumption and tool call/response relationships.
        /// </summary>
        [JsonPropertyName("toolCallId")]
        public string? ToolCallId { get; set; }

        /// <summary>
        /// The name of the tool being called. Used with ToolCall content type.
        /// </summary>
        [JsonPropertyName("toolName")]
        public string? ToolName { get; set; }

        /// <summary>
        /// The tool arguments as JSON string. Used with ToolCall content type.
        /// </summary>
        [JsonPropertyName("toolArguments")]
        public string? ToolArguments { get; set; }
    }
}