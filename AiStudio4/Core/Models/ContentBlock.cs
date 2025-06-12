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
    }
}