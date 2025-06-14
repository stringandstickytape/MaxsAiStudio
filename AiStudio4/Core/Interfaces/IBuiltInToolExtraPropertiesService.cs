


namespace AiStudio4.Core.Interfaces
{
    
    
    
    public interface IBuiltInToolExtraPropertiesService
    {
        
        
        
        
        
        
        
        Dictionary<string, Dictionary<string, string>> LoadAll();

        
        
        
        
        
        
        
        
        Dictionary<string, string> GetExtraProperties(string toolName);

        
        
        
        
        
        void SaveExtraProperties(string toolName, Dictionary<string, string> extraProperties);

        
        
        
        
        void DeleteExtraProperties(string toolName);
    }
}
