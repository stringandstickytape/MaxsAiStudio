// AiStudio4/Dialogs/WpfConfirmationDialog.xaml.cs
using System.Windows;

namespace AiStudio4.Dialogs
{
    public partial class WpfConfirmationDialog : Window
    {
        public bool Confirmed { get; private set; } = false;

        public WpfConfirmationDialog(string title, string promptMessage, string commandToDisplay)
        {            
            InitializeComponent();
            this.Title = title;
            PromptMessageTextBlock.Text = promptMessage;
            CommandToDisplayTextBox.Text = commandToDisplay;
            
            // Set focus to the Cancel button by default for safety
            CancelButton.Focus();
        }

        private void ProceedButton_Click(object sender, RoutedEventArgs e)
        {            
            Confirmed = true;
            this.DialogResult = true;
            this.Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {            
            Confirmed = false;
            this.DialogResult = false;
            this.Close();
        }
    }
}