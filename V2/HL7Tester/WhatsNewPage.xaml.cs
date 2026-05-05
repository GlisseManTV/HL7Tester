using HL7Tester.Core;
using HL7Tester.Services;

namespace HL7Tester;

/// <summary>
/// Modal page showing "What's New" release notes for the current version.
/// </summary>
public partial class WhatsNewPage : ContentPage
{
    private readonly IWhatsNewService _whatsNewService;
    private readonly INetworkSettingsService _settingsService;
    private readonly NetworkSettings _settings;
    private readonly string _currentVersion;

    public WhatsNewPage(IWhatsNewService whatsNewService, INetworkSettingsService settingsService, string currentVersion)
    {
        InitializeComponent();
        _whatsNewService = whatsNewService;
        _settingsService = settingsService;
        _settings = new NetworkSettings();
        _currentVersion = currentVersion;

        VersionLabel.Text = $"v{currentVersion}";
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        var content = await _whatsNewService.LoadContentAsync().ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(content))
        {
            // Strip basic markdown for display (headings, bold, etc.)
            ContentLabel.Text = StripMarkdown(content);
        }
        else
        {
            ContentLabel.Text = "No release notes available.";
        }
    }

    private async void OnOkClicked(object? sender, EventArgs e)
    {
        // Save the current version as the last shown What's New version.
        _settings.InstalledVersion = _currentVersion;
        _settings.LastShownWhatNewVersion = _currentVersion;

        try
        {
            await _settingsService.SaveAsync(_settings).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // Non-critical — don't block the user.
            System.Diagnostics.Debug.WriteLine($"[WhatsNew] Failed to save settings: {ex.Message}");
        }

        // Dismiss the modal.
        await Shell.Current.Navigation.PopModalAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Basic markdown stripping for display in a Label (no rich text support).
    /// </summary>
    private static string StripMarkdown(string md)
    {
        if (string.IsNullOrEmpty(md))
            return md;

        var result = md;

        // Remove heading markers
        result = System.Text.RegularExpressions.Regex.Replace(result, @"^#{1,6}\s+", string.Empty, System.Text.RegularExpressions.RegexOptions.Multiline);

        // Remove bold markers
        result = System.Text.RegularExpressions.Regex.Replace(result, @"\*\*(.+?)\*\*", "$1");
        result = System.Text.RegularExpressions.Regex.Replace(result, @"__(.+?)__", "$1");

        // Remove italic markers
        result = System.Text.RegularExpressions.Regex.Replace(result, @"\*(.+?)\*", "$1");
        result = System.Text.RegularExpressions.Regex.Replace(result, @"_(.+?)_", "$1");

        // Remove bullet points
        result = System.Text.RegularExpressions.Regex.Replace(result, @"^\s*[-*+]\s+", "  - ", System.Text.RegularExpressions.RegexOptions.Multiline);

        // Remove link syntax, keep text
        result = System.Text.RegularExpressions.Regex.Replace(result, @"\[(.+?)\]\(.+?\)", "$1");

        // Remove code block markers
        result = System.Text.RegularExpressions.Regex.Replace(result, @"^```[\w]*$\n?", string.Empty, System.Text.RegularExpressions.RegexOptions.Multiline);
        result = System.Text.RegularExpressions.Regex.Replace(result, @"`([^`]+)`", "$1");

        return result.Trim();
    }
}
