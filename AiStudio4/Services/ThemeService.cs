// Services/ThemeService.cs
using AiStudio4.Core.Interfaces;
using AiStudio4.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace AiStudio4.Services
{
    /// <summary>
    /// Service for managing theme library, stored in %AppData%/AiStudio4/Themes/themeLibrary.json.
    /// </summary>
    public class ThemeService : IThemeService
    {
        private readonly string _themeLibraryPath;
        private ThemeLibrary _themeLibrary;
        private readonly object _lock = new();

        public ThemeService()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var themeDir = Path.Combine(appData, "AiStudio4", "Themes");
            if (!Directory.Exists(themeDir)) Directory.CreateDirectory(themeDir);
            _themeLibraryPath = Path.Combine(themeDir, "themeLibrary.json");
            _themeLibrary = new ThemeLibrary();
        }

        /// <summary>
        /// Loads or initializes the theme library from disk.
        /// </summary>
        public async Task InitializeAsync()
        {
            if (File.Exists(_themeLibraryPath))
            {
                var json = await File.ReadAllTextAsync(_themeLibraryPath);
                _themeLibrary = JsonSerializer.Deserialize<ThemeLibrary>(json) ?? new ThemeLibrary();
            }
            else
            {
                _themeLibrary = new ThemeLibrary();
                await SaveAsync();
            }
        }

        /// <summary>
        /// Saves the current theme library to disk.
        /// </summary>
        private async Task SaveAsync()
        {
            var json = JsonSerializer.Serialize(_themeLibrary, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_themeLibraryPath, json);
        }

        /// <summary>
        /// Gets all themes in the library.
        /// </summary>
        public Task<List<Theme>> GetAllThemesAsync()
        {
            lock (_lock)
            {
                return Task.FromResult(_themeLibrary.Themes.Select(CloneTheme).ToList());
            }
        }

        /// <summary>
        /// Gets a theme by its Guid.
        /// </summary>
        public Task<Theme> GetThemeByIdAsync(string themeId)
        {
            lock (_lock)
            {
                var theme = _themeLibrary.Themes.FirstOrDefault(t => t.Guid == themeId);
                return Task.FromResult(theme != null ? CloneTheme(theme) : null);
            }
        }

        /// <summary>
        /// Adds a new theme to the library.
        /// </summary>
        public async Task<Theme> AddThemeAsync(Theme theme)
        {
            if (theme == null) throw new ArgumentNullException(nameof(theme));
            ValidateTheme(theme);
            lock (_lock)
            {
                if (_themeLibrary.Themes.Any(t => t.Guid == theme.Guid))
                    throw new InvalidOperationException($"Theme with Guid {theme.Guid} already exists.");
                theme.Created = DateTime.UtcNow;
                theme.LastModified = DateTime.UtcNow;
                _themeLibrary.Themes.Add(CloneTheme(theme));
            }
            await SaveAsync();
            return theme;
        }

        /// <summary>
        /// Deletes a theme by its Guid.
        /// </summary>
        public async Task<bool> DeleteThemeAsync(string themeId)
        {
            bool removed = false;
            lock (_lock)
            {
                var theme = _themeLibrary.Themes.FirstOrDefault(t => t.Guid == themeId);
                if (theme != null)
                {
                    _themeLibrary.Themes.Remove(theme);
                    removed = true;
                }
            }
            if (removed) await SaveAsync();
            return removed;
        }

        /// <summary>
        /// Exports themes as JSON. If themeIds is null or empty, exports all.
        /// </summary>
        public Task<string> ExportThemesAsync(List<string> themeIds = null)
        {
            lock (_lock)
            {
                var themes = (themeIds == null || themeIds.Count == 0)
                    ? _themeLibrary.Themes
                    : _themeLibrary.Themes.Where(t => themeIds.Contains(t.Guid)).ToList();
                var json = JsonSerializer.Serialize(themes, new JsonSerializerOptions { WriteIndented = true });
                return Task.FromResult(json);
            }
        }

        /// <summary>
        /// Imports themes from JSON. Ignores duplicates.
        /// </summary>
        public async Task<List<Theme>> ImportThemesAsync(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) throw new ArgumentException("No JSON provided.");
            var importedThemes = JsonSerializer.Deserialize<List<Theme>>(json);
            if (importedThemes == null) return new List<Theme>();
            var added = new List<Theme>();
            lock (_lock)
            {
                foreach (var theme in importedThemes)
                {
                    if (_themeLibrary.Themes.Any(t => t.Guid == theme.Guid)) continue;
                    ValidateTheme(theme);
                    theme.Created = DateTime.UtcNow;
                    theme.LastModified = DateTime.UtcNow;
                    _themeLibrary.Themes.Add(CloneTheme(theme));
                    added.Add(CloneTheme(theme));
                }
            }
            if (added.Count > 0) await SaveAsync();
            return added;
        }

        /// <summary>
        /// Validates the theme structure.
        /// </summary>
        private void ValidateTheme(Theme theme)
        {
            if (theme.ThemeJson == null)
                throw new ArgumentException("ThemeJson must not be null.");
            foreach (var comp in theme.ThemeJson)
            {
                if (comp.Value == null)
                    throw new ArgumentException($"ThemeJson component '{comp.Key}' must not be null.");
                foreach (var prop in comp.Value)
                {
                    if (prop.Value == null)
                        throw new ArgumentException($"ThemeJson[{comp.Key}][{prop.Key}] must not be null.");
                }
            }
        }

        /// <summary>
        /// Deep clones a theme object.
        /// </summary>
        private Theme CloneTheme(Theme theme)
        {
            return new Theme
            {
                Guid = theme.Guid,
                Name = theme.Name,
                Description = theme.Description,
                Author = theme.Author,
                PreviewColors = theme.PreviewColors != null ? new List<string>(theme.PreviewColors) : new List<string>(),
                ThemeJson = theme.ThemeJson != null ? theme.ThemeJson.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value != null ? new Dictionary<string, string>(kvp.Value) : new Dictionary<string, string>()) : new Dictionary<string, Dictionary<string, string>>(),
                Created = theme.Created,
                LastModified = theme.LastModified
            };
        }
    }
}