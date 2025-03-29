using AiStudio4.Convs;
using AiStudio4.DataModels;
using System.Text;

namespace AiStudio4.Core.Models
{
    public class ToolProcessingResult
    {
        public bool ShouldContinue { get; set; }
        public string CollatedToolOutput { get; set; }

    }
}
