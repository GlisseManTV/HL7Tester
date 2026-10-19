namespace HL7Tester.Core.Security;

/// <summary>
/// Abstraction for storing and retrieving confidential values (tokens, passwords,
/// client secrets) per FHIR profile. Implementations should persist these values
/// using OS-level secure storage (DPAPI on Windows, Keychain on macOS) whenever
/// available. Configuration values (URLs, endpoints, profile names) must NOT go
/// through this store — only secrets.
/// </summary>
public interface ISecretStore
{
    /// <summary>
    /// Returns the stored secret for the given profile/key, or <c>null</c> if none exists.
    /// </summary>
    Task<string?> GetSecretAsync(string profileId, string key, CancellationToken ct = default);

    /// <summary>
    /// Stores (or overwrites) a secret for the given profile/key.
    /// </summary>
    Task SetSecretAsync(string profileId, string key, string value, CancellationToken ct = default);

    /// <summary>
    /// Removes a previously stored secret. No-op if it does not exist.
    /// </summary>
    Task DeleteSecretAsync(string profileId, string key, CancellationToken ct = default);

    /// <summary>
    /// Indicates whether the current platform provides OS-protected secure storage.
    /// When false, secrets are held in memory only (non-persistent stub).
    /// </summary>
    bool SupportsOsSecureStorage { get; }
}
