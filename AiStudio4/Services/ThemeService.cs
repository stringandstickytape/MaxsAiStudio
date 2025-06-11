
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
        // Cache of the default themes so we only deserialize once per service instance
        private readonly List<Theme> _defaultThemes;
        private string _activeThemeId = "";

        public ThemeService(IConfiguration configuration)
        {
            _settingsFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AiStudio4", "themes.json");
            Directory.CreateDirectory(Path.GetDirectoryName(_settingsFilePath));

            // Deserialize the built-in themes once – we will use them when loading/merging the user settings
            _defaultThemes = DeserializeDefaultThemes();

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

                    // Merge missing default themes (System provided) without overwriting the existing ones
                    var addedDefaults = MergeMissingDefaultThemes();
                    if (addedDefaults)
                    {
                        // Persist the merge so that the json file is up to date
                        SaveSettings();
                    }
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
            var json2 = JObject.Parse(DefaultThemesJson);

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

        /// <summary>
        /// Returns the list of default themes bundled with the application.
        /// </summary>
        private List<Theme> DeserializeDefaultThemes()
        {
            var json = JObject.Parse(DefaultThemesJson);
            var themesSection = json["themes"];

            var settings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.None,
                NullValueHandling = NullValueHandling.Ignore,
                Converters = { new StringEnumConverter() }
            };

            var defaults = JsonConvert.DeserializeObject<List<Theme>>(themesSection!.ToString(), settings) ?? new List<Theme>();
            return defaults;
        }

        /// <summary>
        /// Ensures that any of the default themes that are not yet present in <see cref="_userThemes"/>
        /// are added. Returns true if at least one theme was added.
        /// </summary>
        private bool MergeMissingDefaultThemes()
        {
            var added = false;
            foreach (var def in _defaultThemes)
            {
                if (!_userThemes.Any(t => string.Equals(t.Guid, def.Guid, StringComparison.OrdinalIgnoreCase)))
                {
                    // Clone to avoid accidental reference sharing
                    var themeClone = JsonConvert.DeserializeObject<Theme>(JsonConvert.SerializeObject(def));
                    _userThemes.Add(themeClone);
                    added = true;
                }
            }
            return added;
        }

        /// <summary>
        /// A single string containing the JSON definition of the built-in themes.  Keeping it in one
        /// place allows us to reuse it for both creating the default settings file and merging.
        /// </summary>
        private const string DefaultThemesJson = @"{
  ""themes"": [
    {
      ""guid"": ""42bcc320-b10f-4412-ae72-86bbdb550024"",
      ""name"": ""Ultrachic Noir Elegance"",
      ""description"": ""Sophisticated dark theme with red accents and elegant typography"",
      ""author"": ""System"",
      ""previewColors"": [
        ""#0b0b0b"",
        ""#e63946"",
        ""#f1faee""
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
        ""global-fontCdnUrl"": ""https://fonts.googleapis.com/css2?family=Gabarito:wght@400;700&display=swap"",
        ""global-boxShadow"": ""0 4px 12px rgba(230, 57, 70, 0.15)"",
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
    },
    {
      ""guid"": ""a8f3d2c1-9e4b-4a7c-b5d6-1f2e3a4b5c6d"",
      ""name"": ""Ocean Breeze Light"",
      ""description"": ""Clean and airy light theme with ocean-inspired blues and greens"",
      ""author"": ""System"",
      ""previewColors"": [
        ""#f8fffe"",
        ""#0077be"",
        ""#20b2aa""
      ],
      ""themeJson"": {
        ""theme-name"": ""Ocean Breeze Light"",
        ""global-backgroundColor"": ""#f8fffe"",
        ""global-textColor"": ""#2c3e50"",
        ""global-secondaryTextColor"": ""#7f8c8d"",
        ""global-primaryColor"": ""#0077be"",
        ""global-secondaryColor"": ""#20b2aa"",
        ""global-borderColor"": ""#e1f5fe"",
        ""global-borderRadius"": ""12px"",
        ""global-fontFamily"": ""'Inter', sans-serif"",
        ""global-fontSize"": ""16px"",
        ""global-fontCdnUrl"": ""https://fonts.googleapis.com/css2?family=Inter:wght@300;400;500;600&display=swap"",
        ""global-boxShadow"": ""0 2px 8px rgba(0, 119, 190, 0.1)"",
        ""global-userMessageBackground"": ""#e3f2fd"",
        ""global-userMessageTextColor"": ""#1565c0"",
        ""global-userMessageBorderColor"": ""#0077be"",
        ""global-userMessageBorderWidth"": ""2px"",
        ""global-userMessageBorderStyle"": ""solid"",
        ""global-aiMessageBackground"": ""#e0f2f1"",
        ""global-aiMessageTextColor"": ""#00695c"",
        ""global-aiMessageBorderColor"": ""#20b2aa"",
        ""global-aiMessageBorderWidth"": ""2px"",
        ""global-aiMessageBorderStyle"": ""solid"",
        ""ConvTreeView---convtree-user-node-color"": ""#0077be"",
        ""ConvTreeView---convtree-ai-node-color"": ""#20b2aa"",
        ""ConvTreeView---convtree-user-node-border"": ""#0077be"",
        ""ConvTreeView---convtree-ai-node-border"": ""#20b2aa"",
        ""ConvTreeView---convtree-link-color"": ""#0077be"",
        ""ConvTreeView---convtree-accent-color"": ""#20b2aa"",
        ""MarkdownPane-codeHeaderBackground"": ""#f5f5f5"",
        ""MarkdownPane-codeHeaderText"": ""#0077be"",
        ""MarkdownPane-codeHeaderBorder"": ""#0077be"",
        ""MarkdownPane-codeHeaderAccent"": ""#20b2aa"",
        ""MarkdownPane-style"": ""border-radius: 12px; font-family: 'Inter', sans-serif; font-weight: 400;""
      },
      ""fontCdnUrl"": """",
      ""created"": ""05/31/2025 18:20:00"",
      ""lastModified"": ""2025-05-31T18:20:00.0000000Z""
    },
    {
      ""guid"": ""7b9e4f2a-3c8d-4e1b-9a5f-6d2c8e4a1b7f"",
      ""name"": ""Cyberpunk Neon"",
      ""description"": ""Futuristic dark theme with vibrant neon accents and tech aesthetics"",
      ""author"": ""System"",
      ""previewColors"": [
        ""#0a0a0a"",
        ""#00ff88"",
        ""#ff0080""
      ],
      ""themeJson"": {
        ""theme-name"": ""Cyberpunk Neon"",
        ""global-backgroundColor"": ""#0a0a0a"",
        ""global-textColor"": ""#00ff88"",
        ""global-secondaryTextColor"": ""#888888"",
        ""global-primaryColor"": ""#00ff88"",
        ""global-secondaryColor"": ""#ff0080"",
        ""global-borderColor"": ""#333333"",
        ""global-borderRadius"": ""4px"",
        ""global-fontFamily"": ""'JetBrains Mono', monospace"",
        ""global-fontSize"": ""15px"",
        ""global-fontCdnUrl"": ""https://fonts.googleapis.com/css2?family=JetBrains+Mono:wght@300;400;500;700&display=swap"",
        ""global-boxShadow"": ""0 0 20px rgba(0, 255, 136, 0.3)"",
        ""global-userMessageBackground"": ""#1a1a1a"",
        ""global-userMessageTextColor"": ""#00ff88"",
        ""global-userMessageBorderColor"": ""#00ff88"",
        ""global-userMessageBorderWidth"": ""1px"",
        ""global-userMessageBorderStyle"": ""solid"",
        ""global-aiMessageBackground"": ""#1a0a1a"",
        ""global-aiMessageTextColor"": ""#ff0080"",
        ""global-aiMessageBorderColor"": ""#ff0080"",
        ""global-aiMessageBorderWidth"": ""1px"",
        ""global-aiMessageBorderStyle"": ""solid"",
        ""ConvTreeView---convtree-user-node-color"": ""#00ff88"",
        ""ConvTreeView---convtree-ai-node-color"": ""#ff0080"",
        ""ConvTreeView---convtree-user-node-border"": ""#00ff88"",
        ""ConvTreeView---convtree-ai-node-border"": ""#ff0080"",
        ""ConvTreeView---convtree-link-color"": ""#00ff88"",
        ""ConvTreeView---convtree-accent-color"": ""#ff0080"",
        ""MarkdownPane-codeHeaderBackground"": ""#1a1a1a"",
        ""MarkdownPane-codeHeaderText"": ""#00ff88"",
        ""MarkdownPane-codeHeaderBorder"": ""#00ff88"",
        ""MarkdownPane-codeHeaderAccent"": ""#ff0080"",
        ""MarkdownPane-style"": ""border-radius: 4px; font-family: 'JetBrains Mono', monospace; font-weight: 400; text-shadow: 0 0 5px currentColor;""
      },
      ""fontCdnUrl"": """",
      ""created"": ""05/31/2025 18:21:00"",
      ""lastModified"": ""2025-05-31T18:21:00.0000000Z""
    },
    {
      ""guid"": ""c5e8a3f1-2d7b-4c9e-8a1f-3b6d9c2e5a8b"",
      ""name"": ""Warm Sunset"",
      ""description"": ""Cozy light theme with warm oranges, yellows, and earth tones"",
      ""author"": ""System"",
      ""previewColors"": [
        ""#fef7f0"",
        ""#ff6b35"",
        ""#f7931e""
      ],
      ""themeJson"": {
        ""theme-name"": ""Warm Sunset"",
        ""global-backgroundColor"": ""#fef7f0"",
        ""global-textColor"": ""#5d4037"",
        ""global-secondaryTextColor"": ""#8d6e63"",
        ""global-primaryColor"": ""#ff6b35"",
        ""global-secondaryColor"": ""#f7931e"",
        ""global-borderColor"": ""#ffcc9c"",
        ""global-borderRadius"": ""16px"",
        ""global-fontFamily"": ""'Poppins', sans-serif"",
        ""global-fontSize"": ""16px"",
        ""global-fontCdnUrl"": ""https://fonts.googleapis.com/css2?family=Poppins:wght@300;400;500;600;700&display=swap"",
        ""global-boxShadow"": ""0 4px 16px rgba(255, 107, 53, 0.15)"",
        ""global-userMessageBackground"": ""#fff3e0"",
        ""global-userMessageTextColor"": ""#e65100"",
        ""global-userMessageBorderColor"": ""#ff6b35"",
        ""global-userMessageBorderWidth"": ""2px"",
        ""global-userMessageBorderStyle"": ""solid"",
        ""global-aiMessageBackground"": ""#fff8e1"",
        ""global-aiMessageTextColor"": ""#f57c00"",
        ""global-aiMessageBorderColor"": ""#f7931e"",
        ""global-aiMessageBorderWidth"": ""2px"",
        ""global-aiMessageBorderStyle"": ""solid"",
        ""ConvTreeView---convtree-user-node-color"": ""#ff6b35"",
        ""ConvTreeView---convtree-ai-node-color"": ""#f7931e"",
        ""ConvTreeView---convtree-user-node-border"": ""#ff6b35"",
        ""ConvTreeView---convtree-ai-node-border"": ""#f7931e"",
        ""ConvTreeView---convtree-link-color"": ""#ff6b35"",
        ""ConvTreeView---convtree-accent-color"": ""#f7931e"",
        ""MarkdownPane-codeHeaderBackground"": ""#f5f5f5"",
        ""MarkdownPane-codeHeaderText"": ""#ff6b35"",
        ""MarkdownPane-codeHeaderBorder"": ""#ff6b35"",
        ""MarkdownPane-codeHeaderAccent"": ""#f7931e"",
        ""MarkdownPane-style"": ""border-radius: 16px; font-family: 'Poppins', sans-serif; font-weight: 400;""
      },
      ""fontCdnUrl"": """",
      ""created"": ""05/31/2025 18:22:00"",
      ""lastModified"": ""2025-05-31T18:22:00.0000000Z""
    },
    {
      ""guid"": ""d9f2b4e7-6a3c-4d8e-9b2f-5c7a8d1e4b9c"",
      ""name"": ""Midnight Purple"",
      ""description"": ""Deep dark theme with rich purples and mystical ambiance"",
      ""author"": ""System"",
      ""previewColors"": [
        ""#1a0d26"",
        ""#8b5cf6"",
        ""#c084fc""
      ],
      ""themeJson"": {
        ""theme-name"": ""Midnight Purple"",
        ""global-backgroundColor"": ""#1a0d26"",
        ""global-textColor"": ""#e2d5f1"",
        ""global-secondaryTextColor"": ""#a78bfa"",
        ""global-primaryColor"": ""#8b5cf6"",
        ""global-secondaryColor"": ""#c084fc"",
        ""global-borderColor"": ""#4c1d95"",
        ""global-borderRadius"": ""8px"",
        ""global-fontFamily"": ""'Nunito', sans-serif"",
        ""global-fontSize"": ""17px"",
        ""global-fontCdnUrl"": ""https://fonts.googleapis.com/css2?family=Nunito:wght@400;500;600;700&display=swap"",
        ""global-boxShadow"": ""0 8px 32px rgba(139, 92, 246, 0.2)"",
        ""global-userMessageBackground"": ""#2d1b4e"",
        ""global-userMessageTextColor"": ""#e2d5f1"",
        ""global-userMessageBorderColor"": ""#8b5cf6"",
        ""global-userMessageBorderWidth"": ""2px"",
        ""global-userMessageBorderStyle"": ""solid"",
        ""global-aiMessageBackground"": ""#3b2764"",
        ""global-aiMessageTextColor"": ""#e2d5f1"",
        ""global-aiMessageBorderColor"": ""#c084fc"",
        ""global-aiMessageBorderWidth"": ""2px"",
        ""global-aiMessageBorderStyle"": ""solid"",
        ""ConvTreeView---convtree-user-node-color"": ""#8b5cf6"",
        ""ConvTreeView---convtree-ai-node-color"": ""#c084fc"",
        ""ConvTreeView---convtree-user-node-border"": ""#8b5cf6"",
        ""ConvTreeView---convtree-ai-node-border"": ""#c084fc"",
        ""ConvTreeView---convtree-link-color"": ""#8b5cf6"",
        ""ConvTreeView---convtree-accent-color"": ""#c084fc"",
        ""MarkdownPane-codeHeaderBackground"": ""#2d1b4e"",
        ""MarkdownPane-codeHeaderText"": ""#8b5cf6"",
        ""MarkdownPane-codeHeaderBorder"": ""#8b5cf6"",
        ""MarkdownPane-codeHeaderAccent"": ""#c084fc"",
        ""MarkdownPane-style"": ""border-radius: 8px; font-family: 'Nunito', sans-serif; font-weight: 500;""
      },
      ""fontCdnUrl"": """",
      ""created"": ""05/31/2025 18:23:00"",
      ""lastModified"": ""2025-05-31T18:23:00.0000000Z""
    },
    {
      ""guid"": ""e1a7c4d8-5b2f-4e9a-8c3d-7f1b5e8a2c6f"",
      ""name"": ""Forest Zen"",
      ""description"": ""Calming light theme with natural greens and earthy tones"",
      ""author"": ""System"",
      ""previewColors"": [
        ""#f7fdf7"",
        ""#2e7d32"",
        ""#66bb6a""
      ],
      ""themeJson"": {
        ""theme-name"": ""Forest Zen"",
        ""global-backgroundColor"": ""#f7fdf7"",
        ""global-textColor"": ""#2e4e2e"",
        ""global-secondaryTextColor"": ""#6b8e6b"",
        ""global-primaryColor"": ""#2e7d32"",
        ""global-secondaryColor"": ""#66bb6a"",
        ""global-borderColor"": ""#c8e6c9"",
        ""global-borderRadius"": ""14px"",
        ""global-fontFamily"": ""'Lato', sans-serif"",
        ""global-fontSize"": ""16px"",
        ""global-fontCdnUrl"": ""https://fonts.googleapis.com/css2?family=Lato:wght@300;400;700;900&display=swap"",
        ""global-boxShadow"": ""0 3px 12px rgba(46, 125, 50, 0.12)"",
        ""global-userMessageBackground"": ""#e8f5e8"",
        ""global-userMessageTextColor"": ""#1b5e20"",
        ""global-userMessageBorderColor"": ""#2e7d32"",
        ""global-userMessageBorderWidth"": ""2px"",
        ""global-userMessageBorderStyle"": ""solid"",
        ""global-aiMessageBackground"": ""#f1f8e9"",
        ""global-aiMessageTextColor"": ""#33691e"",
        ""global-aiMessageBorderColor"": ""#66bb6a"",
        ""global-aiMessageBorderWidth"": ""2px"",
        ""global-aiMessageBorderStyle"": ""solid"",
        ""ConvTreeView---convtree-user-node-color"": ""#2e7d32"",
        ""ConvTreeView---convtree-ai-node-color"": ""#66bb6a"",
        ""ConvTreeView---convtree-user-node-border"": ""#2e7d32"",
        ""ConvTreeView---convtree-ai-node-border"": ""#66bb6a"",
        ""ConvTreeView---convtree-link-color"": ""#2e7d32"",
        ""ConvTreeView---convtree-accent-color"": ""#66bb6a"",
        ""MarkdownPane-codeHeaderBackground"": ""#f5f5f5"",
        ""MarkdownPane-codeHeaderText"": ""#2e7d32"",
        ""MarkdownPane-codeHeaderBorder"": ""#2e7d32"",
        ""MarkdownPane-codeHeaderAccent"": ""#66bb6a"",
        ""MarkdownPane-style"": ""border-radius: 14px; font-family: 'Lato', sans-serif; font-weight: 400;""
      },
      ""fontCdnUrl"": """",
      ""created"": ""05/31/2025 18:24:00"",
      ""lastModified"": ""31/05/2025 18:24:00.0000000Z""
    },
    {
      ""guid"": ""f3b8d5e2-7c4a-4f1e-9d6b-8a2c5f9e3b7d"",
      ""name"": ""Arctic Blue"",
      ""description"": ""Cool dark theme with icy blues and crisp whites"",
      ""author"": ""System"",
      ""previewColors"": [
        ""#0d1b2a"",
        ""#415a77"",
        ""#778da9""
      ],
      ""themeJson"": {
        ""theme-name"": ""Arctic Blue"",
        ""global-backgroundColor"": ""#0d1b2a"",
        ""global-textColor"": ""#e0e1dd"",
        ""global-secondaryTextColor"": ""#778da9"",
        ""global-primaryColor"": ""#415a77"",
        ""global-secondaryColor"": ""#778da9"",
        ""global-borderColor"": ""#1b263b"",
        ""global-borderRadius"": ""6px"",
        ""global-fontFamily"": ""'Source Sans Pro', sans-serif"",
        ""global-fontSize"": ""16px"",
        ""global-fontCdnUrl"": ""https://fonts.googleapis.com/css2?family=Source+Sans+Pro:wght@300;400;600;700&display=swap"",
        ""global-boxShadow"": ""0 6px 24px rgba(65, 90, 119, 0.25)"",
        ""global-userMessageBackground"": ""#1b263b"",
        ""global-userMessageTextColor"": ""#e0e1dd"",
        ""global-userMessageBorderColor"": ""#415a77"",
        ""global-userMessageBorderWidth"": ""2px"",
        ""global-userMessageBorderStyle"": ""solid"",
        ""global-aiMessageBackground"": ""#1b2a41"",
        ""global-aiMessageTextColor"": ""#e0e1dd"",
        ""global-aiMessageBorderColor"": ""#778da9"",
        ""global-aiMessageBorderWidth"": ""2px"",
        ""global-aiMessageBorderStyle"": ""solid"",
        ""ConvTreeView---convtree-user-node-color"": ""#415a77"",
        ""ConvTreeView---convtree-ai-node-color"": ""#778da9"",
        ""ConvTreeView---convtree-user-node-border"": ""#415a77"",
        ""ConvTreeView---convtree-ai-node-border"": ""#778da9"",
        ""ConvTreeView---convtree-link-color"": ""#415a77"",
        ""ConvTreeView---convtree-accent-color"": ""#778da9"",
        ""MarkdownPane-codeHeaderBackground"": ""#1b263b"",
        ""MarkdownPane-codeHeaderText"": ""#415a77"",
        ""MarkdownPane-codeHeaderBorder"": ""#415a77"",
        ""MarkdownPane-codeHeaderAccent"": ""#778da9"",
        ""MarkdownPane-style"": ""border-radius: 6px; font-family: 'Source Sans Pro', sans-serif; font-weight: 400;""
      },
      ""fontCdnUrl"": """",
      ""created"": ""05/31/2025 18:25:00"",
      ""lastModified"": ""2025-05-31T18:25:00.0000000Z""
    },
    {
      ""guid"": ""a4c7f1e5-8d2b-4a6e-9f3c-5b8e1a4d7c2f"",
      ""name"": ""Coral Reef"",
      ""description"": ""Vibrant light theme with coral pinks and tropical blues"",
      ""author"": ""System"",
      ""previewColors"": [
        ""#fff8f6"",
        ""#ff6b9d"",
        ""#4ecdc4""
      ],
      ""themeJson"": {
        ""theme-name"": ""Coral Reef"",
        ""global-backgroundColor"": ""#fff8f6"",
        ""global-textColor"": ""#2c2c54"",
        ""global-secondaryTextColor"": ""#6c5ce7"",
        ""global-primaryColor"": ""#ff6b9d"",
        ""global-secondaryColor"": ""#4ecdc4"",
        ""global-borderColor"": ""#ffeaa7"",
        ""global-borderRadius"": ""18px"",
        ""global-fontFamily"": ""'Quicksand', sans-serif"",
        ""global-fontSize"": ""16px"",
        ""global-fontCdnUrl"": ""https://fonts.googleapis.com/css2?family=Quicksand:wght@300;400;500;600;700&display=swap"",
        ""global-boxShadow"": ""0 5px 20px rgba(255, 107, 157, 0.2)"",
        ""global-userMessageBackground"": ""#ffeef3"",
        ""global-userMessageTextColor"": ""#d63384"",
        ""global-userMessageBorderColor"": ""#ff6b9d"",
        ""global-userMessageBorderWidth"": ""2px"",
        ""global-userMessageBorderStyle"": ""solid"",
        ""global-aiMessageBackground"": ""#e8fffe"",
        ""global-aiMessageTextColor"": ""#0891b2"",
        ""global-aiMessageBorderColor"": ""#4ecdc4"",
        ""global-aiMessageBorderWidth"": ""2px"",
        ""global-aiMessageBorderStyle"": ""solid"",
        ""ConvTreeView---convtree-user-node-color"": ""#ff6b9d"",
        ""ConvTreeView---convtree-ai-node-color"": ""#4ecdc4"",
        ""ConvTreeView---convtree-user-node-border"": ""#ff6b9d"",
        ""ConvTreeView---convtree-ai-node-border"": ""#4ecdc4"",
        ""ConvTreeView---convtree-link-color"": ""#ff6b9d"",
        ""ConvTreeView---convtree-accent-color"": ""#4ecdc4"",
        ""MarkdownPane-codeHeaderBackground"": ""#f8f9fa"",
        ""MarkdownPane-codeHeaderText"": ""#ff6b9d"",
        ""MarkdownPane-codeHeaderBorder"": ""#ff6b9d"",
        ""MarkdownPane-codeHeaderAccent"": ""#4ecdc4"",
        ""MarkdownPane-style"": ""border-radius: 18px; font-family: 'Quicksand', sans-serif; font-weight: 500;""
      },
      ""fontCdnUrl"": """",
      ""created"": ""05/31/2025 18:26:00"",
      ""lastModified"": ""2025-05-31T18:26:00.0000000Z""
    }
  ]
}";
    }
}