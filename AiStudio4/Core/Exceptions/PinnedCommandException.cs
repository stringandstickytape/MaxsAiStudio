using System;

namespace AiStudio4.Core.Exceptions
{
    public class PinnedCommandException : Exception
    {
        public PinnedCommandException(string message) : base(message)
        {
        }

        public PinnedCommandException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}