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
using AiStudio4.McpStandalone.Views;
using AiStudio4.McpStandalone.Services;
using AiStudio4.McpStandalone.Pages;
using Microsoft.Extensions.DependencyInjection;

namespace AiStudio4.McpStandalone;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : FluentWindow
{
    private readonly IServiceProvider _serviceProvider;
    private readonly StandaloneSettingsService _settingsService;
    private readonly MainViewModel _viewModel;
    
    public MainWindow(MainViewModel viewModel, IServiceProvider serviceProvider, StandaloneSettingsService settingsService)
    {
        InitializeComponent();
        DataContext = viewModel;
        _viewModel = viewModel;
        _serviceProvider = serviceProvider;
        _settingsService = settingsService;
        
        // Subscribe to NavigationView events
        RootNavigation.SelectionChanged += NavigationView_SelectionChanged;
        
        // Navigate to the first page (Server page)
        if (RootNavigation.ContentOverlay is System.Windows.Controls.Frame frame)
        {
            frame.Navigate(new ServerPage() { DataContext = _viewModel });
        }
    }

    private void NavigationView_SelectionChanged(object sender, RoutedEventArgs e)
    {
        if (sender is NavigationView navigationView && 
            navigationView.SelectedItem is NavigationViewItem navigationItem)
        {
            var tag = navigationItem.Tag?.ToString();
            NavigateToPage(tag);
        }
    }
    
    private void NavigationItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is NavigationViewItem navigationItem)
        {
            var tag = navigationItem.Tag?.ToString();
            NavigateToPage(tag);
        }
    }
    
    private void NavigateToPage(string? pageTag)
    {
        if (RootNavigation.ContentOverlay is System.Windows.Controls.Frame frame)
        {
            switch (pageTag)
            {
                case "Server":
                    frame.Navigate(new ServerPage() { DataContext = _viewModel });
                    break;
                case "Tools":
                    frame.Navigate(new ToolsPage() { DataContext = _viewModel });
                    break;
                case "Settings":
                    frame.Navigate(new SettingsPage() { DataContext = _viewModel });
                    break;
            }
        }
    }
    
    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }
}