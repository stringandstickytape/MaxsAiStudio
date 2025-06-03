
using AiStudio4.Core.Interfaces;
using AiStudio4.Core.Models;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Converters;

namespace AiStudio4.Services
{
    
    
    
    public class ThemeService : IThemeService
    {
        private readonly string _settingsFilePath;
        private readonly object _lock = new object();
        private List<Theme> _userThemes = new List<Theme>();
        private string _activeThemeId = "";

        public ThemeService(IConfiguration configuration)
        {
            _settingsFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AiStudio4", "themes.json");
            Directory.CreateDirectory(Path.GetDirectoryName(_settingsFilePath));
            LoadSettings();
        }

        
        
        
        private void LoadSettings()
        {
            lock (_lock)
            {
                if (!File.Exists(_settingsFilePath))
                {
                    CreateDefaultTheme2();

                    SaveSettings();
                    return;
                }

                var json = JObject.Parse(File.ReadAllText(_settingsFilePath));
                
                var themesSection = json["themes"];
                if (themesSection != null)
                {
                    
                    var settings = new JsonSerializerSettings
                    {
                        TypeNameHandling = TypeNameHandling.None,
                        NullValueHandling = NullValueHandling.Ignore,
                        Converters = { new StringEnumConverter() }
                    };
                    
                    _userThemes = JsonConvert.DeserializeObject<List<Theme>>(themesSection.ToString(), settings) 
                        ?? new List<Theme>();
                }
                else
                {
                    _userThemes = new List<Theme>();
                }

                var activeThemeSection = json["activeTheme"];
                if (activeThemeSection != null)
                {
                    _activeThemeId = activeThemeSection.ToString();
                }
                else
                {
                    _activeThemeId = "";
                }
                
                
                if (_userThemes.Count == 0)
                {
                    CreateDefaultTheme2();
                    SaveSettings();
                }
                else if (string.IsNullOrEmpty(_activeThemeId) || !_userThemes.Any(t => t.Guid == _activeThemeId))
                {
                    
                    _activeThemeId = _userThemes.First().Guid;
                    SaveSettings();
                }
            }
        }

