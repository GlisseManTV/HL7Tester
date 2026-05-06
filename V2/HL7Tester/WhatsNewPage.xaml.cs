using HL7Tester.Core;
using HL7Tester.Services;

namespace HL7Tester;

/// <summary>
/// Announcement-style "What's New" page shown to the user after an update.
/// Each ### section in whatsnew.md is rendered as a visual feature card.
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

        VersionLabel.Text = $"v{currentVersion}";
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Load existing settings to avoid overwriting them on OK.
        _settings = await _settingsService.LoadAsync();

        var content = await _whatsNewService.LoadContentAsync();

        FeatureCardsContainer.Children.Clear();

        if (!string.IsNullOrWhiteSpace(content))
        {
            var cards = BuildAnnouncementCards(content);
            foreach (var card in cards)
                FeatureCardsContainer.Children.Add(card);
        }
        else
        {
            FeatureCardsContainer.Children.Add(new Label
            {
                Text = "No release notes available.",
                FontSize = 15,
                TextColor = Colors.Gray
            });
        }
    }

    private async void OnOkClicked(object? sender, EventArgs e)
    {
        _settings.InstalledVersion = _currentVersion;
        _settings.LastShownWhatNewVersion = _currentVersion;

        try
        {
            await _settingsService.SaveAsync(_settings);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[WhatsNew] Failed to save settings: {ex.Message}");
        }

        await Shell.Current.GoToAsync("//MainPage");
    }

    /// <summary>
    /// Parses whatsnew.md and returns one Border card per ### feature section.
    /// Format expected:
    ///   ## vX.Y.Z          — version header (ignored in card rendering)
    ///   ### 📂 Title        — feature card title (emoji + text)
    ///   > Tagline sentence  — short user-facing benefit shown as subtitle
    ///   - Bullet point      — optional benefit bullet (max 4 recommended)
    /// </summary>
    private static List<View> BuildAnnouncementCards(string markdown)
    {
        var cards = new List<View>();
        var lines = markdown.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');

        string? currentEmoji = null;
        string? currentTitle = null;
        string? currentTagline = null;
        var currentBullets = new List<string>();

        void FlushCard()
        {
            if (currentTitle == null) return;
            cards.Add(CreateFeatureCard(currentEmoji ?? "✨", currentTitle, currentTagline, currentBullets));
            currentEmoji = null;
            currentTitle = null;
            currentTagline = null;
            currentBullets = new List<string>();
        }

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();

            // Skip version header and separators
            if (line.StartsWith("## ", StringComparison.Ordinal) || line == "---" || string.IsNullOrWhiteSpace(line))
                continue;

            // New feature section — flush previous card
            if (line.StartsWith("### ", StringComparison.Ordinal))
            {
                FlushCard();
                var titleRaw = line[4..].Trim();
                // Extract leading emoji (first "word" that is non-ASCII / emoji)
                var spaceIndex = titleRaw.IndexOf(' ');
                if (spaceIndex > 0)
                {
                    currentEmoji = titleRaw[..spaceIndex].Trim();
                    currentTitle = titleRaw[(spaceIndex + 1)..].Trim();
                }
                else
                {
                    currentEmoji = "✨";
                    currentTitle = titleRaw;
                }
                continue;
            }

            // Tagline (blockquote)
            if (line.StartsWith("> ", StringComparison.Ordinal))
            {
                currentTagline = line[2..].Trim();
                continue;
            }

            // Bullet point
            if (line.StartsWith("- ", StringComparison.Ordinal))
            {
                currentBullets.Add(line[2..].Trim());
                continue;
            }
        }

        FlushCard(); // flush last card
        return cards;
    }

    /// <summary>
    /// Creates a styled feature card Border with emoji, title, tagline and optional bullets.
    /// </summary>
    private static Border CreateFeatureCard(string emoji, string title, string? tagline, List<string> bullets)
    {
        // Emoji column
        var emojiLabel = new Label
        {
            Text = emoji,
            FontSize = 34,
            VerticalOptions = LayoutOptions.Start,
            Margin = new Thickness(0, 2, 16, 0)
        };

        // Title label
        var titleLabel = new Label
        {
            Text = title,
            FontSize = 20,
            FontFamily = "OpenSansSemibold",
            FontAttributes = FontAttributes.Bold,
            LineBreakMode = LineBreakMode.WordWrap,
            TextColor = Application.Current?.RequestedTheme == AppTheme.Dark
                ? Color.FromArgb("#FFFFFF")
                : Color.FromArgb("#190649") // MidnightBlue
        };

        var textStack = new VerticalStackLayout
        {
            Spacing = 7,
            Children = { titleLabel }
        };

        // Tagline (benefit sentence)
        if (!string.IsNullOrWhiteSpace(tagline))
        {
            textStack.Children.Add(new Label
            {
                Text = tagline,
                FontSize = 16,
                LineBreakMode = LineBreakMode.WordWrap,
                TextColor = Application.Current?.RequestedTheme == AppTheme.Dark
                    ? Color.FromArgb("#ACACAC") // Gray300
                    : Color.FromArgb("#6E6E6E")  // Gray500
            });
        }

        // Bullets
        foreach (var bullet in bullets)
        {
            textStack.Children.Add(new Label
            {
                Text = $"• {bullet}",
                FontSize = 15,
                LineBreakMode = LineBreakMode.WordWrap,
                TextColor = Application.Current?.RequestedTheme == AppTheme.Dark
                    ? Color.FromArgb("#C8C8C8") // Gray200
                    : Color.FromArgb("#404040")  // Gray600
            });
        }

        // Card layout: emoji left, text right
        var cardGrid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Star }
            }
        };

        cardGrid.Add(emojiLabel, 0, 0);
        cardGrid.Add(textStack, 1, 0);

        var card = new Border
        {
            Padding = new Thickness(16),
            Margin = new Thickness(0, 0),
            StrokeThickness = 1,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = new CornerRadius(12) },
            Background = Application.Current?.RequestedTheme == AppTheme.Dark
                ? new SolidColorBrush(Color.FromArgb("#212121")) // Gray900
                : new SolidColorBrush(Colors.White),
            Stroke = Application.Current?.RequestedTheme == AppTheme.Dark
                ? new SolidColorBrush(Color.FromArgb("#404040")) // Gray600
                : new SolidColorBrush(Color.FromArgb("#C8C8C8")), // Gray200
            Shadow = new Shadow
            {
                Radius = 8,
                Opacity = 0.08f,
                Offset = new Point(0, 2),
                Brush = new SolidColorBrush(Colors.Black)
            },
            Content = cardGrid
        };

        return card;
    }
}
