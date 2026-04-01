using System.Text.Json;

namespace HL7Tester.Core;

/// <summary>
/// Représente la configuration réseau (IP/port + nickname + historique des connexions).
/// </summary>
public sealed class NetworkSettings
{
    public string? LastIpAddress { get; set; }
    public string? LastPort { get; set; }
    public string? Nickname { get; set; }

    /// <summary>
    /// Indique si l'application doit vérifier automatiquement la présence
    /// d'une nouvelle version au démarrage.
    /// Par défaut à <c>false</c> pour éviter des appels réseau implicites
    /// sur des environnements sans accès Internet.
    /// </summary>
    public bool AutoUpdateCheck { get; set; } = false;

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
    public string? Nickname { get; set; }
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
    void AddToHistory(NetworkSettings settings, string ip, string port, string? nickname = null, int maxEntries = int.MaxValue);
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

    public void AddToHistory(NetworkSettings settings, string ip, string port, string? nickname = null, int maxEntries = int.MaxValue)
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

        // Formater le nickname pour éviter les espaces superflus
        var formattedNickname = string.IsNullOrWhiteSpace(nickname) ? null : nickname.Trim();

        // Vérifier si l'entrée IP:Port existe déjà (sans tenir compte du nickname)
        var existingEntryIndex = settings.History.FindIndex(e => 
            string.Equals(e.Ip, ip, StringComparison.OrdinalIgnoreCase) && 
            string.Equals(e.Port, port, StringComparison.OrdinalIgnoreCase));

        if (existingEntryIndex >= 0)
        {
            // Entrée existe déjà, la supprimer et l'ajouter en tête avec le nouveau nickname
            settings.History.RemoveAt(existingEntryIndex);
            
            // Garder le nickname existant si pas de nouveau fourni
            if (string.IsNullOrEmpty(formattedNickname))
            {
                formattedNickname = settings.History.ElementAtOrDefault(existingEntryIndex)?.Nickname;
            }
        }

        settings.History.Insert(0, new ConnectionHistoryEntry 
        { 
            Ip = ip, 
            Port = port,
            Nickname = formattedNickname
        });

        // Limiter la taille de l'historique si nécessaire (par défaut: illimité)
        if (settings.History.Count > maxEntries && maxEntries > 0)
        {
            settings.History.RemoveRange(maxEntries, settings.History.Count - maxEntries);
        }
    }
}
