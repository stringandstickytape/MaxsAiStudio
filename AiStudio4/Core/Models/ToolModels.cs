




namespace AiStudio4.Core.Models
{
    public class Tool
    {
        private string schema;

        public string Guid { get; set; } = System.Guid.NewGuid().ToString();
        public string Name { get; set; }
        public string Description { get; set; }

        // Extra dynamic properties (string key-value pairs)
        public Dictionary<string, string> ExtraProperties { get; set; } = new Dictionary<string, string>();

        public string SchemaName { get { return _schemaName; } }
        private string _schemaName;
        public string Schema { get => schema; 
            set {
                schema = value;
                _schemaName = JObject.Parse(value)?["name"].ToString();
            } 
        }
        public string SchemaType { get; set; } = "function";
        public List<string> Categories { get; set; } = new List<string>();
        public DateTime LastModified { get; set; } = DateTime.UtcNow;
        public bool IsBuiltIn { get; set; } = false;
        public string Filetype { get; set; } = string.Empty;
        public string OutputFileType { get; set; } = "unknown";

        public bool ValidateSchema()
        {
            try
            {
                var jobj = JObject.Parse(Schema);
                return jobj != null && jobj["name"] != null;
            }
            catch
            {
                return false;
            }
        }
    }

    public class ToolCategory
    {
        public string Id { get; set; } = System.Guid.NewGuid().ToString();
        public string Name { get; set; }
        public int Priority { get; set; } = 0;
    }

    public class ToolLibrary
    {
        public List<Tool> Tools { get; set; } = new List<Tool>();
        public List<ToolCategory> Categories { get; set; } = new List<ToolCategory>();
    }

    public class ToolSelectionRequest
    {
        public List<string> ToolIds { get; set; } = new List<string>();
    }
}
