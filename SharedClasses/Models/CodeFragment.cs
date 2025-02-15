using System;
using System.Collections.Generic;
using System.Text;

namespace SharedClasses.Models
{
    public class CodeFragment
    {
        public string Content { get; set; }
        public string Type { get; set; }
        public string FilePath { get; set; }
        public int LineNumber { get; set; }

        public string Class { get; set; }
        public string Namespace { get; set; }
    }
}
