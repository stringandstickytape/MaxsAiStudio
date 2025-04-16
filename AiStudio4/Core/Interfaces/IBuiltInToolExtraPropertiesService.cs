// Core/Interfaces/IBuiltInToolExtraPropertiesService.cs
using System.Collections.Generic;

namespace AiStudio4.Core.Interfaces
{
    /// <summary>
    /// Defines methods to manage persistence of extra properties for built-in tools.
    /// </summary>
    public interface IBuiltInToolExtraPropertiesService
    {
        /// <summary>
        /// Loads all extra properties for all built-in tools.
        /// </summary>
        /// <returns>
        /// A dictionary where each key is the tool name and the value is another dictionary 
        /// of property name and property value pairs.
        /// </returns>
        Dictionary<string, Dictionary<string, string>> LoadAll();

        /// <summary>
        /// Gets the extra properties for a specific built-in tool.
        /// </summary>
        /// <param name="toolName">The name of the built-in tool.</param>
        /// <returns>
        /// A dictionary of property name and property value pairs for the specified tool.
        /// Returns an empty dictionary if no properties are found.
        /// </returns>
        Dictionary<string, string> GetExtraProperties(string toolName);

        /// <summary>
        /// Saves or updates the extra properties for a specific built-in tool.
        /// </summary>
        /// <param name="toolName">The name of the built-in tool.</param>
        /// <param name="extraProperties">A dictionary of property name and property value pairs to save.</param>
        void SaveExtraProperties(string toolName, Dictionary<string, string> extraProperties);

        /// <summary>
        /// Deletes all extra properties associated with a specific built-in tool.
        /// </summary>
        /// <param name="toolName">The name of the built-in tool.</param>
        void DeleteExtraProperties(string toolName);
    }
}