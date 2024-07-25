namespace AiTool3.UI
{
    
    public class ChatWebViewSimpleEventArgs
    {
        public string EventType { get; set; }

        public string Guid { get; set; }
        public ChatWebViewSimpleEventArgs(string type)
        {
            EventType = type;
        }

        public ChatWebViewSimpleEventArgs(string type, string guid)
        {
            EventType = type;
            Guid = guid;
        }
    }
}