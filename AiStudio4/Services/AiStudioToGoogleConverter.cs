// AiStudio4/Services/AiStudioToGoogleConverter.cs
 // For v4BranchedConv, v4BranchedConvMessageRole





namespace AiStudio4.Services
{
    // Internal classes to represent the Google AI Studio JSON structure
    internal class GoogleRunSettings
    {
        public float temperature { get; set; }
        public string model { get; set; }
        public float topP { get; set; }
        public int topK { get; set; }
        public int maxOutputTokens { get; set; }
        public List<GoogleSafetySetting> safetySettings { get; set; }
        public string responseMimeType { get; set; }
        public bool enableCodeExecution { get; set; }
        public bool enableSearchAsATool { get; set; }
        public bool enableBrowseAsATool { get; set; }
        public bool enableAutoFunctionResponse { get; set; }
    }

    internal class GoogleSafetySetting
    {
        public string category { get; set; }
        public string threshold { get; set; }
    }

    internal class GoogleCitation
    {
        public string uri { get; set; }
    }
    
    internal class GoogleSystemInstructionPart
    {
        public string text { get; set; }
    }
    internal class GoogleSystemInstruction
    {
         public List<GoogleSystemInstructionPart> parts { get; set; }
    }


    internal class GoogleChunk
    {
        public string text { get; set; }
        public string role { get; set; } // "user", "model", "system"
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public int? tokenCount { get; set; } // Optional
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public bool? isThought { get; set; } // Optional
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public List<string> thoughtSignatures { get; set; } // Optional
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string finishReason { get; set; } // Optional, e.g., "STOP"
    }

    internal class GoogleChunkedPrompt
    {
        public List<GoogleChunk> chunks { get; set; }
        public List<GooglePendingInput> pendingInputs {get; set;}
    }
    
    internal class GooglePendingInput
    {
        public string text {get; set;}
        public string role {get; set;}
    }


    internal class GoogleRootObject
    {
        public GoogleRunSettings runSettings { get; set; }
        public List<GoogleCitation> citations { get; set; }
        public GoogleSystemInstruction systemInstruction { get; set; }
        public GoogleChunkedPrompt chunkedPrompt { get; set; }
    }

    public static class AiStudioToGoogleConverter
    {
        public static string Convert(v4BranchedConv aiStudioConv, string selectedMessageId, string baseModelName = "models/gemini-1.5-pro-latest")
        {
            if (aiStudioConv == null) throw new ArgumentNullException(nameof(aiStudioConv));

            var rootObject = new GoogleRootObject
            {
                runSettings = new GoogleRunSettings
                {
                    temperature = 1.0f,
                    model = baseModelName,
                    topP = 0.95f,
                    topK = 64,
                    maxOutputTokens = 8192, // Default from example, was 65536
                    safetySettings = new List<GoogleSafetySetting>
                    {
                        new GoogleSafetySetting { category = "HARM_CATEGORY_HARASSMENT", threshold = "BLOCK_NONE" }, // OFF in example, BLOCK_NONE seems more standard for APIs
                        new GoogleSafetySetting { category = "HARM_CATEGORY_HATE_SPEECH", threshold = "BLOCK_NONE" },
                        new GoogleSafetySetting { category = "HARM_CATEGORY_SEXUALLY_EXPLICIT", threshold = "BLOCK_NONE" },
                        new GoogleSafetySetting { category = "HARM_CATEGORY_DANGEROUS_CONTENT", threshold = "BLOCK_NONE" }
                    },
                    responseMimeType = "text/plain",
                    enableCodeExecution = false,
                    enableSearchAsATool = false,
                    enableBrowseAsATool = false,
                    enableAutoFunctionResponse = false
                },
                citations = new List<GoogleCitation>(), // Empty for now
                systemInstruction = new GoogleSystemInstruction { parts = new List<GoogleSystemInstructionPart>() }, // Empty for now
                chunkedPrompt = new GoogleChunkedPrompt
                {
                    chunks = new List<GoogleChunk>(),
                    pendingInputs = new List<GooglePendingInput> { new GooglePendingInput { text = "", role = "user" } }
                }
            };

            // Get the message history for the selected thread
            var messageHistory = aiStudioConv.GetMessageHistory(selectedMessageId);

            foreach (var msg in messageHistory)
            {
                string googleRole;
                switch (msg.Role)
                {
                    case v4BranchedConvMessageRole.System:
                        // Add system prompt content to systemInstruction.parts
                        if(msg.ContentBlocks.Any())
                        {
                            foreach (var block in msg.ContentBlocks)
                            {
                                if (block.ContentType == Core.Models.ContentType.Text)
                                {
                                    rootObject.systemInstruction.parts.Add(new GoogleSystemInstructionPart { text = block.Content });
                                }
                                else
                                {
                                    // Handle other content types if needed, e.g., images, files
                                    // For now, we only handle text content
                                }
                            }
                        }
                        //if (!string.IsNullOrWhiteSpace(msg.UserMessage)) {
                        //    rootObject.systemInstruction.parts.Add(new GoogleSystemInstructionPart { text = msg.UserMessage });
                        //}
                        continue; // System messages are handled by systemInstruction, not as a chunk
                    case v4BranchedConvMessageRole.User:
                        googleRole = "user";
                        break;
                    case v4BranchedConvMessageRole.Assistant:
                        googleRole = "model";
                        break;
                    default:
                        continue; // Skip unknown roles
                }

                var chunk = new GoogleChunk
                {
                    text = string.Join("\n\n", msg.ContentBlocks.Where(x => x.ContentType == Core.Models.ContentType.Text).Select(x => x.Content)),
                    role = googleRole,
                    isThought = false, // AiStudio4 doesn't store thoughts this way
                    // tokenCount and thoughtSignatures are omitted
                };
                
                // Add attachments as part of the text content if they are text-based
                if (msg.Attachments != null && msg.Attachments.Any())
                {
                    var textAttachments = msg.Attachments.Where(a => a.Type.StartsWith("text/") || a.Type == "application/json" || a.Type == "application/xml");
                    if (textAttachments.Any())
                    {
                        chunk.text += "\n\n--- Attached Files ---\n";
                        foreach (var attachment in textAttachments)
                        {
                            chunk.text += $"Filename: {attachment.Name}\nContent:\n{attachment.TextContent}\n---\n";
                        }
                    }
                }


                rootObject.chunkedPrompt.chunks.Add(chunk);
            }

            // Set finishReason for the last "model" chunk
            var lastModelChunk = rootObject.chunkedPrompt.chunks.LastOrDefault(c => c.role == "model");
            if (lastModelChunk != null)
            {
                lastModelChunk.finishReason = "STOP";
            }

            return JsonConvert.SerializeObject(rootObject, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
        }
    }
}
