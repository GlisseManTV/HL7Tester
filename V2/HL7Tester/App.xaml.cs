using HL7Tester.Core;
using HL7Tester.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices;

namespace HL7Tester;

public partial class App : Application
{
	private readonly ILogger<App> _logger;
	private readonly INetworkSettingsService _networkSettingsService;
	private readonly IUpdateChecker _updateChecker;
	private readonly IWhatsNewService _whatsNewService;

	public App(ILogger<App> logger, INetworkSettingsService networkSettingsService, IUpdateChecker updateChecker, IWhatsNewService whatsNewService)
	{
		InitializeComponent();
		_logger = logger;
		_networkSettingsService = networkSettingsService;
		_updateChecker = updateChecker;
		_whatsNewService = whatsNewService;

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

					var mainWindow = Application.Current?.Windows[0]?.Page;
					bool open = await mainWindow?.DisplayAlertAsync(
						"Update available",
						$"A new version v{result.LatestVersion} is available. You are currently using v{result.CurrentVersion}\n\nDo you want to open the releases page?",
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

	private async Task ShowWhatsNewPopupIfNeededAsync()
	{
		try
		{
			// Do NOT use ConfigureAwait(false) here — we need to stay on the main thread
			// so that MainThread.BeginInvokeOnMainThread works correctly on Windows.
			var settings = await _networkSettingsService.LoadAsync();
			var versionString = AppInfo.Current.VersionString;

			_logger.LogDebug("What's New check: InstalledVersion={Installed}, LastShown={LastShown}, Current={Current}",
				settings.InstalledVersion,
				settings.LastShownWhatNewVersion,
				versionString);

			// Show popup if the flag has never been set (first install) or if the version changed.
			if (string.IsNullOrEmpty(settings.LastShownWhatNewVersion) ||
				!string.Equals(settings.LastShownWhatNewVersion, versionString, StringComparison.Ordinal))
			{
				_logger.LogInformation("Showing What's New popup for version {Version}.", versionString);

				MainThread.BeginInvokeOnMainThread(async () =>
				{
					try
					{
						var page = new WhatsNewPage(_whatsNewService, _networkSettingsService, versionString);
						await Shell.Current.Navigation.PushModalAsync(page).ConfigureAwait(false);

						_logger.LogInformation("What's New popup dismissed for version {Version}.", versionString);
					}
					catch (Exception ex)
					{
						_logger.LogError(ex, "Error displaying What's New popup.");
					}
				});
			}
			else
			{
				_logger.LogDebug("What's New popup not shown (version {Version} already displayed).", versionString);
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Unexpected error during What's New check.");
		}
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		var window = new Window(new AppShell());
		// Delay What's New popup until the window and Shell are fully initialized.
		// Calling it from the constructor causes "Unable to find main thread" on Windows.
		window.Created += (s, e) => _ = ShowWhatsNewPopupIfNeededAsync();
		return window;
	}
}
