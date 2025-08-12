using AiStudio4.Core.Interfaces;
using System.Threading.Tasks;

namespace AiStudio4.Services.Adapters
{
    /// <summary>
    /// Adapter that bridges between the main app's StatusMessageService and the shared library's minimal interface
    /// </summary>
    public class StatusMessageServiceAdapter : AiStudio4.Tools.Interfaces.IStatusMessageService
    {
        private readonly IStatusMessageService _originalService;

        public StatusMessageServiceAdapter(IStatusMessageService originalService)
        {
            _originalService = originalService;
        }

        public Task SendStatusMessageAsync(string clientId, string message)
        {
            return _originalService.SendStatusMessageAsync(clientId, message);
        }
    }
}