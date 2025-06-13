// C:\Users\maxhe\source\repos\MaxsAiStudio\AiStudio4\Models\LogEntry.cs



namespace AiStudio4.Models
{
    /// <summary>
    /// Represents a single application log entry that can be displayed in the in-app log viewer.
    /// </summary>
    public class LogEntry
    {
        public DateTime Timestamp { get; set; }
        public LogLevel Level { get; set; }
        public string LevelString => Level.ToString();
        public string Category { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? ExceptionMessage { get; set; }
    }
}
