using HL7Tester.ViewModels;

namespace HL7Tester;

public partial class BatchSendPage : ContentPage
{
    private readonly BatchSendViewModel _viewModel;

    public BatchSendPage(BatchSendViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = _viewModel.LoadTemplatesAsync();
    }

    private void OnHomeClicked(object? sender, EventArgs e)
    {
        Shell.Current.GoToAsync("//MainPage");
    }
}
