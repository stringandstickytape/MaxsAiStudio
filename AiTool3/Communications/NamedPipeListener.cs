using AiTool3.Conversations;
using AiTool3.DataModels;
using AiTool3.Helpers;
using AiTool3.Providers;
using AiTool3.Tools;
using Newtonsoft.Json;
using SharedClasses;
using System.Diagnostics;
using System.IO.Pipes;
using System.Text;
using System.Windows.Forms;

namespace AiTool3.Communications;

public class NamedPipeListener
{
    private NamedPipeServerStream pipeServer;
    private StreamReader reader;
    private StreamWriter writer;

    public event EventHandler<VsixMessage> NamedPipeMessageReceived;

    public NamedPipeListener()
    {
        StartListening();
    }

    private async Task StartListening()
    {
        pipeServer = new NamedPipeServerStream("MaxsAIStudioVSIX", PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);

        await pipeServer.WaitForConnectionAsync();
        Debug.WriteLine("Client connected.");

        reader = new StreamReader(pipeServer);
        writer = new StreamWriter(pipeServer) { AutoFlush = true };

        while (true)
        {
            try
            {
                string message = await reader.ReadLineAsync();
                if(message==null)
                {
                    continue;
                }
                var vsixMessage = JsonConvert.DeserializeObject<VsixMessage>(message);
                NamedPipeMessageReceived?.Invoke(this, vsixMessage);
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
                //CloseConnection();
            }
        }
    }

    internal async Task SendResponseAsync(char messageType, string responseText)
    {
        if (pipeServer != null && pipeServer.IsConnected)
        {
            try
            {
                string jsonMessage = JsonConvert.SerializeObject($"{messageType}{responseText}");
                await writer.WriteLineAsync(jsonMessage);
                await writer.FlushAsync();
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
}