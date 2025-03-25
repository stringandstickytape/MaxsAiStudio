using AiStudio4.Core.Interfaces;
using AiStudio4.Core.Models;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AiStudio4.Core.Exceptions;
using ModelContextProtocol.Protocol.Transport;
using ModelContextProtocol.Protocol.Types;
using ModelContextProtocol.Configuration; // Assuming McpCommunicationException is defined here

namespace AiStudio4.Services
{
    public class McpService : IMcpService
    {
        private readonly ILogger<McpService> _logger;
        private readonly string _configDirectory;
        private readonly string _definitionsFilePath;
        private List<McpServerDefinition> _serverDefinitions = new List<McpServerDefinition>();
        private readonly ConcurrentDictionary<string, IMcpClient> _activeClients = new ConcurrentDictionary<string, IMcpClient>();
        private const string DEFINITIONS_FILENAME = "mcpServers.json";
        private bool _isInitialized = false;
        private readonly object _lock = new object();

        public McpService(ILogger<McpService> logger)
        {
            _logger = logger;
            _configDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AiStudio4", "Config");
            _definitionsFilePath = Path.Combine(_configDirectory, DEFINITIONS_FILENAME);

            Directory.CreateDirectory(_configDirectory);
        }

        public async Task InitializeAsync()
        {
            if (_isInitialized) return;

            lock (_lock)
            {
                if (_isInitialized) return;

                LoadDefinitions();

                // Create default if none exist
                if (!_serverDefinitions.Any())
                {
                    _logger.LogInformation("No MCP server definitions found, creating default 'Everything' server.");
                    var defaultDefinition = new McpServerDefinition
                    {
                        Id = "everything",
                        Name = "Everything",
                        Command = "uvx",
                        Arguments = "blender-mcp", // Note: This might need changing if uvx expects different args for 'Everything'
                        Description = "Example MCP server using uvx (Update arguments if needed)",
                        IsEnabled = false
                    };
                    _serverDefinitions.Add(defaultDefinition);
                    SaveDefinitions();
                }

                _isInitialized = true;
                _logger.LogInformation("McpService initialized with {Count} definitions.", _serverDefinitions.Count);
            }
            await Task.CompletedTask; // Keep async signature
        }

        private void LoadDefinitions()
        {
            if (!File.Exists(_definitionsFilePath))
            {
                _serverDefinitions = new List<McpServerDefinition>();
                return;
            }

            try
            {
                var json = File.ReadAllText(_definitionsFilePath);
                _serverDefinitions = JsonConvert.DeserializeObject<List<McpServerDefinition>>(json) ?? new List<McpServerDefinition>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading MCP server definitions from {FilePath}", _definitionsFilePath);
                _serverDefinitions = new List<McpServerDefinition>(); // Start fresh if file is corrupt
            }
        }

