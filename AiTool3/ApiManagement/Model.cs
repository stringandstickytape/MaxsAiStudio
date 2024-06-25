namespace AiTool3.ApiManagement
{
    public class Model
    {
        public string ModelName { get; set; }
        public string ServiceName { get; set; }
        public string Key { get; set; }
        public string Url { get; set; }

        public override string ToString()
        {
            return $"{ServiceName}: {ModelName}";
        }
    }
}
