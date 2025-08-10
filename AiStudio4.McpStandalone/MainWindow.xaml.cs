using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Wpf.Ui.Controls;
using AiStudio4.McpStandalone.ViewModels;

namespace AiStudio4.McpStandalone;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : FluentWindow
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void CopyClaudeCommand_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Clipboard.SetText("claude mcp add --transport http McpStandalone http://localhost:7071/");
            // Optionally show a snackbar or notification that it was copied
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Failed to copy to clipboard: {ex.Message}", "Copy Error", 
                System.Windows.MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}