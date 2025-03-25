using AiStudio4.Core.Models;
using ModelContextProtocol.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AiStudio4.Core.Interfaces
{
    public interface IMcpService
    {
        /// <summary>
        /// Initializes the service, loading server definitions from storage.
        /// </summary>
        Task InitializeAsync();

        /// <summary>
        /// Gets all configured MCP server definitions.
        /// </summary>
        Task<List<McpServerDefinition>> GetAllServerDefinitionsAsync();

        /// <summary>
        /// Gets a specific MCP server definition by its ID.
        /// </summary>
        Task<McpServerDefinition> GetServerDefinitionByIdAsync(string id);

        /// <summary>
        /// Adds a new MCP server definition and saves it to storage.
        /// </summary>
        Task<McpServerDefinition> AddServerDefinitionAsync(McpServerDefinition definition);

        /// <summary>
        /// Updates an existing MCP server definition and saves it to storage.
        /// Stops the corresponding client if it's running.
        /// </summary>
        Task<McpServerDefinition> UpdateServerDefinitionAsync(McpServerDefinition definition);

        /// <summary>
        /// Deletes an MCP server definition from storage.
        /// Stops the corresponding client if it's running.
        /// </summary>
        Task<bool> DeleteServerDefinitionAsync(string id);

        /// <summary>
        /// Lists the tools available from a specific MCP server.
        /// Starts the server client if it's not already running.
        /// </summary>
        /// <param name="serverId">The ID of the server definition.</param>
        /// <returns>A collection of tool information.</returns>
        /// <exception cref="AiStudio4.Core.Exceptions.McpCommunicationException">Thrown if communication with the server fails.</exception>
        /// <exception cref="KeyNotFoundException">Thrown if the server definition doesn't exist.</exception>
        Task<IEnumerable<ModelContextProtocol.Protocol.Types.Tool>> ListToolsAsync(string serverId);

        /// <summary>
        /// Checks if the client for a specific MCP server is currently running.
        /// </summary>
        Task<bool> IsServerRunningAsync(string serverId);

        /// <summary>
        /// Explicitly stops and disposes the client for a specific MCP server.
        /// </summary>
        Task StopServerAsync(string serverId);
    }
}
