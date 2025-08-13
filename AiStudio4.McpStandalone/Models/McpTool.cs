using CommunityToolkit.Mvvm.ComponentModel;

namespace AiStudio4.McpStandalone.Models
{
    public class McpTool : ObservableObject
    {
        private string name = string.Empty;
        private string description = string.Empty;
        private string category = string.Empty;
        private bool isSelected;
        private string serverId = string.Empty;
        private string toolId = string.Empty;

        public string Name 
        { 
            get => name;
            set => SetProperty(ref name, value);
        }

        public string Description 
        { 
            get => description;
            set => SetProperty(ref description, value);
        }

        public string Category 
        { 
            get => category;
            set => SetProperty(ref category, value);
        }

        public bool IsSelected 
        { 
            get => isSelected;
            set => SetProperty(ref isSelected, value);
        }

        public string ServerId 
        { 
            get => serverId;
            set => SetProperty(ref serverId, value);
        }

        public string ToolId 
        { 
            get => toolId;
            set => SetProperty(ref toolId, value);
        }
    }
}