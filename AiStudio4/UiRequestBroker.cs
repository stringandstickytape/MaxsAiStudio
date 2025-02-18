using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace AiStudio4
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
                case "getConfig":
                default:
                    return JsonSerializer.Serialize(new { success = true, data = string.Join(",",_settingsManager.CurrentSettings.ModelList.Select(x => x.ModelName)) });
            }
        }
    }
}