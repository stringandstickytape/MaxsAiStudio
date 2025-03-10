namespace AiStudio4.Core.Models
{
    public class ModelCostConfig
    {
        public string ModelName { get; set; }
        public decimal InputCostPer1M { get; set; }
        public decimal OutputCostPer1M { get; set; }
    }
}