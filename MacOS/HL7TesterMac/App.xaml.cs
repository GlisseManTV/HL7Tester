using HL7Tester.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices;

namespace HL7TesterMac;

public partial class App : Application
{
	private readonly ILogger<App> _logger;
	private readonly INetworkSettingsService _networkSettingsService;

	public App(ILogger<App> logger, INetworkSettingsService networkSettingsService)
	{
		InitializeComponent();
		_logger = logger;
		_networkSettingsService = networkSettingsService;

		LogStartupInformation();
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

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(new AppShell());
	}
}