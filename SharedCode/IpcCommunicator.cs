using System;
using System.IO;
using System.IO.Pipes;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace AiTool3.SharedCode
{ 
    public static class IpcCommunicator
    {
        private const string DefaultPipeName = "MyAppIpcPipe";
        private const int DefaultTimeout = 5000; // 5 seconds

        public static void SendObject<T>(T obj, string pipeName = DefaultPipeName, int timeout = DefaultTimeout)
        {
            using var pipe = new NamedPipeServerStream(pipeName, PipeDirection.Out, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);

            var connectTask = pipe.WaitForConnectionAsync();
            if (!connectTask.Wait(timeout))
            {
                throw new TimeoutException("Connection timeout while sending object.");
            }

            var json = JsonSerializer.Serialize(obj);
            using var writer = new StreamWriter(pipe);
            writer.WriteLine(json);
            writer.Flush();
        }

        public static T ReceiveObject<T>(string pipeName = DefaultPipeName, int timeout = DefaultTimeout)
        {
            using var pipe = new NamedPipeClientStream(".", pipeName, PipeDirection.In, PipeOptions.Asynchronous);

            try
            {
                pipe.Connect(timeout);
            }
            catch (TimeoutException)
            {
                throw new TimeoutException("Connection timeout while receiving object.");
            }

            using var reader = new StreamReader(pipe);
            var json = reader.ReadLine();
            return JsonSerializer.Deserialize<T>(json);
        }

        public static async Task SendObjectAsync<T>(T obj, string pipeName = DefaultPipeName, CancellationToken cancellationToken = default)
        {
            using var pipe = new NamedPipeServerStream(pipeName, PipeDirection.Out, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);

            await pipe.WaitForConnectionAsync(cancellationToken);

            var json = JsonSerializer.Serialize(obj);
            using var writer = new StreamWriter(pipe);
            await writer.WriteLineAsync(json);
            await writer.FlushAsync();
        }

        public static async Task<T> ReceiveObjectAsync<T>(string pipeName = DefaultPipeName, CancellationToken cancellationToken = default)
        {
            using var pipe = new NamedPipeClientStream(".", pipeName, PipeDirection.In, PipeOptions.Asynchronous);

            await pipe.ConnectAsync(cancellationToken);

            using var reader = new StreamReader(pipe);
            var json = await reader.ReadLineAsync();
            return JsonSerializer.Deserialize<T>(json);
        }
    }
}