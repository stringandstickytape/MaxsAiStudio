// AiStudio4/Dialogs/LicensesWindow.xaml.cs





namespace AiStudio4.Dialogs
{
    public partial class LicensesWindow : Window
    {
        public LicensesWindow(string clientDistLicensesPath, string appNugetLicensePath, string sharedClassesNugetLicensePath)
        {
            InitializeComponent();
            LoadAndDisplayLicensesAsync(clientDistLicensesPath, appNugetLicensePath, sharedClassesNugetLicensePath);
        }

        private async void LoadAndDisplayLicensesAsync(string clientDistLicensesPath, string appNugetLicensePath, string sharedClassesNugetLicensePath)
        {
            LicensesTextBox.Text = "Loading license data...";
            try
            {
                using (var licenseService = new LicenseService())
                {
                    string licensesText = await licenseService.GetFormattedAllLicensesAsync(
                        clientDistLicensesPath,
                        appNugetLicensePath,
                        sharedClassesNugetLicensePath
                    );
                    LicensesTextBox.Text = licensesText;
                }
            }
            catch (Exception ex)
            {
                LicensesTextBox.Text = $"Error loading license data: {ex.Message}";
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
