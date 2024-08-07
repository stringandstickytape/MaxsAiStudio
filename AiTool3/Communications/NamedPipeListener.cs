
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

    public NamedPipeListener()
    {
        StartListening();
    }

    private async Task StartListening()
    {
        // Create a named pipe server
        pipeServer = new NamedPipeServerStream("MaxsAIStudioVSIX", PipeDirection.InOut);

        // Start a new task to listen for incoming connections
        await Task.Run(async () => 
        {
            while (true)
            {
                try
                {
                    // Wait for a client to connect
                    pipeServer.WaitForConnection();
                    Debug.WriteLine("Client connected.");

                    // Create a StreamReader to read from the pipe
                    using (var reader = new StreamReader(pipeServer))
                    {
                        // Read messages from the pipe until the client disconnects
                        string message = await reader.ReadLineAsync();
                        StringBuilder fullMessage = new StringBuilder();
                        while (message != "<END>")
                        {
                            fullMessage.AppendLine(message);
                            message = await reader.ReadLineAsync();
                        }
                        string returnMessage = fullMessage.ToString();
                        Debug.Write(returnMessage);

                        using (var writer = new StreamWriter(pipeServer))
                        {
                            writer.AutoFlush = true;
                            await writer.WriteAsync("Reply!" + "\n<END>\n");
                            await writer.FlushAsync();
                        }
                    }
                }
                catch (IOException ex)
                {
                    Debug.WriteLine($"IO Exception: {ex.Message}");
                }
                catch (Exception ex)
                {
                    // Handle other exceptions
                    Debug.WriteLine($"Unexpected error: {ex.Message}");
                }
                finally
                {
                    // Dispose of the pipeServer and create a new one for the next connection
                    pipeServer.Dispose();
                    pipeServer = new NamedPipeServerStream("MaxsAIStudioVSIX", PipeDirection.InOut);
                }
            }
        });
    }
}
