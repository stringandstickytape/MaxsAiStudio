// AiStudio4/Dialogs/MessageSelectionDialog.xaml.cs



using System.Collections.ObjectModel;
using System.ComponentModel;




namespace AiStudio4.Dialogs
{
    /// <summary>
    /// Interaction logic for MessageSelectionDialog.xaml
    /// </summary>
    public partial class MessageSelectionDialog : Window
    {
        public List<v4BranchedConvMessage> SelectedMessages { get; private set; }
        
        private ObservableCollection<MessageSelectionItem> _messageItems;

        public MessageSelectionDialog(v4BranchedConv conversation)
        {
            InitializeComponent();
            
            ConversationTitle.Text = $"Conversation: {conversation.Summary ?? "Untitled"}";
            
            // Convert messages to selectable items, excluding the system root message
            _messageItems = new ObservableCollection<MessageSelectionItem>(
                conversation.Messages
                    .Where(m => m.Role != v4BranchedConvMessageRole.System) // Exclude system messages
                    .OrderBy(m => m.Timestamp)
                    .Select(m => new MessageSelectionItem(m))
            );
            
            MessagesDataGrid.ItemsSource = _messageItems;
            
            // Select all messages by default
            foreach (var item in _messageItems)
            {
                item.IsSelected = true;
            }
        }

        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in _messageItems)
            {
                item.IsSelected = true;
            }
        }

        private void SelectNone_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in _messageItems)
            {
                item.IsSelected = false;
            }
        }

        private void SelectUserMessages_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in _messageItems)
            {
                item.IsSelected = item.Message.Role == v4BranchedConvMessageRole.User;
            }
        }

        private void SelectAiMessages_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in _messageItems)
            {
                item.IsSelected = item.Message.Role == v4BranchedConvMessageRole.Assistant;
            }
        }

        private void ImportSelected_Click(object sender, RoutedEventArgs e)
        {
            var selectedItems = _messageItems.Where(item => item.IsSelected).ToList();
            
            if (!selectedItems.Any())
            {
                MessageBox.Show("Please select at least one message to import.", "No Messages Selected", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            SelectedMessages = selectedItems.Select(item => item.Message).ToList();
            DialogResult = true;
        }

        private void ImportAll_Click(object sender, RoutedEventArgs e)
        {
            // Select all messages and import
            foreach (var item in _messageItems)
            {
                item.IsSelected = true;
            }
            
            SelectedMessages = _messageItems.Select(item => item.Message).ToList();
            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }

    /// <summary>
    /// Wrapper class for displaying messages in the DataGrid with selection capability
    /// </summary>
    public class MessageSelectionItem : INotifyPropertyChanged
    {
        private bool _isSelected;
        
        public v4BranchedConvMessage Message { get; }
        
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged(nameof(IsSelected));
                }
            }
        }


        public MessageSelectionItem(v4BranchedConvMessage message)
        {
            Message = message ?? throw new ArgumentNullException(nameof(message));
        }

        public event PropertyChangedEventHandler PropertyChanged;
        
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
