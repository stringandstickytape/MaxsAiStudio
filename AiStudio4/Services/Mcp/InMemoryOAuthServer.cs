using System;
using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace AiStudio4.Services.Mcp
{
    public class InMemoryOAuthServer : IDisposable
    {
        private readonly int _port;
        private readonly ILogger<OAuthSseServerTransport>? _logger;
        private HttpListener? _listener;
        private CancellationTokenSource? _cancellationTokenSource;
        private readonly RsaSecurityKey _signingKey;
        private readonly SigningCredentials _signingCredentials;
        private readonly string _issuer;
        private readonly string _audience;
        
        private readonly ConcurrentDictionary<string, OAuthClient> _clients = new();
        private readonly ConcurrentDictionary<string, AuthorizationData> _authorizationCodes = new();
        
        public InMemoryOAuthServer(int port, string issuer, string audience, ILogger<OAuthSseServerTransport>? logger = null)
        {
            _port = port;
            _issuer = issuer;
            _audience = audience;
            _logger = logger;
            
            var rsa = RSA.Create();
            _signingKey = new RsaSecurityKey(rsa);
            _signingCredentials = new SigningCredentials(_signingKey, SecurityAlgorithms.RsaSha256);
            
            RegisterDefaultClient();
        }
        
        public RsaSecurityKey GetSigningKey()
        {
            return _signingKey;
        }
        
        private void RegisterDefaultClient()
        {
            _clients.TryAdd("mcp-client", new OAuthClient
            {
                ClientId = "mcp-client",
                ClientSecret = "mcp-secret",
                Name = "MCP Client",
                Scopes = new[] { "mcp:tools" }
            });
        }
        
        public async Task RunAsync(CancellationToken cancellationToken)
        {
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            
            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://*:{_port}/");
            
            try
            {
                _listener.Start();
                _logger?.LogInformation($"OAuth server started on http://localhost:{_port}/");
            }
            catch (HttpListenerException ex)
            {
                _logger?.LogError(ex, "Failed to start OAuth server on port {Port}", _port);
                throw new InvalidOperationException($"Failed to start OAuth server on port {_port}. Make sure the port is not already in use.", ex);
            }
            
            var listenerTask = Task.Run(async () =>
            {
                while (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    try
                    {
                        var context = await _listener.GetContextAsync();
                        _ = Task.Run(() => HandleRequest(context, _cancellationTokenSource.Token), _cancellationTokenSource.Token);
                    }
                    catch (HttpListenerException) when (_cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        break;
                    }
                    catch (ObjectDisposedException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Error accepting OAuth connection");
                    }
                }
            }, _cancellationTokenSource.Token);
            
            try
            {
                await listenerTask;
            }
            finally
            {
                _listener?.Stop();
                _listener?.Close();
            }
        }
        
        private async Task HandleRequest(HttpListenerContext context, CancellationToken cancellationToken)
        {
            try
            {
                var path = context.Request.Url?.AbsolutePath;
                var method = context.Request.HttpMethod;
                var query = context.Request.QueryString.ToString();
                
                _logger?.LogInformation($"OAuth Server: {method} {path} {query}");
                
                context.Response.Headers.Add("Access-Control-Allow-Origin", "*");
                context.Response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
                context.Response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Authorization");
                
                if (context.Request.HttpMethod == "OPTIONS")
                {
                    _logger?.LogDebug("OAuth Server: Handling OPTIONS request");
                    context.Response.StatusCode = 200;
                    context.Response.Close();
                    return;
                }
                
                switch (path)
                {
                    case "/.well-known/openid_configuration":
                        _logger?.LogInformation("OAuth Server: Serving OpenID configuration");
                        await HandleOpenIdConfiguration(context);
                        break;
                    case "/.well-known/oauth-authorization-server":
                        _logger?.LogInformation("OAuth Server: Serving OAuth authorization server metadata");
                        await HandleOAuthAuthorizationServerMetadata(context);
                        break;
                    case "/.well-known/jwks":
                        _logger?.LogInformation("OAuth Server: Serving JWKS");
                        await HandleJwks(context);
                        break;
                    case "/.well-known/oauth-protected-resource":
                        _logger?.LogInformation("OAuth Server: Serving protected resource metadata");
                        await HandleProtectedResourceMetadata(context);
                        break;
                    case "/register":
                        _logger?.LogInformation("OAuth Server: Handling dynamic client registration");
                        await HandleClientRegistration(context);
                        break;
                    case "/token":
                        _logger?.LogInformation("OAuth Server: Handling token request");
                        await HandleTokenRequest(context);
                        break;
                    case "/auth":
                        _logger?.LogInformation("OAuth Server: Handling authorization request");
                        await HandleAuthorizationRequest(context);
                        break;
                    default:
                        _logger?.LogWarning($"OAuth Server: Unknown path requested: {path}");
                        await HandleNotFound(context);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error handling OAuth request");
                try
                {
                    context.Response.StatusCode = 500;
                    await WriteJsonResponse(context, new { error = "internal_server_error", error_description = ex.Message });
                }
                catch { }
            }
        }
        
        private async Task HandleOpenIdConfiguration(HttpListenerContext context)
        {
            var config = new
            {
                issuer = _issuer,
                authorization_endpoint = $"{_issuer}/auth",
                token_endpoint = $"{_issuer}/token",
                jwks_uri = $"{_issuer}/.well-known/jwks",
                registration_endpoint = $"{_issuer}/register",
                response_types_supported = new[] { "code" },
                grant_types_supported = new[] { "authorization_code", "client_credentials" },
                subject_types_supported = new[] { "public" },
                id_token_signing_alg_values_supported = new[] { "RS256" },
                scopes_supported = new[] { "openid", "mcp:tools" },
                token_endpoint_auth_methods_supported = new[] { "client_secret_post", "client_secret_basic", "none" },
                claims_supported = new[] { "sub", "name", "preferred_username", "email" },
                code_challenge_methods_supported = new[] { "S256", "plain" }
            };
            
            await WriteJsonResponse(context, config);
        }
        
        private async Task HandleOAuthAuthorizationServerMetadata(HttpListenerContext context)
        {
            var metadata = new
            {
                issuer = _issuer,
                authorization_endpoint = $"{_issuer}/auth",
                token_endpoint = $"{_issuer}/token",
                jwks_uri = $"{_issuer}/.well-known/jwks",
                registration_endpoint = $"{_issuer}/register",
                response_types_supported = new[] { "code" },
                grant_types_supported = new[] { "authorization_code", "client_credentials" },
                token_endpoint_auth_methods_supported = new[] { "client_secret_post", "client_secret_basic", "none" },
                scopes_supported = new[] { "mcp:tools" },
                code_challenge_methods_supported = new[] { "S256", "plain" }
            };
            
            await WriteJsonResponse(context, metadata);
        }
        
        private async Task HandleClientRegistration(HttpListenerContext context)
        {
            if (context.Request.HttpMethod != "POST")
            {
                context.Response.StatusCode = 405;
                await WriteJsonResponse(context, new { error = "invalid_request", error_description = "Only POST method supported" });
                return;
            }
            
            using var reader = new StreamReader(context.Request.InputStream);
            var requestBody = await reader.ReadToEndAsync();
            
            _logger?.LogInformation($"OAuth Server: Client registration request: {requestBody}");
            
            try
            {
                var registrationRequest = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(requestBody);
                
                // Extract redirect URIs from the request
                var redirectUris = new List<string>();
                if (registrationRequest?.TryGetValue("redirect_uris", out var redirectUrisObj) == true)
                {
                    if (redirectUrisObj is System.Text.Json.JsonElement redirectUrisElement && redirectUrisElement.ValueKind == System.Text.Json.JsonValueKind.Array)
                    {
                        redirectUris = redirectUrisElement.EnumerateArray()
                            .Select(item => item.GetString())
                            .Where(uri => !string.IsNullOrEmpty(uri))
                            .Cast<string>()
                            .ToList();
                    }
                }
                
                // Extract other fields
                var clientName = "Claude MCP Client";
                if (registrationRequest?.TryGetValue("client_name", out var clientNameObj) == true)
                {
                    clientName = clientNameObj.ToString() ?? "Claude MCP Client";
                }
                
                var grantTypes = new[] { "authorization_code", "client_credentials" };
                if (registrationRequest?.TryGetValue("grant_types", out var grantTypesObj) == true)
                {
                    if (grantTypesObj is System.Text.Json.JsonElement grantTypesElement && grantTypesElement.ValueKind == System.Text.Json.JsonValueKind.Array)
                    {
                        grantTypes = grantTypesElement.EnumerateArray()
                            .Select(item => item.GetString())
                            .Where(type => !string.IsNullOrEmpty(type))
                            .Cast<string>()
                            .ToArray();
                    }
                }
                
                var responseTypes = new[] { "code" };
                if (registrationRequest?.TryGetValue("response_types", out var responseTypesObj) == true)
                {
                    if (responseTypesObj is System.Text.Json.JsonElement responseTypesElement && responseTypesElement.ValueKind == System.Text.Json.JsonValueKind.Array)
                    {
                        responseTypes = responseTypesElement.EnumerateArray()
                            .Select(item => item.GetString())
                            .Where(type => !string.IsNullOrEmpty(type))
                            .Cast<string>()
                            .ToArray();
                    }
                }
                
                var tokenEndpointAuthMethod = "none";
                if (registrationRequest?.TryGetValue("token_endpoint_auth_method", out var authMethodObj) == true)
                {
                    tokenEndpointAuthMethod = authMethodObj.ToString() ?? "none";
                }
                
                // Generate client credentials
                var clientId = Guid.NewGuid().ToString();
                var clientSecret = tokenEndpointAuthMethod == "none" ? null : Guid.NewGuid().ToString();
                
                // Register the client
                var client = new OAuthClient
                {
                    ClientId = clientId,
                    ClientSecret = clientSecret ?? "",
                    Name = clientName,
                    Scopes = new[] { "mcp:tools" },
                    RedirectUris = redirectUris,
                    TokenEndpointAuthMethod = tokenEndpointAuthMethod
                };
                
                _clients.TryAdd(clientId, client);
                
                _logger?.LogInformation($"OAuth Server: Registered new client: {clientId} with redirect URIs: {string.Join(", ", redirectUris)}");
                
                // Return client credentials
                var response = new Dictionary<string, object>
                {
                    ["client_id"] = clientId,
                    ["client_name"] = clientName,
                    ["redirect_uris"] = redirectUris.ToArray(),
                    ["grant_types"] = grantTypes,
                    ["response_types"] = responseTypes,
                    ["token_endpoint_auth_method"] = tokenEndpointAuthMethod,
                    ["client_secret_expires_at"] = 0 // Never expires
                };
                
                // Only include client_secret if auth method is not "none"
                if (tokenEndpointAuthMethod != "none" && clientSecret != null)
                {
                    response["client_secret"] = clientSecret;
                }
                
                context.Response.StatusCode = 201;
                await WriteJsonResponse(context, response);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error processing client registration");
                context.Response.StatusCode = 400;
                await WriteJsonResponse(context, new { error = "invalid_request", error_description = "Invalid registration request" });
            }
        }
        
        private async Task HandleJwks(HttpListenerContext context)
        {
            var parameters = _signingKey.Rsa!.ExportParameters(false);
            var jwk = new
            {
                kty = "RSA",
                use = "sig",
                kid = "default",
                n = Base64UrlEncoder.Encode(parameters.Modulus!),
                e = Base64UrlEncoder.Encode(parameters.Exponent!)
            };
            
            var jwks = new { keys = new[] { jwk } };
            await WriteJsonResponse(context, jwks);
        }
        
        private async Task HandleProtectedResourceMetadata(HttpListenerContext context)
        {
            var metadata = new
            {
                resource = new Uri(_audience),
                resource_documentation = new Uri("https://docs.example.com/api/mcp"),
                authorization_servers = new[] { new Uri(_issuer) },
                scopes_supported = new[] { "mcp:tools" }
            };
            
            await WriteJsonResponse(context, metadata);
        }
        
        private async Task HandleAuthorizationRequest(HttpListenerContext context)
        {
            var query = context.Request.QueryString;
            var responseType = query["response_type"];
            var clientId = query["client_id"];
            var redirectUri = query["redirect_uri"];
            var scope = query["scope"];
            var state = query["state"];
            var codeChallenge = query["code_challenge"];
            var codeChallengeMethod = query["code_challenge_method"];
            
            _logger?.LogInformation($"OAuth Server: Authorization request - client_id={clientId}, redirect_uri={redirectUri}, scope={scope}, response_type={responseType}");
            
            if (responseType != "code")
            {
                _logger?.LogWarning($"OAuth Server: Unsupported response type: {responseType}");
                await SendAuthError(context, "unsupported_response_type", "Only code response type is supported");
                return;
            }
            
            if (string.IsNullOrEmpty(clientId))
            {
                _logger?.LogWarning("OAuth Server: Missing client_id");
                await SendAuthError(context, "invalid_request", "client_id is required");
                return;
            }
            
            // Handle POST request (form submission)
            if (context.Request.HttpMethod == "POST")
            {
                _logger?.LogInformation("OAuth Server: Handling POST authorization request");
                var form = await ReadFormData(context.Request);
                var action = form.GetValueOrDefault("action", "");
                
                _logger?.LogInformation($"OAuth Server: Form action: {action}");
                
                if (action == "approve")
                {
                    // Generate and store authorization code
                    var authCode = Guid.NewGuid().ToString("N");
                    
                    var authData = new AuthorizationData
                    {
                        Code = authCode,
                        ClientId = clientId,
                        RedirectUri = redirectUri,
                        Scope = scope ?? "mcp:tools",
                        CodeChallenge = codeChallenge,
                        CodeChallengeMethod = codeChallengeMethod,
                        ExpiresAt = DateTime.UtcNow.AddMinutes(10)
                    };
                    
                    _authorizationCodes.TryAdd(authCode, authData);
                    
                    // Redirect back to the client with the authorization code
                    var redirectUrl = $"{redirectUri}?code={authCode}";
                    if (!string.IsNullOrEmpty(state))
                    {
                        redirectUrl += $"&state={state}";
                    }
                    
                    _logger?.LogInformation($"OAuth Server: Redirecting to: {redirectUrl}");
                    
                    context.Response.StatusCode = 302;
                    context.Response.Headers.Add("Location", redirectUrl);
                    context.Response.Close();
                    return;
                }
                else if (action == "deny")
                {
                    // Redirect back with error
                    var redirectUrl = $"{redirectUri}?error=access_denied&error_description=The+user+denied+the+request";
                    if (!string.IsNullOrEmpty(state))
                    {
                        redirectUrl += $"&state={state}";
                    }
                    
                    _logger?.LogInformation($"OAuth Server: User denied, redirecting to: {redirectUrl}");
                    
                    context.Response.StatusCode = 302;
                    context.Response.Headers.Add("Location", redirectUrl);
                    context.Response.Close();
                    return;
                }
            }
            
            // Show authorization page
            _logger?.LogInformation("OAuth Server: Showing authorization page");
            await ShowAuthorizationPage(context, clientId, redirectUri, scope ?? "mcp:tools", state, codeChallenge, codeChallengeMethod);
        }
        
        private async Task ShowAuthorizationPage(HttpListenerContext context, string clientId, string? redirectUri, string scope, string? state, string? codeChallenge, string? codeChallengeMethod)
        {
            var html = $@"
<!DOCTYPE html>
<html>
<head>
    <title>Authorize AiStudio4 MCP Access</title>
    <style>
        body {{
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
            max-width: 500px;
            margin: 50px auto;
            padding: 20px;
            background: #f5f5f5;
        }}
        .auth-container {{
            background: white;
            padding: 30px;
            border-radius: 10px;
            box-shadow: 0 2px 10px rgba(0,0,0,0.1);
        }}
        .app-info {{
            text-align: center;
            margin-bottom: 30px;
        }}
        .app-name {{
            font-size: 24px;
            font-weight: bold;
            color: #333;
            margin-bottom: 10px;
        }}
        .permissions {{
            background: #f8f9fa;
            padding: 15px;
            border-radius: 5px;
            margin: 20px 0;
        }}
        .permissions h3 {{
            margin-top: 0;
            color: #555;
        }}
        .scope {{
            background: #e3f2fd;
            padding: 5px 10px;
            border-radius: 3px;
            display: inline-block;
            margin: 5px;
            font-size: 14px;
        }}
        .buttons {{
            display: flex;
            gap: 10px;
            margin-top: 30px;
        }}
        .btn {{
            flex: 1;
            padding: 12px;
            border: none;
            border-radius: 5px;
            font-size: 16px;
            cursor: pointer;
            transition: background-color 0.2s;
        }}
        .btn-approve {{
            background: #4CAF50;
            color: white;
        }}
        .btn-approve:hover {{
            background: #45a049;
        }}
        .btn-deny {{
            background: #f44336;
            color: white;
        }}
        .btn-deny:hover {{
            background: #da190b;
        }}
        .client-info {{
            color: #666;
            font-size: 14px;
            margin-bottom: 20px;
        }}
        .auto-approve {{
            background: #e8f5e8;
            padding: 10px;
            border-radius: 5px;
            margin: 20px 0;
            font-size: 14px;
            color: #2e7d32;
        }}
    </style>
</head>
<body>
    <div class='auth-container'>
        <div class='app-info'>
            <div class='app-name'>🔐 AiStudio4 MCP Server</div>
            <div class='client-info'>Client ID: <code>{clientId}</code></div>
        </div>
        
        <div class='auto-approve'>
            <strong>🚀 Development Mode:</strong> This authorization will be automatically approved for Claude MCP connections.
        </div>
        
        <div class='permissions'>
            <h3>Requested Permissions:</h3>
            <div class='scope'>🔧 {scope}</div>
            <p style='margin: 10px 0 0 0; font-size: 14px; color: #666;'>
                This allows access to AiStudio4 tools and resources through the MCP protocol.
            </p>
        </div>
        
        <form method='post'>
            <input type='hidden' name='client_id' value='{clientId}'>
            <input type='hidden' name='redirect_uri' value='{redirectUri}'>
            <input type='hidden' name='scope' value='{scope}'>
            <input type='hidden' name='state' value='{state}'>
            <input type='hidden' name='code_challenge' value='{codeChallenge}'>
            <input type='hidden' name='code_challenge_method' value='{codeChallengeMethod}'>
            
            <div class='buttons'>
                <button type='submit' name='action' value='approve' class='btn btn-approve'>
                    ✓ Approve Access
                </button>
                <button type='submit' name='action' value='deny' class='btn btn-deny'>
                    ✗ Deny Access
                </button>
            </div>
        </form>
        
        <script>
            // Auto-approve after 3 seconds for development convenience
            setTimeout(function() {{
                document.querySelector('button[value=""approve""]').click();
            }}, 3000);
        </script>
    </div>
</body>
</html>";
            
            context.Response.ContentType = "text/html";
            var bytes = Encoding.UTF8.GetBytes(html);
            await context.Response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
            context.Response.Close();
        }
        
        private async Task SendAuthError(HttpListenerContext context, string error, string description)
        {
            var errorHtml = $@"
<!DOCTYPE html>
<html>
<head>
    <title>OAuth Error</title>
</head>
<body>
    <h1>OAuth Error</h1>
    <p><strong>Error:</strong> {error}</p>
    <p><strong>Description:</strong> {description}</p>
</body>
</html>";
            context.Response.ContentType = "text/html";
            var bytes = Encoding.UTF8.GetBytes(errorHtml);
            await context.Response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
            context.Response.Close();
        }
        
        private async Task HandleTokenRequest(HttpListenerContext context)
        {
            if (context.Request.HttpMethod != "POST")
            {
                context.Response.StatusCode = 405;
                await WriteJsonResponse(context, new { error = "invalid_request", error_description = "Only POST method supported" });
                return;
            }
            
            var form = await ReadFormData(context.Request);
            
            if (!form.TryGetValue("grant_type", out var grantType))
            {
                context.Response.StatusCode = 400;
                await WriteJsonResponse(context, new { error = "invalid_request", error_description = "grant_type is required" });
                return;
            }
            
            if (grantType == "authorization_code")
            {
                await HandleAuthorizationCodeGrant(context, form);
            }
            else if (grantType == "client_credentials")
            {
                await HandleClientCredentialsGrant(context, form);
            }
            else
            {
                context.Response.StatusCode = 400;
                await WriteJsonResponse(context, new { error = "unsupported_grant_type", error_description = "Only authorization_code and client_credentials grant types are supported" });
            }
        }
        
        private async Task HandleAuthorizationCodeGrant(HttpListenerContext context, Dictionary<string, string> form)
        {
            var code = form.GetValueOrDefault("code", "");
            var redirectUri = form.GetValueOrDefault("redirect_uri", "");
            var clientId = form.GetValueOrDefault("client_id", "");
            var codeVerifier = form.GetValueOrDefault("code_verifier", "");
            
            if (string.IsNullOrEmpty(code))
            {
                context.Response.StatusCode = 400;
                await WriteJsonResponse(context, new { error = "invalid_request", error_description = "code is required" });
                return;
            }
            
            if (!_authorizationCodes.TryGetValue(code, out var authData))
            {
                context.Response.StatusCode = 400;
                await WriteJsonResponse(context, new { error = "invalid_grant", error_description = "Invalid authorization code" });
                return;
            }
            
            if (authData.ExpiresAt < DateTime.UtcNow)
            {
                _authorizationCodes.TryRemove(code, out _);
                context.Response.StatusCode = 400;
                await WriteJsonResponse(context, new { error = "invalid_grant", error_description = "Authorization code has expired" });
                return;
            }
            
            if (authData.ClientId != clientId)
            {
                context.Response.StatusCode = 400;
                await WriteJsonResponse(context, new { error = "invalid_grant", error_description = "client_id mismatch" });
                return;
            }
            
            if (authData.RedirectUri != redirectUri)
            {
                context.Response.StatusCode = 400;
                await WriteJsonResponse(context, new { error = "invalid_grant", error_description = "redirect_uri mismatch" });
                return;
            }
            
            // Verify PKCE if present
            if (!string.IsNullOrEmpty(authData.CodeChallenge))
            {
                if (string.IsNullOrEmpty(codeVerifier))
                {
                    context.Response.StatusCode = 400;
                    await WriteJsonResponse(context, new { error = "invalid_request", error_description = "code_verifier is required" });
                    return;
                }
                
                if (!VerifyCodeChallenge(codeVerifier, authData.CodeChallenge, authData.CodeChallengeMethod))
                {
                    context.Response.StatusCode = 400;
                    await WriteJsonResponse(context, new { error = "invalid_grant", error_description = "Invalid code verifier" });
                    return;
                }
            }
            
            // Remove the used authorization code
            _authorizationCodes.TryRemove(code, out _);
            
            var token = GenerateAccessToken(clientId, authData.Scope);
            
            await WriteJsonResponse(context, new
            {
                access_token = token,
                token_type = "Bearer",
                expires_in = 3600,
                scope = authData.Scope
            });
        }
        
        private async Task HandleClientCredentialsGrant(HttpListenerContext context, Dictionary<string, string> form)
        {
            var clientId = form.GetValueOrDefault("client_id", "");
            var clientSecret = form.GetValueOrDefault("client_secret", "");
            
            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            {
                context.Response.StatusCode = 400;
                await WriteJsonResponse(context, new { error = "invalid_request", error_description = "client_id and client_secret are required" });
                return;
            }
            
            if (!_clients.TryGetValue(clientId, out var client) || client.ClientSecret != clientSecret)
            {
                context.Response.StatusCode = 401;
                await WriteJsonResponse(context, new { error = "invalid_client", error_description = "Invalid client credentials" });
                return;
            }
            
            var scope = form.GetValueOrDefault("scope", "mcp:tools");
            var token = GenerateAccessToken(clientId, scope);
            
            await WriteJsonResponse(context, new
            {
                access_token = token,
                token_type = "Bearer",
                expires_in = 3600,
                scope = scope
            });
        }
        
        private bool VerifyCodeChallenge(string codeVerifier, string codeChallenge, string? method)
        {
            if (method == "S256")
            {
                using var sha256 = System.Security.Cryptography.SHA256.Create();
                var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(codeVerifier));
                var base64Hash = Convert.ToBase64String(hash)
                    .Replace("+", "-")
                    .Replace("/", "_")
                    .Replace("=", "");
                return base64Hash == codeChallenge;
            }
            else if (method == "plain" || string.IsNullOrEmpty(method))
            {
                return codeVerifier == codeChallenge;
            }
            return false;
        }
        
        private string GenerateAccessToken(string clientId, string scope)
        {
            var now = DateTime.UtcNow;
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, clientId),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, new DateTimeOffset(now).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
                new Claim("scope", scope),
                new Claim("client_id", clientId),
                new Claim("name", clientId),
                new Claim("preferred_username", clientId)
            };
            
            var token = new JwtSecurityToken(
                issuer: _issuer,
                audience: _audience,
                claims: claims,
                notBefore: now,
                expires: now.AddHours(1),
                signingCredentials: _signingCredentials);
            
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
        
        private async Task<Dictionary<string, string>> ReadFormData(HttpListenerRequest request)
        {
            var form = new Dictionary<string, string>();
            
            using var reader = new StreamReader(request.InputStream);
            var content = await reader.ReadToEndAsync();
            
            if (request.ContentType?.Contains("application/x-www-form-urlencoded") == true)
            {
                var pairs = content.Split('&');
                foreach (var pair in pairs)
                {
                    var parts = pair.Split('=', 2);
                    if (parts.Length == 2)
                    {
                        form[Uri.UnescapeDataString(parts[0])] = Uri.UnescapeDataString(parts[1]);
                    }
                }
            }
            
            return form;
        }
        
        private async Task WriteJsonResponse(HttpListenerContext context, object data)
        {
            context.Response.ContentType = "application/json";
            var json = System.Text.Json.JsonSerializer.Serialize(data, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower });
            var bytes = Encoding.UTF8.GetBytes(json);
            await context.Response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
            context.Response.Close();
        }
        
        private async Task HandleNotFound(HttpListenerContext context)
        {
            context.Response.StatusCode = 404;
            await WriteJsonResponse(context, new { error = "not_found", error_description = "Endpoint not found" });
        }
        
        public void Dispose()
        {
            _cancellationTokenSource?.Cancel();
            try
            {
                _listener?.Stop();
            }
            catch { }
            _listener?.Close();
            _cancellationTokenSource?.Dispose();
        }
        
        private class OAuthClient
        {
            public string ClientId { get; set; } = "";
            public string ClientSecret { get; set; } = "";
            public string Name { get; set; } = "";
            public string[] Scopes { get; set; } = Array.Empty<string>();
            public List<string> RedirectUris { get; set; } = new();
            public string TokenEndpointAuthMethod { get; set; } = "none";
        }
        
        private class AuthorizationData
        {
            public string Code { get; set; } = "";
            public string ClientId { get; set; } = "";
            public string? RedirectUri { get; set; }
            public string Scope { get; set; } = "";
            public string? CodeChallenge { get; set; }
            public string? CodeChallengeMethod { get; set; }
            public DateTime ExpiresAt { get; set; }
        }
    }
}