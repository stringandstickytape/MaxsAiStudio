

namespace AiStudio4.DataModels
{
    public class Attachment
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; }
        public string Type { get; set; }
        public string Content { get; set; }
        public long Size { get; set; }
        
        // Optional metadata
        public int? Width { get; set; }
        public int? Height { get; set; }
        public string TextContent { get; set; }
        public long? LastModified { get; set; }
    }
}
