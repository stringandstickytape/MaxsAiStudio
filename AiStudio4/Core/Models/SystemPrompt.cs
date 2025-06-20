


namespace AiStudio4.Core.Models
{
    public class SystemPrompt
    {
        public string Guid { get; set; } = System.Guid.NewGuid().ToString();
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsDefault { get; set; } = false;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;
        public List<string> Tags { get; set; } = new List<string>();
        public List<string> AssociatedTools { get; set; } = new List<string>(); // Tool GUIDs
        public string AssociatedUserPromptId { get; set; } = string.Empty; // Associated User Prompt GUID
        public List<string> AssociatedMcpServers { get; set; } = new List<string>(); // MCP Server IDs

        // NEW: Model associations
        public string PrimaryModelGuid { get; set; } = string.Empty; // Associated primary model GUID
        public string SecondaryModelGuid { get; set; } = string.Empty; // Associated secondary model GUID
        public bool IncludeGitDiff { get; set; } = false; // Whether to include git diff when this prompt is applied
        
        // NEW: Project association
        public string ProjectGuid { get; set; } = string.Empty; // Associated project GUID
    }
}
