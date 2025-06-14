// AiStudio4/Dialogs/GoogleDriveFileSelectionDialog.xaml.cs

using Google.Apis.Drive.v3.Data;



using System.Windows.Controls;

namespace AiStudio4.Dialogs
{
    /// <summary>
    /// Interaction logic for GoogleDriveFileSelectionDialog.xaml
    /// </summary>
    public partial class GoogleDriveFileSelectionDialog : Window
    {
        public List<GoogleDriveFileInfo> SelectedFiles { get; private set; }

        public GoogleDriveFileSelectionDialog(IEnumerable<GoogleDriveFileInfo> files)
        {
            InitializeComponent();
            FileListListBox.ItemsSource = files.ToList();
        }

        private void ImportSelected_Click(object sender, RoutedEventArgs e)
        {
            SelectedFiles = FileListListBox.SelectedItems.Cast<GoogleDriveFileInfo>().ToList();
            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
