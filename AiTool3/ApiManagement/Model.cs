using AiTool3.Providers;

namespace AiTool3.ApiManagement
{
    public class Model
    {
        public string ModelName { get; set; }
        public string ServiceName { get; set; }
        public string Key { get; set; }
        public string Url { get; set; }
        public decimal input1MTokenPrice { get; set; }
        public decimal output1MTokenPrice { get; set; }
        public Color Color { get; set; }
        public bool Starred { get; set; }

        public Model() { }

        public Model(string modelName, string serviceName, string key, string url, decimal inputPrice, decimal outputPrice, Color color)
        {
            ModelName = modelName;
            ServiceName = serviceName;
            Key = key;
            Url = url;
            input1MTokenPrice = inputPrice;
            output1MTokenPrice = outputPrice;
            Color = color;
        }
        public override string ToString()
        {
            return $"{ModelName} [{ServiceName} API]";
        }

        public string GetCost(TokenUsage tokenUsage)
        {
            return ((tokenUsage.InputTokens * input1MTokenPrice / 1000000) + (tokenUsage.OutputTokens * output1MTokenPrice / 1000000)).ToString("0.00");
        }
    }
}
