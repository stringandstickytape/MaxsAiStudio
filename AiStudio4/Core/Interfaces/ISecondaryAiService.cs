using System.Threading.Tasks;

namespace AiStudio4.Core.Interfaces
{
    /// <summary>
    /// Service to handle direct communication with a secondary AI model
    /// without going through the full chat processing pipeline
    /// </summary>
    public interface ISecondaryAiService
    {
        /// <summary>
        /// Process a request using the secondary AI model
        /// </summary>
        /// <param name="prompt">The prompt to send to the secondary AI</param>
        /// <returns>Response from the secondary AI</returns>
        Task<SecondaryAiResponse> ProcessRequestAsync(string prompt);
    }

    /// <summary>
    /// Response model for secondary AI requests
    /// </summary>
    public class SecondaryAiResponse
    {
        /// <summary>
        /// Indicates if the request was successfully processed
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// The response text from the secondary AI
        /// </summary>
        public string Response { get; set; } = string.Empty;

        /// <summary>
        /// Error message if the request failed
        /// </summary>
        public string Error { get; set; } = string.Empty;
    }
}
