using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Controls;
using Microsoft.VisualStudio.Shell;

namespace VSIXTest
{
    public class VsixDebugLog
    {
        private static VsixDebugLog _instance;
        private readonly ObservableCollection<string> _logMessages;

        public event EventHandler<string> LogMessageAdded;

        public static VsixDebugLog Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new VsixDebugLog(); 
                }
                return _instance;
            }
        }

        private VsixDebugLog()
        {
            _logMessages = new ObservableCollection<string>();
        }

        public ReadOnlyObservableCollection<string> LogMessages
        {
            get { return new ReadOnlyObservableCollection<string>(_logMessages); }
        }

        public void Log(string message)
        {
            string formattedMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}";
            _logMessages.Add(formattedMessage);
            LogMessageAdded?.Invoke(this, formattedMessage);
        }
    }
}