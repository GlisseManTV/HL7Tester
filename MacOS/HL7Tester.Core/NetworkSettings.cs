using System.Text.Json;

namespace HL7Tester.Core;

/// <summary>
/// Représente la configuration réseau (IP/port + historique des connexions).
/// </summary>
public sealed class NetworkSettings
{
    public string? LastIpAddress { get; set; }
    public string? LastPort { get; set; }

    /// <summary>
    /// Niveau de log souhaité ("Debug", "Information", "Warning").
    /// Si null ou vide, le niveau par défaut (Debug) sera utilisé.
    /// </summary>
    public string? LogLevel { get; set; }

    /// <summary>
    /// Historique des dernières connexions IP:Port (dernier en premier).
    /// </summary>
    public List<ConnectionHistoryEntry> History { get; set; } = new();
}

public sealed class ConnectionHistoryEntry
{
    public string Ip { get; set; } = string.Empty;
    public string Port { get; set; } = string.Empty;
}

/// <summary>
/// Service de gestion des paramètres réseau (chargement/sauvegarde + historique).
/// L'implémentation par défaut persiste dans un fichier JSON dont le chemin est fourni par l'appelant.
/// </summary>
public interface INetworkSettingsService
{
    Task<NetworkSettings> LoadAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(NetworkSettings settings, CancellationToken cancellationToken = default);

    /// <summary>
    /// Met à jour LastIp/LastPort et l'historique (liste bornée, sans doublons consécutifs).
    /// </summary>
    void AddToHistory(NetworkSettings settings, string ip, string port, int maxEntries = 10);
}

public sealed class FileNetworkSettingsService : INetworkSettingsService
{
    private readonly string _filePath;

    public FileNetworkSettingsService(string filePath)
    {
        _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
    }

    public async Task<NetworkSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_filePath))
        {
            return new NetworkSettings();
        }

        await using var stream = File.OpenRead(_filePath);
        var settings = await JsonSerializer.DeserializeAsync<NetworkSettings>(stream, cancellationToken: cancellationToken)
                       .ConfigureAwait(false);

        return settings ?? new NetworkSettings();
    }

    public async Task SaveAsync(NetworkSettings settings, CancellationToken cancellationToken = default)
    {
        if (settings is null) throw new ArgumentNullException(nameof(settings));

        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var stream = File.Create(_filePath);
        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        await JsonSerializer.SerializeAsync(stream, settings, options, cancellationToken)
                          .ConfigureAwait(false);
    }

    public void AddToHistory(NetworkSettings settings, string ip, string port, int maxEntries = 10)
    {
        if (settings is null) throw new ArgumentNullException(nameof(settings));

        ip ??= string.Empty;
        port ??= string.Empty;

        settings.LastIpAddress = ip;
        settings.LastPort = port;

        if (string.IsNullOrWhiteSpace(ip) && string.IsNullOrWhiteSpace(port))
        {
            return;
        }

        // Supprimer une éventuelle entrée identique en tête pour éviter les doublons consécutifs.
        if (settings.History.Count > 0 &&
            string.Equals(settings.History[0].Ip, ip, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(settings.History[0].Port, port, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        settings.History.Insert(0, new ConnectionHistoryEntry { Ip = ip, Port = port });

        if (settings.History.Count > maxEntries)
        {
            settings.History.RemoveRange(maxEntries, settings.History.Count - maxEntries);
        }
    }
}
