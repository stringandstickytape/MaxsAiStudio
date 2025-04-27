// AiStudio4/Core/Interfaces/IRequestHandler.cs
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

namespace AiStudio4.Core.Interfaces
{
    /// <summary>
    /// Interface for handling UI requests from clients
    /// </summary>
    public interface IRequestHandler
    {
        /// <summary>
        /// Checks if this handler can process the specified request type
        /// </summary>
        /// <param name="requestType">The type of request to check</param>
        /// <returns>True if this handler can process the request, false otherwise</returns>
        bool CanHandle(string requestType);

        /// <summary>
        /// Handles a request from a client
        /// </summary>
        /// <param name="clientId">The ID of the client making the request</param>
        /// <param name="requestType">The type of request being made</param>
        /// <param name="requestObject">The request data as a JObject</param>
        /// <returns>A JSON string response to send back to the client</returns>
        Task<string> HandleAsync(string clientId, string requestType, JObject requestObject);
    }
}