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

        /// <summary>
        /// Chat container panel size as percentage (0-100)
        /// </summary>
        public int ChatPanelSize { get; set; } = 70;

        /// <summary>
        /// InputBar panel size as percentage (0-100)
        /// </summary>
        public int InputBarPanelSize { get; set; } = 30;

        /// <summary>
        /// Whether to enable automatic stick-to-bottom behavior in chat
        /// </summary>
        public bool StickToBottomEnabled { get; set; } = true;
    }
}