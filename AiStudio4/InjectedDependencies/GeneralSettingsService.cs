// InjectedDependencies/GeneralSettingsService.cs
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;
using AiStudio4.Core.Models;
using SharedClasses.Providers;

namespace AiStudio4.InjectedDependencies
{
    public class GeneralSettingsService : IGeneralSettingsService
    {
        private readonly string _settingsFilePath;
        public GeneralSettings CurrentSettings { get; private set; } = new();
        private readonly object _lock = new();
        
        public event EventHandler SettingsChanged;

        public GeneralSettingsService(IConfiguration configuration)
        {
            _settingsFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AiStudio4", "settings.json");
            Directory.CreateDirectory(Path.GetDirectoryName(_settingsFilePath));
            LoadSettings();
        }

        public void LoadSettings()
        {
            lock (_lock)
            {
                if (!File.Exists(_settingsFilePath))
                {
                    CurrentSettings = new GeneralSettings();

                    CurrentSettings.ServiceProviders = new List<ServiceProvider>
                    {
                        new ServiceProvider
                        {
                            Url = "https://api.anthropic.com/v1/messages",
                            ApiKey = string.Empty,
                            FriendlyName = "Anthropic",
                            ServiceName = "Claude",
                            IconName = "Anthropic",
                            Guid = "8c7eb4ee-6b48-4700-b740-9fa86e0e068b"
                        },
                        new ServiceProvider
                        {
                            Url = "https://generativelanguage.googleapis.com/v1beta/models/",
                            ApiKey = string.Empty,
                            FriendlyName = "Google",
                            ServiceName = "Gemini",
                            IconName = "Google",
                            Guid = "312cb0dc-8f20-49b0-91bb-8577a344a7df"
                        },
                        new ServiceProvider
                        {
                            Url = "https://generativelanguage.googleapis.com/v1beta/models/",
                            ApiKey = string.Empty,
                            FriendlyName = "Google [OpenAI API]",
                            ServiceName = "NetOpenAi",
                            IconName = "Google",
                            Guid = "fac1a7e7-57d0-4a08-96db-b4d0a28a2397"
                        },
                        new ServiceProvider
                        {
                            Url = "https://api.openai.com/v1",
                            ApiKey = string.Empty,
                            FriendlyName = "OpenAI",
                            ServiceName = "NetOpenAi",
                            IconName = "OpenAI",
                            Guid = "58fe0301-f10e-4b5f-a967-481cffc39cc0"
                        },
                        new ServiceProvider
                        {
                            Url = "https://openrouter.ai/api/v1/",
                            ApiKey = string.Empty,
                            FriendlyName = "OpenAI",
                            ServiceName = "NetOpenAi",
                            IconName = "OpenRouter",
                            Guid = "d59ce7e8-db8b-4317-be5b-27f7b54273ab"
                        }
                    };

                    SaveSettings();
                    return;
                }
                var text = File.ReadAllText(_settingsFilePath);
                var json = JObject.Parse(text);
                var section = json["generalSettings"];
                if (section != null)
                {
                    CurrentSettings = section.ToObject<GeneralSettings>() ?? new GeneralSettings();

                    // Notify subscribers that settings have changed
                    SettingsChanged?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    CurrentSettings = new GeneralSettings();
                    SaveSettings();
                }
            }
        }

