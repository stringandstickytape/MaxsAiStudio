using System;

namespace AiStudio4.McpStandalone.Models
{
    public class McpServerConfiguration
    {
        public string Name { get; set; } = string.Empty;
        public bool IsEnabled { get; set; } = true;
        public string Description { get; set; } = string.Empty;
    }
}