using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VSIXTest.Embeddings
{
    public class VsixEmbedding
    {
        public string Code { get; set; }
        public List<float> Value { get; set; }
        public string Filename { get; set; }
        public int LineNumber { get; set; }
        public string Namespace { get; set; }
        public string Class { get; set; }
    }
}
