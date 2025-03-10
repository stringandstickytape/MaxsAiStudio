﻿using AiTool3.DataModels;
using AiTool3.AiServices;
using AiTool3.Settings;
using AiTool3.UI;
using SharedClasses.Models;
using System.Diagnostics;
using System.Security.Policy;
using static System.Net.WebRequestMethods;
using SharedClasses.Providers;

namespace AiTool3
{
    public class SettingsSet

    {
        private string selectedSummaryModel = "";

        public List<ServiceProvider> ServiceProviders { get; set; } = new List<ServiceProvider>();
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

            };

            ServiceProviders = new List<ServiceProvider>
            {
                new ServiceProvider { FriendlyName = "OpenAI", ServiceName = typeof(OpenAI).Name, Url = "https://api.openai.com/v1/chat/completions" },
                new ServiceProvider { FriendlyName = "Anthropic", ServiceName = typeof(Claude).Name, Url = "https://api.anthropic.com/v1/messages" },
                new ServiceProvider { FriendlyName = "Google", ServiceName = typeof(Gemini).Name, Url = "https://generativelanguage.googleapis.com/v1beta/models/" },
                new ServiceProvider { FriendlyName = "Local:11434", ServiceName = typeof(LocalAI).Name, Url = "http://localhost:11434/api/chat/" },
                new ServiceProvider { FriendlyName = "Local:11434  [OpenAI API]", ServiceName = typeof(OpenAI).Name, Url = "http://localhost:11434/v1/chat/completions" },
                new ServiceProvider { FriendlyName = "Google [OpenAI API]", ServiceName = typeof(OpenAI).Name, Url = "https://generativelanguage.googleapis.com/v1beta/openai/chat/completions/" },
                new ServiceProvider { FriendlyName = "Grok", ServiceName = typeof(Groq).Name, Url = "https://api.groq.com/openai/v1/chat/completions"},
                new ServiceProvider { FriendlyName = "OpenRouter", ServiceName = typeof(OpenRouterAI).Name, Url = "https://openrouter.ai/api"},
            };
        }

        public static void Save(SettingsSet mgr)
        {
            // write this object to json
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(mgr, Newtonsoft.Json.Formatting.Indented);
            System.IO.File.WriteAllText("Settings\\settings.json", json);

        }



        public static SettingsSet? Load()
        {
            try
            {
                var text = System.IO.File.ReadAllText("Settings\\settings.json");
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
                var newModel = ModelList.FirstOrDefault(x => x.ProviderGuid == model.ProviderGuid && x.ModelName == model.ModelName);
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

        // Create from SettingsSet
        public ApiSettings ToApiSettings()
        {
            return new ApiSettings
            {
                Temperature = Temperature,
                UsePromptCaching = UsePromptCaching,
                StreamResponses = StreamResponses,
                EmbeddingModel = EmbeddingModel,
                EmbeddingsFilename = EmbeddingsFilename,
                UseEmbeddings = UseEmbeddings
            };
        }
        internal static SettingsSet? LoadOrPromptOnFirstRun()
        {
            SettingsSet settings = null;
            if (!System.IO.File.Exists("Settings\\settings.json"))
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