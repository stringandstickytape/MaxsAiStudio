using System.Text.Json.Serialization;

namespace ModelContextProtocol.TestOAuthServer.Persistence;

/// <summary>
/// Persistent representation of TokenInfo for JSON serialization.
/// </summary>
public sealed class PersistentTokenInfo
{
    [JsonPropertyName("clientId")]
    public required string ClientId { get; init; }

    [JsonPropertyName("scopes")]
    public List<string> Scopes { get; init; } = [];

    [JsonPropertyName("issuedAt")]
    public required DateTimeOffset IssuedAt { get; init; }

    [JsonPropertyName("expiresAt")]
    public required DateTimeOffset ExpiresAt { get; init; }

    [JsonPropertyName("resource")]
    public string? Resource { get; init; }

    [JsonPropertyName("jwtId")]
    public string? JwtId { get; init; }

    /// <summary>
    /// Converts a TokenInfo to a PersistentTokenInfo.
    /// </summary>
    public static PersistentTokenInfo FromTokenInfo(TokenInfo tokenInfo)
    {
        return new PersistentTokenInfo
        {
            ClientId = tokenInfo.ClientId,
            Scopes = tokenInfo.Scopes,
            IssuedAt = tokenInfo.IssuedAt,
            ExpiresAt = tokenInfo.ExpiresAt,
            Resource = tokenInfo.Resource?.ToString(),
            JwtId = tokenInfo.JwtId
        };
    }

    /// <summary>
    /// Converts this PersistentTokenInfo to a TokenInfo.
    /// </summary>
    public TokenInfo ToTokenInfo()
    {
        return new TokenInfo
        {
            ClientId = ClientId,
            Scopes = Scopes,
            IssuedAt = IssuedAt,
            ExpiresAt = ExpiresAt,
            Resource = !string.IsNullOrEmpty(Resource) ? new Uri(Resource) : null,
            JwtId = JwtId
        };
    }
}

/// <summary>
/// Persistent representation of AuthorizationCodeInfo for JSON serialization.
/// </summary>
public sealed class PersistentAuthorizationCodeInfo
{
    [JsonPropertyName("clientId")]
    public required string ClientId { get; init; }

    [JsonPropertyName("redirectUri")]
    public required string RedirectUri { get; init; }

    [JsonPropertyName("codeChallenge")]
    public required string CodeChallenge { get; init; }

    [JsonPropertyName("scope")]
    public List<string> Scope { get; init; } = [];

    [JsonPropertyName("resource")]
    public string? Resource { get; init; }

    [JsonPropertyName("issuedAt")]
    public required DateTimeOffset IssuedAt { get; init; }

    /// <summary>
    /// Converts an AuthorizationCodeInfo to a PersistentAuthorizationCodeInfo.
    /// </summary>
    public static PersistentAuthorizationCodeInfo FromAuthorizationCodeInfo(AuthorizationCodeInfo codeInfo)
    {
        return new PersistentAuthorizationCodeInfo
        {
            ClientId = codeInfo.ClientId,
            RedirectUri = codeInfo.RedirectUri,
            CodeChallenge = codeInfo.CodeChallenge,
            Scope = codeInfo.Scope,
            Resource = codeInfo.Resource?.ToString(),
            IssuedAt = codeInfo.IssuedAt
        };
    }

    /// <summary>
    /// Converts this PersistentAuthorizationCodeInfo to an AuthorizationCodeInfo.
    /// </summary>
    public AuthorizationCodeInfo ToAuthorizationCodeInfo()
    {
        return new AuthorizationCodeInfo
        {
            ClientId = ClientId,
            RedirectUri = RedirectUri,
            CodeChallenge = CodeChallenge,
            Scope = Scope,
            Resource = !string.IsNullOrEmpty(Resource) ? new Uri(Resource) : null,
            IssuedAt = IssuedAt
        };
    }
}

/// <summary>
/// Persistent representation of ClientInfo for JSON serialization.
/// </summary>
public sealed class PersistentClientInfo
{
    [JsonPropertyName("clientId")]
    public required string ClientId { get; init; }

    [JsonPropertyName("clientSecret")]
    public required string ClientSecret { get; init; }

    [JsonPropertyName("redirectUris")]
    public List<string> RedirectUris { get; init; } = [];

    [JsonPropertyName("registeredAt")]
    public required DateTimeOffset RegisteredAt { get; init; }

    /// <summary>
    /// Converts a ClientInfo to a PersistentClientInfo.
    /// </summary>
    public static PersistentClientInfo FromClientInfo(ClientInfo clientInfo)
    {
        return new PersistentClientInfo
        {
            ClientId = clientInfo.ClientId,
            ClientSecret = clientInfo.ClientSecret,
            RedirectUris = clientInfo.RedirectUris,
            RegisteredAt = DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    /// Converts this PersistentClientInfo to a ClientInfo.
    /// </summary>
    public ClientInfo ToClientInfo()
    {
        return new ClientInfo
        {
            ClientId = ClientId,
            ClientSecret = ClientSecret,
            RedirectUris = RedirectUris
        };
    }
}