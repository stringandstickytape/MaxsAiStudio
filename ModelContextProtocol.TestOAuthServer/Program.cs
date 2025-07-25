using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using System.Collections.Concurrent;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ModelContextProtocol.TestOAuthServer.Persistence;

namespace ModelContextProtocol.TestOAuthServer;

public sealed class Program
{
    private const int _port = 7029;
    private static readonly string _url = $"http://localhost:{_port}";

    // Port 5000 is used by tests and port 7071 is used by the ProtectedMCPServer sample
    private static readonly string[] ValidResources = ["http://localhost:5000/", "http://localhost:7071/"];

    private readonly ConcurrentDictionary<string, AuthorizationCodeInfo> _authCodes;
    private readonly ConcurrentDictionary<string, TokenInfo> _tokens;
    private readonly ConcurrentDictionary<string, ClientInfo> _clients = new();

    private readonly RSA _rsa;
    private readonly string _keyId;
    private readonly OAuthPersistenceManager _persistenceManager;

    private readonly ILoggerProvider? _loggerProvider;
    private readonly IConnectionListenerFactory? _kestrelTransport;

    /// <summary>
    /// Initializes a new instance of the <see cref="Program"/> class with logging and transport parameters.
    /// </summary>
    /// <param name="loggerProvider">Optional logger provider for logging.</param>
    /// <param name="kestrelTransport">Optional Kestrel transport for in-memory connections.</param>
    /// <param name="persistenceDataDirectory">Optional directory for persistence data. If null, uses default AppData location.</param>
    public Program(ILoggerProvider? loggerProvider = null, IConnectionListenerFactory? kestrelTransport = null, string? persistenceDataDirectory = null)
    {
        _rsa = RSA.Create(2048);
        _keyId = Guid.NewGuid().ToString();
        _loggerProvider = loggerProvider;
        _kestrelTransport = kestrelTransport;
        
        // Set up persistence
        if (string.IsNullOrEmpty(persistenceDataDirectory))
        {
            var appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            persistenceDataDirectory = Path.Combine(appDataFolder, "AiStudio4", "OAuth");
        }
        
        _persistenceManager = new OAuthPersistenceManager(persistenceDataDirectory);
        
        // Load persisted data
        _tokens = _persistenceManager.LoadTokens();
        _authCodes = _persistenceManager.LoadAuthorizationCodes();
        
        // Load dynamic clients
        var dynamicClients = _persistenceManager.LoadDynamicClients();
        foreach (var kvp in dynamicClients)
        {
            _clients[kvp.Key] = kvp.Value;
        }
    }

    // Track if we've already issued an already-expired token for the CanAuthenticate_WithTokenRefresh test which uses the test-refresh-client registration.
    public bool HasIssuedExpiredToken { get; set; }
    public bool HasIssuedRefreshToken { get; set; }


