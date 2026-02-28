using System;
using HL7TesterMac.ViewModels;

namespace HL7TesterMac;

public partial class NetworkSettingsPage : ContentPage
{
    private readonly NetworkSettingsViewModel _viewModel;

    public NetworkSettingsPage(NetworkSettingsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAsync();
    }

    private async void OnHomeClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//MainPage");
    }
}
