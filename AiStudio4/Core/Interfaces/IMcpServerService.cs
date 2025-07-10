using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ModelContextProtocol.Server;

namespace AiStudio4.Core.Interfaces
{
    public interface IMcpServerService
    {
        Task<IMcpServer> StartServerAsync(McpServerTransportType transportType, McpServerConfig config);
        Task StopServerAsync();
        bool IsRunning { get; }
        McpServerTransportType CurrentTransportType { get; }
        event EventHandler<McpServerStatusChangedEventArgs> StatusChanged;
        IReadOnlyList<string> GetConnectedClients();
    }

    public enum McpServerTransportType
    {
        Stdio,
        Sse,
        AspNetCoreOAuth
    }

    public class McpServerConfig
    {
        public int? HttpPort { get; set; } // For SSE
        public int? OAuthPort { get; set; } // For OAuth SSE
        public string? StdioCommand { get; set; } // For stdio
        public bool EnableLogging { get; set; }
        public List<string> ExcludedToolGuids { get; set; } = new();
    }

    public class McpServerStatusChangedEventArgs : EventArgs
    {
        public bool IsRunning { get; set; }
        public string? Message { get; set; }
        public McpServerTransportType? TransportType { get; set; }
    }
}