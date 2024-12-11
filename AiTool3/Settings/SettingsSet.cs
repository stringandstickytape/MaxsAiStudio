using AiTool3.DataModels;
using AiTool3.Providers;
using AiTool3.Settings;
using AiTool3.UI;
using SharedClasses.Models;
using System.Diagnostics;
using System.Security.Policy;
using static System.Net.WebRequestMethods;

namespace AiTool3
{
    public class SettingsSet

    {
        private string selectedSummaryModel = "";

        public List<Model> ModelList { get; set; } = new List<Model>();

        [MyDisplayNameAttr("Narrate responses using Windows TTS")]
        public bool NarrateResponses { get; set; } = false;

        [MyDisplayNameAttr("Temperature")]
        public float Temperature { get; set; } = 0.9f;

        [MyDisplayNameAttr("Run HTTP webserver on port 8080 (experimental, requires app restart, app must run as administrator)")]
        public bool RunWebServer { get; set; } = false;

        [MyDisplayNameAttr("For user prompts containing [pull:www.example.com], pull that URL, grab text fragments, and insert into prompt")]
        public bool AllowUserPromptUrlPulls { get; set; } = false;


        [MyDisplayNameAttr("Entertain me with dumb software toys while I wait for non-chat tasks")]
        public bool SoftwareToyMode { get; set; } = false;

        [MyDisplayNameAttr("Use embeddings")]
        public bool UseEmbeddings { get; set; } = false;

        [MyDisplayNameAttr("Use prompt caching (Claude only)")]
        public bool UsePromptCaching { get; set; } = true;

        [MyDisplayNameAttr("Stream responses")]
        public bool StreamResponses { get; set; } = false;

        //[MyDisplayNameAttr("OpenAI API key for embeddings")]
        //public string EmbeddingKey { get; set; } = "";

        [IsFileAttribute(".embeddings.json")]
        [MyDisplayNameAttr("Embeddings Filename/path")]
        public string EmbeddingsFilename { get; set; }

        [IsPathAttribute]
        [MyDisplayNameAttr("Default Path")]
        public string DefaultPath { get; set; } = Directory.GetCurrentDirectory();

        [IsPathAttribute]
        [MyDisplayNameAttr("Path to Conda Activate Script")]
        public string PathToCondaActivateScript { get; set; } = "C:\\Users\\username\\miniconda3\\Scripts\\";

        [MyDisplayNameAttr("File extensions to display in the Project Helper")]
        public string ProjectHelperFileExtensions { get; set; } = "*.cs, *.html, *.css, *.js";

        [IsPathAttribute]
        [MyDisplayNameAttr("HuggingFace Token for Diarization")]
        public string HuggingFaceToken { get; set; } = "";

        [MyDisplayNameAttr("Collapse conversation pane at startup")]
        public bool CollapseConversationPane { get; set; } = false;



