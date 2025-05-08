using System;

namespace AiStudio4.Core.Exceptions
{
    public class McpCommunicationException : Exception
    {
        public McpCommunicationException(string message) : base(message)
        {
        }

        public McpCommunicationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
