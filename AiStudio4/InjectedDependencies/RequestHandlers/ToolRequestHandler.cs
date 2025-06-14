// AiStudio4/InjectedDependencies/RequestHandlers/ToolRequestHandler.cs









namespace AiStudio4.InjectedDependencies.RequestHandlers
{
    /// <summary>
    /// Handles tool-related requests
    /// </summary>
    public class ToolRequestHandler : BaseRequestHandler
    {
        private readonly IToolService _toolService;
        private readonly IBuiltinToolService _builtinToolService;

        public ToolRequestHandler(
            IToolService toolService,
            IBuiltinToolService builtinToolService)
        {
            _toolService = toolService ?? throw new ArgumentNullException(nameof(toolService));
            _builtinToolService = builtinToolService ?? throw new ArgumentNullException(nameof(builtinToolService));
        }

        protected override IEnumerable<string> SupportedRequestTypes => new[]
        {
            "getTools",
            "getTool",
            "addTool",
            "updateTool",
            "deleteTool",
            "getToolCategories",
            "addToolCategory",
            "updateToolCategory",
            "deleteToolCategory",
            "validateToolSchema",
            "importTools",
            "exportTools"
        };

        public override async Task<string> HandleAsync(string clientId, string requestType, JObject requestObject)
        {
            try
            {
                return requestType switch
                {
                    "getTools" => await HandleGetToolsRequest(),
                    "getTool" => await HandleGetToolRequest(requestObject),
                    "addTool" => await HandleAddToolRequest(requestObject),
                    "updateTool" => await HandleUpdateToolRequest(requestObject),
                    "deleteTool" => await HandleDeleteToolRequest(requestObject),
                    "getToolCategories" => await HandleGetToolCategoriesRequest(),
                    "addToolCategory" => await HandleAddToolCategoryRequest(requestObject),
                    "updateToolCategory" => await HandleUpdateToolCategoryRequest(requestObject),
                    "deleteToolCategory" => await HandleDeleteToolCategoryRequest(requestObject),
                    "validateToolSchema" => await HandleValidateToolSchemaRequest(requestObject),
                    "importTools" => await HandleImportToolsRequest(requestObject),
                    "exportTools" => await HandleExportToolsRequest(requestObject),
                    _ => SerializeError($"Unsupported request type: {requestType}")
                };
            }
            catch (Exception ex)
            {
                return SerializeError($"Error handling {requestType} request: {ex.Message}");
            }
        }

        private async Task<string> HandleGetToolsRequest()
        {
            try
            {
                var tools = await _toolService.GetAllToolsAsync();
                return JsonConvert.SerializeObject(new { success = true, tools });
            }
            catch (Exception ex)
            {
                return SerializeError($"Error retrieving tools: {ex.Message}");
            }
        }

        private async Task<string> HandleGetToolRequest(JObject requestObject)
        {
            try
            {
                string toolId = requestObject["toolId"]?.ToString();
                if (string.IsNullOrEmpty(toolId)) return SerializeError("Tool ID cannot be empty");
                
                var tool = await _toolService.GetToolByIdAsync(toolId);
                if (tool == null) return SerializeError($"Tool with ID {toolId} not found");
                
                return JsonConvert.SerializeObject(new { success = true, tool });
            }
            catch (Exception ex)
            {
                return SerializeError($"Error retrieving tool: {ex.Message}");
            }
        }

        private async Task<string> HandleAddToolRequest(JObject requestObject)
        {
            try
            {
                var tool = requestObject.ToObject<Core.Models.Tool>();
                if (tool == null) return SerializeError("Invalid tool data");
                
                var result = await _toolService.AddToolAsync(tool);
                return JsonConvert.SerializeObject(new { success = true, tool = result });
            }
            catch (Exception ex)
            {
                return SerializeError($"Error adding tool: {ex.Message}");
            }
        }

        private async Task<string> HandleUpdateToolRequest(JObject requestObject)
        {
            try
            {
                var tool = requestObject.ToObject<Core.Models.Tool>();
                if (tool == null || string.IsNullOrEmpty(tool.Guid)) 
                    return SerializeError("Invalid tool data or missing tool ID");

                var matchedTool = await _toolService.GetToolByIdAsync(tool.Guid);
                if (matchedTool == null) return SerializeError($"Tool with ID {tool.Guid} not found.");

                if (matchedTool.IsBuiltIn)
                {
                    // Save only the extra properties via the BuiltinToolService.
                    string propertyKey = $"{matchedTool.Name.Substring(0, 1).ToLower()}{matchedTool.Name.Substring(1)}";
                    _builtinToolService.SaveBuiltInToolExtraProperties(propertyKey, tool.ExtraProperties);
                    return JsonConvert.SerializeObject(new { success = true, tool });
                }
                else
                {
                    var result = await _toolService.UpdateToolAsync(tool);
                    return JsonConvert.SerializeObject(new { success = true, tool = result });
                }
            }
            catch (Exception ex)
            {
                return SerializeError($"Error updating tool: {ex.Message}");
            }
        }

