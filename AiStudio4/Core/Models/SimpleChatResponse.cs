using System;

namespace AiStudio4.Core.Models
{
    public class SimpleChatResponse
    {
        public bool Success { get; set; }
        public string ResponseText { get; set; }
        public string Error { get; set; }
        public TimeSpan ProcessingTime { get; internal set; }
    }
}