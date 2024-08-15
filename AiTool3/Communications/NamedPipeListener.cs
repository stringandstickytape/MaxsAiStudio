using AiTool3.Conversations;
using AiTool3.DataModels;
using AiTool3.Helpers;
using AiTool3.Providers;
using AiTool3.Tools;
using Newtonsoft.Json;
using System.Diagnostics;
using System.IO.Pipes;
using System.Text;

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

    internal async Task RunCodeAssistant(SettingsSet settings, ToolManager toolManager, VSCodeSelection selection)
    {
        // create a new one-off summary-model conversation with the selected text as the user prompt
        var summaryModel = settings.GetSummaryModel();
        var tempConversationManager = new ConversationManager();
        tempConversationManager.Conversation = new BranchedConversation { ConvGuid = Guid.NewGuid().ToString() };
        tempConversationManager.Conversation.AddNewRoot();

        var content = $"{MaxsAiStudio.ThreeTicks}\n{selection.Before}<CURSOR LOCATION>{selection.After}\n{MaxsAiStudio.ThreeTicks}\n\n The user's instruction is: \n{MaxsAiStudio.ThreeTicks}\n{selection.Selected}\n{MaxsAiStudio.ThreeTicks}\n\n";

        var conversation = new Conversation(DateTime.Now)
        {
            systemprompt = "You are a code completion AI. You return a single code block which will be inserted in the user's current cursor location. The code block must be in the correct language and satisfy the user's request, based on the context before and after the user's current cursor location.",
            messages = new List<ConversationMessage>
                {
                new ConversationMessage { role = "user", content = content }
                }
        };

        var aiService = AiServiceResolver.GetAiService(summaryModel.ServiceName, toolManager);
        var response = await aiService.FetchResponse(summaryModel, conversation, null, null, CancellationToken.None, settings, mustNotUseEmbedding: true, toolNames: null, useStreaming: false);

        var txt = SnippetHelper.StripFirstAndLastLine(response.ResponseText);

        await SendResponseAsync(txt);
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