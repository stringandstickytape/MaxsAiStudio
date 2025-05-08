// AiStudio4/InjectedDependencies/UiRequestRouter.cs
using AiStudio4.Core.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AiStudio4.InjectedDependencies
{
    /// <summary>
    /// Routes UI requests to the appropriate handler
    /// </summary>
    public class UiRequestRouter
    {
        private readonly IEnumerable<IRequestHandler> _handlers;

        public UiRequestRouter(IEnumerable<IRequestHandler> handlers)
        {
            _handlers = handlers ?? throw new ArgumentNullException(nameof(handlers));
        }

        /// <summary>
        /// Routes a request to the appropriate handler
        /// </summary>
        /// <param name="clientId">The ID of the client making the request</param>
        /// <param name="requestType">The type of request being made</param>
        /// <param name="requestData">The request data as a JSON string</param>
        /// <returns>A JSON string response to send back to the client</returns>
        public async Task<string> RouteRequestAsync(string clientId, string requestType, string requestData)
        {
            try
            {
                // Parse request data
                if (!requestData.StartsWith("{"))
                    requestData = $"{{param:{requestData}}}";
                JObject requestObject = JsonConvert.DeserializeObject<JObject>(requestData);

                // Find a handler for this request type
                var handler = _handlers.FirstOrDefault(h => h.CanHandle(requestType));
                if (handler == null)
                {
                    return SerializeError($"No handler found for request type: {requestType}");
                }

                // Handle the request
                return await handler.HandleAsync(clientId, requestType, requestObject);
            }
            catch (Exception ex)
            {
                return SerializeError($"Error routing request: {ex.Message}");
            }
        }

        private string SerializeError(string message) => JsonConvert.SerializeObject(new { success = false, error = message });
    }
}