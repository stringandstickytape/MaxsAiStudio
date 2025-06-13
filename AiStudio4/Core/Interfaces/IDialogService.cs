// AiStudio4.Core.Interfaces/IDialogService.cs


namespace AiStudio4.Core.Interfaces
{
    public interface IDialogService
    {
        Task<bool> ShowConfirmationAsync(string title, string promptMessage, string commandToDisplay);
    }
}
