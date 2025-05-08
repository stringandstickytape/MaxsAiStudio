using System;
using System.Collections.Generic;

namespace AiStudio4.Core.Models
{
    public class UserPrompt
    {
        public string Guid { get; set; } = System.Guid.NewGuid().ToString();
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsFavorite { get; set; } = false;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;
        public List<string> Tags { get; set; } = new List<string>();
        public string Shortcut { get; set; } = string.Empty;
    }

    // Form values model for creating/updating user prompts
    public class UserPromptFormValues
    {
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsFavorite { get; set; } = false;
        public List<string> Tags { get; set; } = new List<string>();
        public string Shortcut { get; set; } = string.Empty;
    }
}