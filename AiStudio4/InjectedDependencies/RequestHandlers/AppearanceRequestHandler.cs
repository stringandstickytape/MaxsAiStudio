// AiStudio4/InjectedDependencies/RequestHandlers/AppearanceRequestHandler.cs







namespace AiStudio4.InjectedDependencies.RequestHandlers
{
    /// <summary>
    /// Handles appearance settings-related requests
    /// </summary>
    public class AppearanceRequestHandler : BaseRequestHandler
    {
        private readonly IAppearanceSettingsService _appearanceSettingsService;

        public AppearanceRequestHandler(IAppearanceSettingsService appearanceSettingsService)
        {
            _appearanceSettingsService = appearanceSettingsService ?? throw new ArgumentNullException(nameof(appearanceSettingsService));
        }

        protected override IEnumerable<string> SupportedRequestTypes => new[]
        {
            "getAppearanceSettings",
            "saveAppearanceSettings"
        };

        public override async Task<string> HandleAsync(string clientId, string requestType, JObject requestObject)
        {
            try
            {
                return requestType switch
                {
                    "getAppearanceSettings" => await HandleGetAppearanceSettingsRequest(requestObject),
                    "saveAppearanceSettings" => await HandleSaveAppearanceSettingsRequest(requestObject),
                    _ => SerializeError($"Unsupported request type: {requestType}")
                };
            }
            catch (Exception ex)
            {
                return SerializeError($"Error handling {requestType} request: {ex.Message}");
            }
        }

        private async Task<string> HandleGetAppearanceSettingsRequest(JObject requestObject)
        {
            try
            {
                var settings = _appearanceSettingsService.GetAppearanceSettings();
                return JsonConvert.SerializeObject(new
                {
                    success = true,
                    fontSize = settings.FontSize,
                    isDarkMode = settings.IsDarkMode,
                    chatPanelSize = settings.ChatPanelSize,
                    inputBarPanelSize = settings.InputBarPanelSize,
                    stickToBottomEnabled = settings.StickToBottomEnabled,
                    chatSpaceWidth = settings.ChatSpaceWidth,
                });
            }
            catch (Exception ex)
            {
                return SerializeError($"Error retrieving appearance settings: {ex.Message}");
            }
        }

        private async Task<string> HandleSaveAppearanceSettingsRequest(JObject requestObject)
        {
            try
            {
                // Get current settings first to preserve existing values
                var currentSettings = _appearanceSettingsService.GetAppearanceSettings();
                
                // Update only the properties that are provided in the request
                var settings = new AppearanceSettings
                {
                    FontSize = requestObject["fontSize"]?.Value<int>() ?? currentSettings.FontSize,
                    IsDarkMode = requestObject["isDarkMode"]?.Value<bool>() ?? currentSettings.IsDarkMode,
                    ChatPanelSize = requestObject["chatPanelSize"]?.Value<int>() ?? currentSettings.ChatPanelSize,
                    InputBarPanelSize = requestObject["inputBarPanelSize"]?.Value<int>() ?? currentSettings.InputBarPanelSize,
                    StickToBottomEnabled = requestObject["stickToBottomEnabled"]?.Value<bool>() ?? currentSettings.StickToBottomEnabled,
                    ChatSpaceWidth = requestObject["chatSpaceWidth"]?.Value<string>() ?? currentSettings.ChatSpaceWidth,
                };

                _appearanceSettingsService.UpdateAppearanceSettings(settings);
                return JsonConvert.SerializeObject(new { success = true });
            }
            catch (Exception ex)
            {
                return SerializeError($"Error saving appearance settings: {ex.Message}");
            }
        }
    }
}
