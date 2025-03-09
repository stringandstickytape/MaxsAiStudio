namespace AiStudio4.InjectedDependencies
{
    /// <summary>
    /// Stores user appearance preferences
    /// </summary>
    public class AppearanceSettings
    {
        /// <summary>
        /// Font size in pixels
        /// </summary>
        public int FontSize { get; set; } = 16;

        /// <summary>
        /// Whether to use dark mode
        /// </summary>
        public bool IsDarkMode { get; set; } = true;
    }
}