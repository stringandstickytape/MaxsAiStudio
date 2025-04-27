// AiStudio4.Core\Interfaces\IUiRequestHandler.cs
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace AiStudio4.Core.Interfaces
{
    /// <summary>
    /// Interface for handling UI requests by type.
    /// </summary>
    public interface IUiRequestHandler
    {
        /// <summary>
        /// Returns true if this handler can process the given requestType.
        /// </summary>
        bool CanHandle(string requestType);

        /// <summary>
        /// Handles the request and returns the response as JSON.
        /// </summary>
        Task<string> HandleAsync(string clientId, string requestType, JObject requestObject);
    }
}