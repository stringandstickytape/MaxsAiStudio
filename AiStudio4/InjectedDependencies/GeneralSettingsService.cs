// AiStudio4/InjectedDependencies/GeneralSettingsService.cs
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;
using AiStudio4.Core.Models; // If your Model class is here
using SharedClasses.Providers; // If your Model/ServiceProvider classes are here
using System.Security.Cryptography; // For ProtectedData
using System.Text; // For Encoding
using System.Drawing; // For Color
using System.Collections.Generic; // Required for List<ServiceProvider> and List<Model>

namespace AiStudio4.InjectedDependencies
{
    public class GeneralSettingsService : IGeneralSettingsService
    {
        private readonly string _settingsFilePath;
        public GeneralSettings CurrentSettings { get; private set; } = new();
        private readonly object _lock = new();
        public event EventHandler SettingsChanged;

        // --- ADD ENTROPY (OPTIONAL BUT RECOMMENDED) ---
        // This should be a unique, static byte array for your application.
        // KEEP THIS SECRET if you distribute your application. For open source,
        // it provides a small hurdle but isn't foolproof if the source is public.
        // For true security, you'd need to not hardcode it.
        // For this example, we'll use a simple one. Replace with your own.
        private static readonly byte[] s_entropy = Encoding.UTF8.GetBytes("A!S@t#u$d%i^o&4*S(e)c-r_e+t");
        // --- END ENTROPY ---

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
                bool migrated = false;
                if (!File.Exists(_settingsFilePath))
                {
                    CurrentSettings = new GeneralSettings();
                    // Initialize default providers and models as before...
                    // (omitted for brevity, but ensure your default setup logic is here)
                     CurrentSettings.ServiceProviders = new List<ServiceProvider> { /* ... your defaults ... */ };
                     CurrentSettings.ModelList = new List<Model> { /* ... your defaults ... */ };
                    SaveSettings(); // This will save the initial empty encrypted fields
                    return;
                }

                var text = File.ReadAllText(_settingsFilePath);
                var json = JObject.Parse(text);
                var section = json["generalSettings"];

                if (section != null)
                {
                    // Try to deserialize directly into the new structure
                    CurrentSettings = section.ToObject<GeneralSettings>() ?? new GeneralSettings();

                    // --- MIGRATION LOGIC ---
                    // Check if old plaintext keys exist and new encrypted ones don't (or are empty)
                    if (!string.IsNullOrEmpty(CurrentSettings.YouTubeApiKey) && string.IsNullOrEmpty(CurrentSettings.EncryptedYouTubeApiKey))
                    {
                        CurrentSettings.EncryptedYouTubeApiKey = CurrentSettings.YouTubeApiKey != null ? Convert.ToBase64String(ProtectData(CurrentSettings.YouTubeApiKey)) : null;
                        CurrentSettings.YouTubeApiKey = null; // Clear plaintext
                        migrated = true;
                    }
                    if (!string.IsNullOrEmpty(CurrentSettings.GitHubApiKey) && string.IsNullOrEmpty(CurrentSettings.EncryptedGitHubApiKey))
                    {
                        CurrentSettings.EncryptedGitHubApiKey = CurrentSettings.GitHubApiKey != null ? Convert.ToBase64String(ProtectData(CurrentSettings.GitHubApiKey)) : null;
                        CurrentSettings.GitHubApiKey = null;
                        migrated = true;
                    }
                    if (!string.IsNullOrEmpty(CurrentSettings.AzureDevOpsPAT) && string.IsNullOrEmpty(CurrentSettings.EncryptedAzureDevOpsPAT))
                    {
                        CurrentSettings.EncryptedAzureDevOpsPAT = CurrentSettings.AzureDevOpsPAT != null ? Convert.ToBase64String(ProtectData(CurrentSettings.AzureDevOpsPAT)) : null;
                        CurrentSettings.AzureDevOpsPAT = null;
                        migrated = true;
                    }
                    // --- END MIGRATION LOGIC ---
                }
                else
                {
                    CurrentSettings = new GeneralSettings();
                }

