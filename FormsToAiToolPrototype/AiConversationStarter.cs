using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SharedClasses;
using SharedClasses.Models; // Assuming VsixMessage is defined here

namespace FormsToAiToolPrototype
{

        public class AiConversationStarter
        {
            private readonly string _serverAddress;
            private readonly int _serverPort;
            private Form _parentForm;

            public event EventHandler<string> ResponseReceived; // Event to signal when a response is received

            public AiConversationStarter(Form parentForm, string serverAddress = "localhost", int serverPort = 35000)
            {
                _parentForm = parentForm;
                _serverAddress = serverAddress;
                _serverPort = serverPort;
                
                // Set up form resize handling
                _parentForm.Resize += HandleFormResize;
            }

            public async Task StartConversationAsync(string initialPrompt)
            {
                try
                {
                    using (var client = new TcpClient())
                    {
                        await client.ConnectAsync(_serverAddress, _serverPort);

                        using (var stream = client.GetStream())
                        {
                            // 1. Send initial conversation request
                            var requestMessage = new VsixMessage
                            {
                                MessageType = "vsRunCompletion", // Or whatever message type your AI app expects
                                Content = initialPrompt
                            };
                            var requestJson = JsonConvert.SerializeObject(requestMessage);
                            await SendLineAsync(stream, requestJson);

                            // 2. Listen for the response
                            var responseJson = await ReceiveLineAsync(stream);

                            // 3. Raise an event with the response
                            OnResponseReceived(responseJson);
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Handle connection or communication errors
                    OnResponseReceived($"Error: {ex.Message}");
                }
            }

            protected virtual void OnResponseReceived(string response)
            {
                ResponseReceived?.Invoke(this, response);
            }

            private async Task SendLineAsync(NetworkStream stream, string message)
            {
                byte[] data = Encoding.UTF8.GetBytes(message + "\n");
                await stream.WriteAsync(data, 0, data.Length);
            }

            private async Task<string> ReceiveLineAsync(NetworkStream stream)
            {
                byte[] buffer = new byte[1024];
                StringBuilder messageBuilder = new StringBuilder();
                int bytesRead;

                do
                {
                    bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    string chunk = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    messageBuilder.Append(chunk);
                } while (bytesRead == buffer.Length); // Continue reading until less than a full buffer is received

                return messageBuilder.ToString();
        }

        private void HandleFormResize(object sender, EventArgs e)
        {
            // Resize all controls on the form to maintain proportions
            foreach (Control control in _parentForm.Controls)
            {
                // Calculate new size based on form size
                if (control is TextBox || control is RichTextBox)
                {
                    control.Width = _parentForm.ClientSize.Width - 40; // Padding of 20 on each side
                    control.Height = _parentForm.ClientSize.Height / 2 - 40; // Take up half the height with padding
                }
                else if (control is Button)
                {
                    control.Width = Math.Min(200, _parentForm.ClientSize.Width - 40);
                    // Keep the button at the bottom of the form
                    control.Top = _parentForm.ClientSize.Height - control.Height - 20;
                    // Center the button horizontally
                    control.Left = (_parentForm.ClientSize.Width - control.Width) / 2;
                }
            }
        }
    }
}