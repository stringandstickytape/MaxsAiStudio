// AiStudio4/Core/Interfaces/IGoogleDriveService.cs




namespace AiStudio4.Core.Interfaces
{
    public interface IGoogleDriveService
    {
        Task<List<GoogleDriveFileInfo>> ListFilesFromAiStudioFolderAsync();
        Task<string> UploadTextFileAsync(string testFileName, string testFileContent, string googleDriveFolderName);
        Task<(string,string)> DownloadFileContentAsync(string fileId);
    }
}
