namespace AiStudio4.Core.Exceptions
{
    public class WebSocketNotificationException : Exception
    {
        public WebSocketNotificationException(string message) : base(message)
        {
        }

        public WebSocketNotificationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}