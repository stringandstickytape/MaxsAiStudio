


using AiStudio4.Core.Models;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Linq;







using System.Threading;

using System.Text;

using System.Security.Cryptography;
using ModelContextProtocol.Protocol;

namespace AiStudio4.Services
{
    /// <summary>
    /// Service responsible for processing tool/function calls.
    /// </summary>
    public class ToolProcessorService : IToolProcessorService
    {
        private readonly ILogger<ToolProcessorService> _logger;
        private readonly IToolService _toolService;
        private readonly IMcpService _mcpService;
        private readonly IBuiltinToolService _builtinToolService;
        private readonly TimeSpan _minimumRequestInterval = TimeSpan.FromSeconds(1);
        private DateTime _lastRequestTime = DateTime.MinValue;
        private readonly object _rateLimitLock = new object(); // Lock object for thread safety
        private readonly Services.Interfaces.INotificationFacade _notificationFacade;

        public ToolProcessorService(
            ILogger<ToolProcessorService> logger,
            IToolService toolService,
            IMcpService mcpService,
            IBuiltinToolService builtinToolService,
            Services.Interfaces.INotificationFacade notificationFacade)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _toolService = toolService ?? throw new ArgumentNullException(nameof(toolService));
            _mcpService = mcpService ?? throw new ArgumentNullException(nameof(mcpService));
            _builtinToolService = builtinToolService ?? throw new ArgumentNullException(nameof(builtinToolService));
            _notificationFacade = notificationFacade ?? throw new ArgumentNullException(nameof(notificationFacade));
        }

        /// <summary>
        /// Re-applies a built-in or MCP tool with its original parameters
        /// </summary>
        public async Task<BuiltinToolResult> ReapplyToolAsync(string toolName, string toolParameters, string clientId)
        {
            if (string.IsNullOrEmpty(toolName))
            {
                _logger.LogError("ReapplyToolAsync called with no tool name.");
                return new BuiltinToolResult { WasProcessed = false, ResultMessage = "Error: Tool name not provided." };
            }

            // First check if it's a built-in tool
            var tool = await _toolService.GetToolByToolNameAsync(toolName);
            if (tool != null && tool.IsBuiltIn)
            {
                var extraProps = tool.ExtraProperties ?? new Dictionary<string, string>();

                _logger.LogInformation("Re-applying built-in tool '{ToolName}' for client {ClientId}", toolName, clientId);

                // Re-run the tool using the existing service, which handles all logic and security.
                var result = await _builtinToolService.ProcessBuiltinToolAsync(toolName, toolParameters, extraProps, clientId);
                
                return result;
            }

            // Check if it's an MCP tool
            var serverDefinitions = await _mcpService.GetAllServerDefinitionsAsync();
            
            // Check direct MCP tool naming pattern (serverId_toolName)
            if (toolName.Contains("_") && serverDefinitions.Any(x => x.IsEnabled && toolName.StartsWith(x.Id + "_")))
            {
                var serverId = toolName.Split('_')[0];
                var actualToolName = string.Join("_", toolName.Split('_').Skip(1));
                
                _logger.LogInformation("Re-applying MCP tool '{ToolName}' on server '{ServerId}' for client {ClientId}", actualToolName, serverId, clientId);
                
                return await ReapplyMcpToolAsync(serverId, actualToolName, toolParameters, clientId, toolName);
            }

            // Check for tools without prefix (Claude sometimes drops the prefix)
            foreach (var serverDefinition in serverDefinitions.Where(x => x.IsEnabled))
            {
                var tools = await _mcpService.ListToolsAsync(serverDefinition.Id);
                var mcpTool = tools.FirstOrDefault(x => x.Name == toolName);

                if (mcpTool != null)
                {
                    _logger.LogInformation("Re-applying MCP tool '{ToolName}' on server '{ServerId}' for client {ClientId}", toolName, serverDefinition.Id, clientId);
                    
                    return await ReapplyMcpToolAsync(serverDefinition.Id, toolName, toolParameters, clientId, toolName);
                }
            }

            _logger.LogWarning("Reapply attempted for a non-existent tool: {ToolName}", toolName);
            return new BuiltinToolResult { WasProcessed = false, ResultMessage = $"Error: Tool '{toolName}' is not a re-appliable tool." };
        }

        private async Task<BuiltinToolResult> ReapplyMcpToolAsync(string serverId, string actualToolName, string toolParameters, string clientId, string displayToolName)
        {
            try
            {
                // Check if the MCP server is still enabled
                var serverDefinition = await _mcpService.GetServerDefinitionByIdAsync(serverId);
                if (serverDefinition == null || !serverDefinition.IsEnabled)
                {
                    _logger.LogWarning("Cannot re-apply MCP tool '{ToolName}' - server '{ServerId}' is not enabled", actualToolName, serverId);
                    return new BuiltinToolResult 
                    { 
                        WasProcessed = false, 
                        ResultMessage = $"Error: MCP server '{serverId}' is not enabled. Please enable the server to re-apply this tool." 
                    };
                }

                // Check if the server is running
                if (!await _mcpService.IsServerRunningAsync(serverId))
                {
                    _logger.LogWarning("Cannot re-apply MCP tool '{ToolName}' - server '{ServerId}' is not running", actualToolName, serverId);
                    return new BuiltinToolResult 
                    { 
                        WasProcessed = false, 
                        ResultMessage = $"Error: MCP server '{serverId}' is not running. The server must be running to re-apply this tool." 
                    };
                }

                // Parse the parameters
                var args = JsonConvert.DeserializeObject<Dictionary<string, object>>(toolParameters);
                
                // Notify that we're re-applying the MCP tool
                await _notificationFacade.SendStatusMessageAsync(clientId, $"Re-applying MCP tool: {displayToolName}");
                
                // Call the MCP tool
                var mcpResult = await _mcpService.CallToolAsync(serverId, actualToolName, args, new CancellationToken());
                
                var result = new BuiltinToolResult 
                { 
                    WasProcessed = true, 
                    ContinueProcessing = true, // Provider will decide when to stop
                    ResultMessage = JsonConvert.SerializeObject(mcpResult.Content),
                    OutputFileType = "json" // MCP tools always return JSON
                };

                // Notify completion
                await _notificationFacade.SendStatusMessageAsync(clientId, $"MCP tool {displayToolName} re-applied successfully");
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error re-applying MCP tool {ToolName} on server {ServerId}", actualToolName, serverId);
                return new BuiltinToolResult 
                { 
                    WasProcessed = false, 
                    ContinueProcessing = false,
                    ResultMessage = $"Error re-applying MCP tool '{displayToolName}': {ex.Message}" 
                };
            }
        }


        private static List<string> ExtractMultipleJsonObjects(string jsonText)
        {
            var result = new List<string>();
            var textReader = new StringReader(jsonText);
            var jsonReader = new JsonTextReader(textReader)
            {
                SupportMultipleContent = true
            };

            while (jsonReader.Read())
            {
                if (jsonReader.TokenType == JsonToken.StartObject)
                {
                    // Read a complete JSON object
                    JObject obj = JObject.Load(jsonReader);
                    result.Add(obj.ToString(Formatting.None));
                }
            }

            return result;
        }


        /// <summary>
        /// Computes SHA256 hash of the input string
        /// </summary>
        /// <param name="text">Text to hash</param>
        /// <returns>Hexadecimal string representation of the hash</returns>
        private string ComputeSha256Hash(string text)
        {
            // Create a SHA256 hash from the input string
            using (SHA256 sha256Hash = SHA256.Create())
            {
                // ComputeHash - returns byte array
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(text));

                // Convert byte array to a string
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }
    }
}
