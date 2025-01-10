using System.Net.Sockets;
using System.Net;
using System.Text;
using WebSocketSharp.Server;
using WebSocketSharp;

public class SimpleServer
{
    private TcpListener tcpListener;
    private WebSocketServer wsServer;
    private List<TcpClient> tcpClients = new List<TcpClient>();
    private bool isRunning = false;

    public event EventHandler<string> LineReceived;

    public async Task StartServer(int tcpPort = 35000, int wsPort = 35001)
    {
        try
        {
            // Start TCP Server
            tcpListener = new TcpListener(IPAddress.Any, tcpPort);
            tcpListener.Start();

            // Start WebSocket Server
            wsServer = new WebSocketServer($"ws://0.0.0.0:{wsPort}");
            wsServer.AddWebSocketService<AiWebSocketBehavior>("/", () => new AiWebSocketBehavior(this));
            wsServer.Start();

            isRunning = true;
            Console.WriteLine($"TCP Server started on port {tcpPort}");
            Console.WriteLine($"WebSocket Server started on port {wsPort}");

            while (isRunning)
            {
                TcpClient client = await tcpListener.AcceptTcpClientAsync();
                tcpClients.Add(client);
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

                string chunk = Encoding.UTF8.GetString(buffer, 0, bytesRead);
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
            tcpClients.Remove(client);
            client.Close();
        }
    }

    public virtual void OnLineReceived(string line)
    {
        LineReceived?.Invoke(this, line);
    }

    public async Task BroadcastLineAsync(string message)
    {
        // TCP Broadcast
        byte[] data = Encoding.UTF8.GetBytes(message + "\n");
        foreach (var client in tcpClients)
        {
            try
            {
                NetworkStream stream = client.GetStream();
                await stream.WriteAsync(data, 0, data.Length);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error broadcasting TCP message: {ex.Message}");
            }
        }

        // WebSocket Broadcast
        if (wsServer != null)
        {
            wsServer.WebSocketServices["/"].Sessions.Broadcast(message);
        }
    }

    public void Stop()
    {
        isRunning = false;
        tcpListener?.Stop();
        wsServer?.Stop();
        foreach (var client in tcpClients)
        {
            client.Close();
        }
        tcpClients.Clear();
    }
}

public class AiWebSocketBehavior : WebSocketBehavior
{
    private readonly SimpleServer _server;

    public AiWebSocketBehavior(SimpleServer server)
    {
        _server = server;
    }

    protected override void OnMessage(MessageEventArgs e)
    {
        try
        {
            // Process the raw message directly like TCP messages
            string message = e.Data;

            // Trigger the same LineReceived event as TCP messages
            _server.OnLineReceived(message);

            // Note: The response will be handled by whatever is subscribed to LineReceived
            // The subscriber should use BroadcastLineAsync to send responses
        }
        catch (Exception ex)
        {
            Send($"Error processing message: {ex.Message}");
        }
    }
}