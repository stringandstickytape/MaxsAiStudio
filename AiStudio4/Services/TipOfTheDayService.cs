using System;
using System.Collections.Generic;
using System.IO;
using AiStudio4.Core.Interfaces;
using AiStudio4.Core.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AiStudio4.Services
{
    public class TipOfTheDayService : ITipOfTheDayService
    {
        private readonly string _settingsFilePath;
        private readonly object _lock = new();
        private TipOfTheDaySettings _settings = new();

        public TipOfTheDayService()
        {
            _settingsFilePath = PathHelper.GetProfileSubPath("settings.json");
            Directory.CreateDirectory(Path.GetDirectoryName(_settingsFilePath) ?? string.Empty);
            LoadSettings();
        }

        public void LoadSettings()
        {
            lock (_lock)
            {
                if (!File.Exists(_settingsFilePath))
                {
                    _settings = CreateDefaultSettings();
                    SaveSettings();
                    return;
                }

                try
                {
                    var json = JObject.Parse(File.ReadAllText(_settingsFilePath));
                    var section = json["tipOfTheDaySettings"];
                    if (section != null)
                    {
                        _settings = section.ToObject<TipOfTheDaySettings>() ?? CreateDefaultSettings();
                    }
                    else
                    {
                        _settings = CreateDefaultSettings();
                        SaveSettings();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading tip of the day settings: {ex.Message}");
                    _settings = CreateDefaultSettings();
                    SaveSettings();
                }
            }
        }

        public void SaveSettings()
        {
            lock (_lock)
            {
                try
                {
                    JObject json;
                    if (File.Exists(_settingsFilePath))
                    {
                        json = JObject.Parse(File.ReadAllText(_settingsFilePath));
                    }
                    else
                    {
                        json = new JObject();
                    }

                    json["tipOfTheDaySettings"] = JToken.FromObject(_settings);
                    File.WriteAllText(_settingsFilePath, json.ToString(Formatting.Indented));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error saving tip of the day settings: {ex.Message}");
                }
            }
        }

        public TipOfTheDaySettings GetSettings() => _settings;

        public void UpdateSettings(TipOfTheDaySettings settings)
        {
            _settings = settings ?? CreateDefaultSettings();
            SaveSettings();
        }

        private TipOfTheDaySettings CreateDefaultSettings()
        {
            return new TipOfTheDaySettings
            {
                ShowOnStartup = true,
                CurrentTipIndex = 0,
                Tips = CreateDefaultTips()
            };
        }

        private List<TipOfTheDay> CreateDefaultTips()
        {
            return new List<TipOfTheDay>
            {
                new TipOfTheDay
                {
                    Id = "tip-001",
                    Tip = "Use system prompts to give your AI assistant specific expertise and behavior patterns.",
                    SamplePrompt = "/system You are an expert Python developer with 10 years of experience. Help me write clean, efficient code following best practices.",
                    Category = "System Prompts",
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