using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using AiTool3.ApiManagement;
using AiTool3.Providers;
using System.ComponentModel;
using System.Diagnostics;

namespace AiTool3
{
    public class SettingsSet

    {
        public List<Model> ModelList { get; set; } = new List<Model>();

        [MyDisplayNameAttr("Narrate responses using Windows TTS")]
        public bool NarrateResponses { get; set; } = false;

        [MyDisplayNameAttr("Temperature")]
        public float Temperature { get; set; } = 0.9f;

        [MyDisplayNameAttr("Run HTTP webserver on port 8080 (experimental, requires app restart, app must run as administrator)")]
        public bool RunWebServer { get; set; } = false;

        [MyDisplayNameAttr("Entertain me with dumb software toys while I wait for non-chat tasks")]
        public bool SoftwareToyMode { get; set; } = false;

        [MyDisplayNameAttr("Use embeddings")]
        public bool UseEmbeddings { get; set; } = false;

        [MyDisplayNameAttr("Stream responses")]
        public bool StreamResponses { get; set; } = false;

        [MyDisplayNameAttr("OpenAI API key for embeddings")]
        public string EmbeddingKey { get; set; } = "";

        [IsFileAttribute(".embeddings.json")]
        [MyDisplayNameAttr("Embeddings Filename/path")]
        public string EmbeddingsFilename { get; set; }

        [IsPathAttribute]
        [MyDisplayNameAttr("Default Path")]
        public string DefaultPath { get; set; } = Directory.GetCurrentDirectory();

        [MyDisplayNameAttr("File extensions to display in the Project Helper")]
        public string ProjectHelperFileExtensions { get; set; } = "*.cs, *.html, *.css, *.js";

        [MyDisplayNameAttr("Collapse conversation pane at startup")]
        public bool CollapseConversationPane { get; set; } = false;


