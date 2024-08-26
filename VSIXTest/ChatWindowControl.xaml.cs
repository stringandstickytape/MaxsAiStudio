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

        private void InputTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                SendMessage();
                e.Handled = true;
            }
        }

        private void InputTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (InputTextBox.Text.EndsWith("#"))
            {
                ShowShortcutMenu();
            }
        }

        private void ShowShortcutMenu()
        {
            var textBoxPosition = InputTextBox.GetRectFromCharacterIndex(InputTextBox.CaretIndex);
            ShortcutMenu.PlacementTarget = InputTextBox;
            ShortcutMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.RelativePoint;
            ShortcutMenu.HorizontalOffset = textBoxPosition.Right;
            ShortcutMenu.VerticalOffset = textBoxPosition.Bottom;
            ShortcutMenu.IsOpen = true;
        }

        private void ShortcutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem)
            {
                int caretIndex = InputTextBox.CaretIndex;
                InputTextBox.Text = InputTextBox.Text.Insert(caretIndex, menuItem.Header.ToString());
                InputTextBox.CaretIndex = caretIndex + menuItem.Header.ToString().Length;
                ShortcutMenu.IsOpen = false;
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

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            SendMessage();
        }
    }
}