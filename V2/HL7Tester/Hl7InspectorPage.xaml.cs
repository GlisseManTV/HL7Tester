using HL7Tester.ViewModels;

namespace HL7Tester;

public partial class Hl7InspectorPage : ContentPage
{
    private readonly Hl7InspectorViewModel _viewModel;

    public Hl7InspectorPage(Hl7InspectorViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    private void OnParseClicked(object? sender, EventArgs e)
    {
        // Read directly from the Editor control to avoid binding timing issues
        var rawText = MessageEditor?.Text ?? string.Empty;
        
        // Set RawMessage before parsing so the ViewModel sees the text
        _viewModel.RawMessage = rawText;
        _viewModel.ParseMessage();
    }

    private async void OnHomeClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//MainPage");
    }
}
