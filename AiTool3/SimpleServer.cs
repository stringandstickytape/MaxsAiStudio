using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace AITool3
{
    public class SimpleServer
    {
        private TcpListener listener;
        private List<TcpClient> clients = new List<TcpClient>();
        private bool isRunning = false;

        public event EventHandler<string> LineReceived;

        public async Task StartServer(int port = 35000)
        {
            try
            {
                listener = new TcpListener(IPAddress.Any, port);
                listener.Start();
                isRunning = true;

                Console.WriteLine($"Server started on port {port}");

                while (isRunning)
                {
                    TcpClient client = await listener.AcceptTcpClientAsync();
                    clients.Add(client);
                    _ = HandleClientAsync(client);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error starting server: {ex.Message}");
            }
        }

        private async Task HandleClientAsync(TcpClient client)
        {
            try
            {
                using NetworkStream stream = client.GetStream();
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
                        OnLineReceived(line);
                        messageBuilder.Remove(0, newlineIndex + 1);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling client: {ex.Message}");
            }
            finally
            {
                clients.Remove(client);
                client.Close();
            }
        }

        protected virtual void OnLineReceived(string line)
        {
            LineReceived?.Invoke(this, line);
        }

        public async Task BroadcastLineAsync(string message)
        {
            byte[] data = Encoding.UTF8.GetBytes(message + "\n");
            foreach (var client in clients)
            {
                try
                {
                    NetworkStream stream = client.GetStream();
                    await stream.WriteAsync(data, 0, data.Length);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error broadcasting message: {ex.Message}");
                }
            }
        }

        public void Stop()
        {
            isRunning = false;
            listener?.Stop();
            foreach (var client in clients)
            {
                client.Close();
            }
            clients.Clear();
        }
    }
}