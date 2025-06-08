using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Drawing;

namespace SharedClasses.Providers
{
    public class ColorConverter : JsonConverter<Color>
    {
        public override Color ReadJson(JsonReader reader, Type objectType, Color existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (string.IsNullOrEmpty((string)reader.Value))
                return Color.LightCyan;

            string colorString = (string)reader.Value;
            return ColorTranslator.FromHtml(colorString);
        }

        public override void WriteJson(JsonWriter writer, Color value, JsonSerializer serializer)
        {
            writer.WriteValue(ColorTranslator.ToHtml(value));
        }
    }

    [DebuggerDisplay("ModelName = {ModelName}")]
    public class Model
    {
        public string ModelName { get; set; }

        public string UserNotes { get; set; }
        public string ProviderGuid { get; set; }

        public string AdditionalParams { get; set; }

        // --- TIERED PRICING PROPERTIES ---
        
        /// <summary>
        /// The token count boundary at which pricing changes.
        /// e.g., for Gemini 1.5 Pro this is 128,000. Null or 0 means no tiered pricing.
        /// </summary>
        public int? PriceBoundary { get; set; }
        
        /// <summary>
        /// The input price per 1M tokens for requests below the boundary (or default price if no tiered pricing).
        /// </summary>
        [JsonProperty("input1MTokenPrice")] // Keep old JSON name for compatibility
        public decimal InputPriceBelowBoundary { get; set; }

        /// <summary>
        /// The output price per 1M tokens for requests below the boundary (or default price if no tiered pricing).
        /// </summary>
        [JsonProperty("output1MTokenPrice")] // Keep old JSON name for compatibility
        public decimal OutputPriceBelowBoundary { get; set; }
        
        /// <summary>
        /// The input price per 1M tokens for requests above the boundary.
        /// </summary>
        public decimal? InputPriceAboveBoundary { get; set; }

        /// <summary>
        /// The output price per 1M tokens for requests above the boundary.
        /// </summary>
        public decimal? OutputPriceAboveBoundary { get; set; }
        
        // --- BACKWARD COMPATIBILITY PROPERTIES ---
        
        [Obsolete("Use InputPriceBelowBoundary instead.")]
        [JsonIgnore] // Prevent this from being serialized
        public decimal input1MTokenPrice { 
            get => InputPriceBelowBoundary; 
            set => InputPriceBelowBoundary = value; 
        }

        [Obsolete("Use OutputPriceBelowBoundary instead.")]
        [JsonIgnore] // Prevent this from being serialized
        public decimal output1MTokenPrice { 
            get => OutputPriceBelowBoundary; 
            set => OutputPriceBelowBoundary = value; 
        }

        [JsonConverter(typeof(ColorConverter))]
        public Color Color { get; set; }
        public bool Starred { get; set; }

        public string FriendlyName { get; set; }

        private string guid;
        public string Guid {
            get { return guid; }
            set {
                Debug.WriteLine(guid);
                guid = value; 
            }
        }

        public Model()
        {
            Guid = System.Guid.NewGuid().ToString();
        }

        public Model(string guid)
        {
            Guid = guid;
        }

        public bool SupportsPrefill { get; set; }

        public bool Requires1fTemp { get; set; }

        // ReasoningEffort: none, low, medium, high
        public string ReasoningEffort { get; set; } = "none";

        public bool IsTtsModel { get; set; } = false;
        public string TtsVoiceName { get; set; } = "Kore"; // Default voice

        public override string ToString()
        {
            return FriendlyName;
        }

        public string GetCost(TokenUsage tokenUsage)
        {
            var cost = ((tokenUsage.InputTokens * InputPriceBelowBoundary) +
                (tokenUsage.CacheCreationInputTokens * InputPriceBelowBoundary * 1.25m) +
                (tokenUsage.CacheReadInputTokens * InputPriceBelowBoundary * 0.1m) +
                (tokenUsage.OutputTokens * OutputPriceBelowBoundary)) / 1000000m;

            return cost.ToString("0.00");
        }
    }
}
