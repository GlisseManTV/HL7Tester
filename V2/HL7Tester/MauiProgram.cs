using System.IO;
using System.Text.Json;
using HL7Tester.Core;
using HL7Tester.ViewModels;
using HL7Tester.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Storage;

namespace HL7Tester;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		// Logging : sortie Debug + fichier texte dans le dossier utilisateur ~/.HL7Tester/ApplicationLogs.log
		var userFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
		var logDirectory = Path.Combine(userFolder, ".HL7Tester");

		// Déterminer le niveau de log minimal à partir des paramètres utilisateur (NetworkSettings)
		var settingsPath = Path.Combine(FileSystem.AppDataDirectory, "networksettings.json");
		var minLogLevel = LogLevel.Debug;
		try
		{
			if (File.Exists(settingsPath))
			{
				var json = File.ReadAllText(settingsPath);
				var settings = JsonSerializer.Deserialize<NetworkSettings>(json) ?? new NetworkSettings();
				var level = settings.LogLevel;

				if (string.Equals(level, "Information", StringComparison.OrdinalIgnoreCase))
					minLogLevel = LogLevel.Information;
				else if (string.Equals(level, "Warning", StringComparison.OrdinalIgnoreCase))
					minLogLevel = LogLevel.Warning;
				else
					minLogLevel = LogLevel.Debug;
			}
		}
		catch
		{
			// En cas de problème de lecture, on reste sur Debug par défaut.
		}

		builder.Logging.AddDebug();
		builder.Logging.AddProvider(new FileLoggerProvider(logDirectory, minLogLevel));

		// Injection de dépendances
		builder.Services.AddSingleton<AdtMessageGenerator>();
		builder.Services.AddSingleton<IHL7NetworkSender, Hl7NetworkSender>();
		builder.Services.AddSingleton<IUpdateChecker, GitHubUpdateChecker>();
		builder.Services.AddTransient<MainViewModel>();
		builder.Services.AddTransient<MainPage>();

		// Service de configuration réseau (fichier JSON dans le répertoire applicatif)
		builder.Services.AddSingleton<INetworkSettingsService>(sp =>
		{
			var path = Path.Combine(FileSystem.AppDataDirectory, "networksettings.json");
			return new FileNetworkSettingsService(path);
		});
		builder.Services.AddTransient<NetworkSettingsViewModel>();
		builder.Services.AddTransient<NetworkSettingsPage>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
