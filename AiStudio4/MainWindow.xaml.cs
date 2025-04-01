using AiStudio4.Core.Interfaces;
using AiStudio4.InjectedDependencies;
using Microsoft.Win32; // Added for OpenFolderDialog
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
    private readonly ISettingsService _settingsService; // Added ISettingsService field

    public WebViewWindow(WindowManager windowManager, IMcpService mcpService, ISettingsService settingsService) // Added ISettingsService parameter
    {
        _windowManager = windowManager;
        _mcpService = mcpService;
        _settingsService = settingsService; // Assign injected service
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
    private void SetProjectPathMenuItem_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Select Project Path",
            InitialDirectory = _settingsService.CurrentSettings.ProjectPath ?? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
        };
        if (dialog.ShowDialog() == true)
        {
            try
            {
                string selectedPath = dialog.FolderName;
                _settingsService.CurrentSettings.ProjectPath = selectedPath;
                _settingsService.SaveSettings();
                MessageBox.Show($"Project path updated to: {selectedPath}", "Project Path Set", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error setting project path: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}