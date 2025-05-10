/*
C:/Users/maxhe/source/repos/MaxsAiStudio/AiStudio4/InjectedDependencies/ProjectHistoryService.cs
*/
using AiStudio4.Core.Models;
using Microsoft.Extensions.Configuration; // Keep if needed for other config, or remove if only for settings path
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AiStudio4.InjectedDependencies
{
    public class ProjectHistoryService : IProjectHistoryService
    {
        private readonly string _settingsFilePath;
        private readonly object _lock = new();
        private List<ProjectFolderEntry> _knownProjectFolders = new();
        private const int MaxKnownFolders = 20;

        public ProjectHistoryService(IConfiguration configuration)
        {
            // Consider if configuration is still needed or if settings path can be determined differently.
            // For now, assuming it might be used for other settings or future expansion.
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
                    _knownProjectFolders = new List<ProjectFolderEntry>();
                    SaveSettings(); // Save an empty list to create the file with the new structure
                    return;
                }
                try
                {
                    var jsonText = File.ReadAllText(_settingsFilePath);
                    var json = JObject.Parse(jsonText);

                    var knownFoldersToken = json["knownProjectFolders"];
                    if (knownFoldersToken != null)
                    {
                        _knownProjectFolders = knownFoldersToken.ToObject<List<ProjectFolderEntry>>() ?? new List<ProjectFolderEntry>();
                    }
                    else
                    {
                        // Attempt to migrate from old format if new format doesn't exist
                        var oldHistoryToken = json["projectHistory"];
                        if (oldHistoryToken != null)
                        {
                            var oldPaths = oldHistoryToken.ToObject<List<string>>() ?? new List<string>();
                            _knownProjectFolders = new List<ProjectFolderEntry>();
                            foreach (var path in oldPaths.Where(p => !string.IsNullOrWhiteSpace(p)))
                            {
                                if (Directory.Exists(path)) // Only migrate existing directories
                                {
                                    _knownProjectFolders.Add(new ProjectFolderEntry
                                    {
                                        Path = Path.GetFullPath(path), // Normalize
                                        Name = System.IO.Path.GetFileName(Path.GetFullPath(path)),
                                        LastAccessedDate = DateTime.UtcNow // Set a default access date
                                    });
                                }
                            }
                            // Sort by name after migration for some initial order
                            _knownProjectFolders = _knownProjectFolders.OrderBy(f => f.Name).ToList(); 
                            // Remove the old key after migration by saving immediately
                            SaveSettings(); 
                        }
                        else
                        {
                            _knownProjectFolders = new List<ProjectFolderEntry>();
                            SaveSettings(); // If neither key exists, save default empty structure
                        }
                    }
                }
                catch (JsonReaderException ex)
                {
                    Console.WriteLine($"Settings file '{_settingsFilePath}' is corrupt. Resetting known project folders. Error: {ex.Message}");
                    _knownProjectFolders = new List<ProjectFolderEntry>();
                    SaveSettings();
                }
                catch (FileNotFoundException)
                {
                    Console.WriteLine($"Settings file '{_settingsFilePath}' not found. Creating a new one.");
                    _knownProjectFolders = new List<ProjectFolderEntry>();
                    SaveSettings();
                }
                catch (DirectoryNotFoundException)
                {
                    Console.WriteLine($"Settings directory for '{_settingsFilePath}' not found. Creating it.");
                    Directory.CreateDirectory(Path.GetDirectoryName(_settingsFilePath));
                    _knownProjectFolders = new List<ProjectFolderEntry>();
                    SaveSettings();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error occurred while loading settings: {ex.Message}");
                    _knownProjectFolders = new List<ProjectFolderEntry>();
                    SaveSettings();
                }
            }
        }

        public void SaveSettings()
        {
            lock (_lock)
            {
                try
                {
                    JObject json;
                    if (File.Exists(_settingsFilePath))
                    {
                        try
                        {
                            json = JObject.Parse(File.ReadAllText(_settingsFilePath));
                        }
                        catch (JsonReaderException ex) 
                        {
                            // If file is corrupt, start fresh to avoid losing data by overwriting with partial data
                            Console.WriteLine($"Error reading settings file for save (it might be corrupt): {ex.Message}. Creating new settings object.");
                            json = new JObject(); 
                        }
                    }
                    else
                    {
                        json = new JObject();
                    }
                    
                    json["knownProjectFolders"] = JToken.FromObject(_knownProjectFolders.OrderByDescending(f => f.LastAccessedDate).ToList());
                    // Explicitly remove the old projectHistory key if it exists to complete migration
                    json.Remove("projectHistory"); 

                    File.WriteAllText(_settingsFilePath, json.ToString(Formatting.Indented));
                }
                catch (DirectoryNotFoundException)
                {
                    Console.WriteLine($"Settings directory for '{_settingsFilePath}' not found. Creating it.");
                    Directory.CreateDirectory(Path.GetDirectoryName(_settingsFilePath));
                    // Attempt to save settings again
                    JObject json = new JObject();
                    json["knownProjectFolders"] = JToken.FromObject(_knownProjectFolders.OrderByDescending(f => f.LastAccessedDate).ToList());
                    File.WriteAllText(_settingsFilePath, json.ToString(Formatting.Indented));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error occurred while saving settings: {ex.Message}");
                }
            }
        }

        public Task<List<ProjectFolderEntry>> GetKnownProjectFoldersAsync()
        {
            lock (_lock)
            {
                // Return a copy, ordered by LastAccessedDate descending
                var sortedList = _knownProjectFolders.OrderByDescending(f => f.LastAccessedDate).ToList();
                return Task.FromResult(new List<ProjectFolderEntry>(sortedList));
            }
        }

        public Task<string> GetProjectPathByIdAsync(string id)
        {
            lock (_lock)
            {
                var folder = _knownProjectFolders.FirstOrDefault(f => f.Id == id);
                return Task.FromResult(folder?.Path);
            }
        }

        public Task AddOrUpdateProjectFolderAsync(string path, string name = null)
        {
            if (string.IsNullOrWhiteSpace(path))
                return Task.CompletedTask;

            var normalizedPath = Path.GetFullPath(path); // Normalize the path

            if (!Directory.Exists(normalizedPath)) // Do not add if directory doesn't exist
            {
                 Console.WriteLine($"Attempted to add non-existent directory to project history: {normalizedPath}");
                 return Task.CompletedTask;
            }

            lock (_lock)
            {
                var existingEntry = _knownProjectFolders.FirstOrDefault(f => Path.GetFullPath(f.Path).Equals(normalizedPath, StringComparison.OrdinalIgnoreCase));

                if (existingEntry != null)
                {
                    existingEntry.LastAccessedDate = DateTime.UtcNow;
                    if (!string.IsNullOrEmpty(name) && existingEntry.Name != name)
                    {
                        existingEntry.Name = name;
                    }
                }
                else
                {
                    var newEntry = new ProjectFolderEntry
                    {
                        Path = normalizedPath,
                        Name = string.IsNullOrEmpty(name) ? System.IO.Path.GetFileName(normalizedPath) : name,
                        LastAccessedDate = DateTime.UtcNow
                        // ID is auto-generated by the model
                    };
                    _knownProjectFolders.Add(newEntry);
                }

                // Sort by LastAccessedDate descending to easily find the oldest if we exceed max count
                _knownProjectFolders = _knownProjectFolders.OrderByDescending(f => f.LastAccessedDate).ToList();

                if (_knownProjectFolders.Count > MaxKnownFolders)
                {
                    _knownProjectFolders = _knownProjectFolders.Take(MaxKnownFolders).ToList();
                }

                SaveSettings();
            }
            return Task.CompletedTask;
        }
    }
}