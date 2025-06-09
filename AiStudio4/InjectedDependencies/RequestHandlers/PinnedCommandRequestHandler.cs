// AiStudio4/InjectedDependencies/RequestHandlers/PinnedCommandRequestHandler.cs
using AiStudio4.Core.Interfaces;
using AiStudio4.Core.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AiStudio4.InjectedDependencies.RequestHandlers
{
    /// <summary>
    /// Handles pinned command-related requests
    /// </summary>
    public class PinnedCommandRequestHandler : BaseRequestHandler
    {
        private readonly IPinnedCommandService _pinnedCommandService;

        public PinnedCommandRequestHandler(IPinnedCommandService pinnedCommandService)
        {
            _pinnedCommandService = pinnedCommandService ?? throw new ArgumentNullException(nameof(pinnedCommandService));
        }

        protected override IEnumerable<string> SupportedRequestTypes => new[]
        {
            "pinnedCommands/get",
            "pinnedCommands/save"
        };

        public override async Task<string> HandleAsync(string clientId, string requestType, JObject requestObject)
        {
            try
            {
                return requestType switch
                {
                    "pinnedCommands/get" => await HandleGetPinnedCommandsRequest(clientId, requestObject),
                    "pinnedCommands/save" => await HandleSavePinnedCommandsRequest(clientId, requestObject),
                    _ => SerializeError($"Unsupported request type: {requestType}")
                };
            }
            catch (Exception ex)
            {
                return SerializeError($"Error handling {requestType} request: {ex.Message}");
            }
        }

        private async Task<string> HandleGetPinnedCommandsRequest(string clientId, JObject requestObject)
        {
            try
            {
                if (string.IsNullOrEmpty(clientId))
                {
                    clientId = requestObject["clientId"]?.ToString();
                    if (string.IsNullOrEmpty(clientId))
                    {
                        return SerializeError("Client ID is required");
                    }
                }

                var (pinnedCommands, categoryOrder) = await _pinnedCommandService.GetPinnedCommandsAsync(clientId);
                var response = new GetPinnedCommandsResponse
                {
                    Success = true,
                    PinnedCommands = pinnedCommands,
                    CategoryOrder = categoryOrder
                };

                return SerializeResponse(response);
            }
            catch (Exception ex)
            {
                return SerializeError($"Error retrieving pinned commands: {ex.Message}");
            }
        }

        private async Task<string> HandleSavePinnedCommandsRequest(string clientId, JObject requestObject)
        {
            try
            {
                if (string.IsNullOrEmpty(clientId))
                {
                    clientId = requestObject["clientId"]?.ToString();
                    if (string.IsNullOrEmpty(clientId))
                    {
                        return SerializeError("Client ID is required");
                    }
                }

                var request = requestObject.ToObject<SavePinnedCommandsRequest>();
                if (request == null) return SerializeError("Invalid request data");

                await _pinnedCommandService.SavePinnedCommandsAsync(clientId, request.PinnedCommands, request.CategoryOrder);
                var response = new SavePinnedCommandsResponse
                {
                    Success = true
                };

                return SerializeResponse(response);
            }
            catch (Exception ex)
            {
                return SerializeError($"Error saving pinned commands: {ex.Message}");
            }
        }
    }
}