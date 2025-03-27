using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AiStudio4.Core.Models
{
    public class McpServerDefinition
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Command { get; set; } = string.Empty;
        public string Arguments { get; set; } = string.Empty;
        public bool IsEnabled { get; set; } = true;
        public string Description { get; set; } = string.Empty;
        public DateTime LastModified { get; set; } = DateTime.UtcNow;
        public Dictionary<string, string> Env { get; set; }
        public bool StdIo { get; set; } = true;
    }
}
