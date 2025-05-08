namespace AiStudio4.Core.Exceptions
{
    public class ConvStorageException : Exception
    {
        public ConvStorageException(string message) : base(message)
        {
        }

        public ConvStorageException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}