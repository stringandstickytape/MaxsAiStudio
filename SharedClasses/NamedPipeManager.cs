using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SharedClasses
{
    using System;
    using System.Collections.Concurrent;
    using System.IO;
    using System.IO.Pipes;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Newtonsoft.Json;

    public class NamedPipeManager : IDisposable
    {
        private readonly string outgoingPipeName;
        private readonly string incomingPipeName;
        private NamedPipeClientStream outgoingPipe;
        private NamedPipeServerStream incomingPipe;
        private StreamWriter writer;
        private StreamReader reader;
        private CancellationTokenSource cts;
        private readonly bool isVsix;
        private readonly ConcurrentQueue<object> messageQueue;
        private readonly SemaphoreSlim sendSemaphore;
        private readonly int maxConcurrentSends;

        public event EventHandler<object> ReceiveMessage;
        public event EventHandler<Exception> ErrorOccurred;

        public NamedPipeManager(bool isVsix, int maxConcurrentSends = 5)
        {
            this.isVsix = isVsix;
            this.maxConcurrentSends = maxConcurrentSends;
            if (isVsix)
            {
                outgoingPipeName = "MaxsAiStudioVsixToApp";
                incomingPipeName = "MaxsAiStudioAppToVsix";
            }
            else
            {
                outgoingPipeName = "MaxsAiStudioAppToVsix";
                incomingPipeName = "MaxsAiStudioVsixToApp";
            }

            cts = new CancellationTokenSource();
            messageQueue = new ConcurrentQueue<object>();
            sendSemaphore = new SemaphoreSlim(maxConcurrentSends, maxConcurrentSends);
            InitializePipes();
        }

        private void InitializePipes()
        {
            outgoingPipe = new NamedPipeClientStream(".", outgoingPipeName, PipeDirection.Out);
            if (incomingPipe != null)
                incomingPipe.Dispose();
                incomingPipe = new NamedPipeServerStream(incomingPipeName, PipeDirection.In, 3);
        }

        public async Task ConnectAsync()
        {
            await ConnectOutgoingPipeAsync();
            await ConnectIncomingPipeAsync();
            _ = ProcessMessageQueueAsync();
        }

        private async Task ConnectOutgoingPipeAsync()
        {
            int attempt = 0;
            while (!cts.Token.IsCancellationRequested)
            {
                try
                {
                    string pipeName = attempt == 0 ? outgoingPipeName : $"{outgoingPipeName}_{attempt}";
                    outgoingPipe = new NamedPipeClientStream(".", pipeName, PipeDirection.Out);
                    await outgoingPipe.ConnectAsync(cts.Token);
                    writer = new StreamWriter(outgoingPipe);
                    break;
                }
                catch (Exception ex)
                {
                    ErrorOccurred?.Invoke(this, ex);
                    attempt++;
                    if (attempt > 10) // Limit the number of attempts
                    {
                        throw new Exception("Unable to connect to outgoing pipe after multiple attempts");
                    }
                    await Task.Delay(1000, cts.Token);
                }
            }
        }

        private async Task ConnectIncomingPipeAsync()
        {
            while (!cts.Token.IsCancellationRequested)
            {
                try
                {
                    await incomingPipe.WaitForConnectionAsync(cts.Token);
                    reader = new StreamReader(incomingPipe);
                    _ = ListenForMessagesAsync();
                    break;
                }
                catch (Exception ex)
                {
                    ErrorOccurred?.Invoke(this, ex);
                    await Task.Delay(1000, cts.Token);
                }
            }
        }

        public void EnqueueMessage(object message)
        {
            messageQueue.Enqueue(message);
        }

        private async Task ProcessMessageQueueAsync()
        {
            while (!cts.Token.IsCancellationRequested)
            {
                if (messageQueue.TryDequeue(out object message))
                {
                    await sendSemaphore.WaitAsync(cts.Token);
                    try
                    {
                        await SendMessageInternal(message);
                    }
                    finally
                    {
                        sendSemaphore.Release();
                    }
                }
                else
                {
                    await Task.Delay(100, cts.Token);
                }
            }
        }

        private async Task SendMessageInternal(object message)
        {
            if (writer == null || outgoingPipe.IsConnected == false)
            {
                await ReconnectOutgoingPipeAsync();
            }

            try
            {
                string jsonMessage = JsonConvert.SerializeObject(message);
                await writer.WriteLineAsync(jsonMessage);
                await writer.FlushAsync();
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, ex);
                await ReconnectOutgoingPipeAsync();
                messageQueue.Enqueue(message); // Re-queue the failed message
            }
        }

        private async Task ReconnectOutgoingPipeAsync()
        {
            writer?.Dispose();
            outgoingPipe?.Dispose();
            InitializePipes();
            await ConnectOutgoingPipeAsync();
        }

        private async Task ListenForMessagesAsync()
        {
            while (!cts.Token.IsCancellationRequested)
            {
                try
                {
                    string jsonMessage = await reader.ReadLineAsync();
                    if (jsonMessage != null)
                    {
                        object message = JsonConvert.DeserializeObject(jsonMessage);
                        ReceiveMessage?.Invoke(this, message);
                    }
                    else
                    {
                        await ReconnectIncomingPipeAsync();
                    }
                }
                catch (Exception ex)
                {
                    ErrorOccurred?.Invoke(this, ex);
                    await ReconnectIncomingPipeAsync();
                }
            }
        }

        private async Task ReconnectIncomingPipeAsync()
        {
            reader?.Dispose();
            incomingPipe?.Dispose();
            InitializePipes();
            await ConnectIncomingPipeAsync();
        }

        public void Dispose()
        {
            cts.Cancel();
            writer?.Dispose();
            reader?.Dispose();
            outgoingPipe?.Dispose();
            incomingPipe?.Dispose();
            sendSemaphore.Dispose();
        }
    }
}
