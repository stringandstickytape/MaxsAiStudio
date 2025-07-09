using System;
using System.Collections.Generic;

namespace AiStudio4.Core.Models
{
    public class TipOfTheDaySettings
    {
        public bool ShowOnStartup { get; set; } = true;
        public int CurrentTipIndex { get; set; } = 0;
        public List<TipOfTheDay> Tips { get; set; } = new List<TipOfTheDay>();
    }

    public class TipOfTheDay
    {
        public string Id { get; set; } = string.Empty;
        public string Tip { get; set; } = string.Empty;
        public string SamplePrompt { get; set; } = string.Empty;

        public string ManualReference { get; set; } = string.Empty;
    }
}