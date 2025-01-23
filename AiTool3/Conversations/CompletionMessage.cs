
using Newtonsoft.Json;

namespace AiTool3.Conversations
{
    public class CompletionMessage
    {

        public CompletionMessage(CompletionRole role)
        {
            Role = role;
            Guid = System.Guid.NewGuid().ToString();
            Children = new List<string>();
        }

        public CompletionRole Role { get; set; }
        public string? Content { get; set; }

        public string? Base64Image { get; set; }

        public string? SystemPrompt { get; set; }

        public string? Guid { get; set; }

        public List<string> Children { get; set; }
        public string? Parent { get; set; }

        [JsonIgnore]
        public bool Omit { get; set; }

        public string ModelGuid { get; set; }

        public DateTime? CreatedAt { get; set; }

        public string GetColorHexForEngine()
        {
            //var c = GetColorForEngine(Engine);
            //return $"#{c.R:X2}{c.G:X2}{c.B:X2}";

            return "#EEEEEE";
        }

        public static Color GetColorForEngine(string? engine)
        {
            if (engine == null) return Color.White;
            if (engine.StartsWith("gpt"))
            {
                return Color.LavenderBlush;
            }
            else if (engine.StartsWith("gemma"))
            {
                return Color.MistyRose;
            }
            else if (engine.StartsWith("ollama"))
            {
                return Color.PeachPuff;
            }
            else if (engine.StartsWith("local"))
            {
                return Color.LightPink;
            }
            else if (engine.StartsWith("gemini"))
            {
                return Color.LemonChiffon;
            }
            else
            {
                return Color.Thistle;
            }
        }

        public int InputTokens { get; set; }
        public int OutputTokens { get; set; }

        public TimeSpan TimeTaken { get; set; }
        public string? Base64Type { get; set; }
    }

    public enum CompletionRole
    {
        User,
        Assistant,
        Root
    }
}