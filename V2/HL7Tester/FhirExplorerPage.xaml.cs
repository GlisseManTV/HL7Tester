using HL7Tester.ViewModels;

namespace HL7Tester;

public partial class FhirExplorerPage : ContentPage
{
    private readonly FhirExplorerViewModel _viewModel;

    public FhirExplorerPage(FhirExplorerViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        BindingContext = _viewModel;
        await _viewModel.LoadAsync();
    }

    // ── Mode toggle ───────────────────────────────────────────────────────────

    private void OnSearchClicked(object? sender, EventArgs e)
        => _viewModel.IsSearchMode = true;

    private void OnReadClicked(object? sender, EventArgs e)
        => _viewModel.IsReadMode = true;

    private void OnCapabilitiesClicked(object? sender, EventArgs e)
        => _viewModel.IsCapabilitiesMode = true;

    // ── Navigation ────────────────────────────────────────────────────────────

    private async void OnHomeClicked(object? sender, EventArgs e)
        => await Shell.Current.GoToAsync("//MainPage");

    private async void OnSettingsClicked(object? sender, EventArgs e)
        => await Shell.Current.GoToAsync("//NetworkSettingsPage");
}
