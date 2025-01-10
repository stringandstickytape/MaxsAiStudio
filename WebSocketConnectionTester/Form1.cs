using Newtonsoft.Json;
using SharedClasses;
using SharedClasses.Models;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;

namespace WebSocketConnectionTester
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private ClientWebSocket? ws;  // Make it nullable and don't initialize here

        static async Task SendMessageAsync(ClientWebSocket ws, string message)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(message);
            await ws.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
        }

        static async Task ReceiveMessagesAsync(ClientWebSocket ws)
        {
            byte[] buffer = new byte[4096];
            while (ws.State == WebSocketState.Open)
            {
                var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                }
                else
                {
                    string receivedMessage = Encoding.UTF8.GetString(buffer, 0, result.Count);

                    var response = JsonConvert.DeserializeObject<VsixMessage>(receivedMessage);
                    var response2 = JsonConvert.DeserializeObject<VsixCompletionRequestResult>(response.Content);
                    Debug.WriteLine($"Received message: {response2.Content}\nThis conversation GUID: {response2.Guid}");
                }
            }
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            // Create a new WebSocket instance for each connection
            if (ws != null && ws.State == WebSocketState.Open)
            {
                try
                {
                    // Close the previous connection
                    await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing previous connection", CancellationToken.None);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error closing previous connection: {ex.Message}");
                }
            }

            ws = new ClientWebSocket();
            Uri serverUri = new Uri("ws://localhost:35001/");

            try
            {
                await ws.ConnectAsync(serverUri, CancellationToken.None);
                Debug.WriteLine("WebSocket connection opened.");

                // Send a test message
                VsixMessage testMessage = new VsixMessage { Content = "Hello from WebSocket client!", MessageType = "vsRunCompletion" };
                string jsonMessage = JsonConvert.SerializeObject(testMessage);
                await SendMessageAsync(ws, jsonMessage);
                Debug.WriteLine($"Sent message: {jsonMessage}");

                // Start receiving messages
                _ = ReceiveMessagesAsync(ws);

                Debug.WriteLine("Press Enter to close the connection and exit...");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error: {ex.Message}");
            }
        }

        // Optional: Add form closing handler to clean up
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            if (ws != null && ws.State == WebSocketState.Open)
            {
                ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Form closing", CancellationToken.None).Wait();
            }
        }
    }
}