// AiStudio4/MainWindow.xaml.cs
using AiStudio4.Core.Interfaces;
using AiStudio4.Core.Tools.CodeDiff.FileOperationHandlers;
using AiStudio4.Core.Tools.CodeDiff.Models;
using AiStudio4.Dialogs; // Added for WpfInputDialog
using AiStudio4.InjectedDependencies;
using AiStudio4.Services;
using AiStudio4.Services.Interfaces; // Added for IDotNetProjectAnalyzerService
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
using AiStudio4.Core.Services;
using static RoslynHelper;

namespace AiStudio4;

public partial class WebViewWindow : Window
{
    private readonly WindowManager _windowManager;
    private readonly IMcpService _mcpService;
    private readonly IGeneralSettingsService _generalSettingsService;
    private readonly IAppearanceSettingsService _appearanceSettingsService;
    private readonly IProjectHistoryService _projectHistoryService;
    private readonly IBuiltinToolService _builtinToolService;
    private readonly IAudioTranscriptionService _audioTranscriptionService; // Add field
    private readonly IWebSocketNotificationService _notificationService;
    private readonly IProjectPackager _projectPackager;
    private readonly ILogger<WebViewWindow> _logger;
    private readonly IDotNetProjectAnalyzerService _dotNetProjectAnalyzerService;
    private readonly IProjectFileWatcherService _projectFileWatcherService;
    private readonly string _licensesJsonPath;
    private readonly string _nugetLicense1Path;
    private readonly string _nugetLicense2Path;
    private string _lastTranscriptionResult = null;

    public WebViewWindow(WindowManager windowManager, IMcpService mcpService, IGeneralSettingsService generalSettingsService, IAppearanceSettingsService appearanceSettingsService, IProjectHistoryService projectHistoryService, IBuiltinToolService builtinToolService, IAudioTranscriptionService audioTranscriptionService, IWebSocketNotificationService notificationService, IProjectPackager projectPackager, IDotNetProjectAnalyzerService dotNetProjectAnalyzerService, IProjectFileWatcherService projectFileWatcherService, ILogger<WebViewWindow> logger)
    {
        _windowManager = windowManager;
        _mcpService = mcpService;
        _generalSettingsService = generalSettingsService;
        _appearanceSettingsService = appearanceSettingsService;
        _projectHistoryService = projectHistoryService;
        _builtinToolService = builtinToolService;
        _audioTranscriptionService = audioTranscriptionService;
        _notificationService = notificationService; // Assign injected service
        _projectPackager = projectPackager;
        _logger = logger;
        _dotNetProjectAnalyzerService = dotNetProjectAnalyzerService;
        _projectFileWatcherService = projectFileWatcherService;
        
        // Initialize license file paths
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        _licensesJsonPath = Path.Combine(baseDir, "AiStudioClient", "dist", "licenses.txt");
        _nugetLicense1Path = Path.Combine(baseDir, "app-nuget-license.txt");
        _nugetLicense2Path = Path.Combine(baseDir, "sharedclasses-nuget-license.txt");
        
        InitializeComponent();
        UpdateWindowTitle(); // Set initial window title
        UpdateRecentProjectsMenu(); // Populate recent projects menu
        UpdateAllowConnectionsOutsideLocalhostMenuItem(); // Set initial checkbox state
        UpdateUseExperimentalCostTrackingMenuItem(); // <-- Add this
        webView.Initialize(_generalSettingsService.CurrentSettings.AllowConnectionsOutsideLocalhost);
        _generalSettingsService.SettingsChanged += OnGeneralSettingsChanged;
    }

