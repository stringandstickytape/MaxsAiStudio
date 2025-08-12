using AiStudio4.Core.Interfaces;
using System.Collections.Generic;

namespace AiStudio4.Services.Adapters
{
    /// <summary>
    /// Adapter that bridges between the main app's BuiltInToolExtraPropertiesService and the shared library's minimal interface
    /// </summary>
    public class BuiltInToolExtraPropertiesServiceAdapter : AiStudio4.Tools.Interfaces.IBuiltInToolExtraPropertiesService
    {
        private readonly IBuiltInToolExtraPropertiesService _originalService;

        public BuiltInToolExtraPropertiesServiceAdapter(IBuiltInToolExtraPropertiesService originalService)
        {
            _originalService = originalService;
        }

        public Dictionary<string, string> GetExtraProperties(string toolName)
        {
            return _originalService.GetExtraProperties(toolName);
        }
    }
}