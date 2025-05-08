using System;

namespace AiStudio4.Core.Exceptions
{
    public class UserPromptException : Exception
    {
        public UserPromptException(string message) : base(message)
        {
        }

        public UserPromptException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}