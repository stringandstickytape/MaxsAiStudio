// AiStudio4/Services/GoogleDriveService.cs
using AiStudio4.Core.Interfaces;
using AiStudio4.Core.Models;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Google.Apis.Upload; // For IUploadProgress
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

                // 1. Find or Create the target folder (e.g., "Google AI Studio")
                string folderId;
                var folderRequest = service.Files.List();
                folderRequest.Q = $"name = '{googleDriveFolderName}' and mimeType = 'application/vnd.google-apps.folder' and trashed = false";
                folderRequest.Spaces = "drive";
                folderRequest.Fields = "files(id, name)";
                var folderResult = await folderRequest.ExecuteAsync();

                if (folderResult.Files != null && folderResult.Files.Any())
                {
                    folderId = folderResult.Files.First().Id;
                    _logger.LogInformation("Found target folder '{FolderName}' with ID: {FolderId}", googleDriveFolderName, folderId);
                }
                else
                {
                    // Folder not found, create it
                    _logger.LogInformation("Folder '{FolderName}' not found. Creating it...", googleDriveFolderName);
                    var folderMetadata = new Google.Apis.Drive.v3.Data.File()
                    {
                        Name = googleDriveFolderName,
                        MimeType = "application/vnd.google-apps.folder"
                    };
                    var createFolderRequest = service.Files.Create(folderMetadata);
                    createFolderRequest.Fields = "id";
                    var createdFolder = await createFolderRequest.ExecuteAsync();
                    folderId = createdFolder.Id;
                    _logger.LogInformation("Created folder '{FolderName}' with ID: {FolderId}", googleDriveFolderName, folderId);
                }

                // 2. Create file metadata
                var fileMetadata = new Google.Apis.Drive.v3.Data.File()
                {
                    Name = testFileName,
                    MimeType = "application/vnd.google-makersuite.prompt", // Explicitly set for JSON content
                    Parents = new List<string> { folderId }
                };

                // 3. Convert text content to stream
                byte[] byteArray = System.Text.Encoding.UTF8.GetBytes(testFileContent);
                using (var stream = new MemoryStream(byteArray))
                {
                    // 4. Create the upload request
                    var request = service.Files.Create(fileMetadata, stream, "application/vnd.google-makersuite.prompt");
                    request.Fields = "id, name, webViewLink"; // Request fields to get back

                    // 5. Upload the file
                    _logger.LogInformation("Uploading file '{FileName}'...", testFileName);
                    var uploadProgress = await request.UploadAsync();
                    
                    if (uploadProgress.Status == Google.Apis.Upload.UploadStatus.Completed)
                    {
                        var createdFile = request.ResponseBody;
                        _logger.LogInformation("Successfully uploaded file '{FileName}' with ID: {FileId}. Viewable at: {WebViewLink}", createdFile.Name, createdFile.Id, createdFile.WebViewLink);
                        return createdFile.Id;
                    }
                    else
                    {
                        _logger.LogError("File upload failed for '{FileName}'. Status: {Status}. Exception: {Exception}", testFileName, uploadProgress.Status, uploadProgress.Exception?.Message);
                        throw new InvalidOperationException($"File upload failed for '{testFileName}' with status: {uploadProgress.Status}. Exception: {uploadProgress.Exception?.Message}");
                    }
                }
            }
            catch (FileNotFoundException fnfEx) // Credentials file not found
            {
                _logger.LogError(fnfEx, "Prerequisite error for Google Drive access during upload.");
                throw; // Re-throw to be handled by the caller (MainWindow)
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while uploading file '{FileName}' to Google Drive.", testFileName);
                throw; // Re-throw to be handled by the caller
            }
        }

        public async Task<List<GoogleDriveFileInfo>> ListFilesFromAiStudioFolderAsync()
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
                    return new List<GoogleDriveFileInfo>();
                }

                var aiStudioFolder = folderResult.Files.First();
                _logger.LogInformation("Found folder '{FolderName}' with ID: {FolderId}", aiStudioFolder.Name, aiStudioFolder.Id);
                Debug.WriteLine($"[GoogleDriveService] Found folder: {aiStudioFolder.Name} (ID: {aiStudioFolder.Id})");

                var filesRequest = service.Files.List();
                filesRequest.Q = $"'{aiStudioFolder.Id}' in parents and trashed = false and mimeType != 'application/vnd.google-apps.folder'";
                filesRequest.Spaces = "drive";
                // Request comprehensive file information for debugging
                filesRequest.Fields = "files(id, name, mimeType, size, createdTime, modifiedTime, owners(displayName, emailAddress), parents, webViewLink, webContentLink, version, md5Checksum, fileExtension, fullFileExtension, originalFilename, quotaBytesUsed, shared, capabilities, properties, appProperties, spaces, trashed, explicitlyTrashed, trashedTime, trashingUser(displayName, emailAddress), lastModifyingUser(displayName, emailAddress), sharingUser(displayName, emailAddress), viewedByMe, viewedByMeTime, copyRequiresWriterPermission, writersCanShare, permissions, hasAugmentedPermissions, isAppAuthorized, description, starred, headRevisionId, iconLink, thumbnailLink, hasThumbnail, thumbnailVersion, videoMediaMetadata, imageMediaMetadata, exportLinks, driveId, teamDriveId, hasAugmentedPermissions)";
                filesRequest.PageSize = 100;

                var fileList = new List<GoogleDriveFileInfo>();
                string pageToken = null;
                do
                {
                    filesRequest.PageToken = pageToken;
                    var result = await filesRequest.ExecuteAsync();
                    if (result.Files != null)
                    {
                        foreach (var file in result.Files)
                        {
                            fileList.Add(new GoogleDriveFileInfo { Id = file.Id, Name = file.Name });
                            
                            // Comprehensive debug logging for each file
                            _logger.LogInformation("=== DETAILED FILE DEBUG INFO ===");
                            _logger.LogInformation("File ID: {FileId}", file.Id ?? "N/A");
                            _logger.LogInformation("File Name: {FileName}", file.Name ?? "N/A");
                            _logger.LogInformation("MIME Type: {MimeType}", file.MimeType ?? "N/A");
                            _logger.LogInformation("File Size: {Size} bytes", file.Size?.ToString() ?? "N/A");
                            _logger.LogInformation("Created Time: {CreatedTime}", file.CreatedTime?.ToString() ?? "N/A");
                            _logger.LogInformation("Modified Time: {ModifiedTime}", file.ModifiedTime?.ToString() ?? "N/A");
                            _logger.LogInformation("File Extension: {FileExtension}", file.FileExtension ?? "N/A");
                            _logger.LogInformation("Full File Extension: {FullFileExtension}", file.FullFileExtension ?? "N/A");
                            _logger.LogInformation("Original Filename: {OriginalFilename}", file.OriginalFilename ?? "N/A");
                            _logger.LogInformation("Version: {Version}", file.Version?.ToString() ?? "N/A");
                            _logger.LogInformation("MD5 Checksum: {Md5Checksum}", file.Md5Checksum ?? "N/A");
                            _logger.LogInformation("Quota Bytes Used: {QuotaBytesUsed}", file.QuotaBytesUsed?.ToString() ?? "N/A");
                            _logger.LogInformation("Shared: {Shared}", file.Shared?.ToString() ?? "N/A");
                            _logger.LogInformation("Starred: {Starred}", file.Starred?.ToString() ?? "N/A");
                            _logger.LogInformation("Viewed By Me: {ViewedByMe}", file.ViewedByMe?.ToString() ?? "N/A");
                            _logger.LogInformation("Viewed By Me Time: {ViewedByMeTime}", file.ViewedByMeTime?.ToString() ?? "N/A");
                            _logger.LogInformation("Trashed: {Trashed}", file.Trashed?.ToString() ?? "N/A");
                            _logger.LogInformation("Explicitly Trashed: {ExplicitlyTrashed}", file.ExplicitlyTrashed?.ToString() ?? "N/A");
                            _logger.LogInformation("Trashed Time: {TrashedTime}", file.TrashedTime?.ToString() ?? "N/A");
                            _logger.LogInformation("Copy Requires Writer Permission: {CopyRequiresWriterPermission}", file.CopyRequiresWriterPermission?.ToString() ?? "N/A");
                            _logger.LogInformation("Writers Can Share: {WritersCanShare}", file.WritersCanShare?.ToString() ?? "N/A");
                            _logger.LogInformation("Has Augmented Permissions: {HasAugmentedPermissions}", file.HasAugmentedPermissions?.ToString() ?? "N/A");
                            _logger.LogInformation("Is App Authorized: {IsAppAuthorized}", file.IsAppAuthorized?.ToString() ?? "N/A");
                            _logger.LogInformation("Description: {Description}", file.Description ?? "N/A");
                            _logger.LogInformation("Head Revision ID: {HeadRevisionId}", file.HeadRevisionId ?? "N/A");
                            _logger.LogInformation("Icon Link: {IconLink}", file.IconLink ?? "N/A");
                            _logger.LogInformation("Thumbnail Link: {ThumbnailLink}", file.ThumbnailLink ?? "N/A");
                            _logger.LogInformation("Has Thumbnail: {HasThumbnail}", file.HasThumbnail?.ToString() ?? "N/A");
                            _logger.LogInformation("Thumbnail Version: {ThumbnailVersion}", file.ThumbnailVersion?.ToString() ?? "N/A");
                            _logger.LogInformation("Web View Link: {WebViewLink}", file.WebViewLink ?? "N/A");
                            _logger.LogInformation("Web Content Link: {WebContentLink}", file.WebContentLink ?? "N/A");
                            _logger.LogInformation("Drive ID: {DriveId}", file.DriveId ?? "N/A");
                            _logger.LogInformation("Team Drive ID: {TeamDriveId}", file.TeamDriveId ?? "N/A");
                            
                            // Log owners information
                            if (file.Owners != null && file.Owners.Any())
                            {
                                _logger.LogInformation("Owners Count: {OwnersCount}", file.Owners.Count);
                                for (int i = 0; i < file.Owners.Count; i++)
                                {
                                    var owner = file.Owners[i];
                                    _logger.LogInformation("Owner {Index}: {DisplayName} ({EmailAddress})", i + 1, owner.DisplayName ?? "N/A", owner.EmailAddress ?? "N/A");
                                }
                            }
                            else
                            {
                                _logger.LogInformation("Owners: None");
                            }
                            
                            // Log parents information
                            if (file.Parents != null && file.Parents.Any())
                            {
                                _logger.LogInformation("Parents Count: {ParentsCount}", file.Parents.Count);
                                for (int i = 0; i < file.Parents.Count; i++)
                                {
                                    _logger.LogInformation("Parent {Index}: {ParentId}", i + 1, file.Parents[i]);
                                }
                            }
                            else
                            {
                                _logger.LogInformation("Parents: None");
                            }
                            
                            // Log spaces information
                            if (file.Spaces != null && file.Spaces.Any())
                            {
                                _logger.LogInformation("Spaces: {Spaces}", string.Join(", ", file.Spaces));
                            }
                            else
                            {
                                _logger.LogInformation("Spaces: None");
                            }
                            
                            // Log last modifying user
                            if (file.LastModifyingUser != null)
                            {
                                _logger.LogInformation("Last Modifying User: {DisplayName} ({EmailAddress})", file.LastModifyingUser.DisplayName ?? "N/A", file.LastModifyingUser.EmailAddress ?? "N/A");
                            }
                            else
                            {
                                _logger.LogInformation("Last Modifying User: None");
                            }
                            
                            // Log trashing user
                            if (file.TrashingUser != null)
                            {
                                _logger.LogInformation("Trashing User: {DisplayName} ({EmailAddress})", file.TrashingUser.DisplayName ?? "N/A", file.TrashingUser.EmailAddress ?? "N/A");
                            }
                            else
                            {
                                _logger.LogInformation("Trashing User: None");
                            }
                            
                            // Log sharing user
                            if (file.SharingUser != null)
                            {
                                _logger.LogInformation("Sharing User: {DisplayName} ({EmailAddress})", file.SharingUser.DisplayName ?? "N/A", file.SharingUser.EmailAddress ?? "N/A");
                            }
                            else
                            {
                                _logger.LogInformation("Sharing User: None");
                            }
                            
                            // Log capabilities
                            if (file.Capabilities != null)
                            {
                                _logger.LogInformation("=== FILE CAPABILITIES ===");
                                _logger.LogInformation("Can Add Children: {CanAddChildren}", file.Capabilities.CanAddChildren?.ToString() ?? "N/A");
                                _logger.LogInformation("Can Change Copy Requires Writer Permission: {CanChangeCopyRequiresWriterPermission}", file.Capabilities.CanChangeCopyRequiresWriterPermission?.ToString() ?? "N/A");
                                _logger.LogInformation("Can Change Viewers Can Copy Content: {CanChangeViewersCanCopyContent}", file.Capabilities.CanChangeViewersCanCopyContent?.ToString() ?? "N/A");
                                _logger.LogInformation("Can Comment: {CanComment}", file.Capabilities.CanComment?.ToString() ?? "N/A");
                                _logger.LogInformation("Can Copy: {CanCopy}", file.Capabilities.CanCopy?.ToString() ?? "N/A");
                                _logger.LogInformation("Can Delete: {CanDelete}", file.Capabilities.CanDelete?.ToString() ?? "N/A");
                                _logger.LogInformation("Can Download: {CanDownload}", file.Capabilities.CanDownload?.ToString() ?? "N/A");
                                _logger.LogInformation("Can Edit: {CanEdit}", file.Capabilities.CanEdit?.ToString() ?? "N/A");
                                _logger.LogInformation("Can List Children: {CanListChildren}", file.Capabilities.CanListChildren?.ToString() ?? "N/A");
                                _logger.LogInformation("Can Move Item Into Team Drive: {CanMoveItemIntoTeamDrive}", file.Capabilities.CanMoveItemIntoTeamDrive?.ToString() ?? "N/A");
                                _logger.LogInformation("Can Move Team Drive Item: {CanMoveTeamDriveItem}", file.Capabilities.CanMoveTeamDriveItem?.ToString() ?? "N/A");
                                _logger.LogInformation("Can Read Revisions: {CanReadRevisions}", file.Capabilities.CanReadRevisions?.ToString() ?? "N/A");
                                _logger.LogInformation("Can Remove Children: {CanRemoveChildren}", file.Capabilities.CanRemoveChildren?.ToString() ?? "N/A");
                                _logger.LogInformation("Can Rename: {CanRename}", file.Capabilities.CanRename?.ToString() ?? "N/A");
                                _logger.LogInformation("Can Share: {CanShare}", file.Capabilities.CanShare?.ToString() ?? "N/A");
                                _logger.LogInformation("Can Trash: {CanTrash}", file.Capabilities.CanTrash?.ToString() ?? "N/A");
                                _logger.LogInformation("Can Untrash: {CanUntrash}", file.Capabilities.CanUntrash?.ToString() ?? "N/A");
                            }
                            else
                            {
                                _logger.LogInformation("Capabilities: None");
                            }
                            
                            // Log custom properties
                            if (file.Properties != null && file.Properties.Any())
                            {
                                _logger.LogInformation("=== CUSTOM PROPERTIES ===");
                                foreach (var prop in file.Properties)
                                {
                                    _logger.LogInformation("Property: {Key} = {Value}", prop.Key, prop.Value ?? "N/A");
                                }
                            }
                            else
                            {
                                _logger.LogInformation("Custom Properties: None");
                            }
                            
                            // Log app properties
                            if (file.AppProperties != null && file.AppProperties.Any())
                            {
                                _logger.LogInformation("=== APP PROPERTIES ===");
                                foreach (var prop in file.AppProperties)
                                {
                                    _logger.LogInformation("App Property: {Key} = {Value}", prop.Key, prop.Value ?? "N/A");
                                }
                            }
                            else
                            {
                                _logger.LogInformation("App Properties: None");
                            }
                            
                            // Log export links
                            if (file.ExportLinks != null && file.ExportLinks.Any())
                            {
                                _logger.LogInformation("=== EXPORT LINKS ===");
                                foreach (var link in file.ExportLinks)
                                {
                                    _logger.LogInformation("Export Format: {Format} = {Link}", link.Key, link.Value ?? "N/A");
                                }
                            }
                            else
                            {
                                _logger.LogInformation("Export Links: None");
                            }
                            
                            // Log video metadata if available
                            if (file.VideoMediaMetadata != null)
                            {
                                _logger.LogInformation("=== VIDEO METADATA ===");
                                _logger.LogInformation("Video Width: {Width}", file.VideoMediaMetadata.Width?.ToString() ?? "N/A");
                                _logger.LogInformation("Video Height: {Height}", file.VideoMediaMetadata.Height?.ToString() ?? "N/A");
                                _logger.LogInformation("Video Duration: {Duration} ms", file.VideoMediaMetadata.DurationMillis?.ToString() ?? "N/A");
                            }
                            
                            // Log image metadata if available
                            if (file.ImageMediaMetadata != null)
                            {
                                _logger.LogInformation("=== IMAGE METADATA ===");
                                _logger.LogInformation("Image Width: {Width}", file.ImageMediaMetadata.Width?.ToString() ?? "N/A");
                                _logger.LogInformation("Image Height: {Height}", file.ImageMediaMetadata.Height?.ToString() ?? "N/A");
                                _logger.LogInformation("Image Rotation: {Rotation}", file.ImageMediaMetadata.Rotation?.ToString() ?? "N/A");
                                if (file.ImageMediaMetadata.Location != null)
                                {
                                    _logger.LogInformation("Image Location - Latitude: {Latitude}, Longitude: {Longitude}, Altitude: {Altitude}", 
                                        file.ImageMediaMetadata.Location.Latitude?.ToString() ?? "N/A",
                                        file.ImageMediaMetadata.Location.Longitude?.ToString() ?? "N/A",
                                        file.ImageMediaMetadata.Location.Altitude?.ToString() ?? "N/A");
                                }
                                _logger.LogInformation("Image Date Taken: {DateTaken}", file.ImageMediaMetadata.Time ?? "N/A");
                                _logger.LogInformation("Camera Make: {CameraMake}", file.ImageMediaMetadata.CameraMake ?? "N/A");
                                _logger.LogInformation("Camera Model: {CameraModel}", file.ImageMediaMetadata.CameraModel ?? "N/A");
                                _logger.LogInformation("Exposure Time: {ExposureTime}", file.ImageMediaMetadata.ExposureTime?.ToString() ?? "N/A");
                                _logger.LogInformation("Aperture: {Aperture}", file.ImageMediaMetadata.Aperture?.ToString() ?? "N/A");
                                _logger.LogInformation("ISO Speed: {IsoSpeed}", file.ImageMediaMetadata.IsoSpeed?.ToString() ?? "N/A");
                                _logger.LogInformation("Focal Length: {FocalLength}", file.ImageMediaMetadata.FocalLength?.ToString() ?? "N/A");
                            }
                            
                            _logger.LogInformation("=== END FILE DEBUG INFO ===");
                            
                            // Also output to Debug for immediate visibility
                            Debug.WriteLine($"[GoogleDriveService] DETAILED FILE: {file.Name} (ID: {file.Id})");
                            Debug.WriteLine($"  - MIME Type: {file.MimeType ?? "N/A"}");
                            Debug.WriteLine($"  - Size: {file.Size?.ToString() ?? "N/A"} bytes");
                            Debug.WriteLine($"  - Created: {file.CreatedTime?.ToString() ?? "N/A"}");
                            Debug.WriteLine($"  - Modified: {file.ModifiedTime?.ToString() ?? "N/A"}");
                            Debug.WriteLine($"  - Extension: {file.FileExtension ?? "N/A"}");
                            Debug.WriteLine($"  - MD5: {file.Md5Checksum ?? "N/A"}");
                            Debug.WriteLine($"  - Shared: {file.Shared?.ToString() ?? "N/A"}");
                            Debug.WriteLine($"  - Web View: {file.WebViewLink ?? "N/A"}");
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

        public async Task<(string,string)> DownloadFileContentAsync(string fileId)
        {
            try
            {
                _logger.LogInformation("Attempting to download file content for file ID: {FileId}", fileId);
                var service = await GetDriveServiceAsync();

                var metadataRequest = service.Files.Get(fileId);
                metadataRequest.Fields = "name"; // Only fetch the name field for efficiency
                var fileMetadata = await metadataRequest.ExecuteAsync();
                var filename = fileMetadata.Name;

                var request = service.Files.Get(fileId);
                var stream = new MemoryStream();
                
                await request.DownloadAsync(stream);
                stream.Position = 0;
                
                using (var reader = new StreamReader(stream))
                {
                    string content = await reader.ReadToEndAsync();
                    _logger.LogInformation("Successfully downloaded file content for file ID: {FileId}, size: {Size} characters", fileId, content.Length);
                    return (filename,content);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading file content for file ID: {FileId}", fileId);
                throw; // Re-throw to be handled by the caller
            }
        }
    }
}