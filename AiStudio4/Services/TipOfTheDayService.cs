using System;
using System.Collections.Generic;
using AiStudio4.Core.Interfaces;
using AiStudio4.Core.Models;
using AiStudio4.InjectedDependencies;

namespace AiStudio4.Services
{
    public class TipOfTheDayService : ITipOfTheDayService
    {
        private readonly IGeneralSettingsService _generalSettingsService;
        private readonly List<TipOfTheDay> _tips;

        public TipOfTheDayService(IGeneralSettingsService generalSettingsService)
        {
            _generalSettingsService = generalSettingsService ?? throw new ArgumentNullException(nameof(generalSettingsService));
            _tips = CreateDefaultTips();
        }

        public TipOfTheDay GetTipOfTheDay()
        {
            if (_tips.Count == 0)
            {
                return new TipOfTheDay
                {
                    Id = "tip-000",
                    Tip = "Welcome to AI Studio! Start exploring the features.",
                    SamplePrompt = "/help",
                    Category = "Welcome",
                    CreatedAt = DateTime.UtcNow.ToString("O")
                };
            }

            var currentTipIndex = _generalSettingsService.CurrentSettings.NextTipNumber % _tips.Count;
            var currentTip = _tips[currentTipIndex];
            
            // Increment the tip number for next time
            var settings = _generalSettingsService.CurrentSettings;
            settings.NextTipNumber = (settings.NextTipNumber + 1) % _tips.Count;
            _generalSettingsService.UpdateSettings(settings);
            
            return currentTip;
        }


        private List<TipOfTheDay> CreateDefaultTips()
        {
            return new List<TipOfTheDay>
            {
                new TipOfTheDay
                {
                    Id = "tip-001",
                    Tip = "You can branch at any point in the conversation history.  Just click any message in the sidebar hierarchy.",
                    SamplePrompt = "",
                    Category = "Branching",
                    CreatedAt = DateTime.UtcNow.ToString("O")
                },
                new TipOfTheDay
                {
                    Id = "tip-002",
                    Tip = "Try using the conversation tree view to explore different conversation branches and see how your chat evolved.",
                    SamplePrompt = "/toggle-conv-tree",
                    Category = "Navigation",
                    CreatedAt = DateTime.UtcNow.ToString("O")
                },
                new TipOfTheDay
                {
                    Id = "tip-003",
                    Tip = "Use file attachments to provide context to your AI assistant about your codebase or documents.",
                    SamplePrompt = "Analyze this code file and suggest improvements for performance and readability.",
                    Category = "File Management",
                    CreatedAt = DateTime.UtcNow.ToString("O")
                },
                new TipOfTheDay
                {
                    Id = "tip-004",
                    Tip = "Create user prompts to save and reuse your favorite prompt templates.",
                    SamplePrompt = "/user-prompt Code Review Template",
                    Category = "User Prompts",
                    CreatedAt = DateTime.UtcNow.ToString("O")
                },
                new TipOfTheDay
                {
                    Id = "tip-005",
                    Tip = "Use the command palette (/) to quickly access features like creating new conversations, changing models, or opening settings.",
                    SamplePrompt = "/new-conv",
                    Category = "Commands",
                    CreatedAt = DateTime.UtcNow.ToString("O")
                },
                new TipOfTheDay
                {
                    Id = "tip-006",
                    Tip = "Enable tools to give your AI assistant powerful capabilities like web search, file operations, and code analysis.",
                    SamplePrompt = "Search the web for the latest React 18 best practices and summarize the key points.",
                    Category = "Tools",
                    CreatedAt = DateTime.UtcNow.ToString("O")
                },
                new TipOfTheDay
                {
                    Id = "tip-007",
                    Tip = "Customize your theme and appearance to create a comfortable working environment that suits your preferences.",
                    SamplePrompt = "/appearance",
                    Category = "Customization",
                    CreatedAt = DateTime.UtcNow.ToString("O")
                }
            };
        }
    }
}