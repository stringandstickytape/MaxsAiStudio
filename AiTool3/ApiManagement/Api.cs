namespace AiTool3.ApiManagement
{
    public class Api
    {
        public string ApiName { get; set; }
        public string ApiUrl { get; set; }
        public List<Model> Models { get; set; }

        public Model GetModelByName(string modelName) => Models.Find(x => x.ModelName == modelName);
    }
}
