// AiStudio4.Core.Interfaces/IDialogService.cs
using System.Threading.Tasks;

namespace AiStudio4.Core.Interfaces
{
    public interface IDialogService
    {
        Task<bool> ShowConfirmationAsync(string title, string promptMessage, string commandToDisplay);
    }
}