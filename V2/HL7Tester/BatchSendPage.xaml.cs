using System.Diagnostics;
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

    private void OnSettingsClicked(object? sender, EventArgs e)
    {
        Shell.Current.GoToAsync("//NetworkSettingsPage");
    }

    private void OnHomeClicked(object? sender, EventArgs e)
    {
        Shell.Current.GoToAsync("//MainPage");
    }

    private async void OnOpenLogsClicked(object? sender, EventArgs e)
    {
        try
        {
            var userFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var logDirectory = Path.Combine(userFolder, ".HL7Tester");

            if (DeviceInfo.Current.Platform == DevicePlatform.MacCatalyst)
            {
                if (!Directory.Exists(logDirectory))
                {
                    await DisplayAlertAsync("Logs", "The log folder does not exist yet. Perform some actions in the application to generate logs.", "OK");
                    return;
                }

                Process.Start("open", logDirectory);
                return;
            }

            var todayFileName = $"{DateTimeOffset.Now:yyyyMMdd}.log";
            var logFilePath = Path.Combine(logDirectory, todayFileName);

            if (!File.Exists(logFilePath))
            {
                await DisplayAlertAsync("Logs", "The log file for today does not exist yet. Perform some actions in the application to generate logs.", "OK");
                return;
            }

            await Launcher.Default.OpenAsync(new OpenFileRequest
            {
                File = new ReadOnlyFile(logFilePath)
            });
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Unable to open log file: {ex.Message}", "OK");
        }
    }
}
