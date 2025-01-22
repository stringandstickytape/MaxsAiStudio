using AiTool3.Interfaces;
using AiTool3.Tools;

namespace AiTool3.AiServices
{
    public static class AiServiceResolver
    {
        public static IAiService? GetAiService(string serviceName, ToolManager toolManager)
        {
            var serviceType = Type.GetType($"AiTool3.AiServices.{serviceName}");
            if (serviceType == null) return null;

            var service = (IAiService)Activator.CreateInstance(serviceType);
            service.ToolManager = toolManager;
            return service;
        }
    }
}