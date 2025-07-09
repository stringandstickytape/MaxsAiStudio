
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;






namespace AiStudio4.Core.Interfaces
{
    public interface IMcpService
    {
        
        
        
        Task InitializeAsync();

        
        
        
        Task<List<McpServerDefinition>> GetAllServerDefinitionsAsync();

        
        
        
        Task<McpServerDefinition> GetServerDefinitionByIdAsync(string id);

        
        
        
        Task<McpServerDefinition> AddServerDefinitionAsync(McpServerDefinition definition);

        
        
        
        
        Task<McpServerDefinition> UpdateServerDefinitionAsync(McpServerDefinition definition);

        
        
        
        
        Task<bool> DeleteServerDefinitionAsync(string id);

        
        
        
        
        
        
        
        
        Task<IEnumerable<ModelContextProtocol.Protocol.Tool>> ListToolsAsync(string serverId);

        Task<CallToolResult> CallToolAsync(string serverId, string toolName, Dictionary<string, object> arguments, CancellationToken cancellationToken);

        
        
        
        Task<bool> IsServerRunningAsync(string serverId);
Task StopServerAsync(string serverId);
    }
}
