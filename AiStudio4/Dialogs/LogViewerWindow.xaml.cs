// C:\Users\maxhe\source\repos\MaxsAiStudio\AiStudio4\Dialogs\LogViewerWindow.xaml.cs
using System.Windows;

namespace AiStudio4.Dialogs
{
    /// <summary>
    /// Interaction logic for LogViewerWindow.xaml
    /// </summary>
    public partial class LogViewerWindow : Window
    {
        private readonly LogViewerViewModel _viewModel;

        public LogViewerWindow(LogViewerViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.Clear();
        }
    }
}