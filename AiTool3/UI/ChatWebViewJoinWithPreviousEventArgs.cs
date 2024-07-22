namespace AiTool3.UI
{
    public class ChatWebViewJoinWithPreviousEventArgs
    {
        public ChatWebViewJoinWithPreviousEventArgs(string guidValue)
        {
            GuidValue = guidValue;
        }

        public string GuidValue { get; set; }
    }
}