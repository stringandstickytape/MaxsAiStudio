// Services/ThemeService.cs
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
    /// <summary>
    /// Service for managing UI themes
    /// </summary>
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

        /// <summary>
        /// Loads theme settings from the file
        /// </summary>
        private void LoadSettings()
        {
            lock (_lock)
            {
                if (!File.Exists(_settingsFilePath))
                {
                    _userThemes = new List<Theme>();
                    _activeThemeId = "";
                    CreateDefaultTheme();
                    SaveSettings();
                    return;
                }

                var json = JObject.Parse(File.ReadAllText(_settingsFilePath));
                
                var themesSection = json["themes"];
                if (themesSection != null)
                {
                    // Use JsonSerializerSettings to handle complex JSON objects
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
                
                // If no themes exist or no active theme is set, create a default theme
                if (_userThemes.Count == 0)
                {
                    CreateDefaultTheme();
                    SaveSettings();
                }
                else if (string.IsNullOrEmpty(_activeThemeId) || !_userThemes.Any(t => t.Guid == _activeThemeId))
                {
                    // Set the first theme as active if no active theme is set or the active theme doesn't exist
                    _activeThemeId = _userThemes.First().Guid;
                    SaveSettings();
                }
            }
        }

        /// <summary>
        /// Saves theme settings to the file
        /// </summary>
        private void SaveSettings()
        {
            lock (_lock)
            {
                // Use JsonSerializerSettings to handle complex JSON objects
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
        
        /// <summary>
        /// Creates a default theme if none exists
        /// </summary>
        private void CreateDefaultTheme()
        {
            var defaultTheme = new Theme
            {
                Guid = Guid.NewGuid().ToString(),
                Name = "Default Theme",
                Description = "Default application theme",
                Author = "System",
                Created = DateTime.UtcNow.ToString("o"),
                LastModified = DateTime.UtcNow.ToString("o"),
                PreviewColors = new List<string> { "#1a1a1a", "#f9e1e9", "#ff8fb1" },
                FontCdnUrl = "", // Empty by default
                ThemeJson = new Dictionary<string, Dictionary<string, string>>
                {
                    ["global"] = new Dictionary<string, string>
                    {
                        ["fontCdnUrl"] = ""
                    },
                    ["InputBar"] = new Dictionary<string, string>
                    {
                        ["backgroundColor"] = "#1a1a1a"
                    },
                    ["SystemPromptComponent"] = new Dictionary<string, string>
                    {
                        ["backgroundColor"] = "#1a1a1a",
                        ["textColor"] = "#f9e1e9",
                        ["borderColor"] = "#ff8fb1",
                        ["borderRadius"] = "12px",
                        ["fontFamily"] = "\"Segoe UI\", \"Noto Sans JP\", sans-serif",
                        ["fontSize"] = "1rem",
                        ["boxShadow"] = "0 4px 12px rgba(0,0,0,0.4)",
                        ["pillActiveBg"] = "#ff8fb133",
                        ["pillInactiveBg"] = "#444",
                        ["popupBackground"] = "rgba(30,30,30,0.95)",
                        ["popupBorderColor"] = "#ff8fb1",
                        ["editBackground"] = "#2a2a2a",
                        ["editTextColor"] = "#f9e1e9"
                    }
                }
            };
            
            _userThemes.Add(defaultTheme);
            _activeThemeId = defaultTheme.Guid;
        }

        /// <summary>
        /// Gets all themes for a client
        /// </summary>
        public List<Theme> GetAllThemes()
        {
            lock (_lock)
            {

                return _userThemes;
            }
        }

        /// <summary>
        /// Gets a theme by its ID
        /// </summary>
        public Theme GetThemeById(string themeId)
        {
            lock (_lock)
            {
                var theme = _userThemes.FirstOrDefault(t => t.Guid == themeId);
                return theme;
            }
        }

        /// <summary>
        /// Adds a new theme
        /// </summary>
        public Theme AddTheme(Theme theme)
        {
            lock (_lock)
            {
                // Ensure the theme has a GUID
                if (string.IsNullOrEmpty(theme.Guid))
                {
                    theme.Guid = Guid.NewGuid().ToString();
                }

                // Set timestamps if not provided
                var now = DateTime.UtcNow.ToString("o");
                if (string.IsNullOrEmpty(theme.Created))
                {
                    theme.Created = now;
                }
                if (string.IsNullOrEmpty(theme.LastModified))
                {
                    theme.LastModified = now;
                }

                // Initialize collections if null
                theme.PreviewColors ??= new List<string>();
                theme.ThemeJson ??= new Dictionary<string, Dictionary<string, string>>();

                _userThemes.Add(theme);
                SaveSettings();

                return theme;
            }
        }

        /// <summary>
        /// Updates an existing theme
        /// </summary>
        public Theme UpdateTheme(Theme theme)
        {
            lock (_lock)
            {
                var existingThemeIndex = _userThemes.FindIndex(t => t.Guid == theme.Guid);
                if (existingThemeIndex == -1)
                {
                    throw new KeyNotFoundException($"Theme with ID {theme.Guid} not found");
                }

                // Update last modified timestamp
                theme.LastModified = DateTime.UtcNow.ToString("o");
                
                // Preserve creation timestamp
                if (string.IsNullOrEmpty(theme.Created))
                {
                    theme.Created = _userThemes[existingThemeIndex].Created;
                }

                // Initialize collections if null
                theme.PreviewColors ??= new List<string>();
                theme.ThemeJson ??= new Dictionary<string, Dictionary<string, string>>();

                _userThemes[existingThemeIndex] = theme;
                SaveSettings();

                return theme;
            }
        }

        /// <summary>
        /// Deletes a theme
        /// </summary>
        public bool DeleteTheme(string themeId)
        {
            lock (_lock)
            {
                var removed = _userThemes.RemoveAll(t => t.Guid == themeId) > 0;

                SaveSettings();

                return removed;
            }
        }

        /// <summary>
        /// Sets the active theme for a client
        /// </summary>
        public bool SetActiveTheme(string themeId)
        {
            lock (_lock)
            {
                _activeThemeId = themeId;
                SaveSettings();

                return true;
            }
        }

        /// <summary>
        /// Gets the active theme ID for a client
        /// </summary>
        public string GetActiveThemeId()
        {
            lock (_lock)
            {
                return _activeThemeId;
            }
        }
    }
}