using System.Text.Json.Serialization;
using ModelContextProtocol.TestOAuthServer.Persistence;

namespace ModelContextProtocol.TestOAuthServer;

[JsonSerializable(typeof(OAuthServerMetadata))]
[JsonSerializable(typeof(AuthorizationServerMetadata))]
[JsonSerializable(typeof(TokenResponse))]
[JsonSerializable(typeof(JsonWebKeySet))]
[JsonSerializable(typeof(JsonWebKey))]
[JsonSerializable(typeof(TokenIntrospectionResponse))]
[JsonSerializable(typeof(OAuthErrorResponse))]
[JsonSerializable(typeof(ClientRegistrationRequest))]
[JsonSerializable(typeof(ClientRegistrationResponse))]
[JsonSerializable(typeof(Dictionary<string, string>))]
[JsonSerializable(typeof(Dictionary<string, PersistentTokenInfo>))]
[JsonSerializable(typeof(Dictionary<string, PersistentAuthorizationCodeInfo>))]
[JsonSerializable(typeof(Dictionary<string, PersistentClientInfo>))]
[JsonSerializable(typeof(PersistentTokenInfo))]
[JsonSerializable(typeof(PersistentAuthorizationCodeInfo))]
[JsonSerializable(typeof(PersistentClientInfo))]
internal sealed partial class OAuthJsonContext : JsonSerializerContext;
