using System.Windows.Controls;

namespace VSIXTest
{
    public partial class ChatControl : UserControl
    {
        public ChatControl()
        {
            InitializeComponent();
        }

        private void SendButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            // Implement sending message logic here
            string message = InputTextBox.Text;
            ChatHistoryTextBox.AppendText($"You: {message}\n");
            InputTextBox.Clear();

            // Here you would send the message through the named pipe
            // and receive the response, then display it in the ChatHistoryTextBox
        }
    }
}