// C:/Users/maxhe/source/repos/MaxsAiStudio/AiStudio4/Services/Interfaces/ILlamaServerService.cs
using AiStudio4.Services;

namespace AiStudio4.Services.Interfaces
{
    public interface ILlamaServerService : IDisposable
    {
        Task<string> EnsureServerRunningAsync(string modelPath, LlamaServerSettings settings = null);
        Task StopServerAsync();
        bool IsServerRunning { get; }
        string ServerBaseUrl { get; }
        Task<bool> IsServerHealthyAsync();
    }
}