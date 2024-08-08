using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading.Tasks;

namespace AiTool3.Communications;

public class NamedPipeListener
{
    private NamedPipeServerStream pipeServer;
    private StreamReader reader;
    private StreamWriter writer;

    public event EventHandler<string> NamedPipeMessageReceived;

    public NamedPipeListener()
    {
        StartListening();
    }

    private async Task StartListening()
    {
        while (true)
        {
            try
            {
                pipeServer = new NamedPipeServerStream("MaxsAIStudioVSIX", PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);

                await pipeServer.WaitForConnectionAsync();
                Debug.WriteLine("Client connected.");

                reader = new StreamReader(pipeServer);
                writer = new StreamWriter(pipeServer) { AutoFlush = true };

                while (true)
                {
                    string message = await reader.ReadLineAsync();
                    if (message == null) break; // Client disconnected

                    StringBuilder fullMessage = new StringBuilder();
                    while (message != "<END>")
                    {
                        fullMessage.AppendLine(message);
                        message = await reader.ReadLineAsync();
                        if (message == null) break; // Client disconnected
                    }

                    if (message == null) break; // Client disconnected

                    string returnMessage = fullMessage.ToString();
                    NamedPipeMessageReceived?.Invoke(this, returnMessage);
                }
            }
            catch (IOException ex)
            {
                Debug.WriteLine($"IO Exception: {ex.Message}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unexpected error: {ex.Message}");
            }
            finally
            {
                CloseConnection();
            }
        }
    }

    private void CloseConnection()
    {
        reader?.Dispose();
        writer?.Dispose();
        pipeServer?.Dispose();
        reader = null;
        writer = null;
        pipeServer = null;
    }

    internal async Task SendResponseAsync(string responseText)
    {
        if (pipeServer != null && pipeServer.IsConnected)
        {
            try
            {
                await writer.WriteLineAsync(responseText);
                await writer.WriteLineAsync("<END>");
                await writer.FlushAsync();
                Debug.WriteLine("Response sent: " + responseText);

            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error sending response: {ex.Message}");
            }
        }
        else
        {
            Debug.WriteLine("Cannot send response, pipe is not connected.");
        }
    }

    public class VSCodeSelection
    {
        [JsonProperty("before")]
        public string Before { get; set; }

        [JsonProperty("selected")]
        public string Selected { get; set; }

        [JsonProperty("after")]
        public string After { get; set; }
    }
}