


using AiStudio4.Core.Models;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;







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
        /// Re-applies a built-in tool with its original parameters
        /// </summary>
        public async Task<BuiltinToolResult> ReapplyToolAsync(string toolName, string toolParameters, string clientId)
        {
            if (string.IsNullOrEmpty(toolName))
            {
                _logger.LogError("ReapplyToolAsync called with no tool name.");
                return new BuiltinToolResult { WasProcessed = false, ResultMessage = "Error: Tool name not provided." };
            }

            var tool = await _toolService.GetToolByToolNameAsync(toolName);
            if (tool == null || !tool.IsBuiltIn)
            {
                _logger.LogWarning("Reapply attempted for a non-existent or non-built-in tool: {ToolName}", toolName);
                return new BuiltinToolResult { WasProcessed = false, ResultMessage = $"Error: Tool '{toolName}' is not a re-appliable built-in tool." };
            }

            var extraProps = tool.ExtraProperties ?? new Dictionary<string, string>();

            _logger.LogInformation("Re-applying built-in tool '{ToolName}' for client {ClientId}", toolName, clientId);

            // Re-run the tool using the existing service, which handles all logic and security.
            var result = await _builtinToolService.ProcessBuiltinToolAsync(toolName, toolParameters, extraProps, clientId);
            
            return result;
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
