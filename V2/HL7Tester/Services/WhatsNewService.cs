using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace HL7Tester.Services;

/// <summary>
/// Service responsible for loading the "What's New" release notes from embedded resources.
/// </summary>
public interface IWhatsNewService
{
    /// <summary>
    /// Loads the raw Markdown content of the "What's New" page.
    /// Returns an empty string if the resource file is not found.
    /// </summary>
    Task<string> LoadContentAsync();
}

/// <summary>
/// Implementation that reads whatsnew.md from the MAUI app package resources.
/// </summary>
public sealed class WhatsNewService : IWhatsNewService
{
    private readonly ILogger<WhatsNewService>? _logger;

    public WhatsNewService(ILogger<WhatsNewService>? logger = null)
    {
        _logger = logger;
    }

    public async Task<string> LoadContentAsync()
    {
        try
        {
            using var stream = await FileSystem.OpenAppPackageFileAsync("whatsnew.md").ConfigureAwait(false);
            using var reader = new StreamReader(stream);
            var content = await reader.ReadToEndAsync().ConfigureAwait(false);

            _logger?.LogDebug("What's New content loaded successfully ({Length} bytes).", content.Length);
            return content;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to load What's New content from embedded resource.");
            return string.Empty;
        }
    }
}