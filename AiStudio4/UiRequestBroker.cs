using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace AiStudio4
{
    public class UiRequestBroker
    {
        private readonly IConfiguration _configuration;

        public UiRequestBroker(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<string> HandleRequestAsync(string requestType, string requestData)
        {
            switch (requestType)
            {
                case "getConfig":
                default:
                    return JsonSerializer.Serialize(new { success = true, data = _configuration });
            }
        }
    }
}