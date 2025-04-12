// Core/Models/Theme.cs
using System.Collections.Generic;

namespace AiStudio4.Core.Models
{
    /// <summary>
    /// Represents a UI theme that can be applied to the application
    /// </summary>
    public class Theme
    {
        /// <summary>
        /// Unique identifier for the theme
        /// </summary>
        public string Guid { get; set; }

        /// <summary>
        /// Display name of the theme
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Description of the theme
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Author of the theme
        /// </summary>
        public string Author { get; set; }

        /// <summary>
        /// List of colors used for theme preview
        /// </summary>
        public List<string> PreviewColors { get; set; }

        /// <summary>
        /// Theme configuration as a nested object
        /// </summary>
        public object ThemeJson { get; set; }

        /// <summary>
        /// Creation timestamp (ISO format)
        /// </summary>
        public string Created { get; set; }

        /// <summary>
        /// Last modification timestamp (ISO format)
        /// </summary>
        public string LastModified { get; set; }
    }
}