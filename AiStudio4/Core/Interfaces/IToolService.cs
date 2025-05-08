using AiStudio4.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AiStudio4.Core.Interfaces
{
    public interface IToolService
    {
        /// <summary>
        /// Initializes the tool service, including any required migrations
        /// </summary>
        Task InitializeAsync();

        /// <summary>
        /// Gets all available tools
        /// </summary>
        Task<List<Tool>> GetAllToolsAsync();

        /// <summary>
        /// Gets a tool by its unique identifier
        /// </summary>
        Task<Tool> GetToolByIdAsync(string toolId);

        /// <summary>
        /// Adds a new tool to the library
        /// </summary>
        Task<Tool> AddToolAsync(Tool tool);

        /// <summary>
        /// Updates an existing tool
        /// </summary>
        Task<Tool> UpdateToolAsync(Tool tool);

        /// <summary>
        /// Deletes a tool by its unique identifier
        /// </summary>
        Task<bool> DeleteToolAsync(string toolId);

        /// <summary>
        /// Gets all tool categories
        /// </summary>
        Task<List<ToolCategory>> GetToolCategoriesAsync();

        /// <summary>
        /// Adds a new tool category
        /// </summary>
        Task<ToolCategory> AddToolCategoryAsync(ToolCategory category);

        /// <summary>
        /// Updates an existing tool category
        /// </summary>
        Task<ToolCategory> UpdateToolCategoryAsync(ToolCategory category);

        /// <summary>
        /// Deletes a tool category by its unique identifier
        /// </summary>
        Task<bool> DeleteToolCategoryAsync(string categoryId);

        /// <summary>
        /// Validates a tool schema
        /// </summary>
        Task<bool> ValidateToolSchemaAsync(string schema);

        /// <summary>
        /// Imports tools from JSON
        /// </summary>
        Task<List<Tool>> ImportToolsAsync(string json);

        /// <summary>
        /// Exports tools to JSON
        /// </summary>
        Task<string> ExportToolsAsync(List<string> toolIds = null);
        Task<Tool> GetToolBySchemaNameAsync(string toolName);
        Task<Tool> GetToolByToolNameAsync(string toolName);
    }
}