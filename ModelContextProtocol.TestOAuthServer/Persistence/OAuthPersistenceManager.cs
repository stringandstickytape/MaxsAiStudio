using System.Text.Json;
using System.Collections.Concurrent;

namespace ModelContextProtocol.TestOAuthServer.Persistence;

/// <summary>
/// Manages persistence of OAuth server state including tokens, authorization codes, and dynamic clients.
/// </summary>
public sealed class OAuthPersistenceManager
{
    private readonly string _dataDirectory;
    private readonly string _tokensFilePath;
    private readonly string _authCodesFilePath;
    private readonly string _dynamicClientsFilePath;
    private readonly object _fileLock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="OAuthPersistenceManager"/> class.
    /// </summary>
    /// <param name="dataDirectory">Directory where persistence files will be stored.</param>
    public OAuthPersistenceManager(string dataDirectory)
    {
        _dataDirectory = dataDirectory;
        _tokensFilePath = Path.Combine(dataDirectory, "oauth_tokens.json");
        _authCodesFilePath = Path.Combine(dataDirectory, "oauth_auth_codes.json");
        _dynamicClientsFilePath = Path.Combine(dataDirectory, "oauth_dynamic_clients.json");
        
        // Ensure directory exists
        Directory.CreateDirectory(dataDirectory);
    }

    /// <summary>
    /// Loads persisted tokens from storage.
    /// </summary>
    /// <returns>Dictionary of tokens keyed by token string.</returns>
    public ConcurrentDictionary<string, TokenInfo> LoadTokens()
    {
        lock (_fileLock)
        {
            try
            {
                if (!File.Exists(_tokensFilePath))
                {
                    return new ConcurrentDictionary<string, TokenInfo>();
                }

                var json = File.ReadAllText(_tokensFilePath);
                var persistedTokens = JsonSerializer.Deserialize<Dictionary<string, PersistentTokenInfo>>(json, OAuthJsonContext.Default.DictionaryStringPersistentTokenInfo);
                
                if (persistedTokens == null)
                {
                    return new ConcurrentDictionary<string, TokenInfo>();
                }

                var tokens = new ConcurrentDictionary<string, TokenInfo>();
                var now = DateTimeOffset.UtcNow;

                foreach (var kvp in persistedTokens)
                {
                    // Only load tokens that haven't expired
                    if (kvp.Value.ExpiresAt > now)
                    {
                        tokens[kvp.Key] = kvp.Value.ToTokenInfo();
                    }
                }

                return tokens;
            }
            catch (Exception)
            {
                // If there's any error loading, start fresh
                return new ConcurrentDictionary<string, TokenInfo>();
            }
        }
    }

    /// <summary>
    /// Saves tokens to persistent storage.
    /// </summary>
    /// <param name="tokens">Dictionary of tokens to save.</param>
    public void SaveTokens(ConcurrentDictionary<string, TokenInfo> tokens)
    {
        lock (_fileLock)
        {
            try
            {
                var persistentTokens = new Dictionary<string, PersistentTokenInfo>();
                var now = DateTimeOffset.UtcNow;

                foreach (var kvp in tokens)
                {
                    // Only save tokens that haven't expired
                    if (kvp.Value.ExpiresAt > now)
                    {
                        persistentTokens[kvp.Key] = PersistentTokenInfo.FromTokenInfo(kvp.Value);
                    }
                }

                var json = JsonSerializer.Serialize(persistentTokens, OAuthJsonContext.Default.DictionaryStringPersistentTokenInfo);
                File.WriteAllText(_tokensFilePath, json);
            }
            catch (Exception)
            {
                // Silently fail - persistence is not critical
            }
        }
    }

    /// <summary>
    /// Loads persisted authorization codes from storage.
    /// </summary>
    /// <returns>Dictionary of authorization codes keyed by code string.</returns>
    public ConcurrentDictionary<string, AuthorizationCodeInfo> LoadAuthorizationCodes()
    {
        lock (_fileLock)
        {
            try
            {
                if (!File.Exists(_authCodesFilePath))
                {
                    return new ConcurrentDictionary<string, AuthorizationCodeInfo>();
                }

                var json = File.ReadAllText(_authCodesFilePath);
                var persistedCodes = JsonSerializer.Deserialize<Dictionary<string, PersistentAuthorizationCodeInfo>>(json, OAuthJsonContext.Default.DictionaryStringPersistentAuthorizationCodeInfo);
                
                if (persistedCodes == null)
                {
                    return new ConcurrentDictionary<string, AuthorizationCodeInfo>();
                }

                var codes = new ConcurrentDictionary<string, AuthorizationCodeInfo>();
                var cutoff = DateTimeOffset.UtcNow.AddMinutes(-10); // Auth codes are valid for 10 minutes

                foreach (var kvp in persistedCodes)
                {
                    // Only load codes that are still valid (issued within last 10 minutes)
                    if (kvp.Value.IssuedAt > cutoff)
                    {
                        codes[kvp.Key] = kvp.Value.ToAuthorizationCodeInfo();
                    }
                }

                return codes;
            }
            catch (Exception)
            {
                // If there's any error loading, start fresh
                return new ConcurrentDictionary<string, AuthorizationCodeInfo>();
            }
        }
    }

