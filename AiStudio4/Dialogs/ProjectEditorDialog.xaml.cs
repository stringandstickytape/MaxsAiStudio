// AiStudio4/Dialogs/ProjectEditorDialog.xaml.cs


using Microsoft.Extensions.DependencyInjection;

using Microsoft.Win32;




using System.Windows.Forms;

namespace AiStudio4.Dialogs
{
    public partial class ProjectEditorDialog : Window
    {
        private readonly IProjectService _projectService;
        private readonly ILogger<ProjectEditorDialog> _logger;
        private readonly Project _project;
        private readonly bool _isEditMode;

        public ProjectEditorDialog(IServiceProvider serviceProvider, Project existingProject = null)
        {
            InitializeComponent();
            
            _projectService = serviceProvider.GetRequiredService<IProjectService>();
            _logger = serviceProvider.GetRequiredService<ILogger<ProjectEditorDialog>>();
            
            _isEditMode = existingProject != null;
            _project = existingProject ?? new Project();
            
            InitializeForm();
        }

        private void InitializeForm()
        {
            if (_isEditMode)
            {
                HeaderTextBlock.Text = "Edit Project";
                Title = "Edit Project";
                
                ProjectNameTextBox.Text = _project.Name;
                ProjectPathTextBox.Text = _project.Path;
                DescriptionTextBox.Text = _project.Description;
            }
            else
            {
                HeaderTextBlock.Text = "Add New Project";
                Title = "Add New Project";
            }
            
            // Focus on the name field
            ProjectNameTextBox.Focus();
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "Select Project Directory",
                    CheckFileExists = false,
                    CheckPathExists = true,
                    FileName = "Select Folder",
                    Filter = "Folders|*.folder",
                    ValidateNames = false
                };

                // Use a folder browser dialog approach
                var folderDialog = new FolderBrowserDialog
                {
                    Description = "Select Project Directory",
                    ShowNewFolderButton = true
                };

                if (!string.IsNullOrEmpty(ProjectPathTextBox.Text) && Directory.Exists(ProjectPathTextBox.Text))
                {
                    folderDialog.SelectedPath = ProjectPathTextBox.Text;
                }

                if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    ProjectPathTextBox.Text = folderDialog.SelectedPath;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error browsing for project path");
                System.Windows.MessageBox.Show($"Error browsing for folder: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateForm())
            {
                return;
            }

            try
            {
                SaveButton.IsEnabled = false;
                SaveButton.Content = "Saving...";

                _project.Name = ProjectNameTextBox.Text.Trim();
                _project.Path = ProjectPathTextBox.Text.Trim();
                _project.Description = DescriptionTextBox.Text.Trim();

                if (_isEditMode)
                {
                    await _projectService.UpdateProjectAsync(_project);
                }
                else
                {
                    await _projectService.CreateProjectAsync(_project);
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving project");
                ShowValidationError($"Error saving project: {ex.Message}");
            }
            finally
            {
                SaveButton.IsEnabled = true;
                SaveButton.Content = "Save";
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private bool ValidateForm()
        {
            HideValidationError();

            // Validate project name
            if (string.IsNullOrWhiteSpace(ProjectNameTextBox.Text))
            {
                ShowValidationError("Project name is required.");
                ProjectNameTextBox.Focus();
                return false;
            }

            // Validate project path
            if (string.IsNullOrWhiteSpace(ProjectPathTextBox.Text))
            {
                ShowValidationError("Project path is required.");
                ProjectPathTextBox.Focus();
                return false;
            }

            var projectPath = ProjectPathTextBox.Text.Trim();
            
            // Check if path exists
            if (!Directory.Exists(projectPath))
            {
                var result = System.Windows.MessageBox.Show(
                    $"The directory '{projectPath}' does not exist. Would you like to create it?",
                    "Directory Not Found",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        Directory.CreateDirectory(projectPath);
                    }
                    catch (Exception ex)
                    {
                        ShowValidationError($"Failed to create directory: {ex.Message}");
                        ProjectPathTextBox.Focus();
                        return false;
                    }
                }
                else
                {
                    ProjectPathTextBox.Focus();
                    return false;
                }
            }

            // Check if path is accessible
            try
            {
                var testPath = Path.Combine(projectPath, "test_access.tmp");
                File.WriteAllText(testPath, "test");
                File.Delete(testPath);
            }
            catch (Exception)
            {
                ShowValidationError("The selected directory is not accessible or writable.");
                ProjectPathTextBox.Focus();
                return false;
            }

            return true;
        }

        private void ShowValidationError(string message)
        {
            ValidationTextBlock.Text = message;
            ValidationTextBlock.Visibility = Visibility.Visible;
        }

        private void HideValidationError()
        {
            ValidationTextBlock.Visibility = Visibility.Collapsed;
        }
    }
}
