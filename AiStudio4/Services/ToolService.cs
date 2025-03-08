using AiStudio4.Core.Interfaces;
using AiStudio4.Core.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AiStudio4.Services
{
    public class ToolService : IToolService
    {
        private readonly ILogger<ToolService> _logger;
        private readonly string _toolsDirectory;
        private ToolLibrary _toolLibrary;
        private const string LIBRARY_FILENAME = "toolLibrary.json";
        private bool _isInitialized = false;

        public ToolService(ILogger<ToolService> logger)
        {
            _logger = logger;
            _toolsDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AiStudio4", "Tools");

            Directory.CreateDirectory(_toolsDirectory);

            // We'll initialize in InitializeAsync now
        }

        public async Task InitializeAsync()
        {
            if (_isInitialized)
                return;

            // Now load the tool library
            LoadToolLibrary();

            _isInitialized = true;
        }

        private void LoadToolLibrary()
        {
            var libraryPath = Path.Combine(_toolsDirectory, LIBRARY_FILENAME);

            if (File.Exists(libraryPath))
            {
                try
                {
                    var json = File.ReadAllText(libraryPath);
                    _toolLibrary = JsonConvert.DeserializeObject<ToolLibrary>(json) ?? new ToolLibrary();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error loading tool library");
                    _toolLibrary = new ToolLibrary();
                }
            }
            else
            {
                _toolLibrary = new ToolLibrary();
                // Initialize with default categories
                _toolLibrary.Categories.Add(new ToolCategory { Name = "API Tools", Priority = 100 });
                _toolLibrary.Categories.Add(new ToolCategory { Name = "Development", Priority = 90 });
                _toolLibrary.Categories.Add(new ToolCategory { Name = "Data Analysis", Priority = 80 });
                _toolLibrary.Categories.Add(new ToolCategory { Name = "Productivity", Priority = 70 });
                SaveToolLibrary();
            }
        }

        private void SaveToolLibrary()
        {
            var libraryPath = Path.Combine(_toolsDirectory, LIBRARY_FILENAME);
            var json = JsonConvert.SerializeObject(_toolLibrary, Formatting.Indented);
            File.WriteAllText(libraryPath, json);
        }

        public async Task<List<Tool>> GetAllToolsAsync()
        {
            await EnsureInitialized();
            return await Task.FromResult(_toolLibrary.Tools);
        }

        public async Task<Tool> GetToolByIdAsync(string toolId)
        {
            await EnsureInitialized();
            return await Task.FromResult(_toolLibrary.Tools.FirstOrDefault(t => t.Guid == toolId));
        }

        public async Task<Tool> AddToolAsync(Tool tool)
        {
            await EnsureInitialized();

            if (string.IsNullOrEmpty(tool.Guid))
            {
                tool.Guid = Guid.NewGuid().ToString();
            }

            // Ensure FileType is not null
            if (tool.Filetype == null)
            {
                tool.Filetype = string.Empty;
            }

            tool.LastModified = DateTime.UtcNow;
            _toolLibrary.Tools.Add(tool);
            SaveToolLibrary();

            return await Task.FromResult(tool);
        }

        public async Task<Tool> UpdateToolAsync(Tool tool)
        {
            await EnsureInitialized();

            var existingTool = _toolLibrary.Tools.FirstOrDefault(t => t.Guid == tool.Guid);
            if (existingTool == null)
            {
                throw new KeyNotFoundException($"Tool with ID {tool.Guid} not found");
            }

            // Ensure FileType is not null
            if (tool.Filetype == null)
            {
                tool.Filetype = string.Empty;
            }

            var index = _toolLibrary.Tools.IndexOf(existingTool);
            tool.LastModified = DateTime.UtcNow;
            _toolLibrary.Tools[index] = tool;
            SaveToolLibrary();

            return await Task.FromResult(tool);
        }

        public async Task<bool> DeleteToolAsync(string toolId)
        {
            await EnsureInitialized();

            var tool = _toolLibrary.Tools.FirstOrDefault(t => t.Guid == toolId);
            if (tool == null)
            {
                return await Task.FromResult(false);
            }

            _toolLibrary.Tools.Remove(tool);
            SaveToolLibrary();

            return await Task.FromResult(true);
        }

        public async Task<Tool> GetToolByNameAsync(string toolName)
        {
            await EnsureInitialized();

            if (string.IsNullOrEmpty(toolName))
            {
                return null;
            }

            // Case-insensitive search for a tool with the exact name
            return await Task.FromResult(_toolLibrary.Tools.FirstOrDefault(t =>
                string.Equals(t.SchemaName, toolName, StringComparison.OrdinalIgnoreCase)));
        }

        public async Task<List<ToolCategory>> GetToolCategoriesAsync()
        {
            await EnsureInitialized();
            return await Task.FromResult(_toolLibrary.Categories);
        }

        public async Task<ToolCategory> AddToolCategoryAsync(ToolCategory category)
        {
            await EnsureInitialized();

            if (string.IsNullOrEmpty(category.Id))
            {
                category.Id = Guid.NewGuid().ToString();
            }

            _toolLibrary.Categories.Add(category);
            SaveToolLibrary();

            return await Task.FromResult(category);
        }

        public async Task<ToolCategory> UpdateToolCategoryAsync(ToolCategory category)
        {
            await EnsureInitialized();

            var existingCategory = _toolLibrary.Categories.FirstOrDefault(c => c.Id == category.Id);
            if (existingCategory == null)
            {
                throw new KeyNotFoundException($"Category with ID {category.Id} not found");
            }

            var index = _toolLibrary.Categories.IndexOf(existingCategory);
            _toolLibrary.Categories[index] = category;
            SaveToolLibrary();

            return await Task.FromResult(category);
        }

        public async Task<bool> DeleteToolCategoryAsync(string categoryId)
        {
            await EnsureInitialized();

            var category = _toolLibrary.Categories.FirstOrDefault(c => c.Id == categoryId);
            if (category == null)
            {
                return await Task.FromResult(false);
            }

            _toolLibrary.Categories.Remove(category);
            SaveToolLibrary();

            return await Task.FromResult(true);
        }

        public async Task<bool> ValidateToolSchemaAsync(string schema)
        {
            await EnsureInitialized();

            try
            {
                var jobj = JObject.Parse(schema);
                return await Task.FromResult(jobj != null && jobj["name"] != null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating tool schema");
                return await Task.FromResult(false);
            }
        }

        public async Task<List<Tool>> ImportToolsAsync(string json)
        {
            await EnsureInitialized();

            try
            {
                var importedTools = JsonConvert.DeserializeObject<List<Tool>>(json);
                if (importedTools == null || !importedTools.Any())
                {
                    return await Task.FromResult(new List<Tool>());
                }

                foreach (var tool in importedTools)
                {
                    if (string.IsNullOrEmpty(tool.Guid))
                    {
                        tool.Guid = Guid.NewGuid().ToString();
                    }

                    // Ensure FileType is not null
                    if (tool.Filetype == null)
                    {
                        tool.Filetype = string.Empty;
                    }

                    tool.LastModified = DateTime.UtcNow;

                    // Check if a tool with the same GUID already exists
                    var existingTool = _toolLibrary.Tools.FirstOrDefault(t => t.Guid == tool.Guid);
                    if (existingTool != null)
                    {
                        // Replace the existing tool
                        var index = _toolLibrary.Tools.IndexOf(existingTool);
                        _toolLibrary.Tools[index] = tool;
                    }
                    else
                    {
                        _toolLibrary.Tools.Add(tool);
                    }
                }

                SaveToolLibrary();
                return await Task.FromResult(importedTools);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing tools");
                throw;
            }
        }

        public async Task<string> ExportToolsAsync(List<string> toolIds = null)
        {
            await EnsureInitialized();

            try
            {
                List<Tool> toolsToExport;

                if (toolIds == null || !toolIds.Any())
                {
                    // Export all tools
                    toolsToExport = _toolLibrary.Tools;
                }
                else
                {
                    // Export only the specified tools
                    toolsToExport = _toolLibrary.Tools.Where(t => toolIds.Contains(t.Guid)).ToList();
                }

                return await Task.FromResult(JsonConvert.SerializeObject(toolsToExport, Formatting.Indented));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting tools");
                throw;
            }
        }

        private async Task EnsureInitialized()
        {
            if (!_isInitialized)
            {
                await InitializeAsync();
            }
        }
    }
}