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
using System.Threading.Tasks;
using System.Linq;
using System.IO;
using System.Diagnostics;

namespace AiStudio4;

public partial class WebViewWindow : Window
{
    private readonly WindowManager _windowManager;
    private readonly IMcpService _mcpService;
    private readonly ISettingsService _settingsService; // Added ISettingsService field
    private readonly IBuiltinToolService _builtinToolService;
    private readonly IAudioTranscriptionService _audioTranscriptionService; // Add field
    private readonly IWebSocketNotificationService _notificationService;

    public WebViewWindow(WindowManager windowManager, IMcpService mcpService, ISettingsService settingsService, IBuiltinToolService builtinToolService, IAudioTranscriptionService audioTranscriptionService, IWebSocketNotificationService notificationService) // Added service parameter
    {
        _windowManager = windowManager;
        _mcpService = mcpService;
        _settingsService = settingsService; // Assign injected service
        _builtinToolService = builtinToolService;
        _audioTranscriptionService = audioTranscriptionService;
        _notificationService = notificationService; // Assign injected service
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
    private void SetCondaPathMenuItem_Click(object sender, RoutedEventArgs e)
    {
        string currentKey = _settingsService.DefaultSettings?.CondaPath ?? string.Empty;
        string prompt = "Enter conda path here, eg C:\\Users\\username\\miniconda3\\Scripts\\conda.exe:";
        string title = "Set conda path";

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
                    _settingsService.UpdateCondaPath(newKey);
                    MessageBox.Show("Conda path updated successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Conda path YouTube API Key: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
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
        string audioExtensions = "*.wav;*.mp3;*.m4a;*.aac;*.ogg;*.oga;*.opus;*.flac;*.wma;*.aiff;*.aif;*.ape;*.ac3;*.dts;*.tta";
        string videoExtensions = "*.mp4;*.mkv;*.avi;*.mov;*.wmv;*.flv;*.webm;*.mpg;*.mpeg;*.m2ts;*.ts;*.vob;*.3gp;*.3g2;*.ogv;*.m4v;*.asf";
        string allSupportedExtensions = $"{audioExtensions};{videoExtensions}";

        var openFileDialog = new OpenFileDialog
        {
            Title = "Select Audio or Video File",

            Filter = $"All Supported Media|{allSupportedExtensions}|" +
                     $"Audio Files|{audioExtensions}|" +
                     $"Video Files|{videoExtensions}|" +
                     "All Files|*.*",

            FilterIndex = 1,
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic), // Or MyVideos
            Multiselect = false
        };

        var result = openFileDialog.ShowDialog();

        if(result.Value)
        {
            var filename = openFileDialog.FileName;

            var condaActivateScriptPath = _settingsService.DefaultSettings.CondaPath;
            
                        // Path to the Miniconda installation
            string condaPath = Path.Combine(condaActivateScriptPath, "activate.bat");

            if (!File.Exists(condaPath))
            {
                MessageBox.Show($"Conda activate script not found at {condaPath}{Environment.NewLine}You can set the path in Edit -> Settings.");
            }

            // Command to activate the WhisperX environment and run Whisper
            string arguments = $"/C {condaPath} && conda activate whisperx && whisperx \"{filename}\"  --language en --model  large-v3 --output_dir \"{Path.GetDirectoryName(filename)}\" ";

            //if(!string.IsNullOrEmpty(hfToken))
            //{
            //    arguments += $"--hf_token {hfToken} --diarize ";
            //}
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (Process process = new Process())
            {
                process.StartInfo = startInfo;
                process.OutputDataReceived += (sender, e) => Debug.WriteLine(e.Data);
                process.ErrorDataReceived += (sender, e) => Debug.WriteLine(e.Data);

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();

                if (process.ExitCode == 0)
                {
                    var filenameOnly = filename.Split('\\').Last();

                    if (filenameOnly.Contains("."))
                    {
                        filenameOnly = filenameOnly.Substring(0, filenameOnly.LastIndexOf('.')) + ".vtt";
                    }
                    else
                    {
                        filenameOnly += ".json";
                    }
                    var fullFilename = Path.Combine(Path.GetDirectoryName(filename)!, filenameOnly);

                    var json = File.ReadAllText(fullFilename);
                    await _notificationService.NotifyTranscription(json);
                }
                else
                {
                    MessageBox.Show("Unable to transcribe file.");
                }
            }
        }
    }
}