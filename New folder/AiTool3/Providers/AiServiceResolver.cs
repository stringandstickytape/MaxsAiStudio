using AiTool3.Interfaces;

namespace AiTool3.Providers
{
    public static class AiServiceResolver
    {

        public static IAiService? GetAiService(string serviceName)
        {
            return (IAiService)Activator.CreateInstance(Type.GetType($"AiTool3.Providers.{serviceName}"));
        }
    }
}