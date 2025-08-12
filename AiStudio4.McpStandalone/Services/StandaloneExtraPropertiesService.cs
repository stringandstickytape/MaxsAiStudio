using AiStudio4.Tools.Interfaces;
using System.Collections.Generic;

namespace AiStudio4.McpStandalone.Services
{
    /// <summary>
    /// Basic implementation of extra properties service for standalone MCP server
    /// </summary>
    public class StandaloneExtraPropertiesService : IBuiltInToolExtraPropertiesService
    {
        private readonly Dictionary<string, Dictionary<string, string>> _toolProperties = new();

        public Dictionary<string, string> GetExtraProperties(string toolName)
        {
            if (_toolProperties.TryGetValue(toolName, out var properties))
            {
                return new Dictionary<string, string>(properties);
            }
            return new Dictionary<string, string>();
        }

        public void SetExtraProperties(string toolName, Dictionary<string, string> properties)
        {
            _toolProperties[toolName] = new Dictionary<string, string>(properties ?? new Dictionary<string, string>());
        }
    }
}