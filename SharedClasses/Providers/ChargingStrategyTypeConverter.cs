// SharedClasses/Providers/ChargingStrategyTypeConverter.cs
using Newtonsoft.Json;
using System;

namespace SharedClasses.Providers
{
    public class ChargingStrategyTypeConverter : JsonConverter<ChargingStrategyType>
    {
        public override void WriteJson(JsonWriter writer, ChargingStrategyType value, JsonSerializer serializer)
        {
            writer.WriteValue(value.ToString());
        }

        public override ChargingStrategyType ReadJson(JsonReader reader, Type objectType, ChargingStrategyType existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.String)
            {
                string stringValue = reader.Value?.ToString();
                if (Enum.TryParse<ChargingStrategyType>(stringValue, true, out var result))
                {
                    return result;
                }
            }
            else if (reader.TokenType == JsonToken.Integer)
            {
                // Handle backward compatibility with numeric values
                int intValue = Convert.ToInt32(reader.Value);
                if (Enum.IsDefined(typeof(ChargingStrategyType), intValue))
                {
                    return (ChargingStrategyType)intValue;
                }
            }

            // Default fallback
            return ChargingStrategyType.NoCaching;
        }
    }
}