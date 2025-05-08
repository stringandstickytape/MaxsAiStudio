// AiStudio4.Core/Interfaces/IAudioTranscriptionService.cs
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace AiStudio4.Core.Interfaces
{
    public interface IAudioTranscriptionService
    {
        Task<string> TranscribeAudioAsync(Stream audioStream, string clientId = null, CancellationToken cancellationToken = default);
    }
}