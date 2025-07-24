using AiStudio4.Core.Interfaces;
using System.Text.Json;
using System.Text;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace AiStudio4.Services.ProtectedMcpServer
{
    /// <summary>
    /// Generates properly typed MCP method signatures based on ITool.GetToolDefinition() schemas
    /// </summary>
    public static class SchemaBasedMcpMethodGenerator
    {
        public static void GenerateUpdatedMethods(IServiceProvider serviceProvider, ILogger logger)
        {
            var toolTypes = typeof(ITool).Assembly.GetTypes()
                .Where(type => type.IsClass && 
                              !type.IsAbstract &&
                              typeof(ITool).IsAssignableFrom(type) &&
                              type.GetCustomAttribute<ModelContextProtocol.Server.McpServerToolTypeAttribute>() != null)
                .ToList();

            var output = new StringBuilder();
            output.AppendLine("// Generated MCP methods with proper schemas");
            output.AppendLine("// Replace the existing MCP bridge methods with these:");
            output.AppendLine();

            foreach (var toolType in toolTypes)
            {
                try
                {
                    var toolInstance = serviceProvider.GetService(toolType) as ITool;
                    if (toolInstance != null)
                    {
                        var toolDefinition = toolInstance.GetToolDefinition();
                        if (toolDefinition != null && !string.IsNullOrEmpty(toolDefinition.Schema))
                        {
                            var generatedMethod = GenerateMethodFromSchema(toolDefinition.Name, toolDefinition.Description, toolDefinition.Schema);
                            output.AppendLine(generatedMethod);
                            output.AppendLine();
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to generate method for {ToolType}", toolType.Name);
                }
            }

            // Write to a temporary file for review
            var outputPath = Path.Combine(Path.GetTempPath(), "GeneratedMcpMethods.cs");
            File.WriteAllText(outputPath, output.ToString());
            logger.LogInformation("Generated MCP methods written to: {OutputPath}", outputPath);
        }

        private static string GenerateMethodFromSchema(string toolName, string description, string schema)
        {
            try
            {
                var schemaJson = JsonDocument.Parse(schema);
                var inputSchema = schemaJson.RootElement.GetProperty("input_schema");
                
                var method = new StringBuilder();
                method.AppendLine($"        [McpServerTool, Description(\"{description}\")]");
                method.Append($"        public async Task<string> {toolName}(");
                
                // Parse properties to create typed parameters
                var parameters = new List<string>();
                
                if (inputSchema.TryGetProperty("properties", out var properties))
                {
                    foreach (var property in properties.EnumerateObject())
                    {
                        var paramName = property.Name;
                        var paramInfo = property.Value;
                        
                        var paramType = GetCSharpType(paramInfo);
                        var paramDescription = paramInfo.TryGetProperty("description", out var desc) ? desc.GetString() : paramName;
                        
                        // Check if parameter is required
                        var isRequired = IsRequiredParameter(inputSchema, paramName);
                        var defaultValue = isRequired ? "" : GetDefaultValue(paramType);
                        
                        var parameter = $"[Description(\"{paramDescription}\")] {paramType} {paramName}{defaultValue}";
                        parameters.Add(parameter);
                    }
                }
                
                // If no typed parameters, fall back to JSON string
                if (parameters.Count == 0)
                {
                    parameters.Add($"[Description(\"JSON parameters for {toolName}\")] string parameters = \"{{}}\"");
                }
                
                method.Append(string.Join(", ", parameters));
                method.AppendLine(")");
                method.AppendLine("        {");
                
                if (parameters.Count > 1 || !parameters[0].Contains("string parameters"))
                {
                    // Generate code to convert typed parameters back to JSON
                    method.AppendLine("            var parametersJson = JsonSerializer.Serialize(new {");
                    foreach (var param in parameters)
                    {
                        var paramName = ExtractParameterName(param);
                        method.AppendLine($"                {paramName},");
                    }
                    method.AppendLine("            });");
                    method.AppendLine($"            return await ExecuteTool(\"{toolName}\", parametersJson);");
                }
                else
                {
                    method.AppendLine($"            return await ExecuteTool(\"{toolName}\", parameters);");
                }
                
                method.AppendLine("        }");
                
                return method.ToString();
            }
            catch (Exception)
            {
                // Fall back to simple method if schema parsing fails
                return $@"        [McpServerTool, Description(""{description}"")]
        public async Task<string> {toolName}([Description(""JSON parameters for {toolName}"")] string parameters = ""{{}}"")
        {{
            return await ExecuteTool(""{toolName}"", parameters);
        }}";
            }
        }

        private static string GetCSharpType(JsonElement paramInfo)
        {
            if (paramInfo.TryGetProperty("type", out var typeElement))
            {
                var type = typeElement.GetString();
                return type switch
                {
                    "string" => "string",
                    "integer" => "int",
                    "number" => "double",
                    "boolean" => "bool",
                    "array" => "string[]", // Simplified - would need more complex logic for proper array types
                    "object" => "object", // Simplified - would need to generate classes for complex objects
                    _ => "string"
                };
            }
            return "string";
        }

        private static bool IsRequiredParameter(JsonElement inputSchema, string paramName)
        {
            if (inputSchema.TryGetProperty("required", out var required))
            {
                foreach (var req in required.EnumerateArray())
                {
                    if (req.GetString() == paramName)
                        return true;
                }
            }
            return false;
        }

        private static string GetDefaultValue(string type)
        {
            return type switch
            {
                "string" => " = \"\"",
                "int" => " = 0",
                "double" => " = 0.0",
                "bool" => " = false",
                "string[]" => " = new string[0]",
                _ => " = null"
            };
        }

        private static string ExtractParameterName(string parameter)
        {
            // Extract parameter name from the full parameter declaration
            var parts = parameter.Split(' ');
            var nameIndex = Array.FindIndex(parts, p => !p.StartsWith("[") && !p.Contains("Description") && !p.Contains("string") && !p.Contains("int") && !p.Contains("double") && !p.Contains("bool"));
            if (nameIndex > 0 && nameIndex < parts.Length)
            {
                return parts[nameIndex].Split('=')[0].Trim();
            }
            return "param";
        }
    }
}