        public string SelectedModel { get; set; } = "";
        public string SelectedSummaryModel { get; set; } = "";
        public string SelectedTheme { get; set; }

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
                    new Model { Url = "https://api.openai.com/v1/chat/completions", ServiceName = typeof(OpenAI).Name, ModelName = "gpt-4o", Color = Color.FromArgb(255, 179, 186), input1MTokenPrice = 5, output1MTokenPrice = 15},
                    new Model { Url = "https://api.openai.com/v1/chat/completions", ServiceName = typeof(OpenAI).Name, ModelName = "gpt-4o-mini-2024-07-18", Color = Color.FromArgb(186, 201, 255) , input1MTokenPrice = 0.15m, output1MTokenPrice = .6m},
                    new Model { Url = "https://api.openai.com/v1/chat/completions", ServiceName = typeof(OpenAI).Name, ModelName = "gpt-4-turbo", Color = Color.FromArgb(186, 255, 201), input1MTokenPrice = 10, output1MTokenPrice = 30},
                    new Model { Url = "https://api.openai.com/v1/chat/completions", ServiceName = typeof(OpenAI).Name, ModelName = "gpt-3.5-turbo", Color = Color.FromArgb(186, 225, 255), input1MTokenPrice = 0.5m, output1MTokenPrice = 1.5m},
                    new Model { Url = "http://localhost:11434/api/chat/", ServiceName = typeof(LocalAI).Name, ModelName = "llava:7b", Color = Color.FromArgb(255, 255, 186)},
                    new Model { Url = "http://localhost:11434/api/chat/", ServiceName = typeof(LocalAI).Name, ModelName = "llava:13b", Color = Color.FromArgb(255, 255, 186)},
                    new Model { Url = "http://localhost:11434/api/chat/", ServiceName = typeof(LocalAI).Name, ModelName = "llama3.1:8b", Color = Color.FromArgb(255, 255, 186)},
                    new Model { Url = "http://localhost:11434/api/chat/", ServiceName = typeof(LocalAI).Name, ModelName = "llama3.1:70b", Color = Color.FromArgb(255, 255, 186)},
                    new Model { Url = "http://localhost:11434/api/chat/", ServiceName = typeof(LocalAI).Name, ModelName = "codestral", Color = Color.FromArgb(255, 255, 186)},
                    new Model { Url = "http://localhost:11434/api/chat/", ServiceName = typeof(LocalAI).Name, ModelName = "gemma2", Color = Color.FromArgb(255, 255, 186)},
                    new Model { Url = "http://localhost:11434/api/chat/", ServiceName = typeof(LocalAI).Name, ModelName = "gemma2:27b", Color = Color.FromArgb(255, 255, 186)},
                    new Model { Url = "http://localhost:11434/api/chat/", ServiceName = typeof(LocalAI).Name, ModelName = "deepseek-coder-v2", Color = Color.FromArgb(255, 255, 186)},
                    new Model { Url = "https://api.groq.com/openai/v1/chat/completions", ServiceName = typeof(Groq).Name, ModelName = "llama3-8b-8192", Color = Color.FromArgb(255, 216, 186)},
                    new Model { Url = "https://api.groq.com/openai/v1/chat/completions", ServiceName = typeof(Groq).Name, ModelName = "llama3-70b-8192", Color = Color.FromArgb(224, 186, 255)},
                    new Model { Url = "https://generativelanguage.googleapis.com/v1beta/models/", ServiceName = typeof(Gemini).Name, ModelName = "gemini-1.5-pro", Color = Color.FromArgb(186, 255, 216), input1MTokenPrice = .7m, output1MTokenPrice = 2.1m},
                    new Model { Url = "https://api.anthropic.com/v1/messages", ServiceName = typeof(Claude).Name, ModelName = "claude-3-5-sonnet-20240620", Color = Color.FromArgb(255, 219, 186) , input1MTokenPrice = 3, output1MTokenPrice = 15},
                    new Model { Url = "https://api.anthropic.com/v1/messages", ServiceName = typeof(Claude).Name, ModelName = "claude-3-opus-20240229", Color = Color.FromArgb(186, 207, 255), input1MTokenPrice = 15, output1MTokenPrice = 75},
                    new Model { Url = "https://api.anthropic.com/v1/messages", ServiceName = typeof(Claude).Name, ModelName = "claude-3-sonnet-20240229", Color = Color.FromArgb(186, 255, 237), input1MTokenPrice = 3, output1MTokenPrice = 15},
                    new Model { Url = "https://api.anthropic.com/v1/messages", ServiceName = typeof(Claude).Name, ModelName = "claude-3-haiku-20240307", Color = Color.FromArgb(216, 186, 255), input1MTokenPrice = .25m, output1MTokenPrice = 1.25m},
                    new Model { Url = "https://mock.com", ServiceName = typeof(MockAiService).Name, ModelName = "lorem-ipsum-1", Color = Color.FromArgb(255, 186, 186)},
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
                retVal.AddMissingApis();
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
            catch(DirectoryNotFoundException e)
            {
                Debug.WriteLine(e.Message);
                Directory.CreateDirectory("Settings");
                var retVal = new SettingsSet();
                retVal.Create();
                Save(retVal);
                return retVal;
            }
        }

        public Model GetModelByName(string modelName) =>  ModelList.FirstOrDefault(x => x.ModelName == modelName);


        public Model GetModelByFullStringReference(string modelName) => ModelList.FirstOrDefault(x => x.ToString() == modelName);

        public Model GetSummaryModel() => GetModelByFullStringReference(SelectedSummaryModel);

        private void AddMissingApis()
        {
            var newSettings = new SettingsSet();
            newSettings.Create();
            foreach (var model in newSettings.ModelList)
            {
                    var newModel = GetModelByName(model.ModelName);
                    if (newModel == null)
                    {
                        // add to correct in apilist
                        ModelList.Add(model);
                    }
            }

            Save(this);
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