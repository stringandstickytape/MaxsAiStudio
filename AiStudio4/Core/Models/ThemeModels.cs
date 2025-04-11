// Core/Models/ThemeModels.cs
using System;
using System.Collections.Generic;

namespace AiStudio4.Core.Models
{
    /// <summary>
    /// Represents a single theme with metadata and theme data.
    /// </summary>
    public class Theme
    {
        /// <summary>
        /// Unique identifier for the theme.
        /// </summary>
        public string Guid { get; set; } = System.Guid.NewGuid().ToString();

        /// <summary>
        /// Name of the theme.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Description of the theme.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Author of the theme.
        /// </summary>
        public string Author { get; set; }

        /// <summary>
        /// List of preview colors representing the theme.
        /// </summary>
        public List<string> PreviewColors { get; set; } = new List<string>();

        /// <summary>
        /// The theme's JSON data, organized by category and property.
        /// </summary>
        public Dictionary<string, Dictionary<string, string>> ThemeJson { get; set; } = new();

        /// <summary>
        /// Date and time when the theme was created (UTC).
        /// </summary>
        public DateTime Created { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Date and time when the theme was last modified (UTC).
        /// </summary>
        public DateTime LastModified { get; set; } = DateTime.UtcNow;
        public object Id { get; internal set; }
    }

    /// <summary>
    /// Represents a library of themes.
    /// </summary>
    public class ThemeLibrary
    {
        /// <summary>
        /// Collection of themes in the library.
        /// </summary>
        public List<Theme> Themes { get; set; } = new();
    }
}