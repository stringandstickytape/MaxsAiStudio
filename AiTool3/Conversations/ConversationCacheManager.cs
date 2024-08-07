using Newtonsoft.Json;

namespace AiTool3.Conversations
{
    public class ConversationCacheManager
    {
        public List<CachedConversation> Conversations { get; set; }

        public ConversationCacheManager()
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "Settings", "conversationCache.json");

            if (File.Exists(filePath))
                Conversations = JsonConvert.DeserializeObject<List<CachedConversation>>(File.ReadAllText(filePath));
            else
                Conversations = new List<CachedConversation>();
        }

        public void Save()
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "Settings", "conversationCache.json");

            File.WriteAllText(filePath, JsonConvert.SerializeObject(Conversations));
        }

        internal CachedConversation GetSummary(string file)
        {
            var conversation = Conversations.FirstOrDefault(x => x.FileName == file);
            var lastWriteTime = new FileInfo(file).LastWriteTime;

            if (conversation != null && lastWriteTime > conversation.LastModified)
            {
                Conversations.Remove(conversation);
                conversation = null;
            }

            if (conversation == null)
            {
                var conv = JsonConvert.DeserializeObject<BranchedConversation>(File.ReadAllText(file));

                var summary = conv.Messages.Any() ? conv.Messages[0].Content : "";

                if (summary.Length > 100)
                    summary = summary.Substring(0, 100) + "...";

                var newConv = new CachedConversation
                {
                    ConvGuid = conv.ConvGuid,
                    Content = conv.Messages[0].Content,
                    Engine = conv.Messages[0].Engine,
                    Summary = conv.Summary ?? summary,
                    FileName = file,
                    LastModified = lastWriteTime,
                    HighlightColour = conv.HighlightColour
                };

                Conversations.Add(newConv);

                Save();

                return newConv;
            }
            else
            {
                return conversation;
            }


        }
    }

    public class CachedConversation
    {
        public string ConvGuid { get; set; }
        public string Content { get; set; }
        public string Engine { get; set; }
        public string Summary { get; set; }
        public string FileName { get; set; }
        public DateTime LastModified { get; set; }
        public Color? HighlightColour { get; internal set; }
    }
}
