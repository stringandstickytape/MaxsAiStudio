using AiTool3.ApiManagement;
using AiTool3;
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

        public override string ToString()
        {
            return $"{ServiceName}: {ModelName}";
        }

        public string GetCost(TokenUsage tokenUsage)
        {
            return ((tokenUsage.InputTokens * input1MTokenPrice / 1000000) + (tokenUsage.OutputTokens * output1MTokenPrice / 1000000)).ToString("0.00");
        }
    }
}
