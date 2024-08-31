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
        private ConcurrentQueue<string> messageQueue;
        private SemaphoreSlim semaphore;
        private const int MaxConcurrentProcessing = 5; // Adjust this value as needed

        public event EventHandler<string> LineReceived;

        public SimpleClient()
        {
            messageQueue = new ConcurrentQueue<string>();
            semaphore = new SemaphoreSlim(MaxConcurrentProcessing, MaxConcurrentProcessing);
        }

        public async Task StartClient()
        {
            try
            {
                client = new TcpClient();
                await client.ConnectAsync("localhost", 35000);
                stream = client.GetStream();

                // Start listening for incoming messages
                _ = Task.Run(ReceiveMessages);

                // Start processing messages
                _ = Task.Run(ProcessMessages);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error starting client: {ex.Message}");
            }
        }

        public void SendLine(string message)
        {
            if (client == null || !client.Connected)
            {
                throw new InvalidOperationException("Client is not connected.");
            }

            byte[] data = Encoding.ASCII.GetBytes(message + "\n");
            stream.Write(data, 0, data.Length);
        }

        private async Task ReceiveMessages()
        {
            byte[] buffer = new byte[1024];
            StringBuilder messageBuilder = new StringBuilder();

            while (true)
            {
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead == 0) break;

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
        }

        private async Task ProcessMessages()
        {
            while (true)
            {
                await semaphore.WaitAsync();

                try
                {
                    if (messageQueue.TryDequeue(out string message))
                    {
                        _ = Task.Run(() => OnLineReceived(message));
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