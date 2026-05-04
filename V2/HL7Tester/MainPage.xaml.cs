using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using HL7Tester.Core;
using HL7Tester.ViewModels;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Storage;

namespace HL7Tester;

public partial class MainPage : ContentPage
{
	public MainPage(MainViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();

		// Check if there's a pending message from the inspector
		var pendingMessage = Hl7InspectorViewModel.PendingParsedMessage;
		if (!string.IsNullOrEmpty(pendingMessage))
		{
			var viewModel = BindingContext as MainViewModel;
			viewModel!.GeneratedMessage = pendingMessage;
			Hl7InspectorViewModel.PendingParsedMessage = null!;
		}
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
					await DisplayAlertAsync("Logs", "The log folder does not exist yet. Perform some actions in the application to generate logs.", "OK");
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

	private async void OnCopyMessageClicked(object? sender, EventArgs e)
	{
		try
		{
			var viewModel = BindingContext as MainViewModel;
			if (viewModel?.GeneratedMessage != null)
			{
				await Clipboard.SetTextAsync(viewModel.GeneratedMessage);
			}
		}
		catch (Exception ex)
		{
			await DisplayAlertAsync("Error", $"Unable to copy message: {ex.Message}", "OK");
		}
	}

	private async void OnInspectMessageClicked(object? sender, EventArgs e)
	{
		try
		{
			var viewModel = BindingContext as MainViewModel;
			if (viewModel?.GeneratedMessage != null && !string.IsNullOrWhiteSpace(viewModel.GeneratedMessage))
			{
				Hl7InspectorViewModel.PendingParsedMessage = viewModel.GeneratedMessage;
				await Shell.Current.GoToAsync("//Hl7InspectorPage");
			}
			else
			{
				await DisplayAlertAsync("Warning", "No message to inspect. Generate a message first.", "OK");
			}
		}
		catch (Exception ex)
		{
			await DisplayAlertAsync("Error", $"Unable to open inspector: {ex.Message}", "OK");
		}
	}

	private void OnSendLogHeaderTapped(object? sender, TappedEventArgs e)
	{
		var viewModel = BindingContext as MainViewModel;
		if (viewModel != null)
		{
			viewModel.ToggleSendLog();
		}
	}
}