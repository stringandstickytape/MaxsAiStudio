// AiStudio4/Core/Models/ContentType.cs
namespace AiStudio4.Core.Models
{
    /// <summary>
    /// Enumerates the supported content block types inside a message. New values can be appended
    /// as richer message parts (e.g. images, code, etc.) are implemented.
    /// </summary>
    public enum ContentType
    {
        Text = 0,
        System = 1,
        AiHidden = 2,
        Tool = 3,
        ToolResponse = 4
    }
}