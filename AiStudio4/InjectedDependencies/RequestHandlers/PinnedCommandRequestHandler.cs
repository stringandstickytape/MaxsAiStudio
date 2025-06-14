// AiStudio4/InjectedDependencies/RequestHandlers/PinnedCommandRequestHandler.cs








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

                var pinnedCommands = await _pinnedCommandService.GetPinnedCommandsAsync(clientId);
                return JsonConvert.SerializeObject(new
                {
                    success = true,
                    pinnedCommands
                });
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

                var pinnedCommands = requestObject["pinnedCommands"]?.ToObject<List<PinnedCommand>>();
                if (pinnedCommands == null)
                {
                    return SerializeError("Pinned commands data is invalid");
                }

                await _pinnedCommandService.SavePinnedCommandsAsync(clientId, pinnedCommands);
                return JsonConvert.SerializeObject(new { success = true });
            }
            catch (Exception ex)
            {
                return SerializeError($"Error saving pinned commands: {ex.Message}");
            }
        }
    }
}
