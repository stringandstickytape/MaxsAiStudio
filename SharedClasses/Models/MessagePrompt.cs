namespace SharedClasses.Models
{
    public class MessagePrompt
    {
        public string MessageType { get; set; }
        public string Prompt { get; set; }
        public string ButtonLabel { get; set; }
        public string Category { get; set; }

        public string Tool { get; set; }
    }

}


