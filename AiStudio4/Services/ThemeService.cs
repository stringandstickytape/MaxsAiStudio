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
        private Dictionary<string, List<Theme>> _userThemes = new Dictionary<string, List<Theme>>();
        private Dictionary<string, string> _activeThemeIds = new Dictionary<string, string>();

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
                    _userThemes = new Dictionary<string, List<Theme>>();
                    _activeThemeIds = new Dictionary<string, string>();
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
                    
                    _userThemes = JsonConvert.DeserializeObject<Dictionary<string, List<Theme>>>(themesSection.ToString(), settings) 
                        ?? new Dictionary<string, List<Theme>>();
                }
                else
                {
                    _userThemes = new Dictionary<string, List<Theme>>();
                }

                var activeThemesSection = json["activeThemes"];
                if (activeThemesSection != null)
                {
                    _activeThemeIds = activeThemesSection.ToObject<Dictionary<string, string>>() ?? new Dictionary<string, string>();
                }
                else
                {
                    _activeThemeIds = new Dictionary<string, string>();
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
                var activeThemesJson = JsonConvert.SerializeObject(_activeThemeIds, settings);
                
                var json = new JObject
                {
                    ["themes"] = JToken.Parse(themesJson),
                    ["activeThemes"] = JToken.Parse(activeThemesJson)
                };

                File.WriteAllText(_settingsFilePath, json.ToString(Formatting.Indented));
            }
        }

        /// <summary>
        /// Gets all themes for a client
        /// </summary>
        public Task<List<Theme>> GetAllThemesAsync(string clientId)
        {
            lock (_lock)
            {
                if (!_userThemes.TryGetValue(clientId, out var themes))
                {
                    themes = new List<Theme>();
                    _userThemes[clientId] = themes;
                    SaveSettings();
                }

                return Task.FromResult(themes);
            }
        }

        /// <summary>
        /// Gets a theme by its ID
        /// </summary>
        public Task<Theme> GetThemeByIdAsync(string clientId, string themeId)
        {
            lock (_lock)
            {
                if (!_userThemes.TryGetValue(clientId, out var themes))
                {
                    return Task.FromResult<Theme>(null);
                }

                var theme = themes.FirstOrDefault(t => t.Guid == themeId);
                return Task.FromResult(theme);
            }
        }

        /// <summary>
        /// Adds a new theme
        /// </summary>
        public Task<Theme> AddThemeAsync(string clientId, Theme theme)
        {
            lock (_lock)
            {
                if (!_userThemes.TryGetValue(clientId, out var themes))
                {
                    themes = new List<Theme>();
                    _userThemes[clientId] = themes;
                }

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

                themes.Add(theme);
                SaveSettings();

                return Task.FromResult(theme);
            }
        }

        /// <summary>
        /// Updates an existing theme
        /// </summary>
        public Task<Theme> UpdateThemeAsync(string clientId, Theme theme)
        {
            lock (_lock)
            {
                if (!_userThemes.TryGetValue(clientId, out var themes))
                {
                    throw new KeyNotFoundException($"No themes found for client {clientId}");
                }

                var existingThemeIndex = themes.FindIndex(t => t.Guid == theme.Guid);
                if (existingThemeIndex == -1)
                {
                    throw new KeyNotFoundException($"Theme with ID {theme.Guid} not found");
                }

                // Update last modified timestamp
                theme.LastModified = DateTime.UtcNow.ToString("o");
                
                // Preserve creation timestamp
                if (string.IsNullOrEmpty(theme.Created))
                {
                    theme.Created = themes[existingThemeIndex].Created;
                }

                // Initialize collections if null
                theme.PreviewColors ??= new List<string>();
                theme.ThemeJson ??= new Dictionary<string, Dictionary<string, string>>();

                themes[existingThemeIndex] = theme;
                SaveSettings();

                return Task.FromResult(theme);
            }
        }

        /// <summary>
        /// Deletes a theme
        /// </summary>
        public Task<bool> DeleteThemeAsync(string clientId, string themeId)
        {
            lock (_lock)
            {
                if (!_userThemes.TryGetValue(clientId, out var themes))
                {
                    return Task.FromResult(false);
                }

                var removed = themes.RemoveAll(t => t.Guid == themeId) > 0;
                if (removed)
                {
                    // If the active theme was deleted, clear it
                    if (_activeThemeIds.TryGetValue(clientId, out var activeThemeId) && activeThemeId == themeId)
                    {
                        _activeThemeIds.Remove(clientId);
                    }

                    SaveSettings();
                }

                return Task.FromResult(removed);
            }
        }

        /// <summary>
        /// Sets the active theme for a client
        /// </summary>
        public Task<bool> SetActiveThemeAsync(string clientId, string themeId)
        {
            lock (_lock)
            {
                // Verify the theme exists
                if (!_userThemes.TryGetValue(clientId, out var themes) || 
                    !themes.Any(t => t.Guid == themeId))
                {
                    return Task.FromResult(false);
                }

                _activeThemeIds[clientId] = themeId;
                SaveSettings();

                return Task.FromResult(true);
            }
        }

        /// <summary>
        /// Gets the active theme ID for a client
        /// </summary>
        public Task<string> GetActiveThemeIdAsync(string clientId)
        {
            lock (_lock)
            {
                _activeThemeIds.TryGetValue(clientId, out var themeId);
                return Task.FromResult(themeId);
            }
        }
    }
}