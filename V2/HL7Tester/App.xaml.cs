using HL7Tester.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices;

namespace HL7Tester;

public partial class App : Application
{
	private readonly ILogger<App> _logger;
	private readonly INetworkSettingsService _networkSettingsService;
	private readonly IUpdateChecker _updateChecker;

	public App(ILogger<App> logger, INetworkSettingsService networkSettingsService, IUpdateChecker updateChecker)
	{
		InitializeComponent();
		_logger = logger;
		_networkSettingsService = networkSettingsService;
		_updateChecker = updateChecker;

		LogStartupInformation();
		_ = CheckForUpdatesAsync();
	}

	private void LogStartupInformation()
	{
		try
		{
			var userName = Environment.UserName;
			var machineName = Environment.MachineName;
			var appVersion = AppInfo.Current.VersionString;
			var device = DeviceInfo.Current;

			NetworkSettings settings;
			try
			{
				// Chargement synchrone des paramètres réseau au démarrage uniquement pour le logging
				settings = _networkSettingsService.LoadAsync().GetAwaiter().GetResult();
			}
			catch (Exception ex)
			{
				_logger.LogWarning(ex, "Unable to load network settings at startup.");
				settings = new NetworkSettings();
			}

			var ip = settings.LastIpAddress ?? string.Empty;
			var port = settings.LastPort ?? string.Empty;

			_logger.LogDebug(
				"Application started. User={User}, Machine={Machine}, Device={Manufacturer} {Model} ({Platform}), AppVersion={Version}, ConfiguredEndpoint={Ip}:{Port}",
				userName,
				machineName,
				device.Manufacturer,
				device.Model,
				device.Platform,
				appVersion,
				ip,
				port);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error while logging application startup information.");
		}
	}

	private async Task CheckForUpdatesAsync()
	{
		try
		{
			// On charge les paramètres pour respecter le toggle AutoUpdateCheck.
			var settings = await _networkSettingsService.LoadAsync();
			if (!settings.AutoUpdateCheck)
			{
				_logger.LogInformation("Automatic update check is disabled by user settings.");
				return;
			}

			var versionString = AppInfo.Current.VersionString;
			if (!Version.TryParse(versionString, out var currentVersion))
			{
				// Fallback minimaliste en cas de format non standard.
				currentVersion = new Version(0, 0, 0, 0);
			}

			var result = await _updateChecker.CheckForUpdateAsync(currentVersion);
			if (!string.IsNullOrEmpty(result.ErrorMessage))
			{
				_logger.LogWarning("Update check error: {Error}", result.ErrorMessage);
				return;
			}

			if (result.IsUpdateAvailable)
			{
				_logger.LogInformation("A newer version is available: {Latest} (current={Current}).", result.LatestVersion, result.CurrentVersion);

				// On prévient l'utilisateur uniquement sur le thread UI.
				MainThread.BeginInvokeOnMainThread(async () =>
				{
					// Sur toutes les plateformes (Windows, macOS, ...),
					// on ouvre simplement la page des releases GitHub, sans
					// tenter de choisir un binaire spécifique (MSI, DMG, ...).
					var url = result.ReleasePageUrl;
					if (string.IsNullOrWhiteSpace(url))
					{
						url = "https://github.com/GlisseManTV/HL7Tester/releases/latest";
					}

					bool open = await Application.Current?.MainPage.DisplayAlert(
						"Update available",
						$"A new version v{result.LatestVersion} is available. You are currently using v{result.CurrentVersion}.\n\nDo you want to open the releases page?",
						"Yes",
						"No")!;

					if (open)
					{
						await Launcher.Default.OpenAsync(url);
					}
				});
			}
			else if (result.IsDevelopmentVersion)
			{
				_logger.LogInformation("Running a development version newer than the latest GitHub release: {Current} > {Latest}.", result.CurrentVersion, result.LatestVersion);
			}
			else
			{
				_logger.LogInformation("Application is up to date (version {Version}).", result.CurrentVersion);
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Unexpected error while checking for updates.");
		}
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(new AppShell());
	}
}