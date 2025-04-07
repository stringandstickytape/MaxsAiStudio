// AiStudio4/MainWindow.xaml.cs
using AiStudio4.Core.Interfaces;
using AiStudio4.Dialogs; // Added for WpfInputDialog
using AiStudio4.InjectedDependencies;
using AiStudio4.Services;
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
using System.Threading.Tasks;
using System.Linq;
using System.IO;

namespace AiStudio4;

public partial class WebViewWindow : Window
{
    private readonly WindowManager _windowManager;
    private readonly IMcpService _mcpService;
    private readonly ISettingsService _settingsService; // Added ISettingsService field
    private readonly IBuiltinToolService _builtinToolService;
    private readonly IAudioTranscriptionService _audioTranscriptionService; // Add field

    public WebViewWindow(WindowManager windowManager, IMcpService mcpService, ISettingsService settingsService, IBuiltinToolService builtinToolService, IAudioTranscriptionService audioTranscriptionService) // Added service parameter
    {
        _windowManager = windowManager;
        _mcpService = mcpService;
        _settingsService = settingsService; // Assign injected service
        _builtinToolService = builtinToolService;
        _audioTranscriptionService = audioTranscriptionService;
        InitializeComponent();
        UpdateWindowTitle(); // Set initial window title
        UpdateRecentProjectsMenu(); // Populate recent projects menu
        webView.Initialize();
    }
    private void UpdateWindowTitle()
    {
        // Ensure ProjectPath is not null or empty before displaying
        var projectPathDisplay = string.IsNullOrWhiteSpace(_settingsService.CurrentSettings.ProjectPath)
            ? "[Project Path Not Set]"
            : _settingsService.CurrentSettings.ProjectPath;
        this.Title = $"AiStudio4 - {projectPathDisplay}";
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
                _settingsService.AddProjectPathToHistory(selectedPath); // Add to history
                _settingsService.SaveSettings();
                _builtinToolService.UpdateProjectRoot();
                UpdateWindowTitle(); // Update title bar after changing the path
                UpdateRecentProjectsMenu(); // Update the recent projects menu
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error setting project path: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void UpdateRecentProjectsMenu()
    {
        RecentProjectsMenuItem.Items.Clear();
        var history = _settingsService.CurrentSettings.ProjectPathHistory;

        if (history == null || !history.Any())
        {
            RecentProjectsMenuItem.IsEnabled = false;
            return;
        }

        RecentProjectsMenuItem.IsEnabled = true;
        for (int i = 0; i < history.Count; i++)
        {
            var path = history[i];
            var menuItem = new MenuItem
            {
                Header = FormatPathForMenu(path, i + 1),
                Tag = path // Store the full path
            };
            menuItem.Click += RecentProjectPathMenuItem_Click;
            RecentProjectsMenuItem.Items.Add(menuItem);
        }
    }

    private string FormatPathForMenu(string path, int index)
    {
        // Simple formatting, potentially shorten long paths
        const int maxLength = 50;
        string displayPath = path.Length > maxLength ? "..." + path.Substring(path.Length - maxLength) : path;
        return $"_{index} {displayPath}"; // Add accelerator key
    }

    private void RecentProjectPathMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem menuItem && menuItem.Tag is string selectedPath)
        {
            try
            {
                _settingsService.CurrentSettings.ProjectPath = selectedPath;
                _settingsService.AddProjectPathToHistory(selectedPath); // Move to top of history
                _settingsService.SaveSettings();
                _builtinToolService.UpdateProjectRoot();
                UpdateWindowTitle();
                UpdateRecentProjectsMenu();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error setting project path from history: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void SetYouTubeApiKeyMenuItem_Click(object sender, RoutedEventArgs e)
    {
        string currentKey = _settingsService.DefaultSettings?.YouTubeApiKey ?? string.Empty;
        string prompt = "Enter your YouTube Data API v3 Key:";
        string title = "Set YouTube API Key";

        var dialog = new WpfInputDialog(title, prompt, currentKey)
        {
            Owner = this // Set the owner to center the dialog over the main window
        };

        if (dialog.ShowDialog() == true)
        {
            string newKey = dialog.ResponseText;

            // Check if the key actually changed
            if (newKey != currentKey)
            {
                try
                {
                    _settingsService.UpdateYouTubeApiKey(newKey);
                    MessageBox.Show("YouTube API Key updated successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error saving YouTube API Key: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }

    private async void TestAudioTranscriptionMenuItem_Click(object sender, RoutedEventArgs e)
    {
        var openFileDialog = new OpenFileDialog
        {
            Title = "Select Audio File for Transcription",
            Filter = "Audio Files|*.wav;*.mp3;*.m4a;*.ogg;*.flac|All Files|*.*", // Add more formats if needed
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic)
        };

        if (openFileDialog.ShowDialog() == true)
        {
            string filePath = openFileDialog.FileName;
            try
            {
                // TODO: Add a loading indicator if transcription takes time
                MessageBox.Show($"Attempting to transcribe: {System.IO.Path.GetFileName(filePath)}", "Transcription Started", MessageBoxButton.OK, MessageBoxImage.Information);

                using var filestream = new FileStream(filePath, FileMode.Open);
                string transcription = await _audioTranscriptionService.TranscribeAudioAsync(filestream); // only accepts wavs...

                if (!string.IsNullOrEmpty(transcription))
                {
                    // Display the transcription in a scrollable text box or a dedicated window for longer text
                    // For simplicity, using MessageBox here, but consider a better UI for long results.
                    var resultWindow = new Window
                    {
                        Title = "Transcription Result",
                        Content = new ScrollViewer { Content = new TextBox { Text = transcription, IsReadOnly = true, TextWrapping = TextWrapping.Wrap, VerticalScrollBarVisibility = ScrollBarVisibility.Auto } },
                        Width = 600,
                        Height = 400,
                        Owner = this,
                        WindowStartupLocation = WindowStartupLocation.CenterOwner
                    };
                    resultWindow.ShowDialog();
                }
                else
                {
                    MessageBox.Show("Transcription returned empty.", "Transcription Result", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (FileNotFoundException fnfEx)
            {
                MessageBox.Show($"Error: The audio file was not found.\n{fnfEx.Message}", "File Not Found Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred during transcription: {ex.Message}", "Transcription Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}