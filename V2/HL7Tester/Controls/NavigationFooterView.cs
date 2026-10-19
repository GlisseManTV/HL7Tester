namespace HL7Tester.Controls;

/// <summary>
/// Identifies the page a <see cref="NavigationFooterView"/> is placed on.
/// The button matching the current page is hidden, the others stay visible.
/// </summary>
public enum FooterPage
{
    Home,
    Settings,
    Connections,
    Other
}

/// <summary>
/// Reusable navigation footer (Home / Settings / Connections) shared by the secondary pages.
/// The button matching <see cref="CurrentPage"/> is hidden; the remaining buttons navigate
/// through the Shell routes //MainPage, //NetworkSettingsPage and //ConnectionsPage.
/// Visuals match the previous per-page footers: padding 16,10, default button style and a
/// White/OffBlack theme-aware background (same palette as Colors.xaml).
/// </summary>
public class NavigationFooterView : ContentView
{
    public static readonly BindableProperty CurrentPageProperty =
        BindableProperty.Create(nameof(CurrentPage), typeof(FooterPage), typeof(NavigationFooterView),
            FooterPage.Other,
            propertyChanged: (b, _, _) => ((NavigationFooterView)b).UpdateButtonVisibility());

    public static readonly BindableProperty CenterTextProperty =
        BindableProperty.Create(nameof(CenterText), typeof(string), typeof(NavigationFooterView), null,
            propertyChanged: (b, _, n) => ((NavigationFooterView)b).UpdateCenterText((string?)n));

    /// <summary>Page the footer is rendered on; the matching button is hidden.</summary>
    public FooterPage CurrentPage
    {
        get => (FooterPage)GetValue(CurrentPageProperty);
        set => SetValue(CurrentPageProperty, value);
    }

    /// <summary>Optional centered label between the Home button and the right-side buttons.</summary>
    public string? CenterText
    {
        get => (string?)GetValue(CenterTextProperty);
        set => SetValue(CenterTextProperty, value);
    }

    /// <summary>Raised before navigating to Home so pages can clear pending state first.</summary>
    public event EventHandler? HomeClicked;

    private readonly Button _homeButton = new() { Text = "Home", HorizontalOptions = LayoutOptions.Start };
    private readonly Button _settingsButton = new() { Text = "Settings", HorizontalOptions = LayoutOptions.End };
    private readonly Button _connectionsButton = new() { Text = "Connections", HorizontalOptions = LayoutOptions.End };
    private readonly Label _centerLabel = new()
    {
        FontAttributes = FontAttributes.Bold,
        FontSize = 14,
        HorizontalOptions = LayoutOptions.Center,
        VerticalOptions = LayoutOptions.Center,
        IsVisible = false
    };

    public NavigationFooterView()
    {
        _homeButton.Clicked += OnHomeButtonClicked;
        _settingsButton.Clicked += OnSettingsButtonClicked;
        _connectionsButton.Clicked += OnConnectionsButtonClicked;

        _centerLabel.SetDynamicResource(Label.TextColorProperty, "Primary");

        var grid = new Grid
        {
            ColumnSpacing = 8,
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Auto)
            }
        };
        grid.Add(_homeButton, 0, 0);
        grid.Add(_centerLabel, 1, 0);
        grid.Add(_settingsButton, 2, 0);
        grid.Add(_connectionsButton, 3, 0);

        Content = grid;

        // Default page footer padding; instances embedded inside a page's own footer
        // Grid (FHIR pages) override it to 0 in XAML.
        Padding = new Thickness(16, 10);

        // Same theme palette as the previous footers: AppThemeBinding Light=White, Dark=OffBlack.
        this.SetAppThemeColor(BackgroundColorProperty, Colors.White, Color.FromArgb("#1F1F1F"));

        UpdateButtonVisibility();
    }

    private void UpdateCenterText(string? text)
    {
        _centerLabel.Text = text ?? string.Empty;
        _centerLabel.IsVisible = !string.IsNullOrEmpty(text);
    }

    private void UpdateButtonVisibility()
    {
        var page = CurrentPage;
        _homeButton.IsVisible = page != FooterPage.Home;
        _settingsButton.IsVisible = page != FooterPage.Settings;
        _connectionsButton.IsVisible = page != FooterPage.Connections;
    }

    private async void OnHomeButtonClicked(object? sender, EventArgs e)
    {
        HomeClicked?.Invoke(this, EventArgs.Empty);
        await Shell.Current.GoToAsync("//MainPage");
    }

    private async void OnSettingsButtonClicked(object? sender, EventArgs e)
        => await Shell.Current.GoToAsync("//NetworkSettingsPage");

    private async void OnConnectionsButtonClicked(object? sender, EventArgs e)
        => await Shell.Current.GoToAsync("//ConnectionsPage");
}
