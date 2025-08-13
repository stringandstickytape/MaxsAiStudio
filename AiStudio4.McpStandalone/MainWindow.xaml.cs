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
    private const double RESPONSIVE_WIDTH_THRESHOLD = 900; // Below this width, sidebar collapses
    
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
        
        // Set initial responsive state
        UpdateNavigationViewMode();
    }

    private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateNavigationViewMode();
    }
    
    private void UpdateNavigationViewMode()
    {
        if (ActualWidth < RESPONSIVE_WIDTH_THRESHOLD)
        {
            // Narrow width - show hamburger, compact mode
            RootNavigation.PaneDisplayMode = NavigationViewPaneDisplayMode.LeftFluent;
            RootNavigation.IsPaneToggleVisible = true;
            RootNavigation.IsPaneOpen = false;
            RootNavigation.CompactPaneLength = 48;
        }
        else
        {
            // Wide width - always show sidebar, no hamburger
            RootNavigation.PaneDisplayMode = NavigationViewPaneDisplayMode.Left;
            RootNavigation.IsPaneToggleVisible = false;
            RootNavigation.IsPaneOpen = true;
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
            
            // In compact mode, close the pane after selection
            if (RootNavigation.PaneDisplayMode == NavigationViewPaneDisplayMode.LeftFluent && 
                RootNavigation.IsPaneOpen)
            {
                RootNavigation.IsPaneOpen = false;
            }
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
                case "Settings":
                    frame.Navigate(new SettingsPage() { DataContext = _viewModel });
                    break;
                case "Legacy":
                    frame.Navigate(new LegacyPage() { DataContext = _viewModel });
                    break;
            }
        }
    }
    
    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        var settingsService = _serviceProvider.GetRequiredService<StandaloneSettingsService>();
        var settingsWindow = new SettingsWindow(settingsService)
        {
            Owner = this
        };
        settingsWindow.ShowDialog();
    }
    
    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }
}