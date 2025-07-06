using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AiStudio4.Core.Interfaces;
using AiStudio4.Core.Models;
using Newtonsoft.Json.Linq;

namespace AiStudio4.InjectedDependencies.RequestHandlers
{
    public class TipOfTheDayRequestHandler : BaseRequestHandler
    {
        private readonly ITipOfTheDayService _tipOfTheDayService;

        public TipOfTheDayRequestHandler(ITipOfTheDayService tipOfTheDayService)
        {
            _tipOfTheDayService = tipOfTheDayService ?? throw new ArgumentNullException(nameof(tipOfTheDayService));
        }

        protected override IEnumerable<string> SupportedRequestTypes => new[]
        {
            "tipOfTheDay/getSettings",
            "tipOfTheDay/saveSettings"
        };

        public override async Task<string> HandleAsync(string clientId, string requestType, JObject requestObject)
        {
            try
            {
                return requestType switch
                {
                    "tipOfTheDay/getSettings" => await HandleGetSettingsRequest(requestObject),
                    "tipOfTheDay/saveSettings" => await HandleSaveSettingsRequest(requestObject),
                    _ => SerializeError($"Unsupported request type: {requestType}")
                };
            }
            catch (Exception ex)
            {
                return SerializeError($"Error handling {requestType} request: {ex.Message}");
            }
        }

        private async Task<string> HandleGetSettingsRequest(JObject requestObject)
        {
            var settings = _tipOfTheDayService.GetSettings();
            return SerializeSuccess(settings);
        }

        private async Task<string> HandleSaveSettingsRequest(JObject requestObject)
        {
            var settingsObj = requestObject["settings"];
            if (settingsObj == null)
            {
                return SerializeError("Settings object is required");
            }

            try
            {
                var settings = settingsObj.ToObject<TipOfTheDaySettings>();
                if (settings == null)
                {
                    return SerializeError("Invalid settings format");
                }

                _tipOfTheDayService.UpdateSettings(settings);
                return SerializeSuccess();
            }
            catch (Exception ex)
            {
                return SerializeError($"Failed to save settings: {ex.Message}");
            }
        }
    }
}