    private void OnGeneralSettingsChanged(object sender, EventArgs e)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            // This ensures all menu items reflecting settings are updated
            UpdateWindowTitle();
            UpdateAllowConnectionsOutsideLocalhostMenuItem();
            UpdateUseExperimentalCostTrackingMenuItem(); // <-- Add this
        });
    }

    protected override void OnClosed(EventArgs e)
    {
        if (_generalSettingsService != null)
        {
            _generalSettingsService.SettingsChanged -= OnGeneralSettingsChanged;
        }
        base.OnClosed(e);
    }

    private void UpdateWindowTitle()
    {
        // Ensure ProjectPath is not null or empty before displaying
        var projectPathDisplay = string.IsNullOrWhiteSpace(_generalSettingsService.CurrentSettings.ProjectPath)
            ? "[Project Path Not Set]"
            : _generalSettingsService.CurrentSettings.ProjectPath;
        this.Title = $"AiStudio4 - {projectPathDisplay}";
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
            InitialDirectory = _generalSettingsService.CurrentSettings.ProjectPath ?? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
        };
        if (dialog.ShowDialog() == true)
        {
            try
            {
                string selectedPath = dialog.FolderName;
                string oldPath = _generalSettingsService.CurrentSettings.ProjectPath;
                
                // Only update if the path actually changed
                if (selectedPath != oldPath)
                {
                    _generalSettingsService.CurrentSettings.ProjectPath = selectedPath;
                    _projectHistoryService.AddProjectPathToHistory(selectedPath);
                    _generalSettingsService.SaveSettings();
                    _projectHistoryService.SaveSettings();
                    
                    // Make sure to update the project root in all tools
                    _builtinToolService.UpdateProjectRoot();
                    
                    UpdateWindowTitle(); // Update title bar after changing the path
                    UpdateRecentProjectsMenu(); // Update the recent projects menu
                }
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
        var history = _projectHistoryService.GetProjectPathHistory();

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
                string oldPath = _generalSettingsService.CurrentSettings.ProjectPath;
                
                // Only update if the path actually changed
                if (selectedPath != oldPath)
                {
                    _generalSettingsService.CurrentSettings.ProjectPath = selectedPath;
                    _projectHistoryService.AddProjectPathToHistory(selectedPath);
                    _generalSettingsService.SaveSettings();
                    _projectHistoryService.SaveSettings();
                    
                    // Make sure to update the project root in all tools
                    _builtinToolService.UpdateProjectRoot();
                    
                    UpdateWindowTitle();
                    UpdateRecentProjectsMenu();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error setting project path from history: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
    
    private void ExploreProjectMenuItem_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            string projectPath = _generalSettingsService.CurrentSettings.ProjectPath;
            if (string.IsNullOrWhiteSpace(projectPath))
            {
                MessageBox.Show("Project path is not set.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (Directory.Exists(projectPath))
            {
                Process.Start("explorer.exe", projectPath);
            }
            else
            {
                MessageBox.Show($"Project directory does not exist: {projectPath}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error opening project folder: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    private void SetCondaPathMenuItem_Click(object sender, RoutedEventArgs e)
    {
        string currentKey = _generalSettingsService.CurrentSettings.CondaPath ?? string.Empty;
        string prompt = "Enter conda path here, eg C:\\Users\\username\\miniconda3\\Scripts";
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
                    _generalSettingsService.UpdateCondaPath(newKey);
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
        string currentKey = _generalSettingsService.CurrentSettings.YouTubeApiKey ?? string.Empty;
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
                    _generalSettingsService.UpdateYouTubeApiKey(newKey);
                    MessageBox.Show("YouTube API Key updated successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error saving YouTube API Key: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
    
    private void SetGitHubApiKeyMenuItem_Click(object sender, RoutedEventArgs e)
    {
        string currentKey = _generalSettingsService.CurrentSettings.GitHubApiKey ?? string.Empty;
        string prompt = "Enter your GitHub API Key:";
        string title = "Set GitHub API Key";

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
                    _generalSettingsService.UpdateGitHubApiKey(newKey);
                    MessageBox.Show("GitHub API Key updated successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error saving GitHub API Key: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
    
    private void SetAzureDevOpsPATMenuItem_Click(object sender, RoutedEventArgs e)
    {
        string currentPAT = _generalSettingsService.CurrentSettings.AzureDevOpsPAT ?? string.Empty;
        string prompt = "Enter your Azure DevOps Personal Access Token:";
        string title = "Set Azure DevOps PAT";

        var dialog = new WpfInputDialog(title, prompt, currentPAT)
        {
            Owner = this // Set the owner to center the dialog over the main window
        };

        if (dialog.ShowDialog() == true)
        {
            string newPAT = dialog.ResponseText;

            // Check if the PAT actually changed
            if (newPAT != currentPAT)
            {
                try
                {
                    _generalSettingsService.UpdateAzureDevOpsPAT(newPAT);
                    MessageBox.Show("Azure DevOps PAT updated successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error saving Azure DevOps PAT: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }

    private void TestAudioTranscriptionMenuItem_Click(object sender, RoutedEventArgs e)
    {
        Task.Run(async () =>
        {
            string audioExtensions = "*.wav;*.mp3;*.m4a;*.aac;*.ogg;*.oga;*.opus;*.flac;*.wma;*.aiff;*.aif;*.ape;*.ac3;*.dts;*.tta";
            string videoExtensions = "*.mp4;*.mkv;*.avi;*.mov;*.wmv;*.flv;*.webm;*.mpg;*.mpeg;*.m2ts;*.ts;*.vob;*.3gp;*.3g2;*.ogv;*.m4v;*.asf";
            string allSupportedExtensions = $"{audioExtensions};{videoExtensions}";

            OpenFileDialog openFileDialog = null;
            bool? dialogResult = null;
            Dispatcher.Invoke(() =>
            {
                openFileDialog = new OpenFileDialog
                {
                    Title = "Select Audio or Video File",
                    Filter = $"All Supported Media|{allSupportedExtensions}|" +
                             $"Audio Files|{audioExtensions}|" +
                             $"Video Files|{videoExtensions}|" +
                             "All Files|*.*",
                    FilterIndex = 1,
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic),
                    Multiselect = false
                };
                dialogResult = openFileDialog.ShowDialog();
            });

            if (dialogResult != true)
                return;

            var filename = openFileDialog.FileName;
            var condaActivateScriptPath = _generalSettingsService.CurrentSettings.CondaPath;
            string condaPath = Path.Combine(condaActivateScriptPath, "activate.bat");

            if (!File.Exists(condaPath))
            {
                Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"Conda activate script not found in the folder {condaPath}{Environment.NewLine}You can set the path in Edit -> Settings.");
                });
                return;
            }

            var destPath = Path.GetDirectoryName(filename);
            //destPath = destPath.Replace("\\", "/");
            if (destPath.EndsWith("/") || destPath.EndsWith("\\"))
                destPath = destPath.Substring(0, destPath.Length - 1);

            string arguments = $"/C {condaPath} && conda activate whisperx && whisperx \"{filename}\"  --language en --model  large-v3 --output_dir \"{destPath}\" ";

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
                process.OutputDataReceived += (sender2, e2) => Debug.WriteLine(e2.Data);
                process.ErrorDataReceived += (sender2, e2) => Debug.WriteLine(e2.Data);

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();

                if (process.ExitCode == 0)
                {
                    var filenameOnly = filename.Split('\\').Last();
                    if (filenameOnly.Contains("."))
                        filenameOnly = filenameOnly.Substring(0, filenameOnly.LastIndexOf('.')) + ".vtt";
                    else
                        filenameOnly += ".json";
                    var fullFilename = Path.Combine(Path.GetDirectoryName(filename)!, filenameOnly);
                    var json = File.ReadAllText(fullFilename);
                    _lastTranscriptionResult = json;
                    Dispatcher.Invoke(() =>
                    {
                        InsertTranscriptionMenuItem.IsEnabled = true;
                        MessageBox.Show(this, "Transcription complete and ready to be inserted.", "Transcription Ready", MessageBoxButton.OK, MessageBoxImage.Information);
                    });
                }
                else
                {
                    _lastTranscriptionResult = null;
                    Dispatcher.Invoke(() =>
                    {
                        InsertTranscriptionMenuItem.IsEnabled = false;
                        MessageBox.Show(this, "Unable to transcribe file.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    });
                }
            }
        });
    }

    private void AllowConnectionsOutsideLocalhostMenuItem_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Toggle the setting
            bool currentValue = _generalSettingsService.CurrentSettings.AllowConnectionsOutsideLocalhost;
            _generalSettingsService.CurrentSettings.AllowConnectionsOutsideLocalhost = !currentValue;
            _generalSettingsService.SaveSettings();
            
            // Update the menu item
            UpdateAllowConnectionsOutsideLocalhostMenuItem();
            
            // Show a message to restart the application
            MessageBox.Show(
                "The server connection setting has been changed. Please restart the application for the changes to take effect.",
                "Restart Required",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error updating connection setting: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    private void UpdateAllowConnectionsOutsideLocalhostMenuItem()
    {
        // Update the checkbox state based on the current setting
        if (AllowConnectionsOutsideLocalhostMenuItem != null)
        {
            AllowConnectionsOutsideLocalhostMenuItem.IsChecked = _generalSettingsService.CurrentSettings.AllowConnectionsOutsideLocalhost;
        }
    }

    private void UseExperimentalCostTrackingMenuItem_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // The IsChecked state is automatically toggled by WPF for a checkable MenuItem on click.
            // We read this new state and save it.
            bool newValue = UseExperimentalCostTrackingMenuItem.IsChecked;
            _generalSettingsService.UpdateUseExperimentalCostTracking(newValue);
            // No need to manually set IsChecked here again as WPF handles it.
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error updating 'Use experimental cost tracking' setting: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void UpdateUseExperimentalCostTrackingMenuItem()
    {
        if (UseExperimentalCostTrackingMenuItem != null)
        {
            UseExperimentalCostTrackingMenuItem.IsChecked = _generalSettingsService.CurrentSettings.UseExperimentalCostTracking;
        }
    }

    private void LicensesMenuItem_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var licensesWindow = new Dialogs.LicensesWindow(_licensesJsonPath, _nugetLicense1Path, _nugetLicense2Path)
            {
                Owner = this
            };
            licensesWindow.ShowDialog();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error displaying licenses: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    private async void PackProjectSourceCode_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Validate project path
            string projectPath = _generalSettingsService.CurrentSettings.ProjectPath;
            if (string.IsNullOrWhiteSpace(projectPath))
            {
                MessageBox.Show("Project path is not set.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!Directory.Exists(projectPath))
            {
                MessageBox.Show($"Project directory does not exist: {projectPath}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Configure SaveFileDialog
            var saveFileDialog = new SaveFileDialog
            {
                Title = "Save Project Source Code Package",
                Filter = "XML Files|*.xml|All Files|*.*",
                FilterIndex = 1,
                DefaultExt = ".xml",
                FileName = $"{Path.GetFileName(projectPath)}_SourceCode.xml",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                // Get file extensions to include from settings
                var includeExtensions = _generalSettingsService.CurrentSettings.PackerIncludeFileTypes ?? new System.Collections.Generic.List<string>();

                // Define binary file extensions to exclude
                var binaryFileExtensions = new List<string>
                {
                    ".exe", ".dll", ".pdb", ".obj", ".bin", ".dat", ".zip", ".rar", ".7z", ".tar", ".gz",
                    ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".ico", ".tif", ".tiff", ".mp3", ".mp4", ".wav",
                    ".avi", ".mov", ".wmv", ".flv", ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx"
                };

                // Show progress message
                MessageBox.Show("Creating project source code package. This may take a while for large projects.", "Processing", MessageBoxButton.OK, MessageBoxImage.Information);

                // Create the package
                string xmlContent = await _projectPackager.CreatePackageAsync(projectPath, includeExtensions, binaryFileExtensions);

                // Save the XML to the selected file
                await File.WriteAllTextAsync(saveFileDialog.FileName, xmlContent);

                MessageBox.Show($"Project source code package created successfully at:\n{saveFileDialog.FileName}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error creating project source code package: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void TestReapplyMergeMenuItem_Click(object sender, RoutedEventArgs e)
    {
        // Create OpenFileDialog to browse for merge failure JSON files
        var openFileDialog = new OpenFileDialog
        {
            Title = "Select Merge Failure JSON File",
            Filter = "JSON Files|*.json|All Files|*.*",
            FilterIndex = 1,
            InitialDirectory = _generalSettingsService.CurrentSettings.ProjectPath ?? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
        };

        if (openFileDialog.ShowDialog() == true)
        {
            string mergeFailureJsonPath = openFileDialog.FileName;
            
            try
            {
                // Get required services from DI
                var app = Application.Current as App;
                var logger = app.Services.GetService(typeof(ILogger<ModifyFileHandler>)) as ILogger;
                var statusMessageService = app.Services.GetService(typeof(IStatusMessageService)) as IStatusMessageService;
                var secondaryAiService = app.Services.GetService(typeof(ISecondaryAiService)) as ISecondaryAiService;
                
                if (logger == null || statusMessageService == null || secondaryAiService == null)
                {
                    MessageBox.Show("Required services not available", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Generate a client ID for status messages
                string clientId = Guid.NewGuid().ToString();

                // Call the static method to reapply the merge failure
                var result = await AiStudio4.Core.Tools.CodeDiff.FileOperationHandlers.ModifyFileHandler.ReapplyMergeFailureAsync(
                    mergeFailureJsonPath,
                    logger,
                    statusMessageService,
                    clientId,
                    secondaryAiService);

                // Show the result
                MessageBox.Show(
                    result.Success ? "Merge successfully reapplied!" : $"Failed to reapply merge: {result.Message}",
                    result.Success ? "Success" : "Error",
                    MessageBoxButton.OK,
                    result.Success ? MessageBoxImage.Information : MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error reapplying merge: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
    
    private void SetPackerIncludeTypesMenuItem_Click(object sender, RoutedEventArgs e)
    {
        var currentList = _generalSettingsService.CurrentSettings.PackerIncludeFileTypes ?? new System.Collections.Generic.List<string>();
        string defaultValue = string.Join(",", currentList);
        var dialog = new WpfInputDialog(
            "Set Packer Include File Types",
            "Enter comma-separated file extensions to include (e.g., .cs,.xaml,.js):",
            defaultValue)
        {
            Owner = this
        };
        if (dialog.ShowDialog() == true)
        {
            var input = dialog.ResponseText;
            var list = input.Split(',')
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .Select(s => s.StartsWith(".") ? s : "." + s)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            _generalSettingsService.CurrentSettings.PackerIncludeFileTypes = list;
            _generalSettingsService.SaveSettings();
            MessageBox.Show("Packer include file types updated successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
    
    private void SetPackerExcludeFilenamesMenuItem_Click(object sender, RoutedEventArgs e)
    {
        var currentList = _generalSettingsService.CurrentSettings.PackerExcludeFilenames ?? new System.Collections.Generic.List<string>();
        string defaultValue = string.Join(Environment.NewLine, currentList);
        var dialog = new WpfInputDialog(
            "Set Packer Exclude Filenames",
            "Enter filenames to exclude (one per line, '*' is wildcard):",
            defaultValue)
        {
            Owner = this
        };
        if (dialog.ShowDialog() == true)
        {
            var input = dialog.ResponseText;
            var list = input.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            _generalSettingsService.CurrentSettings.PackerExcludeFilenames = list;
            _generalSettingsService.SaveSettings();
            MessageBox.Show("Packer exclude filenames updated successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private async void InsertTranscriptionMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(_lastTranscriptionResult))
        {
            await _notificationService.NotifyTranscription(_lastTranscriptionResult);
            MessageBox.Show(this, "Transcription inserted into the prompt.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        else
        {
            MessageBox.Show(this, "No transcription available to insert.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
    
    private async void AnalyzeDotNetProjects_Click(object sender, RoutedEventArgs e)
    {
        _logger.LogInformation("Analyze .NET Projects menu item clicked.");
        try
        {
            // Check if project path is set
            string projectRootPath = _generalSettingsService.CurrentSettings.ProjectPath;
            if (string.IsNullOrEmpty(projectRootPath))
            {
                MessageBox.Show("Project path is not set. Please set a project path first.", "Project Not Ready", MessageBoxButton.OK, MessageBoxImage.Warning);
                _logger.LogWarning("Analysis skipped: Project path not set.");
                return;
            }

            MessageBox.Show("Analysis will begin. This might take a moment.", "Analysis Starting", MessageBoxButton.OK, MessageBoxImage.Information);

            var overallResults = new StringBuilder();
            overallResults.AppendLine("DotNet Project Analysis Results:");
            overallResults.AppendLine($"Analysis Date: {DateTime.Now}");
            overallResults.AppendLine("===================================");

            // Analyze all C# files in the project
            _logger.LogInformation("Analyzing C# files in project: {ProjectPath}", projectRootPath);
            overallResults.AppendLine($"\nProject Directory: {projectRootPath}");
            overallResults.AppendLine("-----------------------------------");
            
            try
            {
                var filesWithMembers = _dotNetProjectAnalyzerService.AnalyzeProjectFiles(projectRootPath);
                
                _logger.LogInformation("Found {Count} files with members", filesWithMembers.Count);
                overallResults.AppendLine($"Found {filesWithMembers.Count} files with analyzable content");
                
                foreach (var fileWithMembers in filesWithMembers)
                {
                    overallResults.AppendLine($"\nFile: {fileWithMembers.FilePath}");
                    overallResults.AppendLine("  Members:");
                    
                    if (fileWithMembers.Members.Any())
                    {
                        var grouped = fileWithMembers.Members.GroupBy(x => x.Namespace);

                        foreach (var group in grouped)
                        {
                            overallResults.AppendLine($"    Namespace: {group.Key}");

                            foreach (var member in group)
                            {
                                overallResults.AppendLine($"        {member.Kind}: {member.Name}");
                            }
                        }
                    }
                    else
                    {
                        overallResults.AppendLine("    (No members found)");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing project files: {ErrorMessage}", ex.Message);
                overallResults.AppendLine($"Error analyzing project files: {ex.Message}");
            }
            overallResults.AppendLine("-----------------------------------");

            string outputFileName = "DotNetProjectAnalysis.txt";
            string outputFilePath = System.IO.Path.Combine(projectRootPath, outputFileName);

            // 3. Write concatenated results to the output file
            try
            {
                await System.IO.File.WriteAllTextAsync(outputFilePath, overallResults.ToString());
                _logger.LogInformation("Successfully wrote analysis results to: {OutputFilePath}", outputFilePath);
                MessageBox.Show($"Analysis complete. Results saved to:\n{outputFilePath}", "Analysis Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error writing analysis results to file: {OutputFilePath}", outputFilePath);
                MessageBox.Show($"Error saving analysis results: {ex.Message}", "File Write Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred during .NET project analysis.");
            MessageBox.Show($"An unexpected error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}