        private void SaveDefinitions()
        {
            try
            {
                var json = JsonConvert.SerializeObject(_serverDefinitions, Formatting.Indented);
                File.WriteAllText(_definitionsFilePath, json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving MCP server definitions to {FilePath}", _definitionsFilePath);
            }
        }

        public async Task<List<McpServerDefinition>> GetAllServerDefinitionsAsync()
        {
            await EnsureInitialized();
            return await Task.FromResult(_serverDefinitions.ToList()); // Return a copy
        }

        public async Task<McpServerDefinition> GetServerDefinitionByIdAsync(string id)
        {
            await EnsureInitialized();
            return await Task.FromResult(_serverDefinitions.FirstOrDefault(d => d.Id == id));
        }

        public async Task<McpServerDefinition> AddServerDefinitionAsync(McpServerDefinition definition)
        {
            await EnsureInitialized();
            if (definition == null) throw new ArgumentNullException(nameof(definition));

            lock (_lock)
            {
                if (string.IsNullOrEmpty(definition.Id) || _serverDefinitions.Any(d => d.Id == definition.Id))
                {
                    definition.Id = Guid.NewGuid().ToString();
                }
                definition.LastModified = DateTime.UtcNow;
                _serverDefinitions.Add(definition);
                SaveDefinitions();
                _logger.LogInformation("Added MCP server definition: {Name} ({Id})", definition.Name, definition.Id);
            }
            return await Task.FromResult(definition);
        }

        public async Task<McpServerDefinition> UpdateServerDefinitionAsync(McpServerDefinition definition)
        {
            await EnsureInitialized();
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (string.IsNullOrEmpty(definition.Id)) throw new ArgumentException("Definition ID cannot be empty for update.");

            bool stoppedClient = false;
            lock (_lock)
            {
                var existingIndex = _serverDefinitions.FindIndex(d => d.Id == definition.Id);
                if (existingIndex < 0)
                {
                    throw new KeyNotFoundException($"MCP Server Definition with ID {definition.Id} not found.");
                }

                // Stop the running client before updating definition
                if (_activeClients.TryGetValue(definition.Id, out var clientToStop))
                {
                    // Run stopping asynchronously without blocking the lock
                    _ = StopAndRemoveClientAsync(definition.Id, clientToStop);
                    stoppedClient = true;
                }

                definition.LastModified = DateTime.UtcNow;
                _serverDefinitions[existingIndex] = definition;
                SaveDefinitions();
                _logger.LogInformation("Updated MCP server definition: {Name} ({Id}). Client stopped: {Stopped}", definition.Name, definition.Id, stoppedClient);
            }
            return await Task.FromResult(definition);
        }

        public async Task<bool> DeleteServerDefinitionAsync(string id)
        {
            await EnsureInitialized();
            if (string.IsNullOrEmpty(id)) throw new ArgumentException("Definition ID cannot be empty for delete.");

            bool removed = false;
            bool stoppedClient = false;
            lock (_lock)
            {
                var definitionToRemove = _serverDefinitions.FirstOrDefault(d => d.Id == id);
                if (definitionToRemove != null)
                {
                    removed = _serverDefinitions.Remove(definitionToRemove);
                    if (removed)
                    {
                        // Stop the running client
                        if (_activeClients.TryGetValue(id, out var clientToStop))
                        {
                            // Run stopping asynchronously
                            _ = StopAndRemoveClientAsync(id, clientToStop);
                            stoppedClient = true;
                        }
                        SaveDefinitions();
                        _logger.LogInformation("Deleted MCP server definition: {Name} ({Id}). Client stopped: {Stopped}", definitionToRemove.Name, id, stoppedClient);
                    }
                }
            }
            return await Task.FromResult(removed);
        }

        public async Task<IEnumerable<ModelContextProtocol.Protocol.Types.Tool>> ListToolsAsync(string serverId)
        {
            await EnsureInitialized();

            var client = await GetOrStartClientAsync(serverId);
            if (client == null)
            {
                // GetOrStartClientAsync logs the error, we can throw or return empty
                throw new KeyNotFoundException($"MCP Server Definition with ID {serverId} not found or is disabled.");
            }

            try
            {
                _logger.LogInformation("Listing tools for MCP server {ServerId}", serverId);
                List<ModelContextProtocol.Protocol.Types.Tool> tools = new List<ModelContextProtocol.Protocol.Types.Tool>();
                await foreach (var tool in client.ListToolsAsync())
                {
                    tools.Add(tool);
                }

                _logger.LogInformation("Successfully listed {Count} tools for MCP server {ServerId}", tools?.Count() ?? 0, serverId);
                return tools ?? Enumerable.Empty<ModelContextProtocol.Protocol.Types.Tool>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing tools for MCP server {ServerId}", serverId);
                // Consider stopping the client if ListToolsAsync fails consistently?
                // await StopServerAsync(serverId);
                throw new McpCommunicationException($"Failed to list tools for server {serverId}.", ex);
            }
        }

        public async Task<bool> IsServerRunningAsync(string serverId)
        {
            await EnsureInitialized();
            // Basic check: is it in our dictionary?
            // A more robust check might involve pinging the client if the library supports it.
            return await Task.FromResult(_activeClients.ContainsKey(serverId));
        }

        public async Task StopServerAsync(string serverId)
        {
            await EnsureInitialized();
            if (_activeClients.TryRemove(serverId, out var clientToStop))
            {
                await StopAndDisposeClientAsync(serverId, clientToStop);
            }
            else
            {
                _logger.LogWarning("Attempted to stop MCP client for server {ServerId}, but it was not found or already stopped.", serverId);
            }
        }

        private async Task<IMcpClient> GetOrStartClientAsync(string serverId)
        {
            if (_activeClients.TryGetValue(serverId, out var existingClient))
            {
                // Basic health check - might need refinement based on library capabilities
                // For StdIO, checking if the process has exited might be needed, but IMcpClient doesn't expose this directly.
                // We'll assume if it's in the dictionary, it's potentially usable.
                _logger.LogDebug("Using existing MCP client for server {ServerId}", serverId);
                return existingClient;
            }

            McpServerDefinition definition;
            lock (_lock)
            {
                 definition = _serverDefinitions.FirstOrDefault(d => d.Id == serverId);
            }

            if (definition == null)
            {
                _logger.LogError("MCP Server Definition with ID {ServerId} not found.", serverId);
                return null;
            }

            if (!definition.IsEnabled)
            {
                _logger.LogWarning("MCP Server Definition {ServerId} ({Name}) is disabled. Cannot start client.", serverId, definition.Name);
                return null;
            }

            if (string.IsNullOrWhiteSpace(definition.Command))
            {
                 _logger.LogError("MCP Server Definition {ServerId} ({Name}) has no command specified. Cannot start client.", serverId, definition.Name);
                return null;
            }

            _logger.LogInformation("Starting MCP client for server {ServerId} ({Name}) using command: {Command} {Arguments}",
                serverId, definition.Name, definition.Command, definition.Arguments);

            try
            {
                var clientOptions = new McpClientOptions
                {
                    ClientInfo = new Implementation { Name = "AiStudio4", Version = "1.0" }
                    // Add other options if needed, e.g., Logger
                };

                var serverInfo = new McpServerConfig
                {
                    Id = definition.Id,
                    Name = definition.Name,
                    TransportType = TransportTypes.StdIo,
                    TransportOptions = new Dictionary<string, string>
                    {
                        { "command", definition.Command },
                        { "arguments", definition.Arguments ?? string.Empty } // Ensure not null
                    }
                };

                var newClient = await McpClientFactory.CreateAsync(serverInfo, clientOptions);

                if (_activeClients.TryAdd(serverId, newClient))
                {
                    _logger.LogInformation("Successfully started and added MCP client for server {ServerId}", serverId);
                    // Optional: Register for client-side events like disconnection if the library provides them
                    return newClient;
                }
                else
                {
                    // Race condition: Another thread might have added it already.
                    _logger.LogWarning("MCP client for server {ServerId} was already added by another thread. Disposing the newly created one.", serverId);
                    await newClient.DisposeAsync();
                    return _activeClients.TryGetValue(serverId, out var concurrentClient) ? concurrentClient : null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create MCP client for server {ServerId}", serverId);
                throw new McpCommunicationException($"Failed to start MCP server process for {serverId}. Check command/arguments and ensure the process is executable.", ex);
            }
        }

        // Helper to stop and log, used when definition is updated/deleted
        private async Task StopAndRemoveClientAsync(string serverId, IMcpClient client)
        {
             _activeClients.TryRemove(serverId, out _); // Attempt removal from dictionary first
            await StopAndDisposeClientAsync(serverId, client);
        }

        // General stop and dispose logic
        private async Task StopAndDisposeClientAsync(string serverId, IMcpClient client)
        {
            try
            {
                _logger.LogInformation("Stopping MCP client for server {ServerId}", serverId);
                await client.DisposeAsync();
                _logger.LogInformation("Successfully stopped MCP client for server {ServerId}", serverId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing MCP client for server {ServerId}", serverId);
            }
        }

        private async Task EnsureInitialized()
        {
            if (!_isInitialized)
            {
                await InitializeAsync();
            }
        }
    }
}
