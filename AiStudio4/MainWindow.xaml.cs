using AiStudio4.Core.Interfaces;
using AiStudio4.InjectedDependencies;
using System;
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

namespace AiStudio4;

public partial class WebViewWindow : Window
{
    private readonly WindowManager _windowManager;
    private readonly IMcpService _mcpService;

    public WebViewWindow(WindowManager windowManager, IMcpService mcpService)
    {
        _windowManager = windowManager;
        _mcpService = mcpService;
        InitializeComponent();
        webView.Initialize();
    }

    private async void McpServersMenuItem_Loaded(object sender, RoutedEventArgs e)
    {
        await RefreshMcpServersMenuAsync();
    }
    private async Task RefreshMcpServersMenuAsync()
    {
        // Clear existing items
        McpServersMenuItem.Items.Clear();
        // Get all server definitions
        var serverDefinitions = await _mcpService.GetAllServerDefinitionsAsync();
        // Sort by name
        var sortedDefinitions = serverDefinitions.OrderBy(d => d.Name).ToList();
        // Add a menu item for each server definition
        foreach (var definition in sortedDefinitions)
        {
            var menuItem = new MenuItem
            {
                Header = definition.Name,
                IsCheckable = true,
                IsChecked = definition.IsEnabled,
                Tag = definition.Id // Store the ID for reference when clicked
            };
            menuItem.Click += McpServerMenuItem_Click;
            McpServersMenuItem.Items.Add(menuItem);
        }
        // Add separator and refresh option
        if (sortedDefinitions.Any())
        {
            McpServersMenuItem.Items.Add(new Separator());
        }
        var refreshMenuItem = new MenuItem { Header = "Refresh List" };
        refreshMenuItem.Click += async (s, e) => await RefreshMcpServersMenuAsync();
        McpServersMenuItem.Items.Add(refreshMenuItem);
    }
    private async void McpServerMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem menuItem && menuItem.Tag is string serverId)
        {
            try
            {
                // Get the current definition
                var definition = await _mcpService.GetServerDefinitionByIdAsync(serverId);
                if (definition != null)
                {
                    // Toggle the IsEnabled property
                    definition.IsEnabled = !definition.IsEnabled;
                    // Update the definition
                    await _mcpService.UpdateServerDefinitionAsync(definition);
                    // Update the menu item
                    menuItem.IsChecked = definition.IsEnabled;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error toggling server status: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
    private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }
}