using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AiStudio4.Core.Models
{
    public class Tool
    {
        public string Guid { get; set; } = System.Guid.NewGuid().ToString();
        public string Name { get; set; }
        public string Description { get; set; }
        public string Schema { get; set; }
        public string SchemaType { get; set; } = "function";
        public List<string> Categories { get; set; } = new List<string>();
        public DateTime LastModified { get; set; } = DateTime.UtcNow;
        public bool IsBuiltIn { get; set; } = false;

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

    public class ToolResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public Tool Tool { get; set; }
        public List<Tool> Tools { get; set; }
    }
}