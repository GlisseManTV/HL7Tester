using System;
using System.Diagnostics;
using System.IO;
using HL7TesterMac.ViewModels;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Storage;

namespace HL7TesterMac;

public partial class MainPage : ContentPage
{
	public MainPage(MainViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}

	private async void OnSettingsClicked(object? sender, EventArgs e)
	{
		await Shell.Current.GoToAsync("//NetworkSettingsPage");
	}

	private async void OnOpenLogsClicked(object? sender, EventArgs e)
	{
		try
		{
			var userFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
			var logDirectory = Path.Combine(userFolder, ".HL7Tester");

			// Comportement spécifique macOS : ouvrir le dossier dans le Finder
			if (DeviceInfo.Current.Platform == DevicePlatform.MacCatalyst)
			{
				if (!Directory.Exists(logDirectory))
				{
					await DisplayAlert("Logs", "The log folder does not exist yet. Perform some actions in the application to generate logs.", "OK");
					return;
				}

				Process.Start("open", logDirectory);
				return;
			}

			// Comportement par défaut (autres plateformes) : ouvrir le fichier du jour
			var todayFileName = $"{DateTimeOffset.Now:yyyyMMdd}.log";
			var logFilePath = Path.Combine(logDirectory, todayFileName);

			if (!File.Exists(logFilePath))
			{
				await DisplayAlert("Logs", "The log file for today does not exist yet. Perform some actions in the application to generate logs.", "OK");
				return;
			}

			await Launcher.Default.OpenAsync(new OpenFileRequest
			{
				File = new ReadOnlyFile(logFilePath)
			});
		}
		catch (Exception ex)
		{
			await DisplayAlert("Error", $"Unable to open log file: {ex.Message}", "OK");
		}
	}
}
