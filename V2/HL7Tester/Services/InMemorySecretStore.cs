using System.Collections.Concurrent;
using HL7Tester.Core.Security;

namespace HL7Tester.Services;

/// <summary>
/// Temporary in-memory <see cref="ISecretStore"/> used so the build succeeds in
/// Phase 1. Secrets live only for the lifetime of the process and are NOT
/// persisted. This is a placeholder: it will be replaced by an OS-protected
/// implementation (DPAPI on Windows, Keychain on macOS) in Phase 3.
/// </summary>
public sealed class InMemorySecretStore : ISecretStore
{
    private readonly ConcurrentDictionary<string, string> _secrets = new();

    private static string BuildKey(string profileId, string key) => $"{profileId}::{key}";

    public Task<string?> GetSecretAsync(string profileId, string key, CancellationToken ct = default)
    {
        _secrets.TryGetValue(BuildKey(profileId, key), out var value);
        return Task.FromResult<string?>(value);
    }

    public Task SetSecretAsync(string profileId, string key, string value, CancellationToken ct = default)
    {
        _secrets[BuildKey(profileId, key)] = value;
        return Task.CompletedTask;
    }

    public Task DeleteSecretAsync(string profileId, string key, CancellationToken ct = default)
    {
        _secrets.TryRemove(BuildKey(profileId, key), out _);
        return Task.CompletedTask;
    }

    public bool SupportsOsSecureStorage => false;
}
