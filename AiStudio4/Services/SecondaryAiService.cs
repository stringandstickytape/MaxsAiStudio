


using SharedClasses.Providers;
using AiStudio4.AiServices;
using AiStudio4.Convs;


using System.Threading;


using AiStudio4.DataModels;

namespace AiStudio4.Services
{
    /// <summary>
    /// Service for communicating with a secondary AI model without going through the normal chat pipeline
    /// </summary>
    public class SecondaryAiService : ISecondaryAiService
    {
        
        private readonly ILogger<SecondaryAiService> _logger;
        private readonly IMcpService _mcpService;
        private readonly IGeneralSettingsService _generalSettingsService;

        public SecondaryAiService(ILogger<SecondaryAiService> logger, IMcpService mcpService, IGeneralSettingsService generalSettingsService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _mcpService = mcpService ?? throw new ArgumentNullException(nameof(mcpService));
            _generalSettingsService = generalSettingsService ?? throw new ArgumentNullException(nameof(generalSettingsService));
        }

        /// <summary>
        /// Process a request directly with the secondary AI model
        /// </summary>
        /// <param name="prompt">The prompt to send to the secondary AI</param>
        /// <returns>Response from the secondary AI</returns>
        public async Task<SecondaryAiResponse> ProcessRequestAsync(string prompt)
        {
            var startTime = DateTime.UtcNow;
            try
            {
                _logger.LogInformation("Processing secondary AI request");
                
                // Get the secondary model
                var secondaryModelName = _generalSettingsService.CurrentSettings.SecondaryModel;
                if (string.IsNullOrEmpty(secondaryModelName))
                {
                    return new SecondaryAiResponse
                    {
                        Success = false,
                        Error = "No secondary model configured"
                    };
                }

                // Find the model and service provider
                var model = _generalSettingsService.CurrentSettings.ModelList.FirstOrDefault(x => x.ModelName == secondaryModelName);
                if (model == null)
                {
                    return new SecondaryAiResponse
                    {
                        Success = false,
                        Error = $"Secondary model '{secondaryModelName}' not found"
                    };
                }

                var service = ServiceProvider.GetProviderForGuid(_generalSettingsService.CurrentSettings.ServiceProviders, model.ProviderGuid);
                var aiService = AiServiceResolver.GetAiService(service.ServiceName, null, _mcpService);

                if (aiService == null)
                {
                    return new SecondaryAiResponse
                    {
                        Success = false,
                        Error = $"Could not resolve AI service for provider '{service.FriendlyName}'"
                    };
                }

                // Create a simple chat request with just the prompt
                var conv = new LinearConv(DateTime.Now)
                {
                    systemprompt = "You are a helpful assistant focused on file operations.",
                    messages = new List<LinearConvMessage>
                    {
                        new LinearConvMessage
                        {
                            role = "user",
                            contentBlocks = new List<ContentBlock>
                            {
                                new ContentBlock
                                {
                                    ContentType = ContentType.Text,
                                    Content = prompt
                                }
                            }
                        }
                    }
                };

                var requestOptions = new AiRequestOptions
                {
                    ServiceProvider = service,
                    Model = model,
                    Conv = conv,
                    CancellationToken = CancellationToken.None,
                    ApiSettings = _generalSettingsService.CurrentSettings.ToApiSettings(),
                    MustNotUseEmbedding = true,

                };
                
                var response = await aiService.FetchResponse(requestOptions, true);
                
                _logger.LogInformation("Secondary AI request completed in {Time}ms", (DateTime.UtcNow - startTime).TotalMilliseconds);
                return new SecondaryAiResponse
                {
                    Success = response.Success,
                    Response = string.Join("\n\n", response.ContentBlocks.Where(x => x.ContentType == Core.Models.ContentType.Text).Select(x => x.Content)),
                    Error = response.Success ? string.Empty : "Failed to process request"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing secondary AI request");
                return new SecondaryAiResponse
                {
                    Success = false,
                    Error = ex.Message
                };
            }
        }
    }
}
