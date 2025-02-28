namespace AiStudio4.Core.Exceptions
{
    public class ChatProcessingException : Exception
    {
        public ChatProcessingException(string message) : base(message)
        {
        }

        public ChatProcessingException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}