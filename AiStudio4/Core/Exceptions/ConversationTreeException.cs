namespace AiStudio4.Core.Exceptions
{
    public class ConversationTreeException : Exception
    {
        public ConversationTreeException(string message) : base(message)
        {
        }

        public ConversationTreeException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}