        private async Task<string> HandleDeleteToolRequest(JObject requestObject)
        {
            try
            {
                string toolId = requestObject["guid"]?.ToString();
                if (string.IsNullOrEmpty(toolId)) return SerializeError("Tool ID cannot be empty");
                
                var success = await _toolService.DeleteToolAsync(toolId);
                return JsonConvert.SerializeObject(new { success });
            }
            catch (Exception ex)
            {
                return SerializeError($"Error deleting tool: {ex.Message}");
            }
        }

        private async Task<string> HandleGetToolCategoriesRequest()
        {
            try
            {
                var categories = await _toolService.GetToolCategoriesAsync();
                return JsonConvert.SerializeObject(new { success = true, categories });
            }
            catch (Exception ex)
            {
                return SerializeError($"Error retrieving tool categories: {ex.Message}");
            }
        }

        private async Task<string> HandleAddToolCategoryRequest(JObject requestObject)
        {
            try
            {
                var category = requestObject.ToObject<Core.Models.ToolCategory>();
                if (category == null) return SerializeError("Invalid category data");
                
                var result = await _toolService.AddToolCategoryAsync(category);
                return JsonConvert.SerializeObject(new { success = true, category = result });
            }
            catch (Exception ex)
            {
                return SerializeError($"Error adding tool category: {ex.Message}");
            }
        }

        private async Task<string> HandleUpdateToolCategoryRequest(JObject requestObject)
        {
            try
            {
                var category = requestObject.ToObject<Core.Models.ToolCategory>();
                if (category == null || string.IsNullOrEmpty(category.Id)) 
                    return SerializeError("Invalid category data or missing category ID");
                
                var result = await _toolService.UpdateToolCategoryAsync(category);
                return JsonConvert.SerializeObject(new { success = true, category = result });
            }
            catch (Exception ex)
            {
                return SerializeError($"Error updating tool category: {ex.Message}");
            }
        }
        
        private async Task<string> HandleDeleteToolCategoryRequest(JObject requestObject)
        {
            try
            {
                string categoryId = requestObject["categoryId"]?.ToString();
                if (string.IsNullOrEmpty(categoryId)) return SerializeError("Category ID cannot be empty");
                
                var success = await _toolService.DeleteToolCategoryAsync(categoryId);
                return JsonConvert.SerializeObject(new { success });
            }
            catch (Exception ex)
            {
                return SerializeError($"Error deleting tool category: {ex.Message}");
            }
        }

        private async Task<string> HandleValidateToolSchemaRequest(JObject requestObject)
        {
            try
            {
                string schema = requestObject["schema"]?.ToString();
                if (string.IsNullOrEmpty(schema)) return SerializeError("Schema cannot be empty");
                
                var isValid = await _toolService.ValidateToolSchemaAsync(schema);
                return JsonConvert.SerializeObject(new { success = true, isValid });
            }
            catch (Exception ex)
            {
                return SerializeError($"Error validating tool schema: {ex.Message}");
            }
        }

        private async Task<string> HandleImportToolsRequest(JObject requestObject)
        {
            try
            {
                string json = requestObject["json"]?.ToString();
                if (string.IsNullOrEmpty(json)) return SerializeError("Import data cannot be empty");
                
                var tools = await _toolService.ImportToolsAsync(json);
                return JsonConvert.SerializeObject(new { success = true, tools });
            }
            catch (Exception ex)
            {
                return SerializeError($"Error importing tools: {ex.Message}");
            }
        }

        private async Task<string> HandleExportToolsRequest(JObject requestObject)
        {
            try
            {
                var toolIds = requestObject["toolIds"]?.ToObject<List<string>>();
                var json = await _toolService.ExportToolsAsync(toolIds);
                return JsonConvert.SerializeObject(new { success = true, json });
            }
            catch (Exception ex)
            {
                return SerializeError($"Error exporting tools: {ex.Message}");
            }
        }
    }
}
