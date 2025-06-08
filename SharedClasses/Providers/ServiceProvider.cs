using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

using SharedClasses.Providers;

namespace SharedClasses.Providers
{
    public class ServiceProvider
    {
        public string Url { get; set; }
        public string ApiKey { get; set; }

        public string FriendlyName { get; set; }
        public string ServiceName { get; set; }
        public string IconName { get; set; }

        // create a guid
        public string Guid { get; set; }

        /// <summary>
        /// Specifies the cost calculation strategy for this provider.
        /// </summary>
        [JsonConverter(typeof(ChargingStrategyTypeConverter))]
        public ChargingStrategyType ChargingStrategy { get; set; } = ChargingStrategyType.Claude;

        public ServiceProvider()
        {
            Guid = System.Guid.NewGuid().ToString();
        }

        public override string ToString()
        {
            return $"{FriendlyName}";
        }

        public static ServiceProvider GetProviderForGuid(List<ServiceProvider> services, string guid)
        {
            return services.FirstOrDefault(x => x.Guid == guid);
        }
    }
}