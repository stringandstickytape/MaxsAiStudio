// AiStudio4/InjectedDependencies/RequestHandlers/FileSystemRequestHandler.cs
using AiStudio4.Core.Interfaces;
using AiStudio4.InjectedDependencies;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Threading.Tasks;

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
        {  "getFileSystem" };

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

            return SerializeError($"Unknown request type: {requestType}");
        }
    }
}