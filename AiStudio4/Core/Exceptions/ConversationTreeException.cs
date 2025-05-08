namespace AiStudio4.Core.Exceptions
{
    public class ConvTreeException : Exception
    {
        public ConvTreeException(string message) : base(message)
        {
        }

        public ConvTreeException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}