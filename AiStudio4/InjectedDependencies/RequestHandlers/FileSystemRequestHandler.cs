// AiStudio4/InjectedDependencies/RequestHandlers/FileSystemRequestHandler.cs
using AiStudio4.Core.Interfaces;
using AiStudio4.InjectedDependencies;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Linq;
using Microsoft.Win32;

namespace AiStudio4.InjectedDependencies.RequestHandlers
{
    /// <summary>
    /// Handles requests related to the file system
    /// </summary>
    public class FileSystemRequestHandler : BaseRequestHandler
    {
        private readonly IProjectFileWatcherService _fileWatcherService;

        public FileSystemRequestHandler(IProjectFileWatcherService fileWatcherService)
        {
            _fileWatcherService = fileWatcherService ?? throw new ArgumentNullException(nameof(fileWatcherService));
        }

        protected override IEnumerable<string> SupportedRequestTypes => new[]
        {  "getFileSystem", "getFileContent", "attachFile" };

        public override async Task<string> HandleAsync(string clientId, string requestType, JObject requestData)
        {
            if (requestType == "getFileSystem")
            {
                return await Task.FromResult(JsonConvert.SerializeObject(new
                {
                    success = true,
                    directories = _fileWatcherService.Directories,
                    files = _fileWatcherService.Files
                }));
            }
            else if (requestType == "getFileContent")
            {
                return await GetFileContentAsync(requestData);
            }
            else if (requestType == "attachFile")
            {
                return await AttachFileAsync(requestData);
            }

            return SerializeError($"Unknown request type: {requestType}");
        }

