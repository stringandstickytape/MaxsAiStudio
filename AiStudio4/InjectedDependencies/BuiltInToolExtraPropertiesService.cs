// InjectedDependencies/BuiltInToolExtraPropertiesService.cs






namespace AiStudio4.InjectedDependencies
{
    /// <summary>
    /// Service for managing persistent extra properties of built-in tools.
    /// </summary>
    public class BuiltInToolExtraPropertiesService : IBuiltInToolExtraPropertiesService
    {
        private readonly string _filePath;
        private readonly object _lock = new();
        private Dictionary<string, Dictionary<string, string>> _cache;

        /// <summary>
        /// Initializes a new instance of the <see cref="BuiltInToolExtraPropertiesService"/> class.
        /// </summary>
        public BuiltInToolExtraPropertiesService()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var directory = Path.Combine(appData, "AiStudio4");
            _filePath = Path.Combine(directory, "builtinToolExtraProps.json");

            Directory.CreateDirectory(directory);
            LoadCache();
        }

        /// <summary>
        /// Loads the cache from the persistent JSON store.
        /// </summary>
        private void LoadCache()
        {
            lock (_lock)
            {
                if (File.Exists(_filePath))
                {
                    try
                    {
                        var json = File.ReadAllText(_filePath);
                        _cache = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(json)
                                 ?? new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
                    }
                    catch
                    {
                        // If deserialization fails, reset to empty cache
                        _cache = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
                    }
                }
                else
                {
                    _cache = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
                }
            }
        }

        /// <summary>
        /// Saves the cache to the persistent JSON store.
        /// </summary>
        private void SaveCache()
        {
            lock (_lock)
            {
                var json = JsonConvert.SerializeObject(_cache, Formatting.Indented);
                File.WriteAllText(_filePath, json);
            }
        }

        /// <inheritdoc/>
        public Dictionary<string, Dictionary<string, string>> LoadAll()
        {
            lock (_lock)
            {
                LoadCache();
                var result = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
                foreach (var kvp in _cache)
                {
                    result[kvp.Key] = new Dictionary<string, string>(kvp.Value, StringComparer.OrdinalIgnoreCase);
                }
                return result;
            }
        }

        /// <inheritdoc/>
        public Dictionary<string, string> GetExtraProperties(string toolName)
        {
            if (string.IsNullOrWhiteSpace(toolName))
                throw new ArgumentException("Tool name must be provided.", nameof(toolName));

            lock (_lock)
            {
                LoadCache();
                if (_cache.TryGetValue(toolName, out var props))
                {
                    return new Dictionary<string, string>(props, StringComparer.OrdinalIgnoreCase);
                }
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }
        }

        /// <inheritdoc/>
        public void SaveExtraProperties(string toolName, Dictionary<string, string> extraProperties)
        {
            if (string.IsNullOrWhiteSpace(toolName))
                throw new ArgumentException("Tool name must be provided.", nameof(toolName));
            if (extraProperties == null)
                throw new ArgumentNullException(nameof(extraProperties));

            lock (_lock)
            {
                LoadCache();
                _cache[toolName] = new Dictionary<string, string>(extraProperties, StringComparer.OrdinalIgnoreCase);
                SaveCache();
            }
        }

        /// <inheritdoc/>
        public void DeleteExtraProperties(string toolName)
        {
            if (string.IsNullOrWhiteSpace(toolName))
                throw new ArgumentException("Tool name must be provided.", nameof(toolName));

            lock (_lock)
            {
                LoadCache();
                if (_cache.Remove(toolName))
                {
                    SaveCache();
                }
            }
        }
    }
}
