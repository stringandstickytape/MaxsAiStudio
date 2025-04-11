// Core/Interfaces/IThemeService.cs
using AiStudio4.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AiStudio4.Core.Interfaces
{
    /// <summary>
    /// Interface for theme library service.
    /// Provides methods to manage, import, and export themes.
    /// </summary>
    public interface IThemeService
    {
        /// <summary>
        /// Initializes the theme service, loading any required resources.
        /// </summary>
        Task InitializeAsync();

        /// <summary>
        /// Retrieves all available themes.
        /// </summary>
        /// <returns>List of all themes.</returns>
        Task<List<Theme>> GetAllThemesAsync();

        /// <summary>
        /// Retrieves a theme by its unique identifier.
        /// </summary>
        /// <param name="themeId">The unique ID of the theme.</param>
        /// <returns>The theme if found; otherwise, null.</returns>
        Task<Theme> GetThemeByIdAsync(string themeId);

        /// <summary>
        /// Adds a new theme to the library.
        /// </summary>
        /// <param name="theme">The theme to add.</param>
        /// <returns>The added theme with any generated properties (e.g., ID).</returns>
        Task<Theme> AddThemeAsync(Theme theme);

        /// <summary>
        /// Deletes a theme by its unique identifier.
        /// </summary>
        /// <param name="themeId">The unique ID of the theme to delete.</param>
        /// <returns>True if the theme was deleted; otherwise, false.</returns>
        Task<bool> DeleteThemeAsync(string themeId);

        /// <summary>
        /// Exports themes as a JSON string.
        /// </summary>
        /// <param name="themeIds">Optional list of theme IDs to export. If null, exports all themes.</param>
        /// <returns>JSON string representing the exported themes.</returns>
        Task<string> ExportThemesAsync(List<string> themeIds = null);

        /// <summary>
        /// Imports themes from a JSON string.
        /// </summary>
        /// <param name="json">The JSON string containing theme data.</param>
        /// <returns>List of imported themes.</returns>
        Task<List<Theme>> ImportThemesAsync(string json);

        /// <summary>
        /// Sets a theme as the default theme.
        /// </summary>
        /// <param name="themeId">The unique ID of the theme to set as default.</param>
        /// <returns>True if successful; otherwise, false.</returns>
        Task<bool> SetDefaultThemeAsync(string themeId);

        /// <summary>
        /// Gets the current default theme.
        /// </summary>
        /// <returns>The default theme if set; otherwise, null.</returns>
        Task<Theme> GetDefaultThemeAsync();
    }
}