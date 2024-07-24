namespace AiTool3.UI
{
    
    public class ChatWebViewSimpleEventArgs
    {
        public string EventType { get; set; }
        public ChatWebViewSimpleEventArgs(string type)
        {
            EventType = type;
        }
    }
}