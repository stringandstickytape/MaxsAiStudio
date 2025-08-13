namespace AiStudio4.Tools.Interfaces
{
    public interface IDialogService
    {
        Task<bool> ShowConfirmationAsync(string title, string promptMessage, string commandToDisplay);
    }
}