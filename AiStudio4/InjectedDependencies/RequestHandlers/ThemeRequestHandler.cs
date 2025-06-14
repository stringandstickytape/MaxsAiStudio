// AiStudio4/InjectedDependencies/RequestHandlers/ThemeRequestHandler.cs








namespace AiStudio4.InjectedDependencies.RequestHandlers
{
    /// <summary>
    /// Handles theme-related requests
    /// </summary>
    public class ThemeRequestHandler : BaseRequestHandler
    {
        private readonly IThemeService _themeService;

        public ThemeRequestHandler(IThemeService themeService)
        {
            _themeService = themeService ?? throw new ArgumentNullException(nameof(themeService));
        }

        protected override IEnumerable<string> SupportedRequestTypes => new[]
        {
            "themes/getAll",
            "themes/getById",
            "themes/add",
            "themes/update",
            "themes/delete",
            "themes/setActive",
            "themes/getActive"
        };

        public override async Task<string> HandleAsync(string clientId, string requestType, JObject requestObject)
        {
            try
            {
                return requestType switch
                {
                    "themes/getAll" => await HandleGetAllThemesRequest(clientId, requestObject),
                    "themes/getById" => await HandleGetThemeByIdRequest(clientId, requestObject),
                    "themes/add" => await HandleAddThemeRequest(clientId, requestObject),
                    "themes/update" => await HandleUpdateThemeRequest(clientId, requestObject),
                    "themes/delete" => await HandleDeleteThemeRequest(requestObject),
                    "themes/setActive" => await HandleSetActiveThemeRequest(requestObject),
                    "themes/getActive" => await HandleGetActiveThemeRequest(clientId, requestObject),
                    _ => SerializeError($"Unsupported request type: {requestType}")
                };
            }
            catch (Exception ex)
            {
                return SerializeError($"Error handling {requestType} request: {ex.Message}");
            }
        }

        private async Task<string> HandleGetAllThemesRequest(string clientId, JObject requestObject)
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

                var themes = _themeService.GetAllThemes();
                return JsonConvert.SerializeObject(new { success = true, themes });
            }
            catch (Exception ex)
            {
                return SerializeError($"Error retrieving themes: {ex.Message}");
            }
        }

        private async Task<string> HandleGetThemeByIdRequest(string clientId, JObject requestObject)
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

                string themeId = requestObject["themeId"]?.ToString();
                if (string.IsNullOrEmpty(themeId))
                {
                    return SerializeError("Theme ID is required");
                }

                var theme = _themeService.GetThemeById(themeId);
                if (theme == null)
                {
                    return SerializeError($"Theme with ID {themeId} not found");
                }

                return JsonConvert.SerializeObject(new { success = true, theme });
            }
            catch (Exception ex)
            {
                return SerializeError($"Error retrieving theme: {ex.Message}");
            }
        }

        private async Task<string> HandleAddThemeRequest(string clientId, JObject requestObject)
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

                var theme = requestObject.ToObject<Theme>();
                if (theme == null)
                {
                    return SerializeError("Invalid theme data");
                }

                var result = _themeService.AddTheme(theme);
                return JsonConvert.SerializeObject(new { success = true, theme = result });
            }
            catch (Exception ex)
            {
                return SerializeError($"Error adding theme: {ex.Message}");
            }
        }

        private async Task<string> HandleUpdateThemeRequest(string clientId, JObject requestObject)
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

                var theme = requestObject.ToObject<Theme>();
                if (theme == null || string.IsNullOrEmpty(theme.Guid))
                {
                    return SerializeError("Invalid theme data or missing theme ID");
                }

                var result = _themeService.UpdateTheme(theme);
                return JsonConvert.SerializeObject(new { success = true, theme = result });
            }
            catch (Exception ex)
            {
                return SerializeError($"Error updating theme: {ex.Message}");
            }
        }

        private async Task<string> HandleDeleteThemeRequest(JObject requestObject)
        {
            try
            {
                string themeId = requestObject["themeId"]?.ToString();
                if (string.IsNullOrEmpty(themeId))
                {
                    return SerializeError("Theme ID is required");
                }

                var success = _themeService.DeleteTheme(themeId);
                return JsonConvert.SerializeObject(new { success });
            }
            catch (Exception ex)
            {
                return SerializeError($"Error deleting theme: {ex.Message}");
            }
        }

        private async Task<string> HandleSetActiveThemeRequest(JObject requestObject)
        {
            try
            {
                string themeId = requestObject["themeId"]?.ToString();
                if (string.IsNullOrEmpty(themeId))
                {
                    return SerializeError("Theme ID is required");
                }

                var success = _themeService.SetActiveTheme(themeId);
                return JsonConvert.SerializeObject(new { success });
            }
            catch (Exception ex)
            {
                return SerializeError($"Error setting active theme: {ex.Message}");
            }
        }

        private async Task<string> HandleGetActiveThemeRequest(string clientId, JObject requestObject)
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

                var themeId = _themeService.GetActiveThemeId();
                return JsonConvert.SerializeObject(new { success = true, themeId });
            }
            catch (Exception ex)
            {
                return SerializeError($"Error retrieving active theme: {ex.Message}");
            }
        }
    }
}
