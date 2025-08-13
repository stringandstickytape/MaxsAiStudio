using System.Windows;
using AiStudio4.Tools.Interfaces;

namespace AiStudio4.McpStandalone.Services
{
    /// <summary>
    /// WPF implementation of IDialogService for the standalone MCP server
    /// </summary>
    public class StandaloneDialogService : IDialogService
    {
        /// <summary>
        /// Shows a confirmation dialog to the user
        /// </summary>
        public async Task<bool> ShowConfirmationAsync(string title, string promptMessage, string commandToDisplay)
        {
            return await Task.Run(() =>
            {
                // Run on UI thread
                return System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    string message = $"{promptMessage}\n\nCommand:\n{commandToDisplay}";
                    
                    var result = System.Windows.MessageBox.Show(
                        message,
                        title,
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);
                    
                    return result == MessageBoxResult.Yes;
                });
            });
        }
    }
}