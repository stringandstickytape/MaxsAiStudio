
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;
using AiStudio4.Core.Models; 
using SharedClasses.Providers; 
using System.Security.Cryptography; 
using System.Text; 
using System.Collections.Generic; 
using System.Drawing; 

namespace AiStudio4.InjectedDependencies
{
    public class GeneralSettingsService : IGeneralSettingsService
    {
        private readonly string _settingsFilePath;
        public GeneralSettings CurrentSettings { get; private set; } = new();
        private readonly object _lock = new();
        public event EventHandler SettingsChanged;

        
        private static readonly byte[] s_entropy = Encoding.UTF8.GetBytes("A!S@t#u$d%i^o&4*S(e)c-r_e+t");

        public GeneralSettingsService(IConfiguration configuration)
        {    
            _settingsFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AiStudio4", "settings.json");
            Directory.CreateDirectory(Path.GetDirectoryName(_settingsFilePath));
            LoadSettings();
        }

        private byte[] ProtectData(string data)
        {
            if (string.IsNullOrEmpty(data)) return null;
            byte[] dataBytes = Encoding.UTF8.GetBytes(data);
            
            
            return ProtectedData.Protect(dataBytes, s_entropy, DataProtectionScope.CurrentUser);
        }

        private string UnprotectData(byte[] encryptedData)
        {    
            if (encryptedData == null || encryptedData.Length == 0) return null;
            
            byte[] decryptedDataBytes = ProtectedData.Unprotect(encryptedData, s_entropy, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(decryptedDataBytes);
        }

        public void LoadSettings()
        {
            lock (_lock)
            {
                bool settingsModifiedDuringLoad = false; 

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
                            FriendlyName = "OpenRouter",
                            ServiceName = "NetOpenAi",
                            IconName = "OpenRouter",
                            Guid = "d59ce7e8-db8b-4317-be5b-27f7b54273ab"
                        }
                    };


                    CurrentSettings.ModelList = new List<Model>
                    {
                        new Model
                        {
                            ModelName = "claude-sonnet-4-20250514",
                            UserNotes = "",
                            ProviderGuid = "8c7eb4ee-6b48-4700-b740-9fa86e0e068b",
                            AdditionalParams = null,
                            input1MTokenPrice = 3.0m,
                            output1MTokenPrice = 15.0m,
                            Color = Color.FromArgb(0x4F46E5),
                            Starred = false,
                            FriendlyName = "Sonnet 4",
                            Guid = "6d21047e-78bd-4adb-a0f7-e3fa6b48ef61",
                            SupportsPrefill = false,
                            Requires1fTemp = false,
                            ReasoningEffort = "none",
                            IsTtsModel = false,
                            TtsVoiceName = "Kore"
                        },
                        new Model
                        {
                            ModelName = "gpt-4.1-mini",
                            UserNotes = "",
                            ProviderGuid = "58fe0301-f10e-4b5f-a967-481cffc39cc0",
                            AdditionalParams = null,
                            input1MTokenPrice = 0.4m,
                            output1MTokenPrice = 1.6m,
                            Color = Color.FromArgb(0x4F46E5),
                            Starred = false,
                            FriendlyName = "GPT 4.1 Mini",
                            Guid = "6c21b1dd-2a91-4b5a-b904-a0ee04147ed1",
                            SupportsPrefill = false,
                            Requires1fTemp = false,
                            ReasoningEffort = "none",
                            IsTtsModel = false,
                            TtsVoiceName = "Kore"
                        },
                        new Model
                        {
                            ModelName = "gemini-2.5-pro-preview-05-06",
                            UserNotes = "",
                            ProviderGuid = "312cb0dc-8f20-49b0-91bb-8577a344a7df",
                            AdditionalParams = null,
                            input1MTokenPrice = 2.5m,
                            output1MTokenPrice = 15.0m,
                            Color = Color.FromArgb(0xAEAA3D),
                            Starred = false,
                            FriendlyName = "Gemini 2.5 Pro Exp 05 06",
                            Guid = "60c7c581-8fa2-4efd-b393-31c7019ab1aa",
                            SupportsPrefill = false,
                            Requires1fTemp = false,
                            ReasoningEffort = "none",
                            IsTtsModel = false,
                            TtsVoiceName = "Kore"
                        },
                        new Model
                        {
                            ModelName = "qwen/qwen3-235b-a22b",
                            UserNotes = "",
                            ProviderGuid = "d59ce7e8-db8b-4317-be5b-27f7b54273ab",
                            AdditionalParams = null,
                            input1MTokenPrice = 0.1m,
                            output1MTokenPrice = 0.1m,
                            Color = Color.FromArgb(0x4F46E5),
                            Starred = false,
                            FriendlyName = "OpenRouter qwen3-235b-a22b",
                            Guid = "b77ebaae-aa7d-4354-a584-20d33f184f97",
                            SupportsPrefill = false,
                            Requires1fTemp = false,
                            ReasoningEffort = "none",
                            IsTtsModel = false,
                            TtsVoiceName = "Kore"
                        }
                    };

                    
                    settingsModifiedDuringLoad = true; 
                }
                else
                {
                    var text = File.ReadAllText(_settingsFilePath);
                    var json = JObject.Parse(text);
                    var section = json["generalSettings"];

                    if (section != null)
                    {
                        
                        var loadedSettings = section.ToObject<GeneralSettings>() ?? new GeneralSettings();
                        CurrentSettings = loadedSettings; 

                        
                        if (!string.IsNullOrEmpty(CurrentSettings.YouTubeApiKey) && string.IsNullOrEmpty(CurrentSettings.EncryptedYouTubeApiKey))
                        {
                            CurrentSettings.EncryptedYouTubeApiKey = CurrentSettings.YouTubeApiKey != null ? Convert.ToBase64String(ProtectData(CurrentSettings.YouTubeApiKey)) : null;
                            CurrentSettings.YouTubeApiKey = null;
                            settingsModifiedDuringLoad = true;
                        }
                        
                         if (!string.IsNullOrEmpty(CurrentSettings.GitHubApiKey) && string.IsNullOrEmpty(CurrentSettings.EncryptedGitHubApiKey))
                        {
                            CurrentSettings.EncryptedGitHubApiKey = CurrentSettings.GitHubApiKey != null ? Convert.ToBase64String(ProtectData(CurrentSettings.GitHubApiKey)) : null;
                            CurrentSettings.GitHubApiKey = null; 
                            settingsModifiedDuringLoad = true;
                        }
                        if (!string.IsNullOrEmpty(CurrentSettings.AzureDevOpsPAT) && string.IsNullOrEmpty(CurrentSettings.EncryptedAzureDevOpsPAT))
                        {
                            CurrentSettings.EncryptedAzureDevOpsPAT = CurrentSettings.AzureDevOpsPAT != null ? Convert.ToBase64String(ProtectData(CurrentSettings.AzureDevOpsPAT)) : null;
                            CurrentSettings.AzureDevOpsPAT = null;
                            settingsModifiedDuringLoad = true;
                        }

                        // Initialize PackerExcludeFolderNames if it's null (e.g., from older settings file)
                        if (CurrentSettings.PackerExcludeFolderNames == null)
                        {
                            CurrentSettings.PackerExcludeFolderNames = new List<string>();
                            settingsModifiedDuringLoad = true;
                        }

                        // Initialize TopP if it's missing (e.g., from older settings file)
                        // GeneralSettings constructor defaults TopP to 0.9f.
                        // This explicit check ensures settingsModifiedDuringLoad is true if it was missing.
                        if (section["TopP"] == null) // Check if TopP was actually missing in the loaded JSON
                        {
                            CurrentSettings.TopP = 0.9f; // Ensure default if somehow not set by constructor or if we want to force save
                            settingsModifiedDuringLoad = true;
                        }

                        
                        if (CurrentSettings.ServiceProviders != null)
                        {
                            var decryptedProviders = new List<ServiceProvider>();
                            foreach (var provider in CurrentSettings.ServiceProviders)
                            {
                                var decryptedProvider = provider; 
                                if (!string.IsNullOrEmpty(provider.ApiKey))
                                {
                                    try
                                    {
                                        
                                        byte[] encryptedApiKeyBytes = Convert.FromBase64String(provider.ApiKey);
                                        decryptedProvider.ApiKey = UnprotectData(encryptedApiKeyBytes);
                                    }
                                    catch (FormatException) 
                                    {
                                        
                                        
                                        
                                        settingsModifiedDuringLoad = true; 
                                    }
                                    catch (CryptographicException ex)
                                    {
                                        Console.WriteLine($"Error decrypting API Key for provider '{provider.FriendlyName}': {ex.Message}. Key cleared.");
                                        decryptedProvider.ApiKey = null; 
                                        settingsModifiedDuringLoad = true; 
                                    }
                                }
                                decryptedProviders.Add(decryptedProvider);
                            }
                            CurrentSettings.ServiceProviders = decryptedProviders;
                        }
                        
                    }
                    else
                    {
                        CurrentSettings = new GeneralSettings();
                        settingsModifiedDuringLoad = true; 
                    }
                }

