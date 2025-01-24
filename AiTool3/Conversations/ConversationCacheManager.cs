using Newtonsoft.Json;

namespace AiTool3.Conversations
{
    public class ConversationCacheManager
    {
        public Dictionary<string, CachedConversation> Conversations { get; set; }

        public ConversationCacheManager()
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "Settings", "conversationCache.json");

            if (File.Exists(filePath))
            {
                var list = JsonConvert.DeserializeObject<List<CachedConversation>>(File.ReadAllText(filePath));
                Conversations = list?.ToDictionary(x => x.FileName) ?? new Dictionary<string, CachedConversation>();
            }
            else
            {
                Conversations = new Dictionary<string, CachedConversation>();
            }
        }

        public void Save()
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "Settings", "conversationCache.json");
            var list = Conversations.Values.ToList();
            File.WriteAllText(filePath, JsonConvert.SerializeObject(list));
        }

        internal CachedConversation GetSummary(string file)
        {
            var lastWriteTime = new FileInfo(file).LastWriteTime;

            if (Conversations.TryGetValue(file, out var conversation))
            {
                if (lastWriteTime > conversation.LastModified)
                {
                    Conversations.Remove(file);
                    conversation = null;
                }
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
                    Summary = conv.Summary ?? summary,
                    FileName = file,
                    LastModified = lastWriteTime,
                    HighlightColour = conv.HighlightColour
                };

                Conversations[file] = newConv;

                Save();

                return newConv;
            }

            return conversation;
        }
    }

    public class CachedConversation
    {
        public string ConvGuid { get; set; }
        public string Summary { get; set; }
        public string FileName { get; set; }
        public DateTime LastModified { get; set; }
        public Color? HighlightColour { get; internal set; }
    }
}
