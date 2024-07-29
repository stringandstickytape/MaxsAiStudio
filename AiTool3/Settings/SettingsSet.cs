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
        public List<Api>? ApiList { get; set; }

        [MyDisplayNameAttr("Narrate responses using Windows TTS")]
        public bool NarrateResponses { get; set; } = false;

        [MyDisplayNameAttr("Ollama Local Port")]
        public int OllamaLocalPort { get; set; } = 11434;

        [MyDisplayNameAttr("Temperature")]
        public float Temperature { get; set; } = 0.9f;

        [MyDisplayNameAttr("Show Developer Tools for WebViews (requires app restart)")]
        public bool ShowDevTools { get; set; } = false;

        [MyDisplayNameAttr("Run HTTP webserver on port 8080 (experimental, requires app restart, app must run as administrator)")]
        public bool RunWebServer { get; set; } = false;

        [MyDisplayNameAttr("Entertain me with dumb software toys while I wait for non-chat tasks")]
        public bool SoftwareToyMode { get; set; } = false;

        [MyDisplayNameAttr("Use embeddings (experimental)")]
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

        public void AddOrUpdateModel(string apiName, Model model)
        {
            var api = ApiList.FirstOrDefault(a => a.ApiName == apiName);
            if (api == null)
            {
                api = new Api { ApiName = apiName, ApiUrl = model.Url, Models = new List<Model>() };
                ApiList.Add(api);
            }

            var existingModel = api.Models.FirstOrDefault(m => m.ModelName == model.ModelName);
            if (existingModel != null)
            {
                api.Models.Remove(existingModel);
            }
            api.Models.Add(model);
        }

        public void RemoveModel(string apiName, string modelName)
        {
            var api = ApiList.FirstOrDefault(a => a.ApiName == apiName);
            if (api != null)
            {
                api.Models.RemoveAll(m => m.ModelName == modelName);
                if (api.Models.Count == 0)
                {
                    ApiList.Remove(api);
                }
            }
        }

        private void Create()
        {
            ApiList = new List<Api>();

            ApiList.Add(new Api
            {
                ApiName = "OpenAI",
                ApiUrl = "https://api.openai.com/v1/chat/completions",
                Models = new List<Model>
                {// gpt-4o-mini
                    new Model { Url = "https://api.openai.com/v1/chat/completions", ServiceName = typeof(OpenAI).Name, ModelName = "gpt-4o", Color = Color.FromArgb(255, 179, 186)},
                    new Model { Url = "https://api.openai.com/v1/chat/completions", ServiceName = typeof(OpenAI).Name, ModelName = "gpt-4o-mini-2024-07-18", Color = Color.FromArgb(186, 201, 255) },
                    new Model { Url = "https://api.openai.com/v1/chat/completions", ServiceName = typeof(OpenAI).Name, ModelName = "gpt-4-turbo", Color = Color.FromArgb(186, 255, 201)},
                    new Model { Url = "https://api.openai.com/v1/chat/completions", ServiceName = typeof(OpenAI).Name, ModelName = "gpt-3.5-turbo", Color = Color.FromArgb(186, 225, 255)}
                },

            });

            ApiList.Add(new Api
            {
                ApiName = "Ollama (Port 11434 default)",
                ApiUrl = "http://localhost:11434/api/chat/",
                Models = new List<Model>
                {
                    new Model { Url = "http://localhost:11434/api/chat/", ServiceName = typeof(LocalAI).Name, ModelName = "llava:7b", Color = Color.FromArgb(255, 255, 186)},
                    new Model { Url = "http://localhost:11434/api/chat/", ServiceName = typeof(LocalAI).Name, ModelName = "llava:13b", Color = Color.FromArgb(255, 255, 186)},
                    new Model { Url = "http://localhost:11434/api/chat/", ServiceName = typeof(LocalAI).Name, ModelName = "llama3.1:8b", Color = Color.FromArgb(255, 255, 186)},
                    new Model { Url = "http://localhost:11434/api/chat/", ServiceName = typeof(LocalAI).Name, ModelName = "llama3.1:70b", Color = Color.FromArgb(255, 255, 186)},
                    new Model { Url = "http://localhost:11434/api/chat/", ServiceName = typeof(LocalAI).Name, ModelName = "codestral", Color = Color.FromArgb(255, 255, 186)},
                    new Model { Url = "http://localhost:11434/api/chat/", ServiceName = typeof(LocalAI).Name, ModelName = "gemma2", Color = Color.FromArgb(255, 255, 186)},
                    new Model { Url = "http://localhost:11434/api/chat/", ServiceName = typeof(LocalAI).Name, ModelName = "gemma2:27b", Color = Color.FromArgb(255, 255, 186)},
                    new Model { Url = "http://localhost:11434/api/chat/", ServiceName = typeof(LocalAI).Name, ModelName = "deepseek-coder-v2", Color = Color.FromArgb(255, 255, 186)},
                },
            });

            ApiList.Add(new Api
            {
                ApiName = "Groq",
                ApiUrl = "https://api.groq.com/openai/v1/chat/completions",
                Models = new List<Model>
                {
                    new Model { Url = "https://api.groq.com/openai/v1/chat/completions", ServiceName = typeof(Groq).Name, ModelName = "llama3-8b-8192", Color = Color.FromArgb(255, 216, 186)},
                    new Model { Url = "https://api.groq.com/openai/v1/chat/completions", ServiceName = typeof(Groq).Name, ModelName = "llama3-70b-8192", Color = Color.FromArgb(224, 186, 255)},
                },
            });

            ApiList.Add(new Api
            {
                ApiName = "Gemini",
                ApiUrl = "https://generativelanguage.googleapis.com/v1beta/models/",
                Models = new List<Model>
                {
                    new Model { Url = "https://generativelanguage.googleapis.com/v1beta/models/", ServiceName = typeof(Gemini).Name, ModelName = "gemini-1.5-pro", Color = Color.FromArgb(186, 255, 216)},
                },
            });

            ApiList.Add(new Api
            {
                ApiName = "Anthropic",
                ApiUrl = "https://api.anthropic.com/v1/messages",
                Models = new List<Model>
                {
                    new Model { Url = "https://api.anthropic.com/v1/messages", ServiceName = typeof(Claude).Name, ModelName = "claude-3-5-sonnet-20240620", Color = Color.FromArgb(255, 219, 186) },
                    new Model { Url = "https://api.anthropic.com/v1/messages", ServiceName = typeof(Claude).Name, ModelName = "claude-3-opus-20240229", Color = Color.FromArgb(186, 207, 255)},
                    new Model { Url = "https://api.anthropic.com/v1/messages", ServiceName = typeof(Claude).Name, ModelName = "claude-3-sonnet-20240229", Color = Color.FromArgb(186, 255, 237)},
                    new Model { Url = "https://api.anthropic.com/v1/messages", ServiceName = typeof(Claude).Name, ModelName = "claude-3-haiku-20240307", Color = Color.FromArgb(216, 186, 255)},
                },
            });

            // mock service
            ApiList.Add(new Api
            {
                ApiName = "Mock AI Service",
                ApiUrl = "Not Applicable",
                Models = new List<Model>
                {
                    new Model { Url = "https://mock.com", ServiceName = typeof(MockAiService).Name, ModelName = "lorem-ipsum-1", Color = Color.FromArgb(255, 186, 186)},
                },

            });
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

        public Model GetModelByName(string modelName) =>  ApiList.SelectMany(x => x.Models).FirstOrDefault(x => x.ModelName == modelName);


        public Model GetModelByFullStringReference(string modelName) => ApiList.SelectMany(x => x.Models).FirstOrDefault(x => x.ToString() == modelName);

        public Model GetSummaryModel() => GetModelByFullStringReference(SelectedSummaryModel);

        private void AddMissingApis()
        {
            var newSettings = new SettingsSet();
            newSettings.Create();
            foreach (var api in newSettings.ApiList)
            {
                if (!ApiList.Any(x => x.ApiName == api.ApiName))
                {
                    ApiList.Add(api);
                }


                foreach(var model in api.Models)
                {
                    var newModel = GetModelByName(model.ModelName);
                    if (newModel == null)
                    {
                        // add to correct in apilist
                        var correctApi = ApiList.First(x => x.ApiName == api.ApiName);
                        correctApi.Models.Add(model);
                    }
                }
            }

            Save(this);
        }

        internal IEnumerable<Model> GetAllModels()
        {
            return ApiList!.SelectMany(x => x.Models).ToList();
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