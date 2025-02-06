using AiTool3.AiServices;
using Newtonsoft.Json;
using System.Diagnostics;

namespace AiTool3.DataModels
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

        public decimal input1MTokenPrice { get; set; }
        public decimal output1MTokenPrice { get; set; }

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

        public override string ToString()
        {
            return FriendlyName;
        }

        public string GetCost(TokenUsage tokenUsage)
        {
            var cost = ((tokenUsage.InputTokens * input1MTokenPrice) +
                (tokenUsage.CacheCreationInputTokens * input1MTokenPrice * 1.25m) +
                (tokenUsage.CacheReadInputTokens * input1MTokenPrice * 0.1m) +
                (tokenUsage.OutputTokens * output1MTokenPrice)) / 1000000m;

            return cost.ToString("0.00");
        }
    }
}
