﻿
namespace AiTool3.Conversations
{
    public class CompletionMessage
    {
        public CompletionRole Role { get; set; }
        public string Content { get; set; }

        public string SystemPrompt { get; set; }

        public string Guid { get; set; }

        public List<string> Children { get; set; }
        public string Parent { get; set; }

        public string Engine { get; set; }

        public Color GetColorForEngine()
        {
            if (Engine == null) return Color.White;
            if (Engine.StartsWith("gpt"))
            {
                return Color.LavenderBlush;
            }
            else if (Engine.StartsWith("llama"))
            {
                return Color.MistyRose;
            }
            else if (Engine.StartsWith("ollama"))
            {
                return Color.PeachPuff;
            }
            else if (Engine.StartsWith("local"))
            {
                return Color.LightPink;
            }
            else if (Engine.StartsWith("gemini"))
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
                return $"{Engine} / {(TimeTaken == null ? "" : String.Format("{0:00}:{1:00}:{2:00}",
                    TimeTaken.Minutes, TimeTaken.Seconds, TimeTaken.Milliseconds / 10))} / {inputTokens}{outputTokens}";
            }
        }

        public TimeSpan TimeTaken { get; set; }
    }

    public enum CompletionRole
    {
        User,
        Assistant,
        Root
    }
}