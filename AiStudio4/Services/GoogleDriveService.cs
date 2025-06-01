// AiStudio4/Services/GoogleDriveService.cs
using AiStudio4.Core.Interfaces;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Microsoft.Extensions.Logging; // Add this using
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AiStudio4.Services
{
    public class GoogleDriveService : IGoogleDriveService
    {
        private readonly ILogger<GoogleDriveService> _logger;
        private static readonly string[] Scopes = { DriveService.Scope.Drive };
        private static readonly string ApplicationName = "AiStudio4";
        private static readonly string CredentialsFileName = "credentials.json";
        private static readonly string TokenFolderName = "GoogleDriveToken";
        private static readonly string AiStudioFolderName = "Google AI Studio";

        private readonly string _credentialsPath;
        private readonly string _tokenStoragePath;

        public GoogleDriveService(ILogger<GoogleDriveService> logger)
        {
            _logger = logger;
            string appDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AiStudio4");
            Directory.CreateDirectory(appDataFolder);

            _credentialsPath = Path.Combine(appDataFolder, "Config", CredentialsFileName);
            Directory.CreateDirectory(Path.GetDirectoryName(_credentialsPath));

            _tokenStoragePath = Path.Combine(appDataFolder, TokenFolderName);
        }

        private async Task<DriveService> GetDriveServiceAsync()
        {
            UserCredential credential;

            if (!File.Exists(_credentialsPath))
            {
                string errorMsg = $"Google API credentials file ('{CredentialsFileName}') not found. Expected at: {_credentialsPath}. Please download it from Google Cloud Console (OAuth 2.0 Client ID for Desktop app) and place it there.";
                _logger.LogError(errorMsg);
                throw new FileNotFoundException(errorMsg, _credentialsPath);
            }

            using (var stream = new FileStream(_credentialsPath, FileMode.Open, FileAccess.Read))
            {
                try
                {
                    credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                        GoogleClientSecrets.FromStream(stream).Secrets,
                        Scopes,
                        "user", // Using "user" as identifier for stored credentials
                        CancellationToken.None,
                        new FileDataStore(_tokenStoragePath, true)
                    );
                    _logger.LogInformation("Google Drive API credential loaded/authorized for user: {User}", credential.UserId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during GoogleWebAuthorizationBroker.AuthorizeAsync. Ensure correct 'credentials.json' and consent screen setup.");
                    throw; // Re-throw to be caught by the caller
                }
            }

            return new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
            });
        }

        public async Task<string> UploadTextFileAsync(string testFileName, string testFileContent, string googleDriveFolderName)
        {
            try
            {
                _logger.LogInformation("Attempting to upload text file '{FileName}' to Google Drive folder: '{FolderName}'", testFileName, googleDriveFolderName);
                var service = await GetDriveServiceAsync();

                // Find the target folder
                var folderRequest = service.Files.List();
                folderRequest.Q = $"name = '{googleDriveFolderName}' and mimeType = 'application/vnd.google-apps.folder' and trashed = false";
                folderRequest.Spaces = "drive";
                folderRequest.Fields = "files(id, name)";
                var folderResult = await folderRequest.ExecuteAsync();

                if (folderResult.Files == null || !folderResult.Files.Any())
                {
                    _logger.LogWarning("Folder '{FolderName}' not found in Google Drive.", googleDriveFolderName);
                    throw new DirectoryNotFoundException($"Folder '{googleDriveFolderName}' not found in Google Drive.");
                }

                var targetFolder = folderResult.Files.First();
                _logger.LogInformation("Found target folder '{FolderName}' with ID: {FolderId}", targetFolder.Name, targetFolder.Id);

                // Create file metadata
                var fileMetadata = new Google.Apis.Drive.v3.Data.File()
                {
                    Name = testFileName,
                    Parents = new List<string> { targetFolder.Id }
                };

                // Convert text content to stream
                using (var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(testFileContent)))
                {
                    // Create the upload request
                    var request = service.Files.Create(fileMetadata, stream, "text/plain");
                    request.Fields = "id, name";

                    // Upload the file
                    var uploadedFile = await request.UploadAsync();
                    
                    if (uploadedFile.Status == Google.Apis.Upload.UploadStatus.Completed)
                    {
                        var createdFile = request.ResponseBody;
                        _logger.LogInformation("Successfully uploaded file '{FileName}' with ID: {FileId}", createdFile.Name, createdFile.Id);
                        return createdFile.Id;
                    }
                    else
                    {
                        _logger.LogError("File upload failed with status: {Status}", uploadedFile.Status);
                        throw new InvalidOperationException($"File upload failed with status: {uploadedFile.Status}");
                    }
                }
            }
            catch (FileNotFoundException fnfEx)
            {
                _logger.LogError(fnfEx, "Prerequisite error for Google Drive access.");
                throw; // Re-throw to be handled by the UI layer
            }
            catch (DirectoryNotFoundException dnfEx)
            {
                _logger.LogError(dnfEx, "Target folder not found in Google Drive.");
                throw; // Re-throw to be handled by the UI layer
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while uploading file to Google Drive.");
                throw; // Re-throw to be handled by the UI layer
            }
        }

        public async Task<List<string>> ListFilesFromAiStudioFolderAsync()
        {
            try
            {
                _logger.LogInformation("Attempting to list files from Google Drive folder: '{FolderName}'", AiStudioFolderName);
                var service = await GetDriveServiceAsync();

                var folderRequest = service.Files.List();
                folderRequest.Q = $"name = '{AiStudioFolderName}' and mimeType = 'application/vnd.google-apps.folder' and trashed = false";
                folderRequest.Spaces = "drive";
                folderRequest.Fields = "files(id, name)";
                var folderResult = await folderRequest.ExecuteAsync();

                if (folderResult.Files == null || !folderResult.Files.Any())
                {
                    _logger.LogWarning("Folder '{FolderName}' not found in Google Drive.", AiStudioFolderName);
                    Debug.WriteLine($"[GoogleDriveService] Folder '{AiStudioFolderName}' not found.");
                    return new List<string>();
                }

                var aiStudioFolder = folderResult.Files.First();
                _logger.LogInformation("Found folder '{FolderName}' with ID: {FolderId}", aiStudioFolder.Name, aiStudioFolder.Id);
                Debug.WriteLine($"[GoogleDriveService] Found folder: {aiStudioFolder.Name} (ID: {aiStudioFolder.Id})");

                var filesRequest = service.Files.List();
                filesRequest.Q = $"'{aiStudioFolder.Id}' in parents and trashed = false and mimeType != 'application/vnd.google-apps.folder'";
                filesRequest.Spaces = "drive";
                filesRequest.Fields = "files(id, name)";
                filesRequest.PageSize = 100;

                var fileList = new List<string>();
                string pageToken = null;
                do
                {
                    filesRequest.PageToken = pageToken;
                    var result = await filesRequest.ExecuteAsync();
                    if (result.Files != null)
                    {
                        foreach (var file in result.Files)
                        {
                            fileList.Add(file.Name);
                            Debug.WriteLine($"[GoogleDriveService] File in '{AiStudioFolderName}': {file.Name} (ID: {file.Id})");
                        }
                    }
                    pageToken = result.NextPageToken;
                } while (pageToken != null);

                _logger.LogInformation("Found {FileCount} files in folder '{FolderName}'.", fileList.Count, AiStudioFolderName);
                return fileList;
            }
            catch (FileNotFoundException fnfEx)
            {
                _logger.LogError(fnfEx, "Prerequisite error for Google Drive access.");
                throw; // Re-throw to be handled by the UI layer
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while listing files from Google Drive.");
                Debug.WriteLine($"[GoogleDriveService] ERROR listing files: {ex.Message}");
                return null; // Indicate an error to the caller
            }
        }
    }
}