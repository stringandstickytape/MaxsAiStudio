using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
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
            _tips = LoadTipsFromJson();
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


        private List<TipOfTheDay> LoadTipsFromJson()
        {
            try
            {
                var assemblyLocation = Assembly.GetExecutingAssembly().Location;
                var assemblyDirectory = Path.GetDirectoryName(assemblyLocation);
                var tipsFilePath = Path.Combine(assemblyDirectory, "Data", "tips.json");
                
                if (File.Exists(tipsFilePath))
                {
                    var jsonContent = File.ReadAllText(tipsFilePath);
                    var tips = JsonSerializer.Deserialize<List<TipOfTheDay>>(jsonContent);
                    return tips ?? CreateFallbackTips();
                }
                
                return CreateFallbackTips();
            }
            catch
            {
                return CreateFallbackTips();
            }
        }

        private List<TipOfTheDay> CreateFallbackTips()
        {
            return new List<TipOfTheDay>
            {
                new TipOfTheDay
                {
                    Id = "tip-fallback",
                    Tip = "Welcome to AI Studio! Start exploring the features.",
                    SamplePrompt = "/help",
                    Category = "Welcome",
                    CreatedAt = DateTime.UtcNow.ToString("O")
                }
            };
        }
    }
}