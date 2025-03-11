using AiStudio4.Convs;
using AiStudio4.DataModels;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharedClasses.Providers;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace AiStudio4.AiServices
{
    internal class LocalAI : AiServiceBase
    {
        public LocalAI()
        {
        }
        protected override async Task<AiResponse> FetchResponseInternal(AiRequestOptions options)
        {
            InitializeHttpClient(options.ServiceProvider, options.Model, options.ApiSettings);
            
            // Apply custom system prompt if provided
            if (!string.IsNullOrEmpty(options.CustomSystemPrompt))
            {
                options.Conv.systemprompt = options.CustomSystemPrompt;
            }

            var requestPayload = CreateRequestPayload(ApiModel, options.Conv, options.UseStreaming, options.ApiSettings);

            var messagesArray = new JArray();
            //Add system prompt
            messagesArray.Add(new JObject
            {
                ["role"] = "system",
                ["content"] = options.Conv.SystemPromptWithDateTime()
            });
            //Add user messages
            foreach (var m in options.Conv.messages)
            {
                var messageObj = CreateMessageObject(m);
                messagesArray.Add(messageObj);
            }

            requestPayload["messages"] = messagesArray;

            if (options.AddEmbeddings)
            {
                var newInput = await AddEmbeddingsIfRequired(options.Conv, options.ApiSettings, options.MustNotUseEmbedding, options.AddEmbeddings, options.Conv.messages.Last().content);
                ((JObject)((JArray)requestPayload["messages"]).Last)["content"] = newInput;
            }

            var json = JsonConvert.SerializeObject(requestPayload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            StartOllama(ApiModel);
            return await HandleResponse(content, options.UseStreaming, options.CancellationToken);
        }


        protected override JObject CreateMessageObject(LinearConvMessage message)
        {
            var messageObj = new JObject
            {
                ["role"] = message.role,
                ["content"] = message.content
            };

            if (!string.IsNullOrEmpty(message.base64image))
            {
                messageObj["images"] = new JArray { message.base64image };
            }

            return messageObj;
        }

        protected override JObject CreateRequestPayload(string modelName, LinearConv conv, bool useStreaming, ApiSettings apiSettings)
        {
            return new JObject
            {
                ["model"] = modelName,
                ["stream"] = useStreaming
            };
        }

        protected override async Task<AiResponse> HandleStreamingResponse(HttpContent content, CancellationToken cancellationToken)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, ApiUrl);
            request.Content = content;
            client.Timeout = TimeSpan.FromSeconds(1800);
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

            StringBuilder fullResponse = new StringBuilder();
            StringBuilder lineBuilder = new StringBuilder();
            int promptEvalCount = 0;
            int evalCount = 0;

            byte[] buffer = new byte[48];  // Read in larger chunks
            var decoder = Encoding.UTF8.GetDecoder();

            while (true)
            {
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                if (bytesRead == 0) break;

                char[] chars = new char[decoder.GetCharCount(buffer, 0, bytesRead)];
                int charsDecodedCount = decoder.GetChars(buffer, 0, bytesRead, chars, 0);

                for (int i = 0; i < charsDecodedCount; i++)
                {
                    char c = chars[i];
                    lineBuilder.Append(c);

                    if (c == '\n')
                    {
                        Debug.WriteLine(lineBuilder.ToString().Trim());
                        ProcessLine(lineBuilder.ToString().Trim(), fullResponse, ref promptEvalCount, ref evalCount);
                        lineBuilder.Clear();
                    }
                }
            }

            // Process any remaining content
            if (lineBuilder.Length > 0)
            {
                ProcessLine(lineBuilder.ToString().Trim(), fullResponse, ref promptEvalCount, ref evalCount);
            }
            OnStreamingComplete();
            return new AiResponse
            {
                ResponseText = fullResponse.ToString(),
                Success = true,
                TokenUsage = new TokenUsage(promptEvalCount.ToString(), evalCount.ToString())
            };
        }

        private void ProcessLine(string line, StringBuilder fullResponse, ref int promptEvalCount, ref int evalCount)
        {
            if (string.IsNullOrEmpty(line)) return;

            try
            {
                var chunkResponse = JObject.Parse(line);

                if (chunkResponse["message"] != null && !string.IsNullOrEmpty(chunkResponse["message"]["content"]?.ToString()))
                {
                    fullResponse.Append(chunkResponse["message"]["content"]);
                    Debug.WriteLine(chunkResponse["message"]["content"]);
                    OnStreamingDataReceived(chunkResponse["message"]["content"].ToString());
                }

                if (chunkResponse["done"]?.Value<bool>() == true)
                {
                    promptEvalCount = chunkResponse["prompt_eval_count"]?.Value<int>() ?? 0;
                    evalCount = chunkResponse["eval_count"]?.Value<int>() ?? 0;
                }
            }
            catch (JsonException)
            {
                // Handle or log JSON parsing errors
            }
        }

        protected override async Task<AiResponse> HandleNonStreamingResponse(HttpContent content, CancellationToken cancellationToken)
        {
            var response = await client.PostAsync(ApiUrl, content, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JObject.Parse(responseContent);

            return new AiResponse
            {
                ResponseText = result["message"]?["content"]?.ToString(),
                Success = true,
                TokenUsage = new TokenUsage(
                    result["prompt_eval_count"]?.ToString() ?? "0",
                  result["eval_count"]?.ToString() ?? "0"
               )
            };
        }
        protected override TokenUsage ExtractTokenUsage(JObject response)
        {
            return new TokenUsage(
                   response["prompt_eval_count"]?.ToString() ?? "0",
                   response["eval_count"]?.ToString() ?? "0"
               );
        }
        private bool IsPortOpen(int port)
        {
            try
            {
                using (var client = new TcpClient())
                {
                    return client.ConnectAsync("127.0.0.1", port).Wait(100);
                }
            }
            catch
            {
                return false;
            }
        }

        public static void StartOllama(string modelName)
        {
            var psi = new ProcessStartInfo("ollama", $"run {modelName} /set parameter num_ctx 16384")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            var process = Process.Start(psi);

            new Thread(() =>
            {
                Thread.Sleep(1000);
                process!.Kill();
            }).Start();
        }

        private string GetAdjustedUrl(string originalUrl, int localPort)
        {
            if (originalUrl.Contains("11434") && localPort != 11434)
            {
                return originalUrl.Replace("11434", localPort.ToString());
            }
            return originalUrl;
        }
    }
}