        public void SaveSettings()
        {
            lock (_lock)
            {
                JObject json;
                if (File.Exists(_settingsFilePath))
                {
                    json = JObject.Parse(File.ReadAllText(_settingsFilePath));
                }
                else
                {
                    json = new JObject();
                }
                json["generalSettings"] = JToken.FromObject(CurrentSettings);
                File.WriteAllText(_settingsFilePath, json.ToString(Formatting.Indented));
                
                // Notify subscribers that settings have changed
                SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public void UpdateSettings(GeneralSettings newSettings)
        {
            // Store the old project path to check if it changed
            string oldProjectPath = CurrentSettings.ProjectPath;
            
            CurrentSettings = newSettings;
            SaveSettings();
            
            // Notify subscribers that settings have changed
            SettingsChanged?.Invoke(this, EventArgs.Empty);
            
            // If project path changed, notify services that depend on the project path
            if (oldProjectPath != CurrentSettings.ProjectPath)
            {
                // This will be handled by the caller (MainWindow.xaml.cs)
                // which should call _builtinToolService.UpdateProjectRoot()
                
                // The ProjectFileWatcherService will be notified through the SettingsChanged event
                // and will initialize itself with the new path when needed
            }
        }

        public void UpdateDefaultModel(string modelNameOrGuid)
        {
            // Check if the parameter is a GUID or a name
            var model = CurrentSettings.ModelList.FirstOrDefault(m => m.Guid == modelNameOrGuid);
            if (model == null)
            {
                // If not found by GUID, try to find by name (for backward compatibility)
                model = CurrentSettings.ModelList.FirstOrDefault(m => m.ModelName == modelNameOrGuid);
                if (model == null)
                {
                    // If still not found, just store the value as is
                    CurrentSettings.DefaultModel = modelNameOrGuid;
                    CurrentSettings.DefaultModelGuid = string.Empty;
                    SaveSettings();
                    return;
                }
            }
            
            // Store both for backward compatibility
            CurrentSettings.DefaultModel = model.ModelName;
            CurrentSettings.DefaultModelGuid = model.Guid;
            SaveSettings();
        }

        public void UpdateSecondaryModel(string modelNameOrGuid)
        {
            // Check if the parameter is a GUID or a name
            var model = CurrentSettings.ModelList.FirstOrDefault(m => m.Guid == modelNameOrGuid);
            if (model == null)
            {
                // If not found by GUID, try to find by name (for backward compatibility)
                model = CurrentSettings.ModelList.FirstOrDefault(m => m.ModelName == modelNameOrGuid);
                if (model == null)
                {
                    // If still not found, just store the value as is
                    CurrentSettings.SecondaryModel = modelNameOrGuid;
                    CurrentSettings.SecondaryModelGuid = string.Empty;
                    SaveSettings();
                    return;
                }
            }
            
            // Store both for backward compatibility
            CurrentSettings.SecondaryModel = model.ModelName;
            CurrentSettings.SecondaryModelGuid = model.Guid;
            SaveSettings();
        }
        
        /// <summary>
        /// Simple implementation for single user - just sets GUIDs based on current model names if needed
        /// </summary>
        public void MigrateModelNamesToGuids()
        {
            // For a single user, we can just do a simple implementation
            // If we have model names but no GUIDs, try to find the models and set the GUIDs
            
            if (!string.IsNullOrEmpty(CurrentSettings.DefaultModel) && string.IsNullOrEmpty(CurrentSettings.DefaultModelGuid))
            {
                var model = CurrentSettings.ModelList.FirstOrDefault(m => m.ModelName == CurrentSettings.DefaultModel);
                if (model != null)
                {
                    CurrentSettings.DefaultModelGuid = model.Guid;
                }
            }
            
            if (!string.IsNullOrEmpty(CurrentSettings.SecondaryModel) && string.IsNullOrEmpty(CurrentSettings.SecondaryModelGuid))
            {
                var model = CurrentSettings.ModelList.FirstOrDefault(m => m.ModelName == CurrentSettings.SecondaryModel);
                if (model != null)
                {
                    CurrentSettings.SecondaryModelGuid = model.Guid;
                }
            }
        }

        public void AddModel(Model model)
        {
            CurrentSettings.ModelList.Add(model);
            SaveSettings();
        }

        public void UpdateModel(Model updatedModel)
        {
            var existing = CurrentSettings.ModelList.FirstOrDefault(m => m.Guid == updatedModel.Guid);
            if (existing != null)
            {
                var idx = CurrentSettings.ModelList.IndexOf(existing);
                CurrentSettings.ModelList[idx] = updatedModel;
                SaveSettings();
            }
        }

        public void DeleteModel(string modelGuid)
        {
            var existing = CurrentSettings.ModelList.FirstOrDefault(m => m.Guid == modelGuid);
            if (existing != null)
            {
                CurrentSettings.ModelList.Remove(existing);
                SaveSettings();
            }
        }

        public void AddServiceProvider(ServiceProvider provider)
        {
            CurrentSettings.ServiceProviders.Add(provider);
            SaveSettings();
        }

        public void UpdateServiceProvider(ServiceProvider updatedProvider)
        {
            var existing = CurrentSettings.ServiceProviders.FirstOrDefault(p => p.Guid == updatedProvider.Guid);
            if (existing != null)
            {
                var idx = CurrentSettings.ServiceProviders.IndexOf(existing);
                CurrentSettings.ServiceProviders[idx] = updatedProvider;
                SaveSettings();
            }
        }

        public void DeleteServiceProvider(string providerGuid)
        {
            var existing = CurrentSettings.ServiceProviders.FirstOrDefault(p => p.Guid == providerGuid);
            if (existing != null)
            {
                CurrentSettings.ServiceProviders.Remove(existing);
                SaveSettings();
            }
        }

        public void UpdateYouTubeApiKey(string apiKey)
        {
            CurrentSettings.YouTubeApiKey = apiKey;
            SaveSettings();
        }

        public void UpdateGitHubApiKey(string apiKey)
        {
            CurrentSettings.GitHubApiKey = apiKey;
            SaveSettings();
        }

        public void UpdateAzureDevOpsPAT(string pat)
        {
            CurrentSettings.AzureDevOpsPAT = pat;
            SaveSettings();
        }

        public void UpdateCondaPath(string path)
        {
            CurrentSettings.CondaPath = path;
            SaveSettings();
        }

        public void UpdateUseExperimentalCostTracking(bool value)
        {
            CurrentSettings.UseExperimentalCostTracking = value;
            SaveSettings(); // This will persist the change and invoke SettingsChanged event
        }

        public void UpdateConversationZipRetentionDays(int days)
        {
            CurrentSettings.ConversationZipRetentionDays = days;
            SaveSettings();
        }

        public void UpdateConversationDeleteZippedRetentionDays(int days)
        {
            CurrentSettings.ConversationDeleteZippedRetentionDays = days;
            SaveSettings();
        }
    }
}