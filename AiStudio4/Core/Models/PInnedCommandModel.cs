using System.Collections.Generic;

namespace AiStudio4.Core.Models
{
    
    
    
    public class PinnedCommand
    {
        
        
        
        public string Id { get; set; }

        
        
        
        public string Name { get; set; }

        
        
        
        public string IconName { get; set; }

        
        
        
        public string IconSet { get; set; }

        
        
        
        public string Section { get; set; }
    }

    
    
    
    public class GetPinnedCommandsRequest
    {
        public string ClientId { get; set; }
    }

    
    
    
    public class GetPinnedCommandsResponse
    {
        public bool Success { get; set; }
        public List<PinnedCommand> PinnedCommands { get; set; }
        public string Error { get; set; }
    }

    
    
    
    public class SavePinnedCommandsRequest
    {
        public string ClientId { get; set; }
        public List<PinnedCommand> PinnedCommands { get; set; }
    }

    
    
    
    public class SavePinnedCommandsResponse
    {
        public bool Success { get; set; }
        public string Error { get; set; }
    }
}
