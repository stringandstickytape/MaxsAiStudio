// InjectedDependencies/ProjectHistoryService.cs
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AiStudio4.InjectedDependencies
{
    public class ProjectHistoryService : IProjectHistoryService
    {
        private readonly string _settingsFilePath;
        private readonly object _lock = new();
        private List<string> _projectPathHistory = new();

        public ProjectHistoryService(IConfiguration configuration)
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
                    _projectPathHistory = new List<string>();
                    SaveSettings();
                    return;
                }
                try
                {
                    var json = JObject.Parse(File.ReadAllText(_settingsFilePath));
                    var section = json["projectHistory"];
                    if (section != null)
                    {
                        _projectPathHistory = section.ToObject<List<string>>() ?? new List<string>();
                    }
                    else
                    {
                        _projectPathHistory = new List<string>();
                        SaveSettings();
                    }
                }
                catch (JsonReaderException)
                {
                    // Handle the case where the settings file is corrupt.
                    // You might want to log this error and/or reset the settings.
                    Console.WriteLine("Settings file is corrupt. Resetting project history.");
                    _projectPathHistory = new List<string>();
                    SaveSettings();
                }
                catch (FileNotFoundException)
                {
                    // Handle the case where the settings file is not found.
                    Console.WriteLine("Settings file not found. Creating a new one.");
                    _projectPathHistory = new List<string>();
                    SaveSettings();
                }
                catch (DirectoryNotFoundException)
                {
                    // Handle the case where the settings directory is not found.
                    Console.WriteLine("Settings directory not found. Creating a new one.");
                    Directory.CreateDirectory(Path.GetDirectoryName(_settingsFilePath));
                    _projectPathHistory = new List<string>();
                    SaveSettings();
                }
                catch (Exception ex)
                {
                    // Handle any other exceptions that might occur.
                    Console.WriteLine($"An error occurred while loading settings: {ex.Message}");
                    _projectPathHistory = new List<string>();
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
                        json = JObject.Parse(File.ReadAllText(_settingsFilePath));
                    }
                    else
                    {
                        json = new JObject();
                    }
                    json["projectHistory"] = JToken.FromObject(_projectPathHistory);
                    File.WriteAllText(_settingsFilePath, json.ToString(Formatting.Indented));
                }
                catch (DirectoryNotFoundException)
                {
                    // Handle the case where the settings directory is not found.
                    Console.WriteLine("Settings directory not found. Creating a new one.");
                    Directory.CreateDirectory(Path.GetDirectoryName(_settingsFilePath));
                    // Attempt to save settings again after creating the directory.
                    JObject json = new JObject();
                    json["projectHistory"] = JToken.FromObject(_projectPathHistory);
                    File.WriteAllText(_settingsFilePath, json.ToString(Formatting.Indented));
                }
                catch (Exception ex)
                {
                    // Handle any other exceptions that might occur.
                    Console.WriteLine($"An error occurred while saving settings: {ex.Message}");
                }
            }
        }

        public List<string> GetProjectPathHistory()
        {
            lock (_lock)
            {
                return new List<string>(_projectPathHistory);
            }
        }

        public void AddProjectPathToHistory(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return;
            lock (_lock)
            {
                _projectPathHistory ??= new List<string>();
                _projectPathHistory.Remove(path);
                _projectPathHistory.Insert(0, path);
                const int maxHistoryItems = 10;
                if (_projectPathHistory.Count > maxHistoryItems)
                {
                    _projectPathHistory = _projectPathHistory.Take(maxHistoryItems).ToList();
                }
                SaveSettings();
            }
        }
    }
}