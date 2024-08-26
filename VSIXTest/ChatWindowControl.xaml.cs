using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace VSIXTest
{
    public partial class ChatWindowControl : UserControl
    {
        public ChatWindowControl()
        {
            InitializeComponent();
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            SendMessage();
        }

        private void InputTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SendMessage();
                e.Handled = true;
            }
        }

        private void SendMessage()
        {
            string message = InputTextBox.Text.Trim();
            if (!string.IsNullOrEmpty(message))
            {
                ChatHistoryTextBox.AppendText($"You: {message}\n");
                InputTextBox.Clear();

                // Send message through named pipe
                VSIXTestPackage.Instance.SendMessageThroughPipe(message);
            }
        }

        public void ReceiveMessage(string message)
        {
            ChatHistoryTextBox.AppendText($"AI: {message}\n");
        }
    }
}