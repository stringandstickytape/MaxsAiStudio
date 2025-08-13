using System.Windows;
using System.Windows.Controls;
using AiStudio4.McpStandalone.ViewModels;

namespace AiStudio4.McpStandalone.Pages
{
    /// <summary>
    /// Interaction logic for ToolsPage.xaml
    /// </summary>
    public partial class ToolsPage : Page
    {
        public ToolsPage()
        {
            InitializeComponent();
        }
        
        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel viewModel)
            {
                foreach (var tool in viewModel.AvailableTools)
                {
                    tool.IsSelected = true;
                }
            }
        }
        
        private void SelectNone_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel viewModel)
            {
                foreach (var tool in viewModel.AvailableTools)
                {
                    tool.IsSelected = false;
                }
            }
        }
    }
}