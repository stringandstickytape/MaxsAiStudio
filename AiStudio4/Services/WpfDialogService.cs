// AiStudio4.Services/WpfDialogService.cs
using AiStudio4.Core.Interfaces;
using AiStudio4.Dialogs; // Assuming WpfConfirmationDialog is in this namespace
using System.Threading.Tasks;
using System.Windows; // For Application.Current
using System.Windows.Threading; // For Dispatcher

namespace AiStudio4.Services
{
    public class WpfDialogService : IDialogService
    {
        private readonly Dispatcher _uiDispatcher;

        public WpfDialogService()
        {
            // Assumes this service is created on the UI thread or can access Application.Current.
            // If not, Dispatcher might need to be passed in or obtained differently.
            _uiDispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
        }

        public async Task<bool> ShowConfirmationAsync(string title, string promptMessage, string commandToDisplay)
        {            
            bool result = false;
            await _uiDispatcher.InvokeAsync(() =>
            {
                var dialog = new WpfConfirmationDialog(title, promptMessage, commandToDisplay)
                {
                    Owner = Application.Current?.MainWindow // Set owner for proper modality
                };
                
                if (dialog.ShowDialog() == true)
                {                    
                    result = dialog.Confirmed;
                }
            });
            return result;
        }
    }
}