using System;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace VSIXTest
{
    public class SimpleClient
    {
        private TcpClient client;
        private NetworkStream stream;
        private readonly ConcurrentQueue<string> messageQueue;
        private readonly SemaphoreSlim semaphore;
        private const int MaxConcurrentProcessing = 5; // Adjust this value as needed
        private const int ReconnectionDelayMs = 5000; // 5 seconds delay between reconnection attempts

        public event EventHandler<string> LineReceived;

        public SimpleClient()
        {
            messageQueue = new ConcurrentQueue<string>();
            semaphore = new SemaphoreSlim(MaxConcurrentProcessing, MaxConcurrentProcessing);
        }

        public async Task StartClientAsync()
        {
            while (true)
            {
                try
                {
                    await ConnectClientAsync();

                    // Start listening for incoming messages
                    _ = Task.Run(ReceiveMessagesAsync);

                    // Start processing messages
                    _ = Task.Run(ProcessMessagesAsync);

                    return; // Exit the method if connection is successful
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error starting client: {ex.Message}");
                    Console.WriteLine($"Attempting to reconnect in {ReconnectionDelayMs / 1000} seconds...");
                    await Task.Delay(ReconnectionDelayMs);
                }
            }
        }

        private async Task ConnectClientAsync()
        {
            client = new TcpClient();
            await client.ConnectAsync("localhost", 35000);
            stream = client.GetStream();
        }

        public async Task SendLineAsync(string message)
        {
            if (client == null || !client.Connected)
            {
                await ReconnectIfNeededAsync();
            }

            byte[] data = Encoding.ASCII.GetBytes(message + "\n");
            await stream.WriteAsync(data, 0, data.Length);
        }

        private async Task ReconnectIfNeededAsync()
        {
            while (client == null || !client.Connected)
            {
                try
                {
                    Console.WriteLine("Client is not connected. Attempting to reconnect...");
                    await ConnectClientAsync();
                    Console.WriteLine("Reconnection successful.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Reconnection failed: {ex.Message}");
                    Console.WriteLine($"Retrying in {ReconnectionDelayMs / 1000} seconds...");
                    await Task.Delay(ReconnectionDelayMs);
                }
            }
        }

        private async Task ReceiveMessagesAsync()
        {
            byte[] buffer = new byte[1024];
            StringBuilder messageBuilder = new StringBuilder();

            while (true)
            {
                try
                {
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead == 0) continue;

                    string chunk = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                    messageBuilder.Append(chunk);

                    int newlineIndex;
                    while ((newlineIndex = messageBuilder.ToString().IndexOf('\n')) != -1)
                    {
                        string line = messageBuilder.ToString(0, newlineIndex);
                        messageQueue.Enqueue(line);
                        messageBuilder.Remove(0, newlineIndex + 1);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error receiving messages: {ex.Message}");
                    await ReconnectIfNeededAsync();
                }
            }
        }

        private async Task ProcessMessagesAsync()
        {
            while (true)
            {
                await semaphore.WaitAsync();

                try
                {
                    if (messageQueue.TryDequeue(out string message))
                    {
                        OnLineReceived(message);
                    }
                    else
                    {
                        await Task.Delay(10); // Small delay if queue is empty
                    }
                }
                finally
                {
                    semaphore.Release();
                }
            }
        }

        protected virtual void OnLineReceived(string line)
        {
            LineReceived?.Invoke(this, line);
        }

        public void Stop()
        {
            client?.Close();
            stream?.Dispose();
        }
    }
}