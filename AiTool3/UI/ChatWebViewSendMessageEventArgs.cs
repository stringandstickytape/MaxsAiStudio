namespace AiTool3.UI
{
    public class ChatWebViewSendMessageEventArgs
    {
        public string Content { get; set; }

        public List<string> SelectedTools {get; set;}

        public bool SendViaSecondaryAI { get; set; }
        public bool AddEmbeddings { get; internal set; }
    }
}