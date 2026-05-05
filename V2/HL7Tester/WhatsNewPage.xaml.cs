using HL7Tester.Core;
using HL7Tester.Services;
using System.Text.RegularExpressions;

namespace HL7Tester;

/// <summary>
/// Modal page showing "What's New" release notes for the current version.
/// </summary>
public partial class WhatsNewPage : ContentPage
{
    private readonly IWhatsNewService _whatsNewService;
    private readonly INetworkSettingsService _settingsService;
    private NetworkSettings _settings = new();
    private readonly string _currentVersion;

    public WhatsNewPage(IWhatsNewService whatsNewService, INetworkSettingsService settingsService, string currentVersion)
    {
        InitializeComponent();
        _whatsNewService = whatsNewService;
        _settingsService = settingsService;
        _currentVersion = currentVersion;
        _settings = new NetworkSettings();

        VersionLabel.Text = $"v{currentVersion}";
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Load existing settings to avoid overwriting them.
        _settings = await _settingsService.LoadAsync();

        var content = await _whatsNewService.LoadContentAsync();
        if (!string.IsNullOrWhiteSpace(content))
        {
            ContentLabel.FormattedText = BuildFormattedReleaseNotes(content);
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
            await _settingsService.SaveAsync(_settings);
        }
        catch (Exception ex)
        {
            // Non-critical — don't block the user.
            System.Diagnostics.Debug.WriteLine($"[WhatsNew] Failed to save settings: {ex.Message}");
        }

        // Use the same Shell navigation pattern as the Home button and other pages.
        await Shell.Current.GoToAsync("//MainPage");
    }
    
    private static FormattedString BuildFormattedReleaseNotes(string markdown)
    {
        var formatted = new FormattedString();
        var lines = markdown.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();

            if (string.IsNullOrWhiteSpace(line))
            {
                formatted.Spans.Add(new Span { Text = Environment.NewLine });
                continue;
            }

            if (line == "---")
            {
                formatted.Spans.Add(new Span { Text = Environment.NewLine });
                continue;
            }

            if (line.StartsWith("### ", StringComparison.Ordinal))
            {
                formatted.Spans.Add(new Span
                {
                    Text = $"{line[4..]}{Environment.NewLine}",
                    FontSize = 18,
                    FontAttributes = FontAttributes.Bold,
                    FontFamily = "OpenSansSemibold"
                });
                continue;
            }

            if (line.StartsWith("## ", StringComparison.Ordinal))
            {
                formatted.Spans.Add(new Span
                {
                    Text = $"{line[3..]}{Environment.NewLine}{Environment.NewLine}",
                    FontSize = 22,
                    FontAttributes = FontAttributes.Bold,
                    FontFamily = "OpenSansSemibold"
                });
                continue;
            }

            if (line.StartsWith("- ", StringComparison.Ordinal))
            {
                AddInlineMarkdownSpans(formatted, line[2..], 15, "• ");
                formatted.Spans.Add(new Span { Text = Environment.NewLine });
                continue;
            }

            AddInlineMarkdownSpans(formatted, line, 16);
            formatted.Spans.Add(new Span { Text = Environment.NewLine });
        }

        return formatted;
    }

    private static void AddInlineMarkdownSpans(FormattedString formatted, string text, double fontSize, string prefix = "")
    {
        if (!string.IsNullOrEmpty(prefix))
        {
            formatted.Spans.Add(new Span
            {
                Text = prefix,
                FontSize = fontSize,
                FontAttributes = FontAttributes.Bold
            });
        }

        var cleanedText = Regex.Replace(text, @"`([^`]+)`", "$1");
        cleanedText = Regex.Replace(cleanedText, @"\[(.+?)\]\(.+?\)", "$1");

        var matches = Regex.Matches(cleanedText, @"\*\*(.+?)\*\*");
        var currentIndex = 0;

        foreach (Match match in matches)
        {
            if (match.Index > currentIndex)
            {
                formatted.Spans.Add(new Span
                {
                    Text = cleanedText[currentIndex..match.Index],
                    FontSize = fontSize
                });
            }

            formatted.Spans.Add(new Span
            {
                Text = match.Groups[1].Value,
                FontSize = fontSize,
                FontAttributes = FontAttributes.Bold,
                FontFamily = "OpenSansSemibold"
            });

            currentIndex = match.Index + match.Length;
        }

        if (currentIndex < cleanedText.Length)
        {
            formatted.Spans.Add(new Span
            {
                Text = cleanedText[currentIndex..],
                FontSize = fontSize
            });
        }
    }
}
