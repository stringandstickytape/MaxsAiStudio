using System;
using System.Diagnostics;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SharedClasses
{
    public class TcpCommsManager : IDisposable
    {
        private const int Port = 35000;
        private TcpClient client;
        private TcpListener listener;
        private NetworkStream stream;
        private CancellationTokenSource cts;
        private readonly bool isVsix;
        private readonly ConcurrentQueue<object> messageQueue;
        private readonly SemaphoreSlim sendSemaphore;
        private readonly int maxConcurrentSends;

        public event EventHandler<object> ReceiveMessage;
        public event EventHandler<Exception> ErrorOccurred;

        public TcpCommsManager(bool isVsix, int maxConcurrentSends = 5)
        {
            this.isVsix = isVsix;
            this.maxConcurrentSends = maxConcurrentSends;
            cts = new CancellationTokenSource();
            messageQueue = new ConcurrentQueue<object>();
            sendSemaphore = new SemaphoreSlim(maxConcurrentSends, maxConcurrentSends);
            InitializeTcp();
        }

        private void InitializeTcp()
        {
            if (isVsix)
            {
                client = new TcpClient();
            }
            else
            {
                listener = new TcpListener(IPAddress.Loopback, Port);
            }
        }

        public async Task ConnectAsync()
        {
            if (isVsix)
            {
                await ConnectClientAsync();
            }
            else
            {
                await StartServerAsync();
            }
            _ = ProcessMessageQueueAsync();
        }

        private async Task ConnectClientAsync()
        {
            int maxAttempts = 5;
            int attempt = 0;
            while (!cts.Token.IsCancellationRequested && attempt < maxAttempts)
            {
                try
                {
                    await client.ConnectAsync(IPAddress.Loopback, Port);
                    stream = client.GetStream();
                    Debug.WriteLine($"Connected to server on port {Port}");
                    _ = ListenForMessagesAsync();


                    return;
                }
                catch (Exception ex)
                {
                    attempt++;
                    if (attempt >= maxAttempts)
                    {
                        ErrorOccurred?.Invoke(this, ex);
                        throw new Exception($"Unable to connect to server on port {Port} after {maxAttempts} attempts", ex);
                    }
                    await Task.Delay(1000, cts.Token);
                }
            }
        }

        private async Task StartServerAsync()
        {
            listener.Start();
            Debug.WriteLine($"Server listening on port {Port}");
            TcpClient clientConnection = await listener.AcceptTcpClientAsync();
            stream = clientConnection.GetStream();
            Debug.WriteLine($"Client connected on port {Port}");
            _ = ListenForMessagesAsync();
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
            if (stream == null || (isVsix && !client.Connected))
            {
                await ReconnectAsync();
            }

            try
            {
                string jsonMessage = JsonConvert.SerializeObject(message);
                byte[] messageBytes = Encoding.UTF8.GetBytes(jsonMessage + "\n");
                await stream.WriteAsync(messageBytes, 0, messageBytes.Length);
                await stream.FlushAsync();
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, ex);
                await ReconnectAsync();
                messageQueue.Enqueue(message);
            }
        }

        private async Task ReconnectAsync()
        {
            if (stream != null)
            {
                stream.Dispose();
            }

            if (isVsix)
            {
                if (client != null)
                {
                    if (client.Connected)
                    {
                        client.Close(); // Properly close the existing connection
                    }
                    client.Dispose(); // Dispose the existing client
                }

                client = new TcpClient(); // Initialize a new TcpClient instance
                await ConnectClientAsync(); // Attempt to connect again
            }
            else
            {
                // Server side logic (if needed)
                await StartServerAsync();
            }
        }

        private async Task ListenForMessagesAsync()
        {
            byte[] buffer = new byte[4096];
            StringBuilder messageBuilder = new StringBuilder();

            while (!cts.Token.IsCancellationRequested)
            {
                try
                {
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead > 0)
                    {
                        messageBuilder.Append(Encoding.UTF8.GetString(buffer, 0, bytesRead));
                        string messages = messageBuilder.ToString();
                        string[] splitMessages = messages.Split('\n');

                        for (int i = 0; i < splitMessages.Length - 1; i++)
                        {
                            string jsonMessage = splitMessages[i];
                            object message = JsonConvert.DeserializeObject(jsonMessage);
                            ReceiveMessage?.Invoke(this, message);
                        }

                        messageBuilder.Clear();
                        messageBuilder.Append(splitMessages[splitMessages.Length - 1]);
                    }
                    else
                    {
                        //Thread.Sleep(1);
                        await ReconnectAsync();
                    }
                }
                catch (Exception ex)
                {
                    ErrorOccurred?.Invoke(this, ex);
                    await ReconnectAsync();
                }
            }
        }

        public void Dispose()
        {
            cts.Cancel();
            stream?.Dispose();
            if (isVsix)
            {
                client?.Dispose();
            }
            else
            {
                listener?.Stop();
            }
            sendSemaphore.Dispose();
        }
    }
}