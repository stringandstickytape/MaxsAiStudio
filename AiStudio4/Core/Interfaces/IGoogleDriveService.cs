// AiStudio4/Core/Interfaces/IGoogleDriveService.cs
using AiStudio4.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AiStudio4.Core.Interfaces
{
    public interface IGoogleDriveService
    {
        Task<List<GoogleDriveFileInfo>> ListFilesFromAiStudioFolderAsync();
        Task<string> UploadTextFileAsync(string testFileName, string testFileContent, string googleDriveFolderName);
        Task<(string,string)> DownloadFileContentAsync(string fileId);
    }
}