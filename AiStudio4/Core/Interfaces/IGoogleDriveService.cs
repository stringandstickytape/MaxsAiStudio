// AiStudio4/Core/Interfaces/IGoogleDriveService.cs
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AiStudio4.Core.Interfaces
{
    public interface IGoogleDriveService
    {
        Task<List<string>> ListFilesFromAiStudioFolderAsync();
    }
}