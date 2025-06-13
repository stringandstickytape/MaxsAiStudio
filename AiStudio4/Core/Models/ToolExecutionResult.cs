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


        public string AggregatedToolOutput { get; set; }

        
        
        
        public List<Attachment> Attachments { get; set; }

        
        
        
        public bool Success { get; set; }

        
        
        
        public string ErrorMessage { get; set; }

        
        
        
        public bool ShouldContinueToolLoop { get; set; }

        
        
        
        public string RequestedToolsSummary { get; set; }


        
        
        
        public static ToolExecutionResult Successful(string aggregatedToolOutput, List<Attachment> attachments = null)
        {
            return new ToolExecutionResult
            {
                AggregatedToolOutput = aggregatedToolOutput,
                Attachments = attachments,
                Success = true
            };
        }

        
        
        
        public static ToolExecutionResult Failed(string errorMessage)
        {
            return new ToolExecutionResult
            {
                ErrorMessage = errorMessage,
                Success = false
            };
        }
    }
}
