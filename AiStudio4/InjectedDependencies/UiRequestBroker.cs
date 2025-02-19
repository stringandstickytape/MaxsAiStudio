using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Text.Json;

namespace AiStudio4.InjectedDependencies
{
    public class UiRequestBroker
    {
        private readonly IConfiguration _configuration;
        private readonly SettingsManager _settingsManager;
        private readonly WebSocketServer _webSocketServer;

        public UiRequestBroker(IConfiguration configuration, SettingsManager settingsManager, WebSocketServer webSocketServer)
        {
            _configuration = configuration;
            _settingsManager = settingsManager;
            _webSocketServer = webSocketServer;
        }

        public async Task<string> HandleRequestAsync(string requestType, string requestData)
        {
            switch (requestType)
            {
                case "chat":
                    await _webSocketServer.SendToAllClientsAsync(JsonConvert.SerializeObject(new { messageType = "c", content = "fragment" }));
                    return JsonConvert.SerializeObject("LOL");
                case "getConfig":
                default:
                    return JsonConvert.SerializeObject(new { success = true, models = _settingsManager.CurrentSettings.ModelList.Select(x => x.ModelName).ToArray() });
            }
        }
    }
}