    /// <summary>
    /// Runs the OAuth server with the specified parameters.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    /// <param name="cancellationToken">Cancellation token to stop the server.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task RunServerAsync(string[]? args = null, CancellationToken cancellationToken = default)
    {
        Console.WriteLine("Starting in-memory test-only OAuth Server...");

        var builder = WebApplication.CreateEmptyBuilder(new()
        {
            Args = args,
        });

        if (_kestrelTransport is not null)
        {
            // Add passed-in transport before calling UseKestrel() to avoid the SocketsHttpHandler getting added.
            builder.Services.AddSingleton(_kestrelTransport);
        }

        builder.WebHost.UseKestrel(kestrelOptions =>
        {
            kestrelOptions.ListenLocalhost(_port);
        });

        builder.Services.AddRoutingCore();
        builder.Services.AddLogging();

        builder.Services.ConfigureHttpJsonOptions(jsonOptions =>
        {
            jsonOptions.SerializerOptions.TypeInfoResolverChain.Add(OAuthJsonContext.Default);
        });

        builder.Logging.AddConsole();
        if (_loggerProvider is not null)
        {
            builder.Logging.AddProvider(_loggerProvider);
        }

        var app = builder.Build();

        app.UseRouting();
        app.UseEndpoints(_ => { });

        // Set up the demo client
        var clientId = "demo-client";
        var clientSecret = "demo-secret";
        _clients[clientId] = new ClientInfo
        {
            ClientId = clientId,
            ClientSecret = clientSecret,
            RedirectUris = ["http://localhost:1179/callback"],
        };

        // When this client ID is used, the first token issued will already be expired to make
        // testing the refresh flow easier.
        _clients["test-refresh-client"] = new ClientInfo
        {
            ClientId = "test-refresh-client",
            ClientSecret = "test-refresh-secret",
            RedirectUris = ["http://localhost:1179/callback"],
        };

        // The MCP spec tells the client to use /.well-known/oauth-authorization-server but AddJwtBearer looks for
        // /.well-known/openid-configuration by default. To make things easier, we support both with the same response
        // which seems to be common. Ex. https://github.com/keycloak/keycloak/pull/29628
        //
        // The requirements for these endpoints are at https://www.rfc-editor.org/rfc/rfc8414 and
        // https://openid.net/specs/openid-connect-discovery-1_0.html#ProviderMetadata respectively.
        // They do differ, but it's close enough at least for our current testing to use the same response for both.
        // See https://gist.github.com/localden/26d8bcf641703c08a5d8741aa9c3336c
        string[] metadataEndpoints = ["/.well-known/oauth-authorization-server", "/.well-known/openid-configuration"];
        foreach (var metadataEndpoint in metadataEndpoints)
        {
            // OAuth 2.0 Authorization Server Metadata (RFC 8414)
            app.MapGet(metadataEndpoint, () =>
            {
                var metadata = new OAuthServerMetadata
                {
                    Issuer = _url,
                    AuthorizationEndpoint = $"{_url}/authorize",
                    TokenEndpoint = $"{_url}/token",
                    JwksUri = $"{_url}/.well-known/jwks.json",
                    ResponseTypesSupported = ["code"],
                    SubjectTypesSupported = ["public"],
                    IdTokenSigningAlgValuesSupported = ["RS256"],
                    ScopesSupported = ["openid", "profile", "email", "mcp:tools"],
                    TokenEndpointAuthMethodsSupported = ["client_secret_post"],
                    ClaimsSupported = ["sub", "iss", "name", "email", "aud"],
                    CodeChallengeMethodsSupported = ["S256"],
                    GrantTypesSupported = ["authorization_code", "refresh_token"],
                    IntrospectionEndpoint = $"{_url}/introspect",
                    RegistrationEndpoint = $"{_url}/register"
                };

                return Results.Ok(metadata);
            });
        }

        // JWKS endpoint to expose the public key
        app.MapGet("/.well-known/jwks.json", () =>
        {
            var parameters = _rsa.ExportParameters(false);

            // Convert parameters to base64url encoding
            var e = WebEncoders.Base64UrlEncode(parameters.Exponent ?? Array.Empty<byte>());
            var n = WebEncoders.Base64UrlEncode(parameters.Modulus ?? Array.Empty<byte>());

            var jwks = new JsonWebKeySet
            {
                Keys = [
                    new JsonWebKey
                    {
                        KeyType = "RSA",
                        Use = "sig",
                        KeyId = _keyId,
                        Algorithm = "RS256",
                        Exponent = e,
                        Modulus = n
                    }
                ]
            };

            return Results.Ok(jwks);
        });

        // Authorize endpoint
        app.MapGet("/authorize", (
            [FromQuery] string client_id,
            [FromQuery] string? redirect_uri,
            [FromQuery] string response_type,
            [FromQuery] string code_challenge,
            [FromQuery] string code_challenge_method,
            [FromQuery] string? scope,
            [FromQuery] string? state,
            [FromQuery] string? resource) =>
        {
            // Validate client
            if (!_clients.TryGetValue(client_id, out var client))
            {
                return Results.BadRequest(new OAuthErrorResponse
                {
                    Error = "invalid_client",
                    ErrorDescription = "Client not found"
                });
            }

            // Validate redirect_uri
            if (string.IsNullOrEmpty(redirect_uri))
            {
                if (client.RedirectUris.Count == 1)
                {
                    redirect_uri = client.RedirectUris[0];
                }
                else
                {
                    return Results.BadRequest(new OAuthErrorResponse
                    {
                        Error = "invalid_request",
                        ErrorDescription = "redirect_uri is required when client has multiple registered URIs"
                    });
                }
            }
            else if (!client.RedirectUris.Contains(redirect_uri))
            {
                return Results.BadRequest(new OAuthErrorResponse
                {
                    Error = "invalid_request",
                    ErrorDescription = "Unregistered redirect_uri"
                });
            }

            // Validate response_type
            if (response_type != "code")
            {
                return Results.Redirect($"{redirect_uri}?error=unsupported_response_type&error_description=Only+code+response_type+is+supported&state={state}");
            }

            // Validate code challenge method
            if (code_challenge_method != "S256")
            {
                return Results.Redirect($"{redirect_uri}?error=invalid_request&error_description=Only+S256+code_challenge_method+is+supported&state={state}");
            }

            // Validate resource in accordance with RFC 8707
            if (string.IsNullOrEmpty(resource) || !ValidResources.Contains(resource))
            {
                return Results.Redirect($"{redirect_uri}?error=invalid_target&error_description=The+specified+resource+is+not+valid&state={state}");
            }

            // Generate a new authorization code
            var code = GenerateRandomToken();
            var requestedScopes = scope?.Split(' ').ToList() ?? [];

            // Store session information for user confirmation (don't store the actual code yet)
            var sessionId = Guid.NewGuid().ToString();
            _authCodes[sessionId] = new AuthorizationCodeInfo
            {
                ClientId = client_id,
                RedirectUri = redirect_uri,
                CodeChallenge = code_challenge,
                Scope = requestedScopes,
                Resource = !string.IsNullOrEmpty(resource) ? new Uri(resource) : null,
                IssuedAt = DateTimeOffset.UtcNow
            };

            // Show confirmation page instead of immediately redirecting
            var confirmationHtml = $@"
<!DOCTYPE html>
<html>
<head>
    <title>AIStudio4 - Authorization</title>
    <style>
        * {{ margin: 0; padding: 0; box-sizing: border-box; }}
        body {{ 
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif;
            background: #0a0a0a;
            color: #e0e0e0;
            min-height: 100vh;
            display: flex;
            align-items: center;
            justify-content: center;
            position: relative;
            overflow: hidden;
        }}
        body::before {{
            content: '';
            position: absolute;
            top: -50%;
            left: -50%;
            width: 200%;
            height: 200%;
            background: radial-gradient(circle at 20% 30%, rgba(0, 255, 136, 0.08) 0%, transparent 50%),
                        radial-gradient(circle at 80% 70%, rgba(0, 204, 255, 0.08) 0%, transparent 50%);
            animation: float 20s ease-in-out infinite;
        }}
        @keyframes float {{
            0%, 100% {{ transform: translate(0, 0) rotate(0deg); }}
            50% {{ transform: translate(-30px, -30px) rotate(180deg); }}
        }}
        .container {{ 
            max-width: 480px;
            width: 90%;
            background: rgba(16, 16, 16, 0.95);
            backdrop-filter: blur(20px);
            padding: 40px;
            border-radius: 16px;
            box-shadow: 0 0 40px rgba(0, 255, 136, 0.1),
                        0 0 80px rgba(0, 204, 255, 0.05),
                        inset 0 0 0 1px rgba(255, 255, 255, 0.1);
            position: relative;
            z-index: 1;
        }}
        h2 {{ 
            font-size: 28px;
            font-weight: 600;
            margin-bottom: 24px;
            background: linear-gradient(135deg, #00ff88 0%, #00ccff 100%);
            -webkit-background-clip: text;
            -webkit-text-fill-color: transparent;
            background-clip: text;
            text-align: center;
        }}
        .message {{ 
            margin: 20px 0;
            color: #b0b0b0;
            line-height: 1.6;
            text-align: center;
            font-size: 15px;
        }}
        .client-info {{ 
            background: rgba(0, 255, 136, 0.05);
            border: 1px solid rgba(0, 255, 136, 0.2);
            padding: 20px;
            border-radius: 12px;
            margin: 24px 0;
            font-family: 'SF Mono', Monaco, 'Cascadia Code', monospace;
            font-size: 14px;
        }}
        .client-info-item {{
            margin: 8px 0;
            display: flex;
            align-items: baseline;
        }}
        .client-info strong {{ 
            color: #00ff88;
            margin-right: 12px;
            min-width: 80px;
            display: inline-block;
        }}
        .client-info-value {{
            color: #e0e0e0;
            word-break: break-word;
        }}
        .button-group {{
            display: flex;
            gap: 16px;
            margin-top: 32px;
            justify-content: center;
        }}
        button {{ 
            padding: 14px 32px;
            font-size: 15px;
            font-weight: 500;
            border: none;
            border-radius: 8px;
            cursor: pointer;
            transition: all 0.3s ease;
            position: relative;
            overflow: hidden;
        }}
        button::before {{
            content: '';
            position: absolute;
            top: 50%;
            left: 50%;
            width: 0;
            height: 0;
            background: rgba(255, 255, 255, 0.1);
            border-radius: 50%;
            transform: translate(-50%, -50%);
            transition: width 0.6s, height 0.6s;
        }}
        button:active::before {{
            width: 300px;
            height: 300px;
        }}
        .authorize-btn {{ 
            background: linear-gradient(135deg, #00ff88 0%, #00d975 100%);
            color: #0a0a0a;
            box-shadow: 0 4px 20px rgba(0, 255, 136, 0.3);
        }}
        .authorize-btn:hover {{ 
            transform: translateY(-2px);
            box-shadow: 0 6px 30px rgba(0, 255, 136, 0.4);
        }}
        .deny-btn {{ 
            background: rgba(255, 255, 255, 0.05);
            color: #e0e0e0;
            border: 1px solid rgba(255, 255, 255, 0.1);
        }}
        .deny-btn:hover {{ 
            background: rgba(255, 67, 54, 0.1);
            border-color: rgba(255, 67, 54, 0.3);
            color: #ff4336;
        }}
        .status {{ 
            margin-top: 24px;
            font-weight: 500;
            text-align: center;
            height: 24px;
            color: #00ff88;
            opacity: 0;
            transition: opacity 0.3s ease;
        }}
        .status.visible {{ opacity: 1; }}
    </style>
</head>
<body>
    <div class='container'>
        <h2>AIStudio4 Authorization</h2>
        <div class='message'>
            An application is requesting access to your AIStudio4 resources
        </div>
        <div class='client-info'>
            <div class='client-info-item'>
                <strong>Client:</strong>
                <span class='client-info-value'>{client_id}</span>
            </div>
            <div class='client-info-item'>
                <strong>Scopes:</strong>
                <span class='client-info-value'>{(string.IsNullOrEmpty(scope) ? "Default access" : scope)}</span>
            </div>
            <div class='client-info-item'>
                <strong>Resource:</strong>
                <span class='client-info-value'>{resource}</span>
            </div>
        </div>
        <div class='message'>
            Grant this application access to the requested resources?
        </div>
        <div class='button-group'>
            <button class='authorize-btn' onclick='authorize(""{sessionId}"", ""{code}"", ""{redirect_uri}"", ""{state}"")'>Authorize</button>
            <button class='deny-btn' onclick='deny(""{redirect_uri}"", ""{state}"")'>Deny</button>
        </div>
        <div id='status' class='status'></div>
    </div>
    <script>
        function authorize(sessionId, code, redirectUri, state) {{
            var status = document.getElementById('status');
            status.innerHTML = 'Authorizing...';
            status.classList.add('visible');
            
            var url = '/authorize/confirm?session_id=' + encodeURIComponent(sessionId) + 
                     '&code=' + encodeURIComponent(code) + 
                     '&redirect_uri=' + encodeURIComponent(redirectUri);
            if (state) {{
                url += '&state=' + encodeURIComponent(state);
            }}
            
            setTimeout(() => {{
                window.location.href = url;
            }}, 300);
        }}
        
        function deny(redirectUri, state) {{
            var status = document.getElementById('status');
            status.innerHTML = 'Access denied';
            status.style.color = '#ff4336';
            status.classList.add('visible');
            
            var url = redirectUri + '?error=access_denied&error_description=User+denied+authorization';
            if (state) {{
                url += '&state=' + encodeURIComponent(state);
            }}
            
            setTimeout(() => {{
                window.location.href = url;
            }}, 500);
        }}
    </script>
</body>
</html>";

            return Results.Content(confirmationHtml, "text/html");
        });

        // Authorization confirmation endpoint
        app.MapGet("/authorize/confirm", (
            [FromQuery] string session_id,
            [FromQuery] string code,
            [FromQuery] string redirect_uri,
            [FromQuery] string? state) =>
        {
            // Validate session
            if (!_authCodes.TryRemove(session_id, out var sessionInfo))
            {
                return Results.BadRequest("Invalid or expired session");
            }

            // Now that user has approved, store the actual authorization code
            _authCodes[code] = sessionInfo;
            
            // Persist authorization codes
            _persistenceManager.SaveAuthorizationCodes(_authCodes);

            // Build the redirect URL with the authorization code
            var redirectUrl = $"{redirect_uri}?code={code}";
            if (!string.IsNullOrEmpty(state))
            {
                redirectUrl += $"&state={Uri.EscapeDataString(state)}";
            }

            // Show success page and redirect
            var successHtml = $@"
<!DOCTYPE html>
<html>
<head>
    <title>AIStudio4 - Authorization Complete</title>
    <style>
        * {{ margin: 0; padding: 0; box-sizing: border-box; }}
        body {{ 
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif;
            background: #0a0a0a;
            color: #e0e0e0;
            min-height: 100vh;
            display: flex;
            align-items: center;
            justify-content: center;
            position: relative;
            overflow: hidden;
        }}
        body::before {{
            content: '';
            position: absolute;
            top: -50%;
            left: -50%;
            width: 200%;
            height: 200%;
            background: radial-gradient(circle at 50% 50%, rgba(0, 255, 136, 0.15) 0%, transparent 50%);
            animation: pulse 3s ease-in-out infinite;
        }}
        @keyframes pulse {{
            0%, 100% {{ transform: scale(1) rotate(0deg); opacity: 0.8; }}
            50% {{ transform: scale(1.1) rotate(10deg); opacity: 1; }}
        }}
        .container {{ 
            max-width: 480px;
            width: 90%;
            background: rgba(16, 16, 16, 0.95);
            backdrop-filter: blur(20px);
            padding: 40px;
            border-radius: 16px;
            box-shadow: 0 0 60px rgba(0, 255, 136, 0.2),
                        0 0 120px rgba(0, 255, 136, 0.1),
                        inset 0 0 0 1px rgba(0, 255, 136, 0.3);
            position: relative;
            z-index: 1;
            animation: slideIn 0.5s ease-out;
        }}
        @keyframes slideIn {{
            from {{
                opacity: 0;
                transform: scale(0.95);
            }}
            to {{
                opacity: 1;
                transform: scale(1);
            }}
        }}
        .checkmark {{
            width: 80px;
            height: 80px;
            margin: 0 auto 24px;
            position: relative;
            animation: checkmarkScale 0.6s ease-out;
        }}
        @keyframes checkmarkScale {{
            0% {{ transform: scale(0); }}
            50% {{ transform: scale(1.2); }}
            100% {{ transform: scale(1); }}
        }}
        .checkmark svg {{
            width: 100%;
            height: 100%;
        }}
        .checkmark-circle {{
            stroke: #00ff88;
            stroke-width: 2;
            fill: none;
            stroke-dasharray: 166;
            stroke-dashoffset: 166;
            animation: checkmarkCircle 0.6s ease-out forwards;
        }}
        @keyframes checkmarkCircle {{
            to {{ stroke-dashoffset: 0; }}
        }}
        .checkmark-check {{
            stroke: #00ff88;
            stroke-width: 3;
            fill: none;
            stroke-dasharray: 48;
            stroke-dashoffset: 48;
            animation: checkmarkCheck 0.6s ease-out 0.3s forwards;
        }}
        @keyframes checkmarkCheck {{
            to {{ stroke-dashoffset: 0; }}
        }}
        h2 {{ 
            font-size: 28px;
            font-weight: 600;
            margin-bottom: 16px;
            background: linear-gradient(135deg, #00ff88 0%, #00ccff 100%);
            -webkit-background-clip: text;
            -webkit-text-fill-color: transparent;
            background-clip: text;
            text-align: center;
        }}
        .message {{ 
            margin: 20px 0;
            color: #b0b0b0;
            line-height: 1.6;
            text-align: center;
            font-size: 15px;
        }}
        .redirect-notice {{
            margin-top: 32px;
            padding: 16px;
            background: rgba(0, 255, 136, 0.05);
            border: 1px solid rgba(0, 255, 136, 0.2);
            border-radius: 8px;
            font-size: 14px;
            color: #00ff88;
            text-align: center;
            animation: fadeIn 0.5s ease-out 0.5s both;
        }}
        @keyframes fadeIn {{
            from {{ opacity: 0; }}
            to {{ opacity: 1; }}
        }}
        .progress-bar {{
            width: 100%;
            height: 4px;
            background: rgba(255, 255, 255, 0.1);
            border-radius: 2px;
            margin-top: 16px;
            overflow: hidden;
        }}
        .progress-fill {{
            height: 100%;
            background: linear-gradient(90deg, #00ff88 0%, #00ccff 100%);
            animation: progress 2s linear forwards;
        }}
        @keyframes progress {{
            from {{ width: 0%; }}
            to {{ width: 100%; }}
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='checkmark'>
            <svg viewBox='0 0 52 52'>
                <circle class='checkmark-circle' cx='26' cy='26' r='25'/>
                <path class='checkmark-check' d='M14.1 27.2l7.1 7.2 16.7-16.8'/>
            </svg>
        </div>
        <h2>Authorization Complete</h2>
        <div class='message'>
            You have successfully authorized the application. This window will close automatically.
        </div>
        <div class='redirect-notice'>
            This window will close automatically...
            <div class='progress-bar'>
                <div class='progress-fill'></div>
            </div>
        </div>
    </div>
    <script>
        // First redirect to complete the OAuth flow
        window.location.href = '{redirectUrl}';
        
        // Then close the window after a short delay
        setTimeout(function() {{
            window.close();
        }}, 2000);
    </script>
</body>
</html>";

            return Results.Content(successHtml, "text/html");
        });

        // Token endpoint
        app.MapPost("/token", async (HttpContext context) =>
        {
            var form = await context.Request.ReadFormAsync();

            // Authenticate client
            var client = AuthenticateClient(context, form);
            if (client == null)
            {
                context.Response.StatusCode = 401;
                return Results.Problem(
                    statusCode: 401,
                    title: "Unauthorized",
                    detail: "Invalid client credentials",
                    type: "https://tools.ietf.org/html/rfc6749#section-5.2");
            }

            // Validate resource in accordance with RFC 8707
            var resource = form["resource"].ToString();
            if (string.IsNullOrEmpty(resource) || !ValidResources.Contains(resource))
            {
                return Results.BadRequest(new OAuthErrorResponse
                {
                    Error = "invalid_target",
                    ErrorDescription = "The specified resource is not valid."
                });
            }

            var grant_type = form["grant_type"].ToString();
            if (grant_type == "authorization_code")
            {
                var code = form["code"].ToString();
                var code_verifier = form["code_verifier"].ToString();
                var redirect_uri = form["redirect_uri"].ToString();

                // Validate code
                if (string.IsNullOrEmpty(code) || !_authCodes.TryRemove(code, out var codeInfo))
                {
                    return Results.BadRequest(new OAuthErrorResponse
                    {
                        Error = "invalid_grant",
                        ErrorDescription = "Invalid authorization code"
                    });
                }
                
                // Persist authorization codes after removal
                _persistenceManager.SaveAuthorizationCodes(_authCodes);

                // Validate client_id
                if (codeInfo.ClientId != client.ClientId)
                {
                    return Results.BadRequest(new OAuthErrorResponse
                    {
                        Error = "invalid_grant",
                        ErrorDescription = "Authorization code was not issued to this client"
                    });
                }

                // Validate redirect_uri if provided
                if (!string.IsNullOrEmpty(redirect_uri) && redirect_uri != codeInfo.RedirectUri)
                {
                    return Results.BadRequest(new OAuthErrorResponse
                    {
                        Error = "invalid_grant",
                        ErrorDescription = "Redirect URI mismatch"
                    });
                }

                // Validate code verifier
                if (string.IsNullOrEmpty(code_verifier) || !VerifyCodeChallenge(code_verifier, codeInfo.CodeChallenge))
                {
                    return Results.BadRequest(new OAuthErrorResponse
                    {
                        Error = "invalid_grant",
                        ErrorDescription = "Code verifier does not match the challenge"
                    });
                }

                // Generate JWT token response
                var response = GenerateJwtTokenResponse(client.ClientId, codeInfo.Scope, codeInfo.Resource);
                
                // Persist tokens after generation
                _persistenceManager.SaveTokens(_tokens);
                
                return Results.Ok(response);
            }
            else if (grant_type == "refresh_token")
            {
                var refresh_token = form["refresh_token"].ToString();

                // Validate refresh token
                if (string.IsNullOrEmpty(refresh_token) || !_tokens.TryGetValue(refresh_token, out var tokenInfo) || tokenInfo.ClientId != client.ClientId)
                {
                    return Results.BadRequest(new OAuthErrorResponse
                    {
                        Error = "invalid_grant",
                        ErrorDescription = "Invalid refresh token"
                    });
                }

                // Generate new token response, keeping the same scopes
                var response = GenerateJwtTokenResponse(client.ClientId, tokenInfo.Scopes, tokenInfo.Resource);

                // Remove the old refresh token
                if (!string.IsNullOrEmpty(refresh_token))
                {
                    _tokens.TryRemove(refresh_token, out _);
                }

                HasIssuedRefreshToken = true;
                
                // Persist tokens after refresh
                _persistenceManager.SaveTokens(_tokens);
                
                return Results.Ok(response);
            }
            else
            {
                return Results.BadRequest(new OAuthErrorResponse
                {
                    Error = "unsupported_grant_type",
                    ErrorDescription = "Unsupported grant type"
                });
            }
        });

        // Introspection endpoint
        app.MapPost("/introspect", async (HttpContext context) =>
        {
            var form = await context.Request.ReadFormAsync();
            var token = form["token"].ToString();

            if (string.IsNullOrEmpty(token))
            {
                return Results.BadRequest(new OAuthErrorResponse
                {
                    Error = "invalid_request",
                    ErrorDescription = "Token is required"
                });
            }

            // Check opaque access tokens
            if (_tokens.TryGetValue(token, out var tokenInfo))
            {
                if (tokenInfo.ExpiresAt < DateTimeOffset.UtcNow)
                {
                    return Results.Ok(new TokenIntrospectionResponse { Active = false });
                }

                return Results.Ok(new TokenIntrospectionResponse
                {
                    Active = true,
                    ClientId = tokenInfo.ClientId,
                    Scope = string.Join(" ", tokenInfo.Scopes),
                    ExpirationTime = tokenInfo.ExpiresAt.ToUnixTimeSeconds(),
                    Audience = tokenInfo.Resource?.ToString()
                });
            }

            return Results.Ok(new TokenIntrospectionResponse { Active = false });
        });

        // Dynamic Client Registration endpoint (RFC 7591)
        app.MapPost("/register", async (HttpContext context) =>
        {
            using var stream = context.Request.Body;
            var registrationRequest = await JsonSerializer.DeserializeAsync(
                stream,
                OAuthJsonContext.Default.ClientRegistrationRequest,
                context.RequestAborted);

            if (registrationRequest is null)
            {
                return Results.BadRequest(new OAuthErrorResponse
                {
                    Error = "invalid_request",
                    ErrorDescription = "Invalid registration request"
                });
            }

            // Validate redirect URIs are provided
            if (registrationRequest.RedirectUris.Count == 0)
            {
                return Results.BadRequest(new OAuthErrorResponse
                {
                    Error = "invalid_redirect_uri",
                    ErrorDescription = "At least one redirect URI must be provided"
                });
            }

            // Validate redirect URIs
            foreach (var redirectUri in registrationRequest.RedirectUris)
            {
                if (!Uri.TryCreate(redirectUri, UriKind.Absolute, out var uri) ||
                    (uri.Scheme != "http" && uri.Scheme != "https"))
                {
                    return Results.BadRequest(new OAuthErrorResponse
                    {
                        Error = "invalid_redirect_uri",
                        ErrorDescription = $"Invalid redirect URI: {redirectUri}"
                    });
                }
            }

            // Generate client credentials
            var clientId = $"dyn-{Guid.NewGuid():N}";
            var clientSecret = GenerateRandomToken();
            var issuedAt = DateTimeOffset.UtcNow;

            // Store the registered client
            _clients[clientId] = new ClientInfo
            {
                ClientId = clientId,
                ClientSecret = clientSecret,
                RedirectUris = registrationRequest.RedirectUris,
            };
            
            // Persist dynamic clients
            _persistenceManager.SaveDynamicClients(_clients.ToDictionary(kvp => kvp.Key, kvp => kvp.Value));

            var registrationResponse = new ClientRegistrationResponse
            {
                ClientId = clientId,
                ClientSecret = clientSecret,
                ClientIdIssuedAt = issuedAt.ToUnixTimeSeconds(),
                RedirectUris = registrationRequest.RedirectUris,
                GrantTypes = ["authorization_code", "refresh_token"],
                ResponseTypes = ["code"],
                TokenEndpointAuthMethod = "client_secret_post",
            };

            return Results.Ok(registrationResponse);
        });

        app.MapGet("/", () => "Demo In-Memory OAuth 2.0 Server with JWT Support");

        Console.WriteLine($"OAuth Authorization Server running at {_url}");
        Console.WriteLine($"OAuth Server Metadata at {_url}/.well-known/oauth-authorization-server");
        Console.WriteLine($"JWT keys available at {_url}/.well-known/jwks.json");
        Console.WriteLine($"Demo Client ID: {clientId}");
        Console.WriteLine($"Demo Client Secret: {clientSecret}");

        await app.RunAsync(cancellationToken);
    }

    /// <summary>
    /// Authenticates a client based on client credentials in the request.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="form">The form collection containing client credentials.</param>
    /// <returns>The client info if authentication succeeds, null otherwise.</returns>
    private ClientInfo? AuthenticateClient(HttpContext context, IFormCollection form)
    {
        var clientId = form["client_id"].ToString();
        var clientSecret = form["client_secret"].ToString();

        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
        {
            return null;
        }

        if (_clients.TryGetValue(clientId, out var client) && client.ClientSecret == clientSecret)
        {
            return client;
        }

        return null;
    }

    /// <summary>
    /// Generates a JWT token response.
    /// </summary>
    /// <param name="clientId">The client ID.</param>
    /// <param name="scopes">The approved scopes.</param>
    /// <param name="resource">The resource URI.</param>
    /// <returns>A token response.</returns>
    private TokenResponse GenerateJwtTokenResponse(string clientId, List<string> scopes, Uri? resource)
    {
        var expiresIn = TimeSpan.FromHours(1);
        var issuedAt = DateTimeOffset.UtcNow;

        // For test-refresh-client, make the first token expired to test refresh functionality.
        if (clientId == "test-refresh-client" && !HasIssuedExpiredToken)
        {
            HasIssuedExpiredToken = true;
            expiresIn = TimeSpan.FromHours(-1);
        }

        var expiresAt = issuedAt.Add(expiresIn);
        var jwtId = Guid.NewGuid().ToString();

        // Create JWT header and payload
        var header = new Dictionary<string, string>
        {
            { "alg", "RS256" },
            { "typ", "JWT" },
            { "kid", _keyId }
        };

        var payload = new Dictionary<string, string>
        {
            { "iss", _url },
            { "sub", $"user-{clientId}" },
            { "name", $"user-{clientId}" },
            { "aud", resource?.ToString() ?? clientId },
            { "client_id", clientId },
            { "jti", jwtId },
            { "iat", issuedAt.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture) },
            { "exp", expiresAt.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture) },
            { "scope", string.Join(" ", scopes) }
        };

        // Create JWT token
        var headerJson = JsonSerializer.Serialize(header, OAuthJsonContext.Default.DictionaryStringString);
        var payloadJson = JsonSerializer.Serialize(payload, OAuthJsonContext.Default.DictionaryStringString);

        var headerBase64 = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(headerJson));
        var payloadBase64 = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(payloadJson));

        var dataToSign = $"{headerBase64}.{payloadBase64}";
        var signature = _rsa.SignData(Encoding.UTF8.GetBytes(dataToSign), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var signatureBase64 = WebEncoders.Base64UrlEncode(signature);

        var jwtToken = $"{headerBase64}.{payloadBase64}.{signatureBase64}";

        // Generate opaque refresh token
        var refreshToken = GenerateRandomToken();

        // Store token info (for refresh token and introspection)
        var tokenInfo = new TokenInfo
        {
            ClientId = clientId,
            Scopes = scopes,
            IssuedAt = issuedAt,
            ExpiresAt = expiresAt,
            Resource = resource,
            JwtId = jwtId
        };

        _tokens[refreshToken] = tokenInfo;

        return new TokenResponse
        {
            AccessToken = jwtToken,
            RefreshToken = refreshToken,
            TokenType = "Bearer",
            ExpiresIn = (int)expiresIn.TotalSeconds,
            Scope = string.Join(" ", scopes)
        };
    }

    /// <summary>
    /// Generates a random token for authorization code or refresh token.
    /// </summary>
    /// <returns>A Base64Url encoded random token.</returns>
    public static string GenerateRandomToken()
    {
        var bytes = new byte[32];
        Random.Shared.NextBytes(bytes);
        return WebEncoders.Base64UrlEncode(bytes);
    }

    /// <summary>
    /// Verifies a PKCE code challenge against a code verifier.
    /// </summary>
    /// <param name="codeVerifier">The code verifier to verify.</param>
    /// <param name="codeChallenge">The code challenge to verify against.</param>
    /// <returns>True if the code challenge is valid, false otherwise.</returns>
    public static bool VerifyCodeChallenge(string codeVerifier, string codeChallenge)
    {
        using var sha256 = SHA256.Create();
        var challengeBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(codeVerifier));
        var computedChallenge = WebEncoders.Base64UrlEncode(challengeBytes);

        return computedChallenge == codeChallenge;
    }

    /// <summary>
    /// Clears all persisted OAuth data.
    /// </summary>
    public void ClearPersistedData()
    {
        _persistenceManager.ClearPersistedData();
        
        // Clear in-memory data for dynamic clients only (keep demo clients)
        var keysToRemove = _clients.Keys.Where(k => k.StartsWith("dyn-")).ToList();
        foreach (var key in keysToRemove)
        {
            _clients.TryRemove(key, out _);
        }
        
        // Clear tokens and auth codes
        _tokens.Clear();
        _authCodes.Clear();
        
        // Reset test flags
        HasIssuedExpiredToken = false;
        HasIssuedRefreshToken = false;
    }

    /// <summary>
    /// Gets information about the persistent storage.
    /// </summary>
    /// <returns>Dictionary containing storage information.</returns>
    public Dictionary<string, object> GetPersistenceInfo()
    {
        var info = _persistenceManager.GetStorageInfo();
        info["InMemoryTokenCount"] = _tokens.Count;
        info["InMemoryAuthCodeCount"] = _authCodes.Count;
        info["InMemoryDynamicClientCount"] = _clients.Count(kvp => kvp.Key.StartsWith("dyn-"));
        return info;
    }
}
