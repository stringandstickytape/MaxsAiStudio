
using AiStudio4.Services;
using AiStudio4.Services.Interfaces;

namespace AiStudio4.AiServices
{
    public static class AiServiceResolver
    {
        /// <summary>
        /// Gets all available AI service names by finding all classes that inherit from AiServiceBase
        /// </summary>
        /// <returns>A list of available service names</returns>
        public static IEnumerable<string> GetAvailableServiceNames()
        {
            // Get all types in the current assembly
            var assembly = typeof(AiServiceResolver).Assembly;
            
            // Find all non-abstract classes that inherit from AiServiceBase
            var serviceTypes = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && typeof(AiServiceBase).IsAssignableFrom(t));
                
            // Extract the service names (class names without namespace)
            return serviceTypes.Select(t => t.Name);
        }
        
        public static IAiService? GetAiService(string serviceName, IToolService toolService, IMcpService mcpService)
        {
            var serviceType = Type.GetType($"AiStudio4.AiServices.{serviceName}");
            if (serviceType == null) return null;

            var service = (IAiService)Activator.CreateInstance(serviceType);
            service.ToolService = toolService;
            service.McpService = mcpService;
            return service;
        }
    }
}
