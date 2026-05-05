using HL7Tester.ViewModels;
using HL7Tester.Services;

namespace HL7Tester;

public partial class Hl7InspectorPage : ContentPage
{
    private readonly Hl7InspectorViewModel _viewModel;

    public Hl7InspectorPage(Hl7InspectorViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;

        // Check if there's a pending message from MainPage
        var pendingMessage = Hl7InspectorViewModel.PendingParsedMessage;
        if (!string.IsNullOrEmpty(pendingMessage))
        {
            _viewModel.RawMessage = pendingMessage;
            Hl7InspectorViewModel.PendingParsedMessage = null!;
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Check for pending message from MainPage via static property (most reliable)
        var pendingMessage = Hl7InspectorViewModel.PendingParsedMessage;
        if (!string.IsNullOrEmpty(pendingMessage))
        {
            _viewModel.RawMessage = pendingMessage;
            _viewModel.ParseMessage();
            Hl7InspectorViewModel.PendingParsedMessage = null!;
        }
    }

    private void OnParseClicked(object? sender, EventArgs e)
    {
        // Read directly from the Editor control to avoid binding timing issues
        var rawText = MessageEditor?.Text ?? string.Empty;
        
        // Set RawMessage before parsing so the ViewModel sees the text
        _viewModel.RawMessage = rawText;
        _viewModel.ParseMessage();
    }

    private async void OnSendParsedClicked(object? sender, EventArgs e)
    {
        var rawText = MessageEditor?.Text;
        if (string.IsNullOrWhiteSpace(rawText))
        {
            await DisplayAlertAsync("Warning", "No message to send.", "OK");
            return;
        }

        // Store the raw HL7 message so MainPage can pick it up
        Hl7InspectorViewModel.PendingParsedMessage = rawText;
        await Shell.Current.GoToAsync("//MainPage");
    }

    private async void OnHomeClicked(object? sender, EventArgs e)
    {
        // Clear pending message when navigating via Home button
        Hl7InspectorViewModel.PendingParsedMessage = null;
        await Shell.Current.GoToAsync("//MainPage");
    }

    private async void OnSettingsClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//NetworkSettingsPage");
    }

    private async void OnRawMessageFileDropped(object? sender, DropEventArgs e)
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
            MessageEditor.Text = content;
            _viewModel.RawMessage = content;
            _viewModel.ParseMessage();

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