        private async Task<string> AttachFileAsync(JObject requestData)
        {
            try
            {

                // Create OpenFileDialog to allow user to select files
                var openFileDialog = new OpenFileDialog();
                {
                    // Configure dialog
                    openFileDialog.Title = "Select File(s) to Attach";
                    openFileDialog.Filter = "All Files (*.*)|*.*";
                    openFileDialog.Multiselect = true;
                    openFileDialog.CheckFileExists = true;
                    openFileDialog.CheckPathExists = true;

                    // Show dialog and process results
                    if (((bool)openFileDialog.ShowDialog()))
                    {
                        // Process selected files
                        var attachments = new List<object>();
                        foreach (string filePath in openFileDialog.FileNames)
                        {
                            try
                            {
                                // Get file info for metadata
                                var fileInfo = new FileInfo(filePath);
                                string fileName = Path.GetFileName(filePath);
                                string mimeType = GetMimeTypeFromExtension(Path.GetExtension(filePath));
                                bool isBinary = IsBinaryFile(mimeType);
                                
                                // Read file content
                                byte[] fileBytes = await File.ReadAllBytesAsync(filePath);
                                string base64Content = Convert.ToBase64String(fileBytes);
                                
                                // For text files, also include the text content
                                string textContent = null;
                                if (!isBinary)
                                {
                                    try
                                    {
                                        textContent = await File.ReadAllTextAsync(filePath);
                                    }
                                    catch
                                    {
                                        // If we can't read as text, just continue without text content
                                    }
                                }

                                // Add to attachments list
                                attachments.Add(new
                                {
                                    name = fileName,
                                    type = mimeType,
                                    content = base64Content,
                                    size = fileInfo.Length,
                                    lastModified = ((DateTimeOffset)fileInfo.LastWriteTime).ToUnixTimeMilliseconds(),
                                    textContent = textContent
                                });
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error processing file {filePath}: {ex.Message}");
                                // Continue with other files even if one fails
                            }
                        }

                        // Return all attachments
                        return JsonConvert.SerializeObject(new
                        {
                            success = true,
                            attachments = attachments
                        });
                    }
                    else
                    {
                        // User canceled the dialog
                        return JsonConvert.SerializeObject(new
                        {
                            success = true,
                            attachments = new object[0],
                            message = "File selection canceled"
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                return SerializeError($"Error attaching files: {ex.Message}");
            }
        }

        private async Task<string> GetFileContentAsync(JObject requestData)
        {
            try
            {
                string filePath = requestData["filePath"]?.ToString();
                
                if (string.IsNullOrEmpty(filePath))
                {
                    return SerializeError("File path is required");
                }

                if (!File.Exists(filePath))
                {
                    return SerializeError($"File not found: {filePath}");
                }

                // Get file info for metadata
                var fileInfo = new FileInfo(filePath);
                string fileName = Path.GetFileName(filePath);
                string mimeType = GetMimeTypeFromExtension(Path.GetExtension(filePath));
                bool isBinary = IsBinaryFile(mimeType);
                
                // Read file content
                byte[] fileBytes = await File.ReadAllBytesAsync(filePath);
                string base64Content = Convert.ToBase64String(fileBytes);
                
                // For text files, also include the text content
                string textContent = null;
                if (!isBinary)
                {
                    try
                    {
                        textContent = await File.ReadAllTextAsync(filePath);
                    }
                    catch
                    {
                        // If we can't read as text, just continue without text content
                    }
                }

                // Return as attachment object
                return JsonConvert.SerializeObject(new
                {
                    success = true,
                    attachment = new
                    {
                        name = fileName,
                        type = mimeType,
                        content = base64Content,
                        size = fileInfo.Length,
                        lastModified = ((DateTimeOffset)fileInfo.LastWriteTime).ToUnixTimeMilliseconds(),
                        textContent = textContent
                    }
                });
            }
            catch (Exception ex)
            {
                return SerializeError($"Error reading file: {ex.Message}");
            }
        }

        private string GetMimeTypeFromExtension(string extension)
        {
            if (string.IsNullOrEmpty(extension))
                return "application/octet-stream";

            extension = extension.ToLowerInvariant().TrimStart('.');

            // Common mime types
            var mimeTypes = new Dictionary<string, string>
            {
                { "txt", "text/plain" },
                { "html", "text/html" },
                { "htm", "text/html" },
                { "css", "text/css" },
                { "js", "text/javascript" },
                { "json", "application/json" },
                { "xml", "application/xml" },
                { "csv", "text/csv" },
                { "md", "text/markdown" },
                { "pdf", "application/pdf" },
                { "jpg", "image/jpeg" },
                { "jpeg", "image/jpeg" },
                { "png", "image/png" },
                { "gif", "image/gif" },
                { "svg", "image/svg+xml" },
                { "webp", "image/webp" },
                { "ico", "image/x-icon" },
                { "zip", "application/zip" },
                { "doc", "application/msword" },
                { "docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" },
                { "xls", "application/vnd.ms-excel" },
                { "xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" },
                { "ppt", "application/vnd.ms-powerpoint" },
                { "pptx", "application/vnd.openxmlformats-officedocument.presentationml.presentation" },
                // Code files
                { "cs", "text/plain" },
                { "ts", "text/plain" },
                { "tsx", "text/plain" },
                { "jsx", "text/plain" },
                { "py", "text/plain" },
                { "java", "text/plain" },
                { "c", "text/plain" },
                { "cpp", "text/plain" },
                { "h", "text/plain" },
                { "hpp", "text/plain" },
                { "go", "text/plain" },
                { "rs", "text/plain" },
                { "rb", "text/plain" },
                { "php", "text/plain" },
                { "swift", "text/plain" },
                { "kt", "text/plain" },
                { "scala", "text/plain" },
                { "sql", "text/plain" },
                { "sh", "text/plain" },
                { "bat", "text/plain" },
                { "ps1", "text/plain" },
                { "yaml", "text/plain" },
                { "yml", "text/plain" },
                { "toml", "text/plain" },
                { "ini", "text/plain" },
                { "cfg", "text/plain" },
                { "conf", "text/plain" },
                { "csproj", "text/plain" },
                { "sln", "text/plain" }
            };

            return mimeTypes.TryGetValue(extension, out string mimeType) ? mimeType : "application/octet-stream";
        }

        private bool IsBinaryFile(string mimeType)
        {
            // Consider all text/* mime types as non-binary
            if (mimeType.StartsWith("text/"))
                return false;

            // Some application types that are actually text
            var textApplicationTypes = new[]
            {
                "application/json",
                "application/xml",
                "application/javascript"
            };

            if (Array.IndexOf(textApplicationTypes, mimeType) >= 0)
                return false;

            // All other types are considered binary
            return true;
        }
    }
}