        public MessagePrompt[] MessagePrompts = new[]
{
            // Code Analysis and Explanation
            new MessagePrompt { Category = "Code Analysis", ButtonLabel = "Explain Code", MessageType = "explainCode", Prompt = "Provide a detailed explanation of what this code does:" },
            new MessagePrompt { Category = "Code Analysis", ButtonLabel = "Identify Potential Bugs", MessageType = "identifyBugs", Prompt = "Analyze this code for potential bugs or edge cases that might cause issues:" },
            new MessagePrompt { Category = "Code Analysis", ButtonLabel = "Identify Tech Debt", MessageType = "identifyTechDebt", Prompt = "Analyze this code to identify areas of technical debt and suggest improvements:" },

            // Code Improvement and Refactoring
            new MessagePrompt { Category = "Refactoring", ButtonLabel = "Extract Method", MessageType = "extractMethod", Prompt = "Perform an extract method on this:" },
            new MessagePrompt { Category = "Refactoring", ButtonLabel = "Extract Static Method", MessageType = "extractStaticMethod", Prompt = "Perform an extract static method on this:" },
            new MessagePrompt { Category = "Refactoring", ButtonLabel = "DRY This", MessageType = "dryThis", Prompt = "Suggest some clever ways, with examples, to DRY this code:" },
            new MessagePrompt { Category = "Refactoring", ButtonLabel = "General Refactor", MessageType = "generalRefactor", Prompt = "Suggest some clever ways, with examples, to generally refactor this code:" },
            new MessagePrompt { Category = "Refactoring", ButtonLabel = "Improve Performance", MessageType = "improvePerformance", Prompt = "Analyse and, if possible, suggest some clever ways with examples, to improve the performance of this code:" },
            new MessagePrompt { Category = "Refactoring", ButtonLabel = "Simplify Logic", MessageType = "simplifyLogic", Prompt = "Analyze and suggest ways to simplify the logic in this code without changing its functionality:" },
            new MessagePrompt { Category = "Refactoring", ButtonLabel = "Convert to LINQ", MessageType = "convertToLinq", Prompt = "Convert this code to use LINQ expressions where appropriate:" },
            new MessagePrompt { Category = "Refactoring", ButtonLabel = "Extract Best Class", MessageType = "extractBestClass", Prompt = "Analyze this code and identify the single best class that could be extracted to improve general Object-Oriented Programming (OOP) principles. Describe the proposed class, its properties, methods, and how it would enhance the overall design:" },
            new MessagePrompt { Category = "Refactoring", ButtonLabel = "String Interpolation", MessageType = "stringInterpolation", Prompt = "Rewrite this to use string interpolation:" },
            
            // Code Enhancement
            new MessagePrompt { Category = "Enhancement", ButtonLabel = "Add Error Handling", MessageType = "addErrorHandling", Prompt = "Suggest appropriate error handling mechanisms for this code:" },
            new MessagePrompt { Category = "Enhancement", ButtonLabel = "Add Logging", MessageType = "addLogging", Prompt = "Suggest appropriate logging statements to add to this code for better debugging and monitoring:" },

            // Naming and Documentation
            new MessagePrompt { Category = "Documentation", ButtonLabel = "Suggest Name", MessageType = "suggestName", Prompt = "Suggest a concise and descriptive name for this code element:" },
            new MessagePrompt { Category = "Documentation", ButtonLabel = "Commit Message", MessageType = "commitMsg", Prompt = "Give me a short, high-quality, bulleted, tersely-phrased summary for this diff.  Break the changes down by project and category.  Demarcate the summary as a single code block. Do not mention unused categories or insignficiant changes." },
            new MessagePrompt { Category = "Documentation", ButtonLabel = "Generate Code Map", MessageType = "generateCodeMap", Prompt = "Create a DOT diagram file for a high-level code map or diagram representing the structure and relationships in this code:" },

            // Code Generation and Extension
            new MessagePrompt { Category = "Generation", ButtonLabel = "Autocomplete at //! marker", MessageType = "autocompleteThis", Prompt = "Autocomplete this code where you see the marker //! . Give only the inserted text and no other output, demarcated with three ticks before and after." },
            new MessagePrompt { Category = "Generation", ButtonLabel = "Extend Series", MessageType = "addToSeries", Prompt = "Extend the series you see in this code:" },
            new MessagePrompt { Category = "Generation", ButtonLabel = "Create Unit Tests", MessageType = "createUnitTests", Prompt = "Generate unit tests for this code:" },

            // Code Readability
            new MessagePrompt { Category = "Readability", ButtonLabel = "Add Comments", MessageType = "addComments", Prompt = "Add appropriate comments to this code to improve its readability:" },
            new MessagePrompt { Category = "Readability", ButtonLabel = "Remove Comments", MessageType = "removeComments", Prompt = "Remove all comments from this code:" },

            // User Documentation
            new MessagePrompt { Category = "Documentation", ButtonLabel = "Generate README", MessageType = "generateReadme", Prompt = "Generate a comprehensive README.md file for this project based on the code provided:" },
            new MessagePrompt { Category = "Documentation", ButtonLabel = "Create User Guide", MessageType = "createUserGuide", Prompt = "Create a user guide explaining how to use the functionality implemented in this code:" },
            new MessagePrompt { Category = "Documentation", ButtonLabel = "API Documentation", MessageType = "generateApiDocs", Prompt = "Generate API documentation for the public methods and classes in this code:" },

            new MessagePrompt { Category = "Miscellaneous", ButtonLabel = "Send User Prompt with Code Attachments", MessageType = "userPromptWithAttchments", Prompt = "" },
            new MessagePrompt { Category = "Miscellaneous", ButtonLabel = "Cache This", MessageType = "replyWithDot", Prompt = "Reply with a ." },
            new MessagePrompt
{
    Category = "Documentation",
    ButtonLabel = "C# XML Comments",
    MessageType = "generateCSharpXmlComments",
    Prompt = @"You are an AI assistant specialized in creating standards-compliant XML documentation comments for C# code. Analyze the given C# code and generate appropriate XML comments for classes, methods, properties, and other code elements.

Follow these guidelines:
1. Use XML documentation comments starting with ///.
2. Include a <summary> tag for each element, providing a brief description.
3. Use <param> tags for method parameters, describing each parameter.
4. Use <returns> tags for methods that return a value, describing the return value.
5. Use <exception> tags to document exceptions that may be thrown.
6. Use <remarks> tags for additional information when necessary.
7. Use <example> tags to provide usage examples for complex methods or classes.
8. Use <see> and <seealso> tags to reference related code elements.
9. Follow Microsoft's C# documentation style guide for consistency.
10. Ensure that comments are clear, concise, and add value to the code.

Analyze the above C# code and provide appropriate XML documentation comments for the code elements. Do not modify or repeat the original code; give only the comments to go above the relevant method or class. "
},

        };


