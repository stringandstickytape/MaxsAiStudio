


using ModelContextProtocol.Client;


using System.Collections.Concurrent;




using AiStudio4.Core.Exceptions;
using ModelContextProtocol.Protocol;

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
            _configDirectory = PathHelper.GetProfileSubPath("Config");
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


        public async Task<CallToolResult> CallToolAsync(string serverId, string toolName, Dictionary<string, object> arguments, CancellationToken cancellationToken)
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
                _logger.LogInformation("Calling tool {ToolName} for MCP server {ServerId}", toolName, serverId);

                var retval = await client.CallToolAsync(toolName, arguments, null, null, cancellationToken);

                _logger.LogInformation("Successfully called tool {ToolName} for MCP server {ServerId}", toolName, serverId);
                return retval;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling tool {ToolName} for MCP server {ServerId}", toolName, serverId);
                // Consider stopping the client if CallToolAsync fails consistently
                // await StopServerAsync(serverId);
                throw new McpCommunicationException($"Failed to call tool {toolName} for server {serverId}.", ex);
            }
        }

        public async Task<IEnumerable<ModelContextProtocol.Protocol.Tool>> ListToolsAsync(string serverId)
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
                List<ModelContextProtocol.Protocol.Tool> tools = new List<ModelContextProtocol.Protocol.Tool>();
                var availableTools = await client.ListToolsAsync();
                foreach (var tool in availableTools)
                {
                    tools.Add(tool.ProtocolTool);
                }

                _logger.LogInformation("Successfully listed {Count} tools for MCP server {ServerId}", tools?.Count() ?? 0, serverId);
                return tools ?? Enumerable.Empty<ModelContextProtocol.Protocol.Tool>();
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
                IMcpClient newClient;
                
                if (definition.StdIo)
                {
                    // Create a StdioClientTransport for command-line based MCP servers
                    newClient = await McpClientFactory.CreateAsync(
                        new StdioClientTransport(new()
                        {
                            Command = definition.Command,
                            Arguments = definition.Arguments?.Split(' ').ToList() ?? new List<string>(),
                            Name = definition.Name,
                            EnvironmentVariables = definition.Env,
                            //WorkingDirectory
                            //ShutdownTimeout
                        }));
                }
                else
                {
                    // For SSE transport (HTTP-based MCP servers)
                    var sseConfig = new SseClientTransportOptions
                    {
                        Endpoint = new Uri(definition.Command),
                        Name = definition.Name,
                    };

                    // Add OAuth configuration if provided
                    if (!string.IsNullOrEmpty(definition.ClientName) && 
                        !string.IsNullOrEmpty(definition.AuthorizationEndpoint))
                    {
                        sseConfig.OAuth = new()
                        {
                            ClientName = definition.ClientName,
                            RedirectUri = new Uri(definition.RedirectUri),
                            AuthorizationRedirectDelegate = HandleAuthorizationUrlAsync
                        };
                    }

                    newClient = await McpClientFactory.CreateAsync(new SseClientTransport(sseConfig));
                }
                
                if (_activeClients.TryAdd(serverId, newClient))
                {
                    _logger.LogInformation("Successfully started and added MCP client for server {ServerId}", serverId);
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

        private static async Task<string?> HandleAuthorizationUrlAsync(Uri authorizationUrl, Uri redirectUri, CancellationToken cancellationToken)
        {
            try
            {
                // Create a temporary HTML file that opens the authorization URL in a popup window
                var tempHtml = $@"
<!DOCTYPE html>
<html>
<head>
    <title>Opening Authorization...</title>
    <style>
        body {{
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif;
            background: #0a0a0a;
            color: #e0e0e0;
            display: flex;
            align-items: center;
            justify-content: center;
            min-height: 100vh;
            margin: 0;
        }}
        .message {{
            text-align: center;
        }}
    </style>
</head>
<body>
    <div class='message'>
        <p>Opening authorization window...</p>
        <p>If the window doesn't open, <a href='{authorizationUrl}' target='_blank'>click here</a>.</p>
    </div>
    <script>
        // Open OAuth authorization in a popup window that we can control
        var width = 600;
        var height = 700;
        var left = (screen.width - width) / 2;
        var top = (screen.height - height) / 2;
        
        var authWindow = window.open(
            '{authorizationUrl}',
            'AIStudio4_OAuth',
            'width=' + width + ',height=' + height + ',left=' + left + ',top=' + top + ',toolbar=no,menubar=no,location=no,status=no'
        );
        
        // Close this launcher window after a moment
        setTimeout(function() {{
            window.close();
        }}, 1000);
    </script>
</body>
</html>";
                
                var tempFile = Path.Combine(Path.GetTempPath(), $"aistudio4_oauth_{Guid.NewGuid()}.html");
                File.WriteAllText(tempFile, tempHtml);
                
                // Open the temporary HTML file
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = tempFile,
                    UseShellExecute = true
                });
                
                // Clean up temp file after a delay
                _ = Task.Run(async () =>
                {
                    await Task.Delay(5000);
                    try { File.Delete(tempFile); } catch { }
                });

                // Start HTTP listener for the callback
                using var listener = new System.Net.HttpListener();
                var prefix = redirectUri.ToString();
                if (!prefix.EndsWith("/"))
                    prefix += "/";
                listener.Prefixes.Add(prefix);
                listener.Start();

                // Wait for the authorization callback
                var context = await listener.GetContextAsync();
                var query = context.Request.Url?.Query;
                
                // Send a response to the browser
                var response = context.Response;
                string responseString = @"
<!DOCTYPE html>
<html>
<head>
    <title>AIStudio4 - Authentication Complete</title>
    <style>
        * { margin: 0; padding: 0; box-sizing: border-box; }
        body { 
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif;
            background: #0a0a0a;
            color: #e0e0e0;
            min-height: 100vh;
            display: flex;
            align-items: center;
            justify-content: center;
            position: relative;
            overflow: hidden;
        }
        body::before {
            content: '';
            position: absolute;
            top: -50%;
            left: -50%;
            width: 200%;
            height: 200%;
            background: radial-gradient(circle at 50% 50%, rgba(0, 255, 136, 0.15) 0%, transparent 50%);
            animation: pulse 3s ease-in-out infinite;
        }
        @keyframes pulse {
            0%, 100% { transform: scale(1) rotate(0deg); opacity: 0.8; }
            50% { transform: scale(1.1) rotate(10deg); opacity: 1; }
        }
        .container { 
            max-width: 480px;
            width: 90%;
            background: rgba(16, 16, 16, 0.95);
            backdrop-filter: blur(20px);
            padding: 40px;
            border-radius: 16px;
            box-shadow: 0 0 60px rgba(0, 255, 136, 0.2),
                        0 0 120px rgba(0, 255, 136, 0.1),
                        inset 0 0 0 1px rgba(0, 255, 136, 0.3);
            position: relative;
            z-index: 1;
            animation: slideIn 0.5s ease-out;
        }
        @keyframes slideIn {
            from {
                opacity: 0;
                transform: scale(0.95);
            }
            to {
                opacity: 1;
                transform: scale(1);
            }
        }
        .checkmark {
            width: 80px;
            height: 80px;
            margin: 0 auto 24px;
            position: relative;
            animation: checkmarkScale 0.6s ease-out;
        }
        @keyframes checkmarkScale {
            0% { transform: scale(0); }
            50% { transform: scale(1.2); }
            100% { transform: scale(1); }
        }
        .checkmark svg {
            width: 100%;
            height: 100%;
        }
        .checkmark-circle {
            stroke: #00ff88;
            stroke-width: 2;
            fill: none;
            stroke-dasharray: 166;
            stroke-dashoffset: 166;
            animation: checkmarkCircle 0.6s ease-out forwards;
        }
        @keyframes checkmarkCircle {
            to { stroke-dashoffset: 0; }
        }
        .checkmark-check {
            stroke: #00ff88;
            stroke-width: 3;
            fill: none;
            stroke-dasharray: 48;
            stroke-dashoffset: 48;
            animation: checkmarkCheck 0.6s ease-out 0.3s forwards;
        }
        @keyframes checkmarkCheck {
            to { stroke-dashoffset: 0; }
        }
        h2 { 
            font-size: 28px;
            font-weight: 600;
            margin-bottom: 16px;
            background: linear-gradient(135deg, #00ff88 0%, #00ccff 100%);
            -webkit-background-clip: text;
            -webkit-text-fill-color: transparent;
            background-clip: text;
            text-align: center;
        }
        .message { 
            margin: 20px 0;
            color: #b0b0b0;
            line-height: 1.6;
            text-align: center;
            font-size: 15px;
        }
        .close-notice {
            margin-top: 32px;
            padding: 16px;
            background: rgba(0, 255, 136, 0.05);
            border: 1px solid rgba(0, 255, 136, 0.2);
            border-radius: 8px;
            font-size: 14px;
            color: #00ff88;
            text-align: center;
            animation: fadeIn 0.5s ease-out 0.5s both;
        }
        @keyframes fadeIn {
            from { opacity: 0; }
            to { opacity: 1; }
        }
    </style>
</head>
<body>
    <div class='container'>
        <div class='checkmark'>
            <svg viewBox='0 0 52 52'>
                <circle class='checkmark-circle' cx='26' cy='26' r='25'/>
                <path class='checkmark-check' d='M14.1 27.2l7.1 7.2 16.7-16.8'/>
            </svg>
        </div>
        <h2>Authentication Successful</h2>
        <div class='message'>
            You can close this window and return to AIStudio4.
        </div>
        <div class='close-notice'>
            Window will close automatically in a moment...
        </div>
    </div>
    <script>
        // Try to close the window after a short delay
        setTimeout(function() {
            window.close();
        }, 1500);
    </script>
</body>
</html>";
                byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
                response.ContentLength64 = buffer.Length;
                response.OutputStream.Write(buffer, 0, buffer.Length);
                response.OutputStream.Close();

                // Extract authorization code from query parameters
                if (!string.IsNullOrEmpty(query))
                {
                    var queryParams = System.Web.HttpUtility.ParseQueryString(query);
                    return queryParams["code"];
                }
            }
            catch (Exception ex)
            {
                // If we can't open the browser automatically, log the URL for manual access
                Console.WriteLine($"Please open the following URL in your browser to authorize: {authorizationUrl}");
                Console.WriteLine($"Error during authorization: {ex.Message}");
            }
            
            return null;
        }
    }
}
