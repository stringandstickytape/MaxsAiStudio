using System;

namespace AiStudio4.McpStandalone.Models
{
    public class McpServerConfiguration
    {
        public string Name { get; set; } = string.Empty;
        public bool IsEnabled { get; set; } = true;
        public string Description { get; set; } = string.Empty;
        
        // OAuth configuration for SSE transport (defaults for test server)
        public string ClientName { get; set; } = "ProtectedMcpClient";
        public string RedirectUri { get; set; } = "http://localhost:1179/callback";
        public string AuthorizationEndpoint { get; set; } = "http://localhost:7029";
        public string TokenEndpoint { get; set; } = "http://localhost:7029/connect/token";
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
    }
}