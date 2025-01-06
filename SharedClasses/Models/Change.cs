using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedClasses.Models
{
    public class Change
    {
        [JsonProperty("change_type")]
        public string ChangeType { get; set; }
        public string Path { get; set; }
        public int LineNumber { get; set; }
        public string OldContent { get; set; }
        public string NewContent { get; set; }
    }

    public class Changeset
    {
        public List<Change> Changes { get; set; }
    }
}
