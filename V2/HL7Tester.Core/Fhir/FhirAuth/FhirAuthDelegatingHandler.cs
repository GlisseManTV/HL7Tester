using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using HL7Tester.Core.Security;

namespace HL7Tester.Core.Fhir.FhirAuth;

/// <summary>
/// DelegatingHandler that applies the authentication configured on a
/// <see cref="FhirProfile"/> to every outgoing request. Secret values are resolved
/// through <see cref="ISecretStore"/> using well-known keys:
///   BearerToken              -> "token"
///   BasicAuth                -> "password" (username lives on the profile)
///   OAuth2ClientCredentials  -> "client_secret" (client_id lives on the profile)
///   ApiKey                   -> "apikey" (header name lives on the profile)
/// For OAuth2 client-credentials the access token is fetched from the token
/// endpoint and cached in memory until shortly before its expiry.
/// </summary>
public sealed class FhirAuthDelegatingHandler : DelegatingHandler
{
    // Secret keys used against ISecretStore.
    public const string TokenKey = "token";
    public const string PasswordKey = "password";
    public const string ClientSecretKey = "client_secret";
    public const string ApiKeyKey = "apikey";

    private readonly FhirProfile _profile;
    private readonly ISecretStore _secretStore;

    // In-memory OAuth2 token cache.
    private string? _cachedAccessToken;
    private DateTime _cachedAccessTokenExpiresUtc = DateTime.MinValue;
    private readonly SemaphoreSlim _tokenLock = new(1, 1);

    public FhirAuthDelegatingHandler(FhirProfile profile, ISecretStore secretStore)
    {
        _profile = profile ?? throw new ArgumentNullException(nameof(profile));
        _secretStore = secretStore ?? throw new ArgumentNullException(nameof(secretStore));
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        await ApplyAuthAsync(request, ct).ConfigureAwait(false);
        return await base.SendAsync(request, ct).ConfigureAwait(false);
    }

    private async Task ApplyAuthAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var auth = _profile.Auth;
        switch (auth.Type)
        {
            case FhirAuthType.None:
                break;

            case FhirAuthType.BearerToken:
            {
                var token = await _secretStore.GetSecretAsync(_profile.Id, TokenKey, ct).ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(token))
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                break;
            }

            case FhirAuthType.BasicAuth:
            {
                var password = await _secretStore.GetSecretAsync(_profile.Id, PasswordKey, ct).ConfigureAwait(false) ?? string.Empty;
                var username = auth.Username ?? string.Empty;
                var raw = $"{username}:{password}";
                var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(raw));
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", encoded);
                break;
            }

            case FhirAuthType.OAuth2ClientCredentials:
            {
                var token = await GetOrRefreshClientCredentialsTokenAsync(ct).ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(token))
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                break;
            }

            case FhirAuthType.ApiKey:
            {
                var apiKey = await _secretStore.GetSecretAsync(_profile.Id, ApiKeyKey, ct).ConfigureAwait(false);
                var headerName = string.IsNullOrWhiteSpace(auth.HeaderName) ? "X-API-Key" : auth.HeaderName;
                if (!string.IsNullOrWhiteSpace(apiKey))
                    request.Headers.TryAddWithoutValidation(headerName, apiKey);
                break;
            }

            case FhirAuthType.OAuth2AuthorizationCodePkce:
            case FhirAuthType.Mtls:
                // Interactive PKCE login and mTLS client-certificate binding are
                // handled at the transport / UI level in later phases.
                break;
        }
    }

    private async Task<string?> GetOrRefreshClientCredentialsTokenAsync(CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(_cachedAccessToken) &&
            DateTime.UtcNow < _cachedAccessTokenExpiresUtc)
        {
            return _cachedAccessToken;
        }

        await _tokenLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            // Double-check after acquiring the lock.
            if (!string.IsNullOrWhiteSpace(_cachedAccessToken) &&
                DateTime.UtcNow < _cachedAccessTokenExpiresUtc)
            {
                return _cachedAccessToken;
            }

            var auth = _profile.Auth;
            if (string.IsNullOrWhiteSpace(auth.TokenUrl) || string.IsNullOrWhiteSpace(auth.ClientId))
                return null;

            var clientSecret = await _secretStore
                .GetSecretAsync(_profile.Id, ClientSecretKey, ct).ConfigureAwait(false) ?? string.Empty;

            var form = new List<KeyValuePair<string, string>>
            {
                new("grant_type", "client_credentials"),
                new("client_id", auth.ClientId),
                new("client_secret", clientSecret)
            };
            if (!string.IsNullOrWhiteSpace(auth.Scope))
                form.Add(new KeyValuePair<string, string>("scope", auth.Scope));

            using var tokenClient = new HttpClient();
            using var content = new FormUrlEncodedContent(form);
            using var response = await tokenClient
                .PostAsync(auth.TokenUrl, content, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var accessToken = root.TryGetProperty("access_token", out var at) ? at.GetString() : null;
            var expiresIn = root.TryGetProperty("expires_in", out var ei) && ei.TryGetInt32(out var secs)
                ? secs
                : 300;

            if (string.IsNullOrWhiteSpace(accessToken))
                return null;

            _cachedAccessToken = accessToken;
            // Refresh 30s before actual expiry to avoid edge-of-life failures.
            _cachedAccessTokenExpiresUtc = DateTime.UtcNow.AddSeconds(Math.Max(30, expiresIn - 30));
            return _cachedAccessToken;
        }
        finally
        {
            _tokenLock.Release();
        }
    }
}
