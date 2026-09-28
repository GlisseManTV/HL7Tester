using HL7Tester.ViewModels;

namespace HL7Tester;

public partial class BatchSendPage : ContentPage
{
    public BatchSendPage(BatchSendViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    private void OnHomeClicked(object? sender, EventArgs e)
    {
        Shell.Current.GoToAsync("//MainPage");
    }
}
