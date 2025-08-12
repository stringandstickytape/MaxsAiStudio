namespace AiStudio4.Tools.Interfaces
{
    /// <summary>
    /// Service for managing extra properties for built-in tools
    /// </summary>
    public interface IBuiltInToolExtraPropertiesService
    {
        /// <summary>
        /// Gets extra properties for a specific tool
        /// </summary>
        Dictionary<string, string> GetExtraProperties(string toolName);
    }
}