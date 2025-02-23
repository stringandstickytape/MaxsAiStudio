namespace AiStudio4.Core.Exceptions
{
    public class ConversationStorageException : Exception
    {
        public ConversationStorageException(string message) : base(message)
        {
        }

        public ConversationStorageException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}