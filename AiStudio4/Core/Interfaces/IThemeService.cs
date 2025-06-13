// Core/Interfaces/IThemeService.cs




namespace AiStudio4.Core.Interfaces
{
    /// <summary>
    /// Service for managing UI themes
    /// </summary>
    public interface IThemeService
    {
        /// <summary>
        /// Gets all themes for a client
        /// </summary>
        /// <param name="clientId">The client identifier</param>
        /// <returns>List of themes</returns>
        List<Theme> GetAllThemes();

        /// <summary>
        /// Gets a theme by its ID
        /// </summary>
        /// <param name="clientId">The client identifier</param>
        /// <param name="themeId">The theme identifier</param>
        /// <returns>The theme if found, null otherwise</returns>
        Theme GetThemeById( string themeId);

        /// <summary>
        /// Adds a new theme
        /// </summary>
        /// <param name="clientId">The client identifier</param>
        /// <param name="theme">The theme to add</param>
        /// <returns>The added theme with generated ID</returns>
        Theme AddTheme( Theme theme);

        /// <summary>
        /// Updates an existing theme
        /// </summary>
        /// <param name="clientId">The client identifier</param>
        /// <param name="theme">The theme to update</param>
        /// <returns>The updated theme</returns>
        Theme UpdateTheme( Theme theme);

        /// <summary>
        /// Deletes a theme
        /// </summary>
        /// <param name="clientId">The client identifier</param>
        /// <param name="themeId">The theme identifier</param>
        /// <returns>True if deleted, false otherwise</returns>
        bool DeleteTheme( string themeId);

        /// <summary>
        /// Sets the active theme for a client
        /// </summary>
        /// <param name="clientId">The client identifier</param>
        /// <param name="themeId">The theme identifier</param>
        /// <returns>True if successful, false otherwise</returns>
        bool SetActiveTheme( string themeId);

        /// <summary>
        /// Gets the active theme ID for a client
        /// </summary>
        /// <param name="clientId">The client identifier</param>
        /// <returns>The active theme ID, or null if none is set</returns>
        string GetActiveThemeId();
    }
}
