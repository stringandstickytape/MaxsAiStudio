namespace AiTool3.Conversations
{
    public class Conversation
    {
        public List<ConversationMessage> messages { get; set; }
        public string systemprompt { get; set; }
        public Conversation()
        {

        }
        public Conversation(string systemprompt, string userInput)
        {
            messages = new List<ConversationMessage>
            {
                new ConversationMessage
                {
                    role = "user",
                    content = userInput
                }
            };

            this.systemprompt = systemprompt;
        }

        public string SystemPromptWithDateTime()
        {
            return $"{systemprompt}\r\n\r\nThe current date and time is {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}";
        }

        public string uuid { get; set; }

        public string SaveToJsonFile()
        {
            //write the object to a json file
            var json = System.Text.Json.JsonSerializer.Serialize(this);
            var filename = "conversation-" + uuid + ".json";

            //get the runtime directory
            var dir = Directory.GetCurrentDirectory();

            //write the file
            File.WriteAllText(Path.Combine(dir, filename), json);



            return filename;
        }
    }
}