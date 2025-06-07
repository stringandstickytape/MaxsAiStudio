// AiStudio4/Dialogs/ManageProjectsDialog.xaml.cs
using AiStudio4.Core.Interfaces;
using AiStudio4.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Globalization;
using AiStudio4.Core.Tools.CodeDiff.FileOperationHandlers;

namespace AiStudio4.Dialogs
{
    public partial class ManageProjectsDialog : Window
    {
        private readonly IProjectService _projectService;
        private readonly ILogger<ManageProjectsDialog> _logger;
        private readonly ObservableCollection<Project> _projects;

        public ManageProjectsDialog(IServiceProvider serviceProvider)
        {
            InitializeComponent();

            _projectService = serviceProvider.GetRequiredService<IProjectService>();
            _logger = serviceProvider.GetRequiredService<ILogger<ManageProjectsDialog>>();
            _projects = new ObservableCollection<Project>();

            ProjectsDataGrid.ItemsSource = _projects;

            Loaded += ManageProjectsDialog_Loaded;
        }

        private async void ManageProjectsDialog_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadProjectsAsync();
        }

        private async Task LoadProjectsAsync()
        {
            try
            {
                StatusTextBlock.Text = "Loading projects...";

                var projects = await _projectService.GetAllProjectsAsync();
                _projects.Clear();

                foreach (var project in projects)
                {
                    _projects.Add(project);
                }

                var activeProject = await _projectService.GetActiveProjectAsync();
                if (activeProject != null)
                {
                    StatusTextBlock.Text = $"Active project: {activeProject.Name}";
                }
                else
                {
                    StatusTextBlock.Text = "No active project";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading projects");
                StatusTextBlock.Text = "Error loading projects";
                MessageBox.Show($"Error loading projects: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void AddButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new ProjectEditorDialog((Application.Current as App).Services);
                if (dialog.ShowDialog() == true)
                {
                    await LoadProjectsAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding project");
                MessageBox.Show($"Error adding project: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void EditButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ProjectsDataGrid.SelectedItem is Project selectedProject)
                {
                    var dialog = new ProjectEditorDialog((Application.Current as App).Services, selectedProject);
                    if (dialog.ShowDialog() == true)
                    {
                        await LoadProjectsAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error editing project");
                MessageBox.Show($"Error editing project: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ProjectsDataGrid.SelectedItem is Project selectedProject)
                {
                    var result = MessageBox.Show(
                        $"Are you sure you want to delete the project '{selectedProject.Name}'?\\n\\nThis action cannot be undone.",
                        "Confirm Delete",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);

                    if (result == MessageBoxResult.Yes)
                    {
                        StatusTextBlock.Text = "Deleting project...";
                        var success = await _projectService.DeleteProjectAsync(selectedProject.Guid);

                        if (success)
                        {
                            await LoadProjectsAsync();
                        }
                        else
                        {
                            StatusTextBlock.Text = "Failed to delete project";
                            MessageBox.Show("Failed to delete project", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting project");
                StatusTextBlock.Text = "Error deleting project";
                MessageBox.Show($"Error deleting project: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void SetActiveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ProjectsDataGrid.SelectedItem is Project selectedProject)
                {
                    StatusTextBlock.Text = "Setting active project...";
                    var success = await _projectService.SetActiveProjectAsync(selectedProject.Guid);

                    if (success)
                    {
                        StatusTextBlock.Text = $"Active project: {selectedProject.Name}";
                        MessageBox.Show($"'{selectedProject.Name}' is now the active project.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        StatusTextBlock.Text = "Failed to set active project";
                        MessageBox.Show("Failed to set active project", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting active project");
                StatusTextBlock.Text = "Error setting active project";
                MessageBox.Show($"Error setting active project: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadProjectsAsync();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }

    // Simple converter for enabling/disabling buttons based on selection
    public static class BooleanConverters
    {
        public static readonly IValueConverter NotNullToBooleanConverter = new NotNullToBooleanConverter();
    }

    public class NotNullToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value != null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}