        public string SelectedModel {
            get;
            set; 
        } = "";
        public string SelectedSummaryModel
        {
            get => selectedSummaryModel;
            set => selectedSummaryModel = value;
        }
        public string SelectedTheme { get; set; }

        [MyDisplayNameAttr("Name of the Ollama embedding model to use")]
        public string EmbeddingModel { get; internal set; } = "mxbai-embed-large";

        public SettingsSet() { }

        public void SetDefaultPath(string v)
        {
            DefaultPath = v;
            SettingsSet.Save(this);
        }

        private void Create()
        {
            ModelList = new List<Model>()
            {
 new Model { Url = "https://api.openai.com/v1/chat/completions", ServiceName = typeof(OpenAI).Name, ModelName = "gpt-4o", FriendlyName = "GPT-4", Color = Color.FromArgb(255, 179, 186), input1MTokenPrice = 5, output1MTokenPrice = 15},
new Model { Url = "https://api.openai.com/v1/chat/completions", ServiceName = typeof(OpenAI).Name, ModelName = "gpt-4o-2024-11-20", FriendlyName = "GPT-4 2024-11-20", Color = Color.FromArgb(255, 179, 186), input1MTokenPrice = 2.5m, output1MTokenPrice = 10},
new Model { Url = "https://api.openai.com/v1/chat/completions", ServiceName = typeof(OpenAI).Name, ModelName = "gpt-4o-mini-2024-07-18", FriendlyName = "GPT-4 Mini (July 2024)", Color = Color.FromArgb(186, 201, 255), input1MTokenPrice = 0.15m, output1MTokenPrice = .6m},
new Model { Url = "https://api.openai.com/v1/chat/completions", ServiceName = typeof(OpenAI).Name, ModelName = "gpt-4-turbo", FriendlyName = "GPT-4 Turbo", Color = Color.FromArgb(186, 255, 201), input1MTokenPrice = 10, output1MTokenPrice = 30},
new Model { Url = "https://api.openai.com/v1/chat/completions", ServiceName = typeof(OpenAI).Name, ModelName = "gpt-3.5-turbo", FriendlyName = "GPT-3.5 Turbo", Color = Color.FromArgb(186, 225, 255), input1MTokenPrice = 0.5m, output1MTokenPrice = 1.5m},
new Model { Url = "http://localhost:11434/v1/chat/completions", ServiceName = typeof(OpenAI).Name, ModelName = "gemma2:2b", FriendlyName = "Gemma 2 (2B)", Color = Color.FromArgb(186, 225, 255), input1MTokenPrice = 0m, output1MTokenPrice = 0m},
new Model { Url = "http://localhost:11434/v1/chat/completions", ServiceName = typeof(OpenAI).Name, ModelName = "llama3.1:8b", FriendlyName = "LLaMA 3.1 (8B)", Color = Color.FromArgb(186, 225, 255), input1MTokenPrice = 0m, output1MTokenPrice = 0m},
new Model { Url = "http://localhost:11434/v1/chat/completions", ServiceName = typeof(OpenAI).Name, ModelName = "llama3.2-vision", FriendlyName = "LLaMA 3.2 Vision (11B)", Color = Color.FromArgb(186, 225, 255), input1MTokenPrice = 0m, output1MTokenPrice = 0m},
new Model { Url = "http://localhost:11434/api/chat/", ServiceName = typeof(LocalAI).Name, ModelName = "mistral-nemo", FriendlyName = "Mistral Nemo", Color = Color.FromArgb(255, 255, 186)},
new Model { Url = "http://localhost:11434/api/chat/", ServiceName = typeof(LocalAI).Name, ModelName = "llava:7b", FriendlyName = "LLaVA (7B)", Color = Color.FromArgb(255, 255, 186)},
new Model { Url = "http://localhost:11434/api/chat/", ServiceName = typeof(LocalAI).Name, ModelName = "llava:13b", FriendlyName = "LLaVA (13B)", Color = Color.FromArgb(255, 255, 186)},
new Model { Url = "http://localhost:11434/api/chat/", ServiceName = typeof(LocalAI).Name, ModelName = "llama3.1:8b", FriendlyName = "LLaMA 3.1 (8B)", Color = Color.FromArgb(255, 255, 186)},
new Model { Url = "http://localhost:11434/api/chat/", ServiceName = typeof(LocalAI).Name, ModelName = "llama3.1:70b", FriendlyName = "LLaMA 3.1 (70B)", Color = Color.FromArgb(255, 255, 186)},
new Model { Url = "http://localhost:11434/api/chat/", ServiceName = typeof(LocalAI).Name, ModelName = "llama3.2", FriendlyName = "LLaMA 3.2 (3B)", Color = Color.FromArgb(255, 255, 186)},
new Model { Url = "http://localhost:11434/api/chat/", ServiceName = typeof(LocalAI).Name, ModelName = "llama3.2:3b-instruct-fp16", FriendlyName = "LLaMA 3.2 (3B-instruct-fp16)", Color = Color.FromArgb(255, 255, 186)},
new Model { Url = "http://localhost:11434/api/chat/", ServiceName = typeof(LocalAI).Name, ModelName = "llama3.2:3b-instruct-fp16", FriendlyName = "LLaMA 3.2 (3B-instruct-fp16)", Color = Color.FromArgb(255, 255, 186)},
new Model { Url = "http://localhost:11434/api/chat/", ServiceName = typeof(LocalAI).Name, ModelName = "llama3.2-vision:11b-instruct-fp16", FriendlyName = "LLaMA 3.2 Vision (11b-instruct-fp16)", Color = Color.FromArgb(255, 255, 186)},
new Model { Url = "http://localhost:11434/api/chat/", ServiceName = typeof(LocalAI).Name, ModelName = "llama3.3", FriendlyName = "llama3.3 70b", Color = Color.FromArgb(255, 255, 186)},
new Model { Url = "http://localhost:11434/api/chat/", ServiceName = typeof(LocalAI).Name, ModelName = "codestral", FriendlyName = "Codestral", Color = Color.FromArgb(255, 255, 186)},
new Model { Url = "http://localhost:11434/api/chat/", ServiceName = typeof(LocalAI).Name, ModelName = "gemma2", FriendlyName = "Gemma 2", Color = Color.FromArgb(255, 255, 186)},
new Model { Url = "http://localhost:11434/api/chat/", ServiceName = typeof(LocalAI).Name, ModelName = "gemma2:2b", FriendlyName = "Gemma 2 (2B)", Color = Color.FromArgb(255, 255, 186)},
new Model { Url = "http://localhost:11434/api/chat/", ServiceName = typeof(LocalAI).Name, ModelName = "gemma2:27b", FriendlyName = "Gemma 2 (27B)", Color = Color.FromArgb(255, 255, 186)},
new Model { Url = "http://localhost:11434/api/chat/", ServiceName = typeof(LocalAI).Name, ModelName = "deepseek-coder-v2", FriendlyName = "DeepSeek Coder v2", Color = Color.FromArgb(255, 255, 186)},
new Model { Url = "http://localhost:11434/api/chat/", ServiceName = typeof(LocalAI).Name, ModelName = "phi3.5", FriendlyName = "Phi-3.5", Color = Color.FromArgb(255, 255, 186)},
new Model { Url = "http://localhost:11434/api/chat/", ServiceName = typeof(LocalAI).Name, ModelName = "yi-coder:9b", FriendlyName = "Yi Coder (9B)", Color = Color.FromArgb(255, 255, 186)},
new Model { Url = "http://localhost:11434/api/chat/", ServiceName = typeof(LocalAI).Name, ModelName = "reflection", FriendlyName = "Reflection", Color = Color.FromArgb(255, 255, 186)},
new Model { Url = "http://localhost:11434/api/chat/", ServiceName = typeof(LocalAI).Name, ModelName = "minicpm-v", FriendlyName = "MiniCPM-V", Color = Color.FromArgb(255, 255, 186)},
new Model { Url = "http://localhost:11434/api/chat/", ServiceName = typeof(LocalAI).Name, ModelName = "reader-lm", FriendlyName = "Reader LM", Color = Color.FromArgb(255, 255, 186)},
new Model { Url = "http://localhost:11434/api/chat/", ServiceName = typeof(LocalAI).Name, ModelName = "qwen2.5-coder", FriendlyName = "Qwen 2.5 Coder", Color = Color.FromArgb(255, 255, 186)},
new Model { Url = "http://localhost:11434/api/chat/", ServiceName = typeof(LocalAI).Name, ModelName = "qwen2.5-coder:32b", FriendlyName = "Qwen 2.5 Coder (32B)", Color = Color.FromArgb(255, 255, 186)}, // 32b-instruct-q5_0
new Model { Url = "http://localhost:11434/api/chat/", ServiceName = typeof(LocalAI).Name, ModelName = "qwen2.5-coder:32b-instruct-q5_0", FriendlyName = "Qwen 2.5 Coder (32b-instruct-q5_0)", Color = Color.FromArgb(255, 255, 186)}, // 32b-instruct-q5_0
new Model { Url = "http://localhost:11434/api/chat/", ServiceName = typeof(LocalAI).Name, ModelName = "qwen2.5:7b", FriendlyName = "Qwen 2.5 (7B)", Color = Color.FromArgb(255, 255, 186)},
new Model { Url = "http://localhost:11434/api/chat/", ServiceName = typeof(LocalAI).Name, ModelName = "qwen2.5:14b", FriendlyName = "Qwen 2.5 (14B)", Color = Color.FromArgb(255, 255, 186)},
new Model { Url = "http://localhost:11434/api/chat/", ServiceName = typeof(LocalAI).Name, ModelName = "solar-pro", FriendlyName = "Solar Pro", Color = Color.FromArgb(255, 255, 186)},
new Model { Url = "http://localhost:11434/api/chat/", ServiceName = typeof(LocalAI).Name, ModelName = "bespoke-minicheck", FriendlyName = "Bespoke Minicheck", Color = Color.FromArgb(255, 255, 186)},
new Model { Url = "http://localhost:11434/api/chat/", ServiceName = typeof(LocalAI).Name, ModelName = "mistral-small", FriendlyName = "Mistral Small", Color = Color.FromArgb(255, 255, 186)},
new Model { Url = "http://localhost:11434/api/chat/", ServiceName = typeof(LocalAI).Name, ModelName = "marco-o1:7b-q8_0", FriendlyName = "marco-o1:7b-q8_0", Color = Color.FromArgb(255, 255, 186)},
new Model { Url = "http://localhost:11434/api/chat/", ServiceName = typeof(LocalAI).Name, ModelName = "marco-o1-long-ctx", FriendlyName = "marco-o1:7b-q8_0 (32768 ctx)", Color = Color.FromArgb(255, 255, 186)},
new Model { Url = "http://localhost:11434/api/chat/", ServiceName = typeof(LocalAI).Name, ModelName = "llama3.1-70b-long-ctx", FriendlyName = "Llama 3.1 (70b) (32768 ctx)", Color = Color.FromArgb(255, 255, 186)},
new Model { Url = "http://localhost:11434/api/chat/", ServiceName = typeof(LocalAI).Name, ModelName = "qwq", FriendlyName = "QWQ", Color = Color.FromArgb(255, 255, 186)},
new Model { Url = "http://localhost:11434/api/chat/", ServiceName = typeof(LocalAI).Name, ModelName = "qwq-q4-long-ctx", FriendlyName = "QWQ 32b-preview-q4_K_M (32768 ctx)", Color = Color.FromArgb(255, 255, 186)},
new Model { Url = "http://localhost:11434/api/chat/", ServiceName = typeof(LocalAI).Name, ModelName = "qwq-q8-long-ctx", FriendlyName = "QWQ 32b-preview-q8 (32768 ctx)", Color = Color.FromArgb(255, 255, 186)},
new Model { Url = "http://localhost:11434/api/chat/", ServiceName = typeof(LocalAI).Name, ModelName = "qwq:32b-preview-q8_0", FriendlyName = "QWQ 32b-preview-q8", Color = Color.FromArgb(255, 255, 186)},
// llama3.1-70b-long-ctx

new Model { Url = "https://openrouter.ai/api/v1/chat/completions", ServiceName = typeof(OpenRouterAI).Name, ModelName = "meta-llama/llama-3.1-8b-instruct:free", FriendlyName = "LLaMA 3.1 (8B) Instruct", Color = Color.FromArgb(255, 255, 186)},
new Model { Url = "https://api.groq.com/openai/v1/chat/completions", ServiceName = typeof(Groq).Name, ModelName = "llama3-8b-8192", FriendlyName = "LLaMA 3 (8B) 8K", Color = Color.FromArgb(255, 216, 186)},
new Model { Url = "https://api.groq.com/openai/v1/chat/completions", ServiceName = typeof(Groq).Name, ModelName = "llama3-70b-8192", FriendlyName = "LLaMA 3 (70B) 8K", Color = Color.FromArgb(224, 186, 255)},
new Model { Url = "https://generativelanguage.googleapis.com/v1beta/models/", ServiceName = typeof(Gemini).Name, ModelName = "gemini-1.5-pro", FriendlyName = "Gemini 1.5 Pro", Color = Color.FromArgb(186, 255, 216), input1MTokenPrice = 7m, output1MTokenPrice = 21m},
new Model { Url = "https://generativelanguage.googleapis.com/v1beta/models/", ServiceName = typeof(Gemini).Name, ModelName = "gemini-1.5-pro-exp-0801", FriendlyName = "Gemini 1.5 Pro (Exp 0801)", Color = Color.FromArgb(186, 255, 216), input1MTokenPrice = 7m, output1MTokenPrice = 21m},
new Model { Url = "https://generativelanguage.googleapis.com/v1beta/models/", ServiceName = typeof(Gemini).Name, ModelName = "gemini-exp-1206", FriendlyName = "Gemini Exp 1206", Color = Color.FromArgb(186, 255, 216), input1MTokenPrice = .15m, output1MTokenPrice = .6m},
new Model { Url = "https://generativelanguage.googleapis.com/v1beta/openai/", ServiceName = typeof(OpenAI).Name, ModelName = "gemini-exp-1206", FriendlyName = "Gemini Exp 1206 via OpenAI API", Color = Color.FromArgb(186, 255, 216), input1MTokenPrice = .15m, output1MTokenPrice = .6m},
new Model { Url = "https://generativelanguage.googleapis.com/v1beta/models/", ServiceName = typeof(Gemini).Name, ModelName = "gemini-1.5-flash", FriendlyName = "Gemini 1.5 Flash", Color = Color.FromArgb(186, 255, 216), input1MTokenPrice = .15m, output1MTokenPrice = .6m},
new Model { Url = "https://generativelanguage.googleapis.com/v1beta/models/", ServiceName = typeof(Gemini).Name, ModelName = "gemini-1.5-pro-002", FriendlyName = "Gemini 1.5 Pro 002", Color = Color.FromArgb(186, 255, 216), input1MTokenPrice = 7m, output1MTokenPrice = 21m},
new Model { Url = "https://generativelanguage.googleapis.com/v1beta/models/", ServiceName = typeof(Gemini).Name, ModelName = "gemini-1.5-flash-002", FriendlyName = "Gemini 1.5 Flash 002", Color = Color.FromArgb(186, 255, 216), input1MTokenPrice = .15m, output1MTokenPrice = .6m},
new Model { Url = "https://generativelanguage.googleapis.com/v1beta/models/", ServiceName = typeof(Gemini).Name, ModelName = "gemini-1.5-flash-8b", FriendlyName = "Gemini 1.5 Flash 8b", Color = Color.FromArgb(186, 255, 216), input1MTokenPrice = .075m, output1MTokenPrice = .3m},
new Model { Url = "https://generativelanguage.googleapis.com/v1beta/models/", ServiceName = typeof(Gemini).Name, ModelName = "gemini-2.0-flash-exp", FriendlyName = "Gemini 2.0 Flash Exp", Color = Color.FromArgb(186, 255, 216), input1MTokenPrice = .075m, output1MTokenPrice = .3m},
new Model { Url = "https://generativelanguage.googleapis.com/v1beta/openai/chat/completions/", ServiceName = typeof(OpenAI).Name, ModelName = "gemini-2.0-flash-exp", FriendlyName = "Gemini 2.0 Flash Exp via OpenAI API", Color = Color.FromArgb(186, 255, 216), input1MTokenPrice = .075m, output1MTokenPrice = .3m},
new Model { Url = "https://api.anthropic.com/v1/messages", ServiceName = typeof(Claude).Name, SupportsPrefill = true, ModelName = "claude-3-5-sonnet-20241022", FriendlyName = "Claude 3.5 Sonnet New (Oct 2024)", Color = Color.FromArgb(255, 219, 186), input1MTokenPrice = 3, output1MTokenPrice = 15},
new Model { Url = "https://api.anthropic.com/v1/messages", ServiceName = typeof(Claude).Name, SupportsPrefill = true, ModelName = "claude-3-5-haiku-20241022", FriendlyName = "Claude 3.5 Haiku (Oct 2024)", Color = Color.FromArgb(255, 219, 186), input1MTokenPrice = .8m, output1MTokenPrice = 4},
new Model { Url = "https://api.anthropic.com/v1/messages", ServiceName = typeof(Claude).Name, SupportsPrefill = true, ModelName = "claude-3-5-sonnet-20240620", FriendlyName = "Claude 3.5 Sonnet (June 2024)", Color = Color.FromArgb(255, 219, 186), input1MTokenPrice = 3, output1MTokenPrice = 15},
new Model { Url = "https://api.anthropic.com/v1/messages", ServiceName = typeof(Claude).Name, SupportsPrefill = true, ModelName = "claude-3-opus-20240229", FriendlyName = "Claude 3 Opus (Feb 2024)", Color = Color.FromArgb(186, 207, 255), input1MTokenPrice = 15, output1MTokenPrice = 75},
new Model { Url = "https://api.anthropic.com/v1/messages", ServiceName = typeof(Claude).Name, SupportsPrefill = true, ModelName = "claude-3-sonnet-20240229", FriendlyName = "Claude 3 Sonnet (Feb 2024)", Color = Color.FromArgb(186, 255, 237), input1MTokenPrice = 3, output1MTokenPrice = 15},
new Model { Url = "https://api.anthropic.com/v1/messages", ServiceName = typeof(Claude).Name, SupportsPrefill = true, ModelName = "claude-3-haiku-20240307", FriendlyName = "Claude 3 Haiku (March 2024)", Color = Color.FromArgb(216, 186, 255), input1MTokenPrice = .25m, output1MTokenPrice = 1.25m},
new Model { Url = "https://mock.com", ServiceName = typeof(MockAiService).Name, ModelName = "lorem-ipsum-1", FriendlyName = "Lorem Ipsum 1", Color = Color.FromArgb(255, 186, 186) }
            };
        }

