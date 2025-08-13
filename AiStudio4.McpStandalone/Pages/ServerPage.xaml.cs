using System.Windows;
using System.Windows.Controls;
using AiStudio4.McpStandalone.ViewModels;

namespace AiStudio4.McpStandalone.Pages
{
    /// <summary>
    /// Interaction logic for ServerPage.xaml
    /// </summary>
    public partial class ServerPage : Page
    {
        public ServerPage()
        {
            InitializeComponent();
        }
        
        private void CopyCommand_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel viewModel)
            {
                System.Windows.Clipboard.SetText(viewModel.ClaudeInstallCommand);
                // Could show a snackbar or tooltip here to indicate success
            }
        }
    }
}