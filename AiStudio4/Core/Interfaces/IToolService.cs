



namespace AiStudio4.Core.Interfaces
{
    public interface IToolService
    {
        
        
        
        Task InitializeAsync();

        
        
        
        Task<List<Tool>> GetAllToolsAsync();

        
        
        
        Task<Tool> GetToolByIdAsync(string toolId);

        
        
        
        Task<Tool> AddToolAsync(Tool tool);

        
        
        
        Task<Tool> UpdateToolAsync(Tool tool);

        
        
        
        Task<bool> DeleteToolAsync(string toolId);

        
        
        
        Task<List<ToolCategory>> GetToolCategoriesAsync();

        
        
        
        Task<ToolCategory> AddToolCategoryAsync(ToolCategory category);

        
        
        
        Task<ToolCategory> UpdateToolCategoryAsync(ToolCategory category);

        
        
        
        Task<bool> DeleteToolCategoryAsync(string categoryId);

        
        
        
        Task<bool> ValidateToolSchemaAsync(string schema);

        
        
        
        Task<List<Tool>> ImportToolsAsync(string json);

        
        
        
        Task<string> ExportToolsAsync(List<string> toolIds = null);
        Task<Tool> GetToolBySchemaNameAsync(string toolName);
        Task<Tool> GetToolByToolNameAsync(string toolName);
    }
}
