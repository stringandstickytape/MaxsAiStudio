namespace AiStudio4.McpStandalone.Models
{
    public class McpTool
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public bool IsSelected { get; set; }
        public string ServerId { get; set; } = string.Empty;
    }
}