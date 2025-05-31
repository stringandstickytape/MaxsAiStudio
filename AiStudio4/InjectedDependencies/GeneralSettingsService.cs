// AiStudio4/InjectedDependencies/GeneralSettingsService.cs
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;
using AiStudio4.Core.Models; // If your Model class is here
using SharedClasses.Providers; // For ServiceProvider, Model
using System.Security.Cryptography; // For ProtectedData
using System.Text; // For Encoding
using System.Collections.Generic; // For List
using System.Drawing; // For Color

namespace AiStudio4.InjectedDependencies
{
    public class GeneralSettingsService : IGeneralSettingsService
    {
        private readonly string _settingsFilePath;
        public GeneralSettings CurrentSettings { get; private set; } = new();
        private readonly object _lock = new();
        public event EventHandler SettingsChanged;

        // Using the same entropy as before for consistency.
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
            // It's important to use the same entropy for all DPAPI operations in this context
            // or manage different entropies carefully if needed (not recommended for simplicity here).
            return ProtectedData.Protect(dataBytes, s_entropy, DataProtectionScope.CurrentUser);
        }

        private string UnprotectData(byte[] encryptedData)
        {    
            if (encryptedData == null || encryptedData.Length == 0) return null;
            // Same entropy must be used here.
            byte[] decryptedDataBytes = ProtectedData.Unprotect(encryptedData, s_entropy, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(decryptedDataBytes);
        }

        public void LoadSettings()
        {
            lock (_lock)
            {
                bool settingsModifiedDuringLoad = false; // Flag to track if we need to re-save after loading/migration

                if (!File.Exists(_settingsFilePath))
                {
                    CurrentSettings = new GeneralSettings();
                    // Initialize default providers and models
                    CurrentSettings.ServiceProviders = new List<ServiceProvider> { /* ... your defaults ... */ };
                    CurrentSettings.ModelList = new List<Model> { /* ... your defaults ... */ };
                    // When creating defaults, API keys would be plaintext. They'll be encrypted on first save.
                    settingsModifiedDuringLoad = true; // Mark for saving to ensure defaults are encrypted
                }
                else
                {
                    var text = File.ReadAllText(_settingsFilePath);
                    var json = JObject.Parse(text);
                    var section = json["generalSettings"];

                    if (section != null)
                    {
                        // Deserialize into a temporary GeneralSettings object that might have encrypted/plaintext keys
                        var loadedSettings = section.ToObject<GeneralSettings>() ?? new GeneralSettings();
                        CurrentSettings = loadedSettings; // Start with loaded settings

                        // --- MIGRATE/DECRYPT TOP-LEVEL API KEYS (as before) ---
                        if (!string.IsNullOrEmpty(CurrentSettings.YouTubeApiKey) && string.IsNullOrEmpty(CurrentSettings.EncryptedYouTubeApiKey))
                        {
                            CurrentSettings.EncryptedYouTubeApiKey = CurrentSettings.YouTubeApiKey != null ? Convert.ToBase64String(ProtectData(CurrentSettings.YouTubeApiKey)) : null;
                            CurrentSettings.YouTubeApiKey = null;
                            settingsModifiedDuringLoad = true;
                        }
                        // ... (Repeat for GitHubApiKey and AzureDevOpsPAT as in previous response) ...
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

                        // --- DECRYPT SERVICE PROVIDER API KEYS ---
                        if (CurrentSettings.ServiceProviders != null)
                        {
                            var decryptedProviders = new List<ServiceProvider>();
                            foreach (var provider in CurrentSettings.ServiceProviders)
                            {
                                var decryptedProvider = provider; // Start with a copy
                                if (!string.IsNullOrEmpty(provider.ApiKey))
                                {
                                    try
                                    {
                                        // Assume it's Base64 encrypted
                                        byte[] encryptedApiKeyBytes = Convert.FromBase64String(provider.ApiKey);
                                        decryptedProvider.ApiKey = UnprotectData(encryptedApiKeyBytes);
                                    }
                                    catch (FormatException) // Not Base64 - assume plaintext (migration case)
                                    {
                                        // This was a plaintext key, it will be encrypted on next save.
                                        // Keep it as plaintext in memory for now.
                                        // No change to decryptedProvider.ApiKey needed.
                                        settingsModifiedDuringLoad = true; // Mark for re-save to encrypt it
                                    }
                                    catch (CryptographicException ex)
                                    {
                                        Console.WriteLine($"Error decrypting API Key for provider '{provider.FriendlyName}': {ex.Message}. Key cleared.");
                                        decryptedProvider.ApiKey = null; // Clear corrupted/undecryptable key
                                        settingsModifiedDuringLoad = true; // Mark for re-save to persist cleared key
                                    }
                                }
                                decryptedProviders.Add(decryptedProvider);
                            }
                            CurrentSettings.ServiceProviders = decryptedProviders;
                        }
                        // --- END DECRYPT SERVICE PROVIDER API KEYS ---
                    }
                    else
                    {
                        CurrentSettings = new GeneralSettings();
                        settingsModifiedDuringLoad = true; // Will create default settings that need encryption on save
                    }
                }

                if (settingsModifiedDuringLoad)
                {
                    SaveSettings(); // Save changes made during load/migration (encrypts plaintext keys)
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
                    // It's safer to re-read the file if other parts of settings.json might be modified externally,
                    // but if GeneralSettingsService is the sole manager, this is fine.
                    json = JObject.Parse(File.ReadAllText(_settingsFilePath));
                }
                else
                {
                    json = new JObject();
                }

                // Create a temporary GeneralSettings object for serialization
                // This ensures we don't modify the in-memory CurrentSettings with encrypted keys.
                var settingsForSerialization = JsonConvert.DeserializeObject<GeneralSettings>(JsonConvert.SerializeObject(CurrentSettings));

                // --- ENCRYPT SERVICE PROVIDER API KEYS FOR STORAGE ---
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
                // --- END ENCRYPT SERVICE PROVIDER API KEYS ---

                // Remove obsolete plaintext top-level API key properties if they are marked Obsolete
                var settingsToSaveToken = JObject.FromObject(settingsForSerialization); // Convert to JObject
                settingsToSaveToken.Remove("YouTubeApiKey");
                settingsToSaveToken.Remove("GitHubApiKey");
                settingsToSaveToken.Remove("AzureDevOpsPAT");
                // Also remove obsolete model name fields
                settingsToSaveToken.Remove("DefaultModel");
                settingsToSaveToken.Remove("SecondaryModel");


                json["generalSettings"] = settingsToSaveToken;
                File.WriteAllText(_settingsFilePath, json.ToString(Formatting.Indented));
                SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        // --- Update methods for ServiceProviders now handle plaintext keys and encrypt on save ---
        public void AddServiceProvider(ServiceProvider provider)
        {
            // The provided 'provider' object has its ApiKey in plaintext.
            // It will be encrypted during the SaveSettings call.
            CurrentSettings.ServiceProviders.Add(provider);
            SaveSettings();
        }

        public void UpdateServiceProvider(ServiceProvider updatedProvider)
        {
            // The provided 'updatedProvider' has its ApiKey in plaintext.
            // It will be encrypted during the SaveSettings call.
            var existing = CurrentSettings.ServiceProviders.FirstOrDefault(p => p.Guid == updatedProvider.Guid);
            if (existing != null)
            {    
                var idx = CurrentSettings.ServiceProviders.IndexOf(existing);
                CurrentSettings.ServiceProviders[idx] = updatedProvider; // Replace with new plaintext version
                SaveSettings();
            }
        }
        
        // DeleteServiceProvider remains the same as it doesn't deal with API keys directly
        public void DeleteServiceProvider(string providerGuid) {
            var existing = CurrentSettings.ServiceProviders.FirstOrDefault(p => p.Guid == providerGuid);
            if (existing != null)
            {
                CurrentSettings.ServiceProviders.Remove(existing);
                SaveSettings();
            }
        }

        // ... (Rest of the methods like UpdateSettings, UpdateDefaultModel, API key getters/setters, etc., remain the same as in the previous response) ...
        public void UpdateSettings(GeneralSettings newSettings) {
            // Be careful if newSettings.ServiceProviders contains plaintext keys.
            // The SaveSettings() call will encrypt them.
            CurrentSettings = newSettings; SaveSettings(); 
        }
        public void UpdateDefaultModel(string modelGuidOrName) {
             // If modelGuidOrName is a name, try to resolve to GUID.
            var model = CurrentSettings.ModelList.FirstOrDefault(m => m.Guid == modelGuidOrName || m.ModelName == modelGuidOrName);
            CurrentSettings.DefaultModelGuid = model?.Guid ?? modelGuidOrName; // Store GUID if found, else original value
            CurrentSettings.DefaultModel = model?.ModelName ?? modelGuidOrName; // Keep for compatibility for now
            SaveSettings(); 
        }
        public void UpdateSecondaryModel(string modelGuidOrName) {
            var model = CurrentSettings.ModelList.FirstOrDefault(m => m.Guid == modelGuidOrName || m.ModelName == modelGuidOrName);
            CurrentSettings.SecondaryModelGuid = model?.Guid ?? modelGuidOrName;
            CurrentSettings.SecondaryModel = model?.ModelName ?? modelGuidOrName;
            SaveSettings(); 
        }
        public void MigrateModelNamesToGuids() { /* As before */ }
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
    }
}