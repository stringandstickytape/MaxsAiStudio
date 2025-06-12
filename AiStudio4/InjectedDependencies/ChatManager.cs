using AiStudio4.Services;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AiStudio4.InjectedDependencies
{
    public class ChatManager
    {
        private readonly ConvService _convService;
        private readonly ChatProcessingService _chatProcessingService;
        private readonly ILogger<ChatManager> _logger;

        public ChatManager(
            ConvService convService,
            ChatProcessingService chatProcessingService,
            ILogger<ChatManager> logger)
        {
            _convService = convService;
            _chatProcessingService = chatProcessingService;
            _logger = logger;
        }

        public async Task<string> HandleChatRequest(string clientId, JObject requestObject)
        {
            try
            {
                return await _chatProcessingService.HandleChatRequest(clientId, requestObject);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in HandleChatRequest");
                return JsonConvert.SerializeObject(new { success = false, error = ex.Message });
            }
        }


        internal async Task<string> HandleHistoricalConvTreeRequest(string clientId, JObject? requestObject)
        {
            try
            {
                return await _convService.HandleHistoricalConvTreeRequest(clientId, requestObject);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in HandleHistoricalConvTreeRequest");
                return JsonConvert.SerializeObject(new { success = false, error = ex.Message });
            }
        }

        internal async Task<string> HandleGetAllHistoricalConvTreesRequest(string clientId, JObject? requestObject)
        {
            try
            {
                return await _convService.HandleGetAllHistoricalConvTreesRequest(clientId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in HandleGetAllHistoricalConvTreesRequest");
                return JsonConvert.SerializeObject(new { success = false, error = ex.Message });
            }
        }

        internal async Task<string> HandleDeleteMessageWithDescendantsRequest(string clientId, JObject? requestObject)
        {
            try
            {
                return await _convService.HandleDeleteMessageWithDescendantsRequest(clientId, requestObject);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in HandleDeleteMessageWithDescendantsRequest");
                return JsonConvert.SerializeObject(new { success = false, error = ex.Message });
            }
        }

        internal async Task<string> HandleDeleteConvRequest(string clientId, JObject? requestObject)
        {
            try
            {
                return await _convService.HandleDeleteConvRequest(clientId, requestObject);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in HandleDeleteConvRequest");
                return JsonConvert.SerializeObject(new { success = false, error = ex.Message });
            }
        }
    }
}