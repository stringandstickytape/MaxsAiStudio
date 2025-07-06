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
        /// ChatSpace width setting (sm, md, lg, xl, 2xl, 3xl, 4xl, 5xl, 6xl, 7xl, full)
        /// </summary>
        public string ChatSpaceWidth { get; set; } = "3xl";
    }
}