                if (migrated)
                {
                    SaveSettings(); // Save immediately after migration
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
                // Remove obsolete plaintext properties before saving if they are marked Obsolete
                var settingsToSave = JsonConvert.DeserializeObject<JObject>(JsonConvert.SerializeObject(CurrentSettings));
                settingsToSave.Remove("YouTubeApiKey");
                settingsToSave.Remove("GitHubApiKey");
                settingsToSave.Remove("AzureDevOpsPAT");

                json["generalSettings"] = settingsToSave;
                File.WriteAllText(_settingsFilePath, json.ToString(Formatting.Indented));
                SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        // ... (UpdateSettings, UpdateDefaultModel, UpdateSecondaryModel, MigrateModelNamesToGuids, AddModel, etc. remain largely the same) ...
        public void UpdateSettings(GeneralSettings newSettings) { /* ... */ CurrentSettings = newSettings; SaveSettings(); }
        public void UpdateDefaultModel(string modelGuid) { CurrentSettings.DefaultModelGuid = modelGuid; SaveSettings(); }
        public void UpdateSecondaryModel(string modelGuid) { CurrentSettings.SecondaryModelGuid = modelGuid; SaveSettings(); }
        public void MigrateModelNamesToGuids() { /* ... as before ... */ }
        public void AddModel(Model model) { CurrentSettings.ModelList.Add(model); SaveSettings(); }
        public void UpdateModel(Model updatedModel) { /* ... */ SaveSettings(); }
        public void DeleteModel(string modelGuid) { /* ... */ SaveSettings(); }
        public void AddServiceProvider(ServiceProvider provider) { CurrentSettings.ServiceProviders.Add(provider); SaveSettings(); }
        public void UpdateServiceProvider(ServiceProvider updatedProvider) { /* ... */ SaveSettings(); }
        public void DeleteServiceProvider(string providerGuid) { /* ... */ SaveSettings(); }


        // --- IMPLEMENT UPDATED/NEW API KEY METHODS ---
        public void UpdateYouTubeApiKey(string plaintextApiKey)
        {
            CurrentSettings.EncryptedYouTubeApiKey = !string.IsNullOrEmpty(plaintextApiKey) ? Convert.ToBase64String(ProtectData(plaintextApiKey)) : null;
            SaveSettings();
        }

        public string GetDecryptedYouTubeApiKey()
        {
            try
            {
                return !string.IsNullOrEmpty(CurrentSettings.EncryptedYouTubeApiKey) ? UnprotectData(Convert.FromBase64String(CurrentSettings.EncryptedYouTubeApiKey)) : null;
            }
            catch (CryptographicException ex)
            {
                // Handle decryption error, e.g., if entropy changed or data corrupted
                // Or if settings file moved to another user/machine without proper DPAPI handling
                Console.WriteLine($"Error decrypting YouTube API Key: {ex.Message}. Key might be corrupted or from another context.");
                CurrentSettings.EncryptedYouTubeApiKey = null; // Clear corrupted key
                SaveSettings();
                return null;
            }
        }

        public void UpdateGitHubApiKey(string plaintextApiKey)
        {
            CurrentSettings.EncryptedGitHubApiKey = !string.IsNullOrEmpty(plaintextApiKey) ? Convert.ToBase64String(ProtectData(plaintextApiKey)) : null;
            SaveSettings();
        }

        public string GetDecryptedGitHubApiKey()
        {
            try
            {
                return !string.IsNullOrEmpty(CurrentSettings.EncryptedGitHubApiKey) ? UnprotectData(Convert.FromBase64String(CurrentSettings.EncryptedGitHubApiKey)) : null;
            }
            catch (CryptographicException ex)
            {
                Console.WriteLine($"Error decrypting GitHub API Key: {ex.Message}.");
                CurrentSettings.EncryptedGitHubApiKey = null;
                SaveSettings();
                return null;
            }
        }

        public void UpdateAzureDevOpsPAT(string plaintextPat)
        {
            CurrentSettings.EncryptedAzureDevOpsPAT = !string.IsNullOrEmpty(plaintextPat) ? Convert.ToBase64String(ProtectData(plaintextPat)) : null;
            SaveSettings();
        }

        public string GetDecryptedAzureDevOpsPAT()
        {
            try
            {
                return !string.IsNullOrEmpty(CurrentSettings.EncryptedAzureDevOpsPAT) ? UnprotectData(Convert.FromBase64String(CurrentSettings.EncryptedAzureDevOpsPAT)) : null;
            }
            catch (CryptographicException ex)
            {
                Console.WriteLine($"Error decrypting Azure DevOps PAT: {ex.Message}.");
                CurrentSettings.EncryptedAzureDevOpsPAT = null;
                SaveSettings();
                return null;
            }
        }
        // --- END IMPLEMENT UPDATED/NEW API KEY METHODS ---

        public void UpdateCondaPath(string path) { CurrentSettings.CondaPath = path; SaveSettings(); }
        public void UpdateUseExperimentalCostTracking(bool value) { CurrentSettings.UseExperimentalCostTracking = value; SaveSettings(); }
        public void UpdateConversationZipRetentionDays(int days) { CurrentSettings.ConversationZipRetentionDays = days; SaveSettings(); }
        public void UpdateConversationDeleteZippedRetentionDays(int days) { CurrentSettings.ConversationDeleteZippedRetentionDays = days; SaveSettings(); }
    }
}