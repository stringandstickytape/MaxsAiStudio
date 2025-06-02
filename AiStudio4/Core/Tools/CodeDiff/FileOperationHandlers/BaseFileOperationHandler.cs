
using AiStudio4.Core.Interfaces;
using AiStudio4.Core.Tools.CodeDiff.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace AiStudio4.Core.Tools.CodeDiff.FileOperationHandlers
{
    
    
    
    public abstract class BaseFileOperationHandler
    {
        protected readonly ILogger _logger;
        protected readonly IStatusMessageService _statusMessageService;
        protected readonly string _clientId;

        protected BaseFileOperationHandler(ILogger logger, IStatusMessageService statusMessageService, string clientId)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _statusMessageService = statusMessageService;
            _clientId = clientId;
        }

        
        
        
        
        
        
        public abstract Task<FileOperationResult> HandleAsync(string filePath, JObject change);

        
        
        
        
        protected async void SendStatusUpdate(string statusMessage)
        {
            try
            {
                
                if (_statusMessageService != null && !string.IsNullOrEmpty(_clientId))
                {
                    await _statusMessageService.SendStatusMessageAsync(_clientId, statusMessage);
                }
                else
                {
                    _logger.LogDebug("Status update not sent - missing StatusMessageService or clientId: {Message}", statusMessage);
                }
            }
            catch (Exception ex)
            {
                
                _logger.LogWarning(ex, "Failed to send status update: {Message}", statusMessage);
            }
        }

        
        
        
        
        protected void EnsureDirectoryExists(string filePath)
        {
            string targetDir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(targetDir) && !Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir);
                _logger.LogInformation("Created directory '{DirectoryPath}' for file operation.", targetDir);
            }
        }

        
        
        
        protected static string RemoveBacktickQuotingIfPresent(string content)
        {
            if (string.IsNullOrWhiteSpace(content)) return content;

            content = content.Trim(); 

            if (content.StartsWith("```") && content.EndsWith("```"))
            {
                content = content.Substring(3, content.Length - 6).Trim(); 

                
                var firstNewLine = content.IndexOfAny(new[] { '\r', '\n' });
                if (firstNewLine >= 0)
                {
                    string firstLine = content.Substring(0, firstNewLine).Trim();
                    
                    if (firstLine.Length > 0 && firstLine.Length < 20 && !firstLine.Any(c => c == ' ' || c == '{' || c == '(' || c == ';'))
                    {
                        content = content.Substring(firstNewLine).TrimStart(); 
                    }
                }
                return content; 
            }
            return content; 
        }
    }
}
