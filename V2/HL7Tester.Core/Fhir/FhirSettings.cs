namespace HL7Tester.Core.Fhir;

/// <summary>
/// A named FHIR server connection profile: base URL, authentication configuration,
/// default endpoint and transport options. Configuration values are stored in clear
/// text in networksettings.json; confidential values (tokens, passwords, client
/// secrets) are referenced via <see cref="SecretRef"/> and resolved through
/// <c>ISecretStore</c>.
/// </summary>
public sealed class FhirProfile
{
    /// <summary>Stable identifier used as the secret-store namespace.</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>User-facing display name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>FHIR server base URL (e.g. https://hapi.fhir.org/baseR4).</summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>Authentication configuration (type + parameters, no secret values).</summary>
    public FhirAuthConfig Auth { get; set; } = new();

    /// <summary>Default resource endpoint used when the user does not override it (e.g. "Patient").</summary>
    public string? DefaultEndpoint { get; set; }

    /// <summary>When false, TLS certificate validation is bypassed (self-signed servers).</summary>
    public bool TlsVerify { get; set; } = true;

    /// <summary>Request timeout in seconds.</summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>FHIR version advertised by the server (e.g. "4.0" for R4).</summary>
    public string FhirVersion { get; set; } = "4.0";

    /// <summary>
    /// Declares which secret keys this profile references. The actual secret values
    /// never live in this object — only references (see <see cref="SecretRef"/>).
    /// </summary>
    public List<SecretRef> Secrets { get; set; } = new();

    public override string ToString() =>
        string.IsNullOrWhiteSpace(Name) ? BaseUrl : Name;
}

/// <summary>
/// Authentication configuration for a <see cref="FhirProfile"/>. Holds the auth type
/// and non-secret parameters. Secret values (client secret, password, token) are
/// stored separately via ISecretStore and referenced by key.
/// </summary>
public sealed class FhirAuthConfig
{
    public FhirAuthType Type { get; set; } = FhirAuthType.None;

    // ---- OAuth2 ----
    public string? TokenUrl { get; set; }
    public string? AuthorizeUrl { get; set; }
    public string? ClientId { get; set; }
    public string? RedirectUri { get; set; }
    public string? Scope { get; set; }

    // ---- API key ----
    public string? HeaderName { get; set; }

    // ---- Basic auth ----
    public string? Username { get; set; }

    // ---- mTLS ----
    public string? ClientCertPath { get; set; }
}

/// <summary>
/// A reference to a secret owned by a profile. The value is fetched from ISecretStore
/// using (ProfileId, Key).
/// </summary>
public sealed class SecretRef
{
    public string ProfileId { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
}

/// <summary>
/// Supported FHIR authentication mechanisms.
/// </summary>
public enum FhirAuthType
{
    None,
    BearerToken,
    BasicAuth,
    OAuth2ClientCredentials,
    OAuth2AuthorizationCodePkce,
    ApiKey,
    Mtls
}
