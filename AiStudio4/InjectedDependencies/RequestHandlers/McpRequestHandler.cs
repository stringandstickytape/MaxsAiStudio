// AiStudio4/InjectedDependencies/RequestHandlers/McpRequestHandler.cs
using AiStudio4.Core.Interfaces;
using AiStudio4.Core.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AiStudio4.InjectedDependencies.RequestHandlers
{
    /// <summary>
    /// Handles MCP server-related requests
    /// </summary>
    public class McpRequestHandler : BaseRequestHandler
    {
        private readonly IMcpService _mcpService;

        public McpRequestHandler(IMcpService mcpService)
        {
            _mcpService = mcpService ?? throw new ArgumentNullException(nameof(mcpService));
        }

        protected override IEnumerable<string> SupportedRequestTypes => new[]
        {
            "mcpServers/getAll",
            "mcpServers/getById",
            "mcpServers/add",
            "mcpServers/update",
            "mcpServers/delete",
            "mcpServers/setEnabled",
            "mcpServers/getTools"
        };

        public override async Task<string> HandleAsync(string clientId, string requestType, JObject requestObject)
        {
            try
            {
                return requestType switch
                {
                    "mcpServers/getAll" => await HandleGetAllMcpServersRequest(),
                    "mcpServers/getById" => await HandleGetMcpServerByIdRequest(requestObject),
                    "mcpServers/add" => await HandleAddMcpServerRequest(requestObject),
                    "mcpServers/update" => await HandleUpdateMcpServerRequest(requestObject),
                    "mcpServers/delete" => await HandleDeleteMcpServerRequest(requestObject),
                    "mcpServers/setEnabled" => await HandleSetMcpServerEnabledRequest(requestObject),
                    "mcpServers/getTools" => await HandleGetMcpServerToolsRequest(requestObject),
                    _ => SerializeError($"Unsupported request type: {requestType}")
                };
            }
            catch (Exception ex)
            {
                return SerializeError($"Error handling {requestType} request: {ex.Message}");
            }
        }

        private async Task<string> HandleGetAllMcpServersRequest()
        {
            try
            {
                await _mcpService.InitializeAsync(); // Ensure service is initialized
                var servers = await _mcpService.GetAllServerDefinitionsAsync();
                return JsonConvert.SerializeObject(new { success = true, servers });
            }
            catch (Exception ex)
            {
                return SerializeError($"Error retrieving MCP servers: {ex.Message}");
            }
        }

        private async Task<string> HandleGetMcpServerByIdRequest(JObject requestObject)
        {
            try
            {
                string serverId = requestObject["serverId"]?.ToString();
                if (string.IsNullOrEmpty(serverId)) return SerializeError("Server ID cannot be empty");
                
                await _mcpService.InitializeAsync(); // Ensure service is initialized
                var server = await _mcpService.GetServerDefinitionByIdAsync(serverId);
                if (server == null) return SerializeError($"MCP server with ID {serverId} not found");
                
                return JsonConvert.SerializeObject(new { success = true, server });
            }
            catch (Exception ex)
            {
                return SerializeError($"Error retrieving MCP server: {ex.Message}");
            }
        }

        private async Task<string> HandleAddMcpServerRequest(JObject requestObject)
        {
            try
            {
                var server = requestObject.ToObject<McpServerDefinition>();
                if (server == null) return SerializeError("Invalid MCP server data");
                
                await _mcpService.InitializeAsync(); // Ensure service is initialized
                var result = await _mcpService.AddServerDefinitionAsync(server);
                return JsonConvert.SerializeObject(new { success = true, server = result });
            }
            catch (Exception ex)
            {
                return SerializeError($"Error adding MCP server: {ex.Message}");
            }
        }

        private async Task<string> HandleUpdateMcpServerRequest(JObject requestObject)
        {
            try
            {
                var server = requestObject.ToObject<McpServerDefinition>();
                if (server == null || string.IsNullOrEmpty(server.Id))
                    return SerializeError("Invalid MCP server data or missing server ID");

                await _mcpService.InitializeAsync(); // Ensure service is initialized
                var result = await _mcpService.UpdateServerDefinitionAsync(server);
                return JsonConvert.SerializeObject(new { success = true, server = result });
            }
            catch (Exception ex)
            {
                return SerializeError($"Error updating MCP server: {ex.Message}");
            }
        }

        private async Task<string> HandleDeleteMcpServerRequest(JObject requestObject)
        {
            try
            {
                string serverId = requestObject["serverId"]?.ToString();
                if (string.IsNullOrEmpty(serverId)) return SerializeError("Server ID cannot be empty");

                await _mcpService.InitializeAsync(); // Ensure service is initialized
                var success = await _mcpService.DeleteServerDefinitionAsync(serverId);
                return JsonConvert.SerializeObject(new { success });
            }
            catch (Exception ex)
            {
                return SerializeError($"Error deleting MCP server: {ex.Message}");
            }
        }

        private async Task<string> HandleSetMcpServerEnabledRequest(JObject requestObject)
        {
            try
            {
                string serverId = requestObject["serverId"]?.ToString();
                bool? isEnabled = requestObject["isEnabled"]?.Value<bool?>();

                if (string.IsNullOrEmpty(serverId))
                    return SerializeError("Server ID cannot be empty");
                if (isEnabled == null)
                    return SerializeError("isEnabled flag must be provided");

                await _mcpService.InitializeAsync(); // Ensure service initialized
                var server = await _mcpService.GetServerDefinitionByIdAsync(serverId);
                if (server == null)
                    return SerializeError($"MCP server with ID {serverId} not found");

                // Only proceed if change is necessary
                if (server.IsEnabled != isEnabled.Value)
                {
                    server.IsEnabled = isEnabled.Value;
                    server = await _mcpService.UpdateServerDefinitionAsync(server);
                }

                return JsonConvert.SerializeObject(new { success = true, server });
            }
            catch (Exception ex)
            {
                return SerializeError($"Error setting MCP server enabled state: {ex.Message}");
            }
        }

        private async Task<string> HandleGetMcpServerToolsRequest(JObject requestObject)
        {
            try
            {
                string serverId = requestObject["serverId"]?.ToString();
                if (string.IsNullOrEmpty(serverId)) return SerializeError("Server ID cannot be empty");

                await _mcpService.InitializeAsync(); // Ensure service is initialized
                var tools = await _mcpService.ListToolsAsync(serverId);
                return JsonConvert.SerializeObject(new { success = true, tools });
            }
            catch (Exception ex)
            {
                return SerializeError($"Error retrieving MCP server tools: {ex.Message}");
            }
        }
    }
}