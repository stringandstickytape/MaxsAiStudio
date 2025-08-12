using AiStudio4.Core.Interfaces;
using AiStudio4.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AiStudio4.Services.Adapters
{
    /// <summary>
    /// Adapter that wraps a shared library tool to work with the main app's ITool interface
    /// </summary>
    public class SharedToolAdapter : ITool
    {
        private readonly AiStudio4.Tools.Interfaces.ITool _sharedTool;

        public SharedToolAdapter(AiStudio4.Tools.Interfaces.ITool sharedTool)
        {
            _sharedTool = sharedTool;
        }

        public Tool GetToolDefinition()
        {
            var sharedDefinition = _sharedTool.GetToolDefinition();
            
            // Convert from shared library Tool to main app Tool
            return new Tool
            {
                Guid = sharedDefinition.Guid,
                Name = sharedDefinition.Name,
                Description = sharedDefinition.Description,
                Schema = sharedDefinition.Schema,
                SchemaType = sharedDefinition.SchemaType,
                Categories = sharedDefinition.Categories,
                LastModified = sharedDefinition.LastModified,
                IsBuiltIn = sharedDefinition.IsBuiltIn,
                Filetype = sharedDefinition.Filetype,
                OutputFileType = sharedDefinition.OutputFileType,
                ExtraProperties = sharedDefinition.ExtraProperties
            };
        }

        public async Task<BuiltinToolResult> ProcessAsync(string toolParameters, Dictionary<string, string> extraProperties)
        {
            var sharedResult = await _sharedTool.ProcessAsync(toolParameters, extraProperties);
            
            // Convert from shared library BuiltinToolResult to main app BuiltinToolResult
            var result = new BuiltinToolResult
            {
                WasProcessed = sharedResult.WasProcessed,
                ContinueProcessing = sharedResult.ContinueProcessing,
                ResultMessage = sharedResult.ResultMessage,
                StatusMessage = sharedResult.StatusMessage,
                UserInterjection = sharedResult.UserInterjection,
                TaskDescription = sharedResult.TaskDescription,
                OutputFileType = sharedResult.OutputFileType,
                Attachments = new List<DataModels.Attachment>()
            };

            // Convert attachments if any
            if (sharedResult.Attachments != null)
            {
                foreach (var attachment in sharedResult.Attachments)
                {
                    result.Attachments.Add(new DataModels.Attachment
                    {
                        Id = attachment.Id,
                        Name = attachment.Name,
                        Type = attachment.Type,
                        Content = attachment.Content,
                        Size = attachment.Size,
                        Width = attachment.Width,
                        Height = attachment.Height,
                        TextContent = attachment.TextContent,
                        LastModified = attachment.LastModified
                    });
                }
            }

            return result;
        }

        public void UpdateProjectRoot()
        {
            _sharedTool.UpdateProjectRoot();
        }
    }
}