        public static void Save(SettingsSet mgr)
        {
            // write this object to json
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(mgr, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText("Settings\\settings.json", json);

        }

        public static SettingsSet? Load()
        {
            try
            {
                var text = File.ReadAllText("Settings\\settings.json");
                var retVal = Newtonsoft.Json.JsonConvert.DeserializeObject<SettingsSet>(text);
                retVal.Migrate();
                return retVal;
            }
            catch (FileNotFoundException e)
            {
                Debug.WriteLine(e.Message);
                var retVal = new SettingsSet();
                retVal.Create();
                Save(retVal);
                return retVal;
            }
            catch (DirectoryNotFoundException e)
            {
                Debug.WriteLine(e.Message);
                Directory.CreateDirectory("Settings");
                var retVal = new SettingsSet();
                retVal.Create();
                Save(retVal);
                return retVal;
            }
        }

        public Model GetModelByFullStringReference(string modelName) => ModelList.FirstOrDefault(x => x.ModelName == modelName);


        public Model GetModelByNameAndApi(string modelAndApi) => ModelList.FirstOrDefault(x => x.ToString() == modelAndApi);

        public Model GetSummaryModel() => GetModelByNameAndApi(SelectedSummaryModel);

        private void Migrate()
        {
            var newSettings = new SettingsSet();
            newSettings.Create();
            foreach (var model in newSettings.ModelList)
            {
                var newModel = ModelList.FirstOrDefault(x => x.ServiceName == model.ServiceName && x.ModelName == model.ModelName);
                if (newModel == null)
                {
                    // add to correct in apilist
                    ModelList.Add(model);
                }
                else if (model.SupportsPrefill != newModel.SupportsPrefill)
                    newModel.SupportsPrefill = model.SupportsPrefill;
            }

            Save(this);
        }

        public Model GetModel() => GetModelByNameAndApi(SelectedModel);

        internal void SetModelFromDropdownValue(string dropdown, string? modelString)
        {
            var models = ModelList;
            var matchingModel = models.FirstOrDefault(m => m.FriendlyName == modelString);
            if (matchingModel != null)
            {
                if (dropdown == "mainAI")
                {
                    SelectedModel = matchingModel.ToString();
                    SettingsSet.Save(this);
                }
                else if (dropdown == "summaryAI")
                {
                    SelectedSummaryModel = matchingModel.ToString();
                    SettingsSet.Save(this);
                }
            }
        }

        internal static SettingsSet? LoadOrPromptOnFirstRun()
        {
            SettingsSet settings = null;
            if (!File.Exists("Settings\\settings.json"))
            {
                settings = Load()!;
                // show the settings dialog first up
                var settingsForm = new SettingsForm(settings);
                var result = settingsForm.ShowDialog();
                settings = settingsForm.NewSettings;
                SettingsSet.Save(settings);
            }
            else settings = Load()!;

            return settings;
        }

        internal static async Task<SettingsSet> OpenSettingsForm(ChatWebView chatWebView, SettingsSet CurrentSettings)
        {
            var settingsForm = new SettingsForm(CurrentSettings);
            var result = settingsForm.ShowDialog();

            if (result == DialogResult.OK)
            {
                CurrentSettings = settingsForm.NewSettings;
                SettingsSet.Save(CurrentSettings);
                await chatWebView.InitialiseApiList(CurrentSettings);
            }

            return CurrentSettings;
        }
    }


    [AttributeUsage(AttributeTargets.Property)]
    public class MyDisplayNameAttrAttribute : Attribute
    {
        public string DisplayName { get; }

        public MyDisplayNameAttrAttribute(string displayName)
        {
            DisplayName = displayName;
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class IsPathAttribute : Attribute
    {
    }


    [AttributeUsage(AttributeTargets.Property)]
    public class IsFileAttribute : Attribute
    {
        public string Extension { get; set; }

        public IsFileAttribute(string extension)
        {
            Extension = extension;
        }
    }


}