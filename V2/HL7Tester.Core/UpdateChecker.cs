using System.Net.Http;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace HL7Tester.Core;

/// <summary>
/// Résultat d'une vérification de mise à jour sur GitHub.
/// </summary>
public sealed class UpdateCheckResult
{
    public Version CurrentVersion { get; init; } = new(0, 0, 0, 0);
    public Version LatestVersion { get; init; } = new(0, 0, 0, 0);

    /// <summary>
    /// true si une version plus récente que <see cref="CurrentVersion"/> est disponible.
    /// </summary>
    public bool IsUpdateAvailable => LatestVersion > CurrentVersion;

    /// <summary>
    /// true si la version courante est strictement supérieure à la dernière release GitHub
    /// (cas typique d'une version de développement non encore publiée).
    /// </summary>
    public bool IsDevelopmentVersion => CurrentVersion > LatestVersion;

    /// <summary>
    /// URL directe de téléchargement (si disponible dans les assets de la release).
    /// </summary>
    public string? DownloadUrl { get; init; }

    /// <summary>
    /// URL de la page de release GitHub (fallback si aucun asset direct n'est trouvé).
    /// </summary>
    public string? ReleasePageUrl { get; init; }

    /// <summary>
    /// Message d'erreur éventuel (en cas d'échec de l'appel HTTP / parsing JSON).
    /// </summary>
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Service de vérification de mise à jour basé sur les releases GitHub.
/// La logique est indépendante de MAUI et de toute UI.
/// </summary>
public interface IUpdateChecker
{
    /// <summary>
    /// Vérifie la présence d'une version plus récente que <paramref name="currentVersion"/>
    /// dans les releases GitHub du projet HL7Tester.
    /// </summary>
    Task<UpdateCheckResult> CheckForUpdateAsync(Version currentVersion, CancellationToken cancellationToken = default);
}

/// <summary>
/// Implémentation par défaut utilisant l'API GitHub <c>/releases/latest</c>.
/// </summary>
public sealed class GitHubUpdateChecker : IUpdateChecker
{
    // Même URL que dans l'application WinForms historique.
    private const string GitHubApiUrl = "https://api.github.com/repos/GlisseManTV/HL7Tester/releases/latest";

    private readonly ILogger<GitHubUpdateChecker> _logger;
    private readonly HttpClient _httpClient;

    public GitHubUpdateChecker(ILogger<GitHubUpdateChecker>? logger = null, HttpClient? httpClient = null)
    {
        _logger = logger ?? NullLogger<GitHubUpdateChecker>.Instance;
        _httpClient = httpClient ?? new HttpClient();
    }

    public async Task<UpdateCheckResult> CheckForUpdateAsync(Version currentVersion, CancellationToken cancellationToken = default)
    {
        if (currentVersion is null) throw new ArgumentNullException(nameof(currentVersion));

        try
        {
            _logger.LogInformation("Checking for updates on GitHub (current version = {Version})", currentVersion);

            using var request = new HttpRequestMessage(HttpMethod.Get, GitHubApiUrl);
            // GitHub requiert un User-Agent non vide.
            request.Headers.UserAgent.ParseAdd("HL7Tester-MAUI");

            using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            using var doc = await JsonDocument.ParseAsync(responseStream, cancellationToken: cancellationToken).ConfigureAwait(false);

            var root = doc.RootElement;

            var tagName = root.GetProperty("tag_name").GetString() ?? string.Empty;
            var releaseUrl = root.GetProperty("html_url").GetString();

            // Les tags sont typiquement de la forme "v1.2.3" -> on enlève le préfixe éventuel.
            var latestVersionString = tagName.TrimStart('v', 'V');

            if (!Version.TryParse(latestVersionString, out var latestVersion))
            {
                _logger.LogWarning("Unable to parse latest version from GitHub tag '{TagName}'.", tagName);
                latestVersion = currentVersion;
            }

            string? downloadUrl = null;
            if (root.TryGetProperty("assets", out var assetsElement) && assetsElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var asset in assetsElement.EnumerateArray())
                {
                    if (!asset.TryGetProperty("browser_download_url", out var urlProperty))
                    {
                        continue;
                    }

                    var url = urlProperty.GetString();
                    if (!string.IsNullOrWhiteSpace(url))
                    {
                        downloadUrl = url;
                        break;
                    }
                }
            }

            var result = new UpdateCheckResult
            {
                CurrentVersion = currentVersion,
                LatestVersion = latestVersion,
                DownloadUrl = downloadUrl,
                ReleasePageUrl = releaseUrl
            };

            _logger.LogInformation(
                "Update check completed. Current={Current}, Latest={Latest}, IsUpdateAvailable={IsUpdate}, IsDev={IsDev}",
                result.CurrentVersion,
                result.LatestVersion,
                result.IsUpdateAvailable,
                result.IsDevelopmentVersion);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Update check failed.");
            return new UpdateCheckResult
            {
                CurrentVersion = currentVersion,
                LatestVersion = currentVersion,
                ErrorMessage = ex.Message
            };
        }
    }
}
