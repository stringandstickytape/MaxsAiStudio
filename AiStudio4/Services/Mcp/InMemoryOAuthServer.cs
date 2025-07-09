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
            _listener.Prefixes.Add($"https://localhost:{_port}/");
            
            try
            {
                _listener.Start();
                _logger?.LogInformation($"OAuth server started on https://localhost:{_port}/");
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
                context.Response.Headers.Add("Access-Control-Allow-Origin", "*");
                context.Response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
                context.Response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Authorization");
                
                if (context.Request.HttpMethod == "OPTIONS")
                {
                    context.Response.StatusCode = 200;
                    context.Response.Close();
                    return;
                }
                
                var path = context.Request.Url?.AbsolutePath;
                
                switch (path)
                {
                    case "/.well-known/openid_configuration":
                        await HandleOpenIdConfiguration(context);
                        break;
                    case "/.well-known/jwks":
                        await HandleJwks(context);
                        break;
                    case "/.well-known/oauth-protected-resource":
                        await HandleProtectedResourceMetadata(context);
                        break;
                    case "/token":
                        await HandleTokenRequest(context);
                        break;
                    default:
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
                response_types_supported = new[] { "code" },
                subject_types_supported = new[] { "public" },
                id_token_signing_alg_values_supported = new[] { "RS256" },
                scopes_supported = new[] { "openid", "mcp:tools" },
                token_endpoint_auth_methods_supported = new[] { "client_secret_post", "client_secret_basic" },
                claims_supported = new[] { "sub", "name", "preferred_username", "email" }
            };
            
            await WriteJsonResponse(context, config);
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
        
        private async Task HandleTokenRequest(HttpListenerContext context)
        {
            if (context.Request.HttpMethod != "POST")
            {
                context.Response.StatusCode = 405;
                await WriteJsonResponse(context, new { error = "invalid_request", error_description = "Only POST method supported" });
                return;
            }
            
            var form = await ReadFormData(context.Request);
            
            if (!form.TryGetValue("grant_type", out var grantType) || grantType != "client_credentials")
            {
                context.Response.StatusCode = 400;
                await WriteJsonResponse(context, new { error = "unsupported_grant_type", error_description = "Only client_credentials grant type supported" });
                return;
            }
            
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
        }
    }
}