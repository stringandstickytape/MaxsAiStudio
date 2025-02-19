using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Text.Json;

namespace AiStudio4.InjectedDependencies
{
    public class UiRequestBroker
    {
        private readonly IConfiguration _configuration;
        private readonly SettingsManager _settingsManager;

        public UiRequestBroker(IConfiguration configuration, SettingsManager settingsManager)
        {
            _configuration = configuration;
            _settingsManager = settingsManager;
        }

        public async Task<string> HandleRequestAsync(string requestType, string requestData)
        {
            switch (requestType)
            {
                case "chat":

                    return JsonConvert.SerializeObject("LOL");
                case "getConfig":
                default:
                    return JsonConvert.SerializeObject(new { success = true, models = _settingsManager.CurrentSettings.ModelList.Select(x => x.ModelName).ToArray() });
            }
        }
    }
}