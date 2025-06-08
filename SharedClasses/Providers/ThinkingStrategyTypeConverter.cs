// SharedClasses/Providers/ThinkingStrategyTypeConverter.cs
using Newtonsoft.Json;
using System;

namespace SharedClasses.Providers
{
    public class ThinkingStrategyTypeConverter : JsonConverter<ThinkingStrategyType>
    {
        public override void WriteJson(JsonWriter writer, ThinkingStrategyType value, JsonSerializer serializer)
        {
            writer.WriteValue(value.ToString());
        }

        public override ThinkingStrategyType ReadJson(JsonReader reader, Type objectType, ThinkingStrategyType existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.String)
            {
                string stringValue = reader.Value?.ToString();
                if (Enum.TryParse<ThinkingStrategyType>(stringValue, true, out var result))
                {
                    return result;
                }
            }
            else if (reader.TokenType == JsonToken.Integer)
            {
                // Handle backward compatibility with numeric values
                int intValue = Convert.ToInt32(reader.Value);
                if (Enum.IsDefined(typeof(ThinkingStrategyType), intValue))
                {
                    return (ThinkingStrategyType)intValue;
                }
            }

            // Default fallback
            return ThinkingStrategyType.None;
        }
    }
}