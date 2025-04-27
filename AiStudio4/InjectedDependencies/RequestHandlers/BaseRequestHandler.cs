// AiStudio4/InjectedDependencies/RequestHandlers/BaseRequestHandler.cs
using AiStudio4.Core.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AiStudio4.InjectedDependencies.RequestHandlers
{
    /// <summary>
    /// Base class for request handlers that provides common functionality
    /// </summary>
    public abstract class BaseRequestHandler : IRequestHandler
    {
        /// <summary>
        /// Gets the collection of request types that this handler can process
        /// </summary>
        protected abstract IEnumerable<string> SupportedRequestTypes { get; }

        /// <summary>
        /// Checks if this handler can process the specified request type
        /// </summary>
        public bool CanHandle(string requestType)
        {
            if (string.IsNullOrEmpty(requestType))
                return false;

            foreach (var supportedType in SupportedRequestTypes)
            {
                if (string.Equals(supportedType, requestType, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Handles a request from a client
        /// </summary>
        public abstract Task<string> HandleAsync(string clientId, string requestType, JObject requestObject);

        /// <summary>
        /// Serializes an error response
        /// </summary>
        protected string SerializeError(string message) => JsonConvert.SerializeObject(new { success = false, error = message });

        /// <summary>
        /// Serializes a success response with data
        /// </summary>
        protected string SerializeSuccess(object data) => JsonConvert.SerializeObject(new { success = true, data });

        /// <summary>
        /// Serializes a simple success response
        /// </summary>
        protected string SerializeSuccess() => JsonConvert.SerializeObject(new { success = true });
    }
}