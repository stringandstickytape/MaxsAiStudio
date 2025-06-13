using AiStudio4.DataModels;



namespace AiStudio4.Core.Models
{
    
    
    
    public class ToolExecutionResult
    {
        
        
        
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
