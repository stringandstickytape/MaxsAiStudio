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

        public async Task<string> HandleRequestAsync(string clientId, string requestType, string requestData)
        {
            if (requestType == "chat")
            {
                // Start a background task that continues after the method returns

            }

            switch (requestType)
            {
                case "chat":
                    _ = Task.Run(async () =>
                    {
                        for (int i = 0; i < 5; i++)
                        {
                            await _webSocketServer.SendToClientAsync(clientId, JsonConvert.SerializeObject(new { messageType = "c", content = "fragment " }));
                            await Task.Delay(1000); // Wait for 1 second
                        }
                    });
                    return JsonConvert.SerializeObject("Test Completed");
                case "getConfig":
                default:
                    return JsonConvert.SerializeObject(new { success = true, models = _settingsManager.CurrentSettings.ModelList.Select(x => x.ModelName).ToArray() });
            }
        }
    }
}