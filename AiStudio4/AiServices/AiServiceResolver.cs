using AiStudio4.Core.Interfaces;
using AiStudio4.Services.Interfaces;

namespace AiStudio4.AiServices
{
    public static class AiServiceResolver
    {
        public static IAiService? GetAiService(string serviceName, IToolService toolService)
        {
            var serviceType = Type.GetType($"AiStudio4.AiServices.{serviceName}");
            if (serviceType == null) return null;

            var service = (IAiService)Activator.CreateInstance(serviceType);
            service.ToolService = toolService;
            return service;
        }
    }
}