// AiStudio4/Dialogs/ConfigureWikiSyncDialog.xaml.cs










namespace AiStudio4.Dialogs
{
    public partial class ConfigureWikiSyncDialog : Window
    {
        private readonly IGeneralSettingsService _generalSettingsService;
        private readonly ISystemPromptService _systemPromptService;
        private readonly ILogger<ConfigureWikiSyncDialog> _logger;
        private List<SystemPrompt> _systemPrompts;

        public ConfigureWikiSyncDialog(IGeneralSettingsService generalSettingsService, 
                                     ISystemPromptService systemPromptService,
                                     ILogger<ConfigureWikiSyncDialog> logger)
        {
            _generalSettingsService = generalSettingsService ?? throw new ArgumentNullException(nameof(generalSettingsService));
            _systemPromptService = systemPromptService ?? throw new ArgumentNullException(nameof(systemPromptService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            InitializeComponent();
            LoadDataAsync();
        }

        private async void LoadDataAsync()
        {
            try
            {
                // Load current settings
                var settings = _generalSettingsService.CurrentSettings;
                OrganizationTextBox.Text = settings.WikiSyncAdoOrganization ?? string.Empty;
                ProjectTextBox.Text = settings.WikiSyncAdoProject ?? string.Empty;
                WikiIdentifierTextBox.Text = settings.WikiSyncWikiIdentifier ?? string.Empty;
                PagePathTextBox.Text = settings.WikiSyncPagePath ?? string.Empty;

                // Load system prompts
                var systemPrompts = await _systemPromptService.GetAllSystemPromptsAsync();
                _systemPrompts = systemPrompts.ToList();

                // Add a "Select a System Prompt..." placeholder
                var promptsForComboBox = new List<SystemPrompt>
                {
                    new SystemPrompt { Guid = string.Empty, Title = "Select a System Prompt..." }
                };
                promptsForComboBox.AddRange(_systemPrompts);

                SystemPromptComboBox.ItemsSource = promptsForComboBox;

                // Set selected item if we have a configured GUID
                if (!string.IsNullOrEmpty(settings.WikiSyncTargetSystemPromptGuid))
                {
                    var selectedPrompt = promptsForComboBox.FirstOrDefault(p => p.Guid == settings.WikiSyncTargetSystemPromptGuid);
                    if (selectedPrompt != null)
                    {
                        SystemPromptComboBox.SelectedItem = selectedPrompt;
                    }
                }
                else
                {
                    SystemPromptComboBox.SelectedIndex = 0; // Select placeholder
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading data for wiki sync configuration dialog");
                MessageBox.Show($"Error loading configuration data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validate inputs
                if (string.IsNullOrWhiteSpace(OrganizationTextBox.Text))
                {
                    MessageBox.Show("Azure DevOps Organization is required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    OrganizationTextBox.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(ProjectTextBox.Text))
                {
                    MessageBox.Show("Azure DevOps Project is required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    ProjectTextBox.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(WikiIdentifierTextBox.Text))
                {
                    MessageBox.Show("Wiki Identifier is required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    WikiIdentifierTextBox.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(PagePathTextBox.Text))
                {
                    MessageBox.Show("Wiki Page Path is required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    PagePathTextBox.Focus();
                    return;
                }

                var selectedPrompt = SystemPromptComboBox.SelectedItem as SystemPrompt;
                if (selectedPrompt == null || string.IsNullOrEmpty(selectedPrompt.Guid))
                {
                    MessageBox.Show("Please select a system prompt to update.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    SystemPromptComboBox.Focus();
                    return;
                }

                // Save settings
                var settings = _generalSettingsService.CurrentSettings;
                settings.WikiSyncAdoOrganization = OrganizationTextBox.Text.Trim();
                settings.WikiSyncAdoProject = ProjectTextBox.Text.Trim();
                settings.WikiSyncWikiIdentifier = WikiIdentifierTextBox.Text.Trim();
                settings.WikiSyncPagePath = PagePathTextBox.Text.Trim();
                settings.WikiSyncTargetSystemPromptGuid = selectedPrompt.Guid;

                _generalSettingsService.SaveSettings();

                MessageBox.Show("Wiki sync configuration saved successfully. Changes will take effect on next application startup.", 
                              "Configuration Saved", MessageBoxButton.OK, MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving wiki sync configuration");
                MessageBox.Show($"Error saving configuration: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
