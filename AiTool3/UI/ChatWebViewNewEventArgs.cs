namespace AiTool3.UI
{
    public class ChatWebViewNewEventArgs
    {

        public ChatWebViewNewType Type { get; set; }

        public ChatWebViewNewEventArgs(ChatWebViewNewType type)
        {
            Type = type;
        }
    }
    public enum ChatWebViewNewType
    {
        New,
        NewWithPrompt,
        NewWithContext
    }
}