        private void CreateDefaultTheme2()
        {
            var json2 = JObject.Parse(@"{
  ""themes"": [
    {
      ""guid"": ""42bcc320-b10f-4412-ae72-86bbdb550024"",
      ""name"": ""New Default Theme"",
      ""description"": ""Default application theme"",
      ""author"": ""System"",
      ""previewColors"": [
        ""#1a1a1a"",
        ""#f9e1e9"",
        ""#ff8fb1""
      ],
      ""themeJson"": {
        ""theme-name"": ""Ultrachic Noir Elegance with Gabarito"",
        ""global-backgroundColor"": ""#0b0b0b"",
        ""global-textColor"": ""#dcdcdc"",
        ""global-secondaryTextColor"": ""#8a8a8a"",
        ""global-primaryColor"": ""#e63946"",
        ""global-secondaryColor"": ""#f1faee"",
        ""global-borderColor"": ""#2c2c2c"",
        ""global-borderRadius"": ""10px"",
        ""global-fontFamily"": ""'Gabarito', sans-serif"",
        ""global-fontSize"": ""17px"",
        ""global-fontCdnUrl"": ""https://fonts.googleapis.com/css2?family=Gabarito&display=swap"",
        ""global-boxShadow"": ""none"",
        ""global-userMessageBackground"": ""#1f1f1f"",
        ""global-userMessageTextColor"": ""#f1faee"",
        ""global-userMessageBorderColor"": ""#e63946"",
        ""global-userMessageBorderWidth"": ""2.5px"",
        ""global-userMessageBorderStyle"": ""solid"",
        ""global-aiMessageBackground"": ""#141414"",
        ""global-aiMessageTextColor"": ""#dcdcdc"",
        ""global-aiMessageBorderColor"": ""#f1faee"",
        ""global-aiMessageBorderWidth"": ""2.5px"",
        ""global-aiMessageBorderStyle"": ""solid"",
        ""ConvTreeView---convtree-user-node-color"": ""#e63946"",
        ""ConvTreeView---convtree-ai-node-color"": ""#f1faee"",
        ""ConvTreeView---convtree-user-node-border"": ""#e63946"",
        ""ConvTreeView---convtree-ai-node-border"": ""#f1faee"",
        ""ConvTreeView---convtree-link-color"": ""#e63946"",
        ""ConvTreeView---convtree-accent-color"": ""#f1faee"",
        ""MarkdownPane-codeHeaderBackground"": ""#2c2c2c"",
        ""MarkdownPane-codeHeaderText"": ""#e63946"",
        ""MarkdownPane-codeHeaderBorder"": ""#e63946"",
        ""MarkdownPane-codeHeaderAccent"": ""#f1faee"",
        ""MarkdownPane-style"": ""border-radius: 14px; font-family: 'Gabarito', sans-serif; font-weight: 700; font-style: italic;""
      },
      ""fontCdnUrl"": """",
      ""created"": ""05/31/2025 18:19:23"",
      ""lastModified"": ""2025-05-31T18:19:42.9123472Z""
    }
  ]
}");

            var themesSection2 = json2["themes"];

            var settings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.None,
                NullValueHandling = NullValueHandling.Ignore,
                Converters = { new StringEnumConverter() }
            };

            _userThemes = JsonConvert.DeserializeObject<List<Theme>>(themesSection2.ToString(), settings)
                ?? new List<Theme>();

            _activeThemeId = "42bcc320-b10f-4412-ae72-86bbdb550024";
        }

        
        
        
        private void SaveSettings()
        {
            lock (_lock)
            {
                
                var settings = new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.None,
                    NullValueHandling = NullValueHandling.Ignore,
                    Formatting = Formatting.Indented,
                    Converters = { new StringEnumConverter() }
                };
                
                var themesJson = JsonConvert.SerializeObject(_userThemes, settings);
                var activeThemeJson = JsonConvert.SerializeObject(_activeThemeId, settings);
                
                var json = new JObject
                {
                    ["themes"] = JToken.Parse(themesJson),
                    ["activeTheme"] = JToken.Parse(activeThemeJson)
                };

                File.WriteAllText(_settingsFilePath, json.ToString(Formatting.Indented));
            }
        }
        

        
        
        
        public List<Theme> GetAllThemes()
        {
            lock (_lock)
            {

                return _userThemes;
            }
        }

        
        
        
        public Theme GetThemeById(string themeId)
        {
            lock (_lock)
            {
                var theme = _userThemes.FirstOrDefault(t => t.Guid == themeId);
                return theme;
            }
        }

        
        
        
        public Theme AddTheme(Theme theme)
        {
            lock (_lock)
            {
                
                if (string.IsNullOrEmpty(theme.Guid))
                {
                    theme.Guid = Guid.NewGuid().ToString();
                }

                
                var now = DateTime.UtcNow.ToString("o");
                if (string.IsNullOrEmpty(theme.Created))
                {
                    theme.Created = now;
                }
                if (string.IsNullOrEmpty(theme.LastModified))
                {
                    theme.LastModified = now;
                }

                
                theme.PreviewColors ??= new List<string>();
                theme.ThemeJson ??= new Dictionary<string, Dictionary<string, string>>();

                _userThemes.Add(theme);
                SaveSettings();

                return theme;
            }
        }

        
        
        
        public Theme UpdateTheme(Theme theme)
        {
            lock (_lock)
            {
                var existingThemeIndex = _userThemes.FindIndex(t => t.Guid == theme.Guid);
                if (existingThemeIndex == -1)
                {
                    throw new KeyNotFoundException($"Theme with ID {theme.Guid} not found");
                }

                
                theme.LastModified = DateTime.UtcNow.ToString("o");
                
                
                if (string.IsNullOrEmpty(theme.Created))
                {
                    theme.Created = _userThemes[existingThemeIndex].Created;
                }

                
                theme.PreviewColors ??= new List<string>();
                theme.ThemeJson ??= new Dictionary<string, Dictionary<string, string>>();

                _userThemes[existingThemeIndex] = theme;
                SaveSettings();

                return theme;
            }
        }

        
        
        
        public bool DeleteTheme(string themeId)
        {
            lock (_lock)
            {
                var removed = _userThemes.RemoveAll(t => t.Guid == themeId) > 0;

                SaveSettings();

                return removed;
            }
        }

        
        
        
        public bool SetActiveTheme(string themeId)
        {
            lock (_lock)
            {
                _activeThemeId = themeId;
                SaveSettings();

                return true;
            }
        }

        
        
        
        public string GetActiveThemeId()
        {
            lock (_lock)
            {
                return _activeThemeId;
            }
        }
    }
}
