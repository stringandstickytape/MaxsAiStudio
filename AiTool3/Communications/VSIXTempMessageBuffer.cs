using System.Text;
using Newtonsoft.Json;

namespace AiTool3.Communications
{ 
    public class VSIXTempMessageBuffer
    {
        private readonly StringBuilder buffer = new StringBuilder();
        private readonly int bufferThreshold;
        private readonly Func<string, Task> sendToVsixAsync;
        private readonly Func<string, Task> executeScriptAsync;

        public VSIXTempMessageBuffer(
            Func<string, Task> sendToVsixAsync,
            Func<string, Task> executeScriptAsync,
            int bufferThreshold = 20)
        {
            this.sendToVsixAsync = sendToVsixAsync ?? throw new ArgumentNullException(nameof(sendToVsixAsync));
            this.executeScriptAsync = executeScriptAsync ?? throw new ArgumentNullException(nameof(executeScriptAsync));
            this.bufferThreshold = bufferThreshold;
        }

        public async Task UpdateTemp(string message)
        {
            buffer.Append(message);

            if (buffer.Length > bufferThreshold)
            {
                await SendBufferedTempToVsix();
            }

            await executeScriptAsync($"appendMessageText('temp-ai-msg', {JsonConvert.SerializeObject(message)}, 1)");
        }

        private async Task SendBufferedTempToVsix()
        {
            if (buffer.Length > 0)
            {
                string bufferedContent = buffer.ToString();
                //await sendToVsixAsync($"appendMessageText('temp-ai-msg', {JsonConvert.SerializeObject(bufferedContent)}, 1)");
                buffer.Clear();
            }
        }

        public void ClearVSIXTempBuffer()
        {
            buffer.Clear();
        }

        public async Task FlushVSIXTempBuffer()
        {
            await SendBufferedTempToVsix();
        }
    }
}
