using HL7Tester.Services;
using HL7Tester.ViewModels;

namespace HL7Tester;

public partial class FhirConverterPage : ContentPage
{
    private readonly FhirConverterViewModel _viewModel;

    public FhirConverterPage(FhirConverterViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        BindingContext = _viewModel;
    }

    // ── Direction toggle ──────────────────────────────────────────────────────

    private void OnHl7DirectionClicked(object? sender, EventArgs e)
        => _viewModel.IsHl7ToFhir = true;

    private void OnFhirDirectionClicked(object? sender, EventArgs e)
        => _viewModel.IsHl7ToFhir = false;

    // ── Navigation ────────────────────────────────────────────────────────────

    private async void OnHomeClicked(object? sender, EventArgs e)
        => await Shell.Current.GoToAsync("//MainPage");

    private async void OnSettingsClicked(object? sender, EventArgs e)
        => await Shell.Current.GoToAsync("//NetworkSettingsPage");

    // ── Drag & drop import ────────────────────────────────────────────────────

    private async void OnInputFileDropped(object? sender, DropEventArgs e)
    {
        try
        {
            var result = await Hl7FileImportService.ImportDroppedContentAsync(e);
            if (!result.Success)
            {
                await DisplayAlertAsync("File import", result.ErrorMessage ?? "Unable to import the dropped file.", "OK");
                return;
            }

            var content = result.Content ?? string.Empty;
            InputEditor.Text = content;
            _viewModel.InputMessage = content;

            if (result.MultipleFilesDropped)
            {
                await DisplayAlertAsync("File import", $"Imported '{result.FileName}'. Additional dropped files were ignored.", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Unable to import dropped file: {ex.Message}", "OK");
        }
    }
}