                if (settingsModifiedDuringLoad)
                {
                    SaveSettings(); 
                }
                SettingsChanged?.Invoke(this, EventArgs.Empty);
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

                
                
                var settingsForSerialization = JsonConvert.DeserializeObject<GeneralSettings>(JsonConvert.SerializeObject(CurrentSettings));

                
                if (settingsForSerialization.ServiceProviders != null)
                {
                    foreach (var provider in settingsForSerialization.ServiceProviders)
                    {
                        if (!string.IsNullOrEmpty(provider.ApiKey))
                        {
                            byte[] encryptedKeyBytes = ProtectData(provider.ApiKey);
                            provider.ApiKey = encryptedKeyBytes != null ? Convert.ToBase64String(encryptedKeyBytes) : null;
                        }
                    }
                }
                

                
                var settingsToSaveToken = JObject.FromObject(settingsForSerialization); 
                settingsToSaveToken.Remove("YouTubeApiKey");
                settingsToSaveToken.Remove("GitHubApiKey");
                settingsToSaveToken.Remove("AzureDevOpsPAT");
                
                settingsToSaveToken.Remove("DefaultModel");
                settingsToSaveToken.Remove("SecondaryModel");


                json["generalSettings"] = settingsToSaveToken;
                File.WriteAllText(_settingsFilePath, json.ToString(Formatting.Indented));
                SettingsChanged?.Invoke(this, EventArgs.Empty);
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
        
        
        public void DeleteServiceProvider(string providerGuid) {
            var existing = CurrentSettings.ServiceProviders.FirstOrDefault(p => p.Guid == providerGuid);
            if (existing != null)
            {
                CurrentSettings.ServiceProviders.Remove(existing);
                SaveSettings();
            }
        }

        
        public void UpdateSettings(GeneralSettings newSettings) {
            
            
            CurrentSettings = newSettings; SaveSettings(); 
        }
        public void UpdateDefaultModel(string modelGuidOrName) {
             
            var model = CurrentSettings.ModelList.FirstOrDefault(m => m.Guid == modelGuidOrName || m.ModelName == modelGuidOrName);
            CurrentSettings.DefaultModelGuid = model?.Guid ?? modelGuidOrName; 
            CurrentSettings.DefaultModel = model?.ModelName ?? modelGuidOrName; 
            SaveSettings(); 
        }
        public void UpdateSecondaryModel(string modelGuidOrName) {
            var model = CurrentSettings.ModelList.FirstOrDefault(m => m.Guid == modelGuidOrName || m.ModelName == modelGuidOrName);
            CurrentSettings.SecondaryModelGuid = model?.Guid ?? modelGuidOrName;
            CurrentSettings.SecondaryModel = model?.ModelName ?? modelGuidOrName;
            SaveSettings(); 
        }
        public void MigrateModelNamesToGuids() { }
        public void AddModel(Model model) { CurrentSettings.ModelList.Add(model); SaveSettings(); }
        public void UpdateModel(Model updatedModel) { 
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

        public void UpdateYouTubeApiKey(string plaintextApiKey)
        {
            CurrentSettings.EncryptedYouTubeApiKey = !string.IsNullOrEmpty(plaintextApiKey) ? Convert.ToBase64String(ProtectData(plaintextApiKey)) : null;
            SaveSettings();
        }

        public string GetDecryptedYouTubeApiKey()
        {
            try { return !string.IsNullOrEmpty(CurrentSettings.EncryptedYouTubeApiKey) ? UnprotectData(Convert.FromBase64String(CurrentSettings.EncryptedYouTubeApiKey)) : null; }
            catch (CryptographicException) { CurrentSettings.EncryptedYouTubeApiKey = null; SaveSettings(); return null; }
        }

        public void UpdateGitHubApiKey(string plaintextApiKey)
        {
            CurrentSettings.EncryptedGitHubApiKey = !string.IsNullOrEmpty(plaintextApiKey) ? Convert.ToBase64String(ProtectData(plaintextApiKey)) : null;
            SaveSettings();
        }

        public string GetDecryptedGitHubApiKey()
        {
            try { return !string.IsNullOrEmpty(CurrentSettings.EncryptedGitHubApiKey) ? UnprotectData(Convert.FromBase64String(CurrentSettings.EncryptedGitHubApiKey)) : null; }
            catch (CryptographicException) { CurrentSettings.EncryptedGitHubApiKey = null; SaveSettings(); return null; }
        }

        public void UpdateAzureDevOpsPAT(string plaintextPat)
        {
            CurrentSettings.EncryptedAzureDevOpsPAT = !string.IsNullOrEmpty(plaintextPat) ? Convert.ToBase64String(ProtectData(plaintextPat)) : null;
            SaveSettings();
        }

        public string GetDecryptedAzureDevOpsPAT()
        {
            try { return !string.IsNullOrEmpty(CurrentSettings.EncryptedAzureDevOpsPAT) ? UnprotectData(Convert.FromBase64String(CurrentSettings.EncryptedAzureDevOpsPAT)) : null; }
            catch (CryptographicException) { CurrentSettings.EncryptedAzureDevOpsPAT = null; SaveSettings(); return null; }
        }

        public void UpdateCondaPath(string path) { CurrentSettings.CondaPath = path; SaveSettings(); }
        public void UpdateUseExperimentalCostTracking(bool value) { CurrentSettings.UseExperimentalCostTracking = value; SaveSettings(); }
        public void UpdateConversationZipRetentionDays(int days) { CurrentSettings.ConversationZipRetentionDays = days; SaveSettings(); }
        public void UpdateConversationDeleteZippedRetentionDays(int days) { CurrentSettings.ConversationDeleteZippedRetentionDays = days; SaveSettings(); }

        public void UpdateTopP(float topP)
        {
            CurrentSettings.TopP = topP;
            SaveSettings();
        }
    }
}
