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
            "tipOfTheDay/getTipOfTheDay"
        };

        public override async Task<string> HandleAsync(string clientId, string requestType, JObject requestObject)
        {
            try
            {
                return requestType switch
                {
                    "tipOfTheDay/getTipOfTheDay" => await HandleGetTipOfTheDayRequest(requestObject),
                    _ => SerializeError($"Unsupported request type: {requestType}")
                };
            }
            catch (Exception ex)
            {
                return SerializeError($"Error handling {requestType} request: {ex.Message}");
            }
        }

        private async Task<string> HandleGetTipOfTheDayRequest(JObject requestObject)
        {
            var tip = _tipOfTheDayService.GetTipOfTheDay();
            return SerializeSuccess(tip);
        }
    }
}