    /// <summary>
    /// Saves authorization codes to persistent storage.
    /// </summary>
    /// <param name="authCodes">Dictionary of authorization codes to save.</param>
    public void SaveAuthorizationCodes(ConcurrentDictionary<string, AuthorizationCodeInfo> authCodes)
    {
        lock (_fileLock)
        {
            try
            {
                var persistentCodes = new Dictionary<string, PersistentAuthorizationCodeInfo>();
                var cutoff = DateTimeOffset.UtcNow.AddMinutes(-10); // Auth codes are valid for 10 minutes

                foreach (var kvp in authCodes)
                {
                    // Only save codes that are still valid
                    if (kvp.Value.IssuedAt > cutoff)
                    {
                        persistentCodes[kvp.Key] = PersistentAuthorizationCodeInfo.FromAuthorizationCodeInfo(kvp.Value);
                    }
                }

                var json = JsonSerializer.Serialize(persistentCodes, OAuthJsonContext.Default.DictionaryStringPersistentAuthorizationCodeInfo);
                File.WriteAllText(_authCodesFilePath, json);
            }
            catch (Exception)
            {
                // Silently fail - persistence is not critical
            }
        }
    }

    /// <summary>
    /// Loads persisted dynamic clients from storage.
    /// </summary>
    /// <returns>Dictionary of dynamic clients keyed by client ID.</returns>
    public Dictionary<string, ClientInfo> LoadDynamicClients()
    {
        lock (_fileLock)
        {
            try
            {
                if (!File.Exists(_dynamicClientsFilePath))
                {
                    return new Dictionary<string, ClientInfo>();
                }

                var json = File.ReadAllText(_dynamicClientsFilePath);
                var persistedClients = JsonSerializer.Deserialize<Dictionary<string, PersistentClientInfo>>(json, OAuthJsonContext.Default.DictionaryStringPersistentClientInfo);
                
                if (persistedClients == null)
                {
                    return new Dictionary<string, ClientInfo>();
                }

                var clients = new Dictionary<string, ClientInfo>();
                foreach (var kvp in persistedClients)
                {
                    clients[kvp.Key] = kvp.Value.ToClientInfo();
                }

                return clients;
            }
            catch (Exception)
            {
                // If there's any error loading, start fresh
                return new Dictionary<string, ClientInfo>();
            }
        }
    }

    /// <summary>
    /// Saves dynamic clients to persistent storage.
    /// </summary>
    /// <param name="dynamicClients">Dictionary of dynamic clients to save (excludes built-in demo clients).</param>
    public void SaveDynamicClients(Dictionary<string, ClientInfo> dynamicClients)
    {
        lock (_fileLock)
        {
            try
            {
                var persistentClients = new Dictionary<string, PersistentClientInfo>();
                
                foreach (var kvp in dynamicClients)
                {
                    // Only save dynamically registered clients (those starting with "dyn-")
                    if (kvp.Key.StartsWith("dyn-"))
                    {
                        persistentClients[kvp.Key] = PersistentClientInfo.FromClientInfo(kvp.Value);
                    }
                }

                var json = JsonSerializer.Serialize(persistentClients, OAuthJsonContext.Default.DictionaryStringPersistentClientInfo);
                File.WriteAllText(_dynamicClientsFilePath, json);
            }
            catch (Exception)
            {
                // Silently fail - persistence is not critical
            }
        }
    }

    /// <summary>
    /// Clears all persisted data by deleting the storage files.
    /// </summary>
    public void ClearPersistedData()
    {
        lock (_fileLock)
        {
            try
            {
                if (File.Exists(_tokensFilePath))
                    File.Delete(_tokensFilePath);
                
                if (File.Exists(_authCodesFilePath))
                    File.Delete(_authCodesFilePath);
                
                if (File.Exists(_dynamicClientsFilePath))
                    File.Delete(_dynamicClientsFilePath);
            }
            catch (Exception)
            {
                // Silently fail - if we can't delete, it's not critical
            }
        }
    }

    /// <summary>
    /// Gets information about the persistent storage.
    /// </summary>
    /// <returns>Dictionary containing storage information.</returns>
    public Dictionary<string, object> GetStorageInfo()
    {
        lock (_fileLock)
        {
            var info = new Dictionary<string, object>
            {
                ["DataDirectory"] = _dataDirectory,
                ["TokensFileExists"] = File.Exists(_tokensFilePath),
                ["AuthCodesFileExists"] = File.Exists(_authCodesFilePath),
                ["DynamicClientsFileExists"] = File.Exists(_dynamicClientsFilePath)
            };

            try
            {
                if (File.Exists(_tokensFilePath))
                {
                    var tokensJson = File.ReadAllText(_tokensFilePath);
                    var tokens = JsonSerializer.Deserialize<Dictionary<string, PersistentTokenInfo>>(tokensJson, OAuthJsonContext.Default.DictionaryStringPersistentTokenInfo);
                    info["TokenCount"] = tokens?.Count ?? 0;
                }
                else
                {
                    info["TokenCount"] = 0;
                }

                if (File.Exists(_dynamicClientsFilePath))
                {
                    var clientsJson = File.ReadAllText(_dynamicClientsFilePath);
                    var clients = JsonSerializer.Deserialize<Dictionary<string, PersistentClientInfo>>(clientsJson, OAuthJsonContext.Default.DictionaryStringPersistentClientInfo);
                    info["DynamicClientCount"] = clients?.Count ?? 0;
                }
                else
                {
                    info["DynamicClientCount"] = 0;
                }
            }
            catch (Exception)
            {
                info["TokenCount"] = "Error";
                info["DynamicClientCount"] = "Error";
            }

            return info;
        }
    }
}