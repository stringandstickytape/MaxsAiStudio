
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

        // don't json this
        [JsonIgnore]
        public bool Omit { get; set; }
        public string? Engine { get; set; }

        public DateTime? CreatedAt { get; set; }

        public string GetColorHexForEngine()
        {
            var c = GetColorForEngine(Engine);
            return $"#{c.R:X2}{c.G:X2}{c.B:X2}";
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

        public string InfoLabel
        {
            get
            {
                var inputTokens = InputTokens == 0 ? "" : $"{InputTokens} in";
                var outputTokens = OutputTokens == 0 ? "" : $"{OutputTokens} out";
                return $"{(CreatedAt != null ? "Created at" : "")} {CreatedAt?.ToShortTimeString()} {CreatedAt?.ToShortDateString()} {(CreatedAt != null && !string.IsNullOrWhiteSpace(Engine)? "by" : "")} {Engine}{Environment.NewLine}{(String.Format("{0:00}:{1:00}:{2:00}",TimeTaken.Minutes, TimeTaken.Seconds, TimeTaken.Milliseconds / 10))}  {inputTokens}{outputTokens}";
            }
        }

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