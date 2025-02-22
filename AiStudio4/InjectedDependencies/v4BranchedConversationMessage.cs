namespace AiStudio4.InjectedDependencies
{
    public class v4BranchedConversationMessage
    {
        public v4BranchedConversationMessageRole Role { get; set; }
        public List<v4BranchedConversationMessage> Children { get; set; }

        public string UserMessage { get; set; }

        public string Id { get; set; }
    }



}