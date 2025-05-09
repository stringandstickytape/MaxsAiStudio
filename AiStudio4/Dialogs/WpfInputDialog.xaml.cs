// AiStudio4/Dialogs/WpfInputDialog.xaml.cs
using System.Windows;

namespace AiStudio4.Dialogs
{
    /// <summary>
    /// Interaction logic for WpfInputDialog.xaml
    /// </summary>
    public partial class WpfInputDialog : Window
    {
        public string ResponseText { get; private set; } = string.Empty;

        public WpfInputDialog(string title, string prompt, string defaultValue = "")
        {
            InitializeComponent();
            this.Title = title;
            PromptTextBlock.Text = prompt;
            InputTextBox.Text = defaultValue;
            
            // Set focus to the input box when the window loads
            this.Loaded += (sender, e) => InputTextBox.Focus();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            ResponseText = InputTextBox.Text;
            this.DialogResult = true;
            this.Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}