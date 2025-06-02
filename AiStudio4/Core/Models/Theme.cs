
using System.Collections.Generic;

namespace AiStudio4.Core.Models
{
    
    
    
    public class Theme
    {
        
        
        
        public string Guid { get; set; }

        
        
        
        public string Name { get; set; }

        
        
        
        public string Description { get; set; }

        
        
        
        public string Author { get; set; }

        
        
        
        public List<string> PreviewColors { get; set; }

        
        
        
        public object ThemeJson { get; set; }

        
        
        
        public string FontCdnUrl { get; set; }

        
        
        
        public string Created { get; set; }

        
        
        
        public string LastModified { get; set; }
    }
}
