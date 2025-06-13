// C:\Users\maxhe\source\repos\MaxsAiStudio\AiStudio4\Dialogs\LogViewerViewModel.cs
using System.Collections.ObjectModel;
using System.ComponentModel;

using System.Windows.Data;
using AiStudio4.Models;
using AiStudio4.Services.Interfaces;


namespace AiStudio4.Dialogs
{
    /// <summary>
    /// ViewModel backing the <see cref="LogViewerWindow"/>. Supports filtering by minimum level and search text.
    /// </summary>
    public class LogViewerViewModel : INotifyPropertyChanged
    {
        private readonly ILogViewerService _logViewerService;
        private string _searchText = string.Empty;
        private LogLevel _selectedLogLevel = LogLevel.Trace;

        public ObservableCollection<LogEntry> LogEntries { get; }
        public ICollectionView LogEntriesView { get; }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText == value) return;
                _searchText = value;
                OnPropertyChanged(nameof(SearchText));
                LogEntriesView.Refresh();
            }
        }

        public LogLevel[] LogLevels { get; } = (LogLevel[])System.Enum.GetValues(typeof(LogLevel));

        public LogLevel SelectedLogLevel
        {
            get => _selectedLogLevel;
            set
            {
                if (_selectedLogLevel == value) return;
                _selectedLogLevel = value;
                OnPropertyChanged(nameof(SelectedLogLevel));
                LogEntriesView.Refresh();
            }
        }

        public LogViewerViewModel(ILogViewerService logViewerService)
        {
            _logViewerService = logViewerService;
            LogEntries = new ObservableCollection<LogEntry>(_logViewerService.GetLogHistory());
            LogEntriesView = CollectionViewSource.GetDefaultView(LogEntries);
            LogEntriesView.Filter = Filter;

            _logViewerService.OnLogReceived += OnLogReceived;
        }

        private void OnLogReceived(LogEntry entry)
        {
            LogEntries.Add(entry);
        }

        private bool Filter(object obj)
        {
            if (obj is not LogEntry entry) return false;
            if (entry.Level < SelectedLogLevel) return false;
            if (string.IsNullOrWhiteSpace(SearchText)) return true;
            return entry.Message.Contains(SearchText, System.StringComparison.OrdinalIgnoreCase) ||
                   entry.Category.Contains(SearchText, System.StringComparison.OrdinalIgnoreCase);
        }

        public void Clear()
        {
            LogEntries.Clear();
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        #endregion

        ~LogViewerViewModel()
        {
            _logViewerService.OnLogReceived -= OnLogReceived;
        }
    }
}
