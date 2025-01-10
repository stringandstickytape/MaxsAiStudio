using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace AITool3
{
    public class SimpleServer
    {
        private TcpListener listener;
        private Dictionary<string, TcpClient> clients = new Dictionary<string, TcpClient>();
        private bool isRunning = false;
        private string clientId = "";

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
                    string clientId = Guid.NewGuid().ToString();
                    clients.Add(clientId, client);
                    _ = HandleClientAsync(client, clientId);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error starting server: {ex.Message}");
            }
        }

        private async Task HandleClientAsync(TcpClient client, string clientId)
        {
            this.clientId = clientId;
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
                clients.Remove(clientId);
                client.Close();
                clientId = "";
            }
        }

        protected virtual void OnLineReceived(string line)
        {
            LineReceived?.Invoke(this, line);
        }

        public async Task BroadcastLineAsync(string message, string clientId = null)
        {
            byte[] data = Encoding.UTF8.GetBytes(message + "\n");
            
            if(clientId == null && clients.Count == 1)
            {
                clientId = clients.First().Key;
            }

            if (clients.Any() && clients.ContainsKey(clientId))
            {
                TcpClient client = clients[clientId];

                try
                {
                    await client.GetStream().WriteAsync(data, 0, data.Length);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error broadcasting message: {ex.Message}");
                }
            }
        }

        public void Stop()
        {
            isRunning = false;
            listener?.Stop();
            foreach (var client in clients.Values)
            {
                client.Close();
            }
            clients.Clear();
        }
    }
}