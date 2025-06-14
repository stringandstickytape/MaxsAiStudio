using AiStudio4.DataModels;



namespace AiStudio4.Core.Models
{

    public class ToolExecutionResultContentBlocks
    {
        public List<ContentBlock> RequestBlocks { get; set; } = new List<ContentBlock>();
        public List<ContentBlock> ResponseBlocks { get; set; } = new List<ContentBlock>();
    }
    
    
    
    public class ToolExecutionResult
    {

        public List<ToolExecutionResultContentBlocks> ToolsOutputContentBlocks { get; set; } = new List<ToolExecutionResultContentBlocks>();



        
        
        
        public List<Attachment> Attachments { get; set; }

        
        
        
        public bool Success { get; set; }

        
        
        
        public string ErrorMessage { get; set; }

        
        
        
        public bool ShouldContinueToolLoop { get; set; }

        
        
    }
}
