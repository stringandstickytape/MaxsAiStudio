





namespace AiStudio4.Core.Models
{
    public class McpServerDefinition
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Command { get; set; } = string.Empty;
        public string Arguments { get; set; } = string.Empty;
        public bool IsEnabled { get; set; } = true;
        public string Description { get; set; } = string.Empty;
        public DateTime LastModified { get; set; } = DateTime.UtcNow;
        public Dictionary<string, string> Env { get; set; }
        public bool StdIo { get; set; } = true;
        public List<string> Categories { get; set; } = new List<string>();
        public List<string> SelectedTools { get; set; } = new List<string>();
        
        // OAuth configuration for SSE transport (defaults for test server)
        public string ClientName { get; set; } = "ProtectedMcpClient";
        public string RedirectUri { get; set; } = "http://localhost:1179/callback";
        public string AuthorizationEndpoint { get; set; } = "https://localhost:7029";
        public string TokenEndpoint { get; set; } = "https://localhost:7029/connect/token";
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
    }
}
