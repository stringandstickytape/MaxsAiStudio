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
using Drawing = System.Drawing;
using WinForms = System.Windows.Forms;
using System.ComponentModel;

namespace AiStudio4.McpStandalone;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : FluentWindow
{
    private readonly IServiceProvider _serviceProvider;
    private readonly StandaloneSettingsService _settingsService;
    private readonly MainViewModel _viewModel;
    private WinForms.NotifyIcon? _notifyIcon;
    private bool _isExiting = false;
    
    public MainWindow(MainViewModel viewModel, IServiceProvider serviceProvider, StandaloneSettingsService settingsService)
    {
        InitializeComponent();
        DataContext = viewModel;
        _viewModel = viewModel;
        _serviceProvider = serviceProvider;
        _settingsService = settingsService;
        
        // Initialize system tray
        InitializeSystemTray();
        
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
        System.Windows.Application.Current.Shutdown();
    }
    
    private void InitializeSystemTray()
    {
        // Create context menu for system tray
        var contextMenu = new WinForms.ContextMenuStrip();
        contextMenu.Items.Add("Show", null, (s, e) => RestoreFromTray());
        contextMenu.Items.Add(new WinForms.ToolStripSeparator());
        contextMenu.Items.Add("Exit", null, (s, e) => ExitApplication());
        
        // Create notification icon
        _notifyIcon = new WinForms.NotifyIcon
        {
            Icon = Drawing.SystemIcons.Application, // You can replace with custom icon
            Text = "AiStudio4 Standalone MCP Server",
            ContextMenuStrip = contextMenu,
            Visible = false
        };
        
        // Handle double-click on tray icon
        _notifyIcon.DoubleClick += (s, e) => RestoreFromTray();
    }
    
    protected override void OnStateChanged(EventArgs e)
    {
        base.OnStateChanged(e);
        
        // Minimize to tray when window is minimized
        if (WindowState == WindowState.Minimized && !_isExiting)
        {
            Hide();
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = true;
                _notifyIcon.ShowBalloonTip(1000, "AiStudio4 MCP Server", 
                    "Application minimized to system tray", WinForms.ToolTipIcon.Info);
            }
        }
    }
    
    private void RestoreFromTray()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
        if (_notifyIcon != null)
        {
            _notifyIcon.Visible = false;
        }
    }
    
    private void ExitApplication()
    {
        _isExiting = true;
        if (_notifyIcon != null)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _notifyIcon = null;
        }
        System.Windows.Application.Current.Shutdown();
    }
    
    protected override void OnClosing(CancelEventArgs e)
    {
        // Minimize to tray instead of closing
        if (!_isExiting)
        {
            e.Cancel = true;
            WindowState = WindowState.Minimized;
        }
        else
        {
            // Clean up notify icon on actual exit
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
                _notifyIcon = null;
            }
        }
        
        base.OnClosing(e);
    }
}