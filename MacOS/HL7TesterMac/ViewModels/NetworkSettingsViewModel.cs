using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using HL7Tester.Core;
using Microsoft.Extensions.Logging;

namespace HL7TesterMac.ViewModels;

/// <summary>
/// ViewModel pour la configuration réseau (IP/Port + historique) côté MAUI.
/// </summary>
public sealed class NetworkSettingsViewModel : INotifyPropertyChanged
{
    private readonly INetworkSettingsService _service;
    private NetworkSettings _settings = new();
    private readonly ILogger<NetworkSettingsViewModel> _logger;

    public event PropertyChangedEventHandler? PropertyChanged;

    private string _ipAddress = string.Empty;
    public string IpAddress
    {
        get => _ipAddress;
        set => SetField(ref _ipAddress, value);
    }

    private string _port = string.Empty;
    public string Port
    {
        get => _port;
        set => SetField(ref _port, value);
    }

    public ObservableCollection<string> HistoryEntries { get; } = new();

    public ObservableCollection<string> AvailableLogLevels { get; } =
        new(["Debug", "Information", "Warning"]);

    private string _selectedLogLevel = "Debug";
    public string SelectedLogLevel
    {
        get => _selectedLogLevel;
        set => SetField(ref _selectedLogLevel, value);
    }

    public ICommand SaveCommand { get; }
    public ICommand SelectHistoryEntryCommand { get; }

    public NetworkSettingsViewModel(INetworkSettingsService service, ILogger<NetworkSettingsViewModel> logger)
    {
		_service = service;
		_logger = logger;
        SaveCommand = new Command(async () => await SaveAsync());
        SelectHistoryEntryCommand = new Command<string>(OnSelectHistoryEntry);
    }

    public async Task LoadAsync()
    {
        _settings = await _service.LoadAsync();

        IpAddress = _settings.LastIpAddress ?? string.Empty;
        Port = _settings.LastPort ?? string.Empty;

        SelectedLogLevel = string.IsNullOrWhiteSpace(_settings.LogLevel)
            ? "Debug"
            : _settings.LogLevel;

        HistoryEntries.Clear();
        foreach (var entry in _settings.History)
        {
            if (!string.IsNullOrWhiteSpace(entry.Ip) || !string.IsNullOrWhiteSpace(entry.Port))
            {
                HistoryEntries.Add($"{entry.Ip}:{entry.Port}");
            }
        }
    }

    private async Task SaveAsync()
    {
		var oldIp = _settings.LastIpAddress ?? string.Empty;
		var oldPort = _settings.LastPort ?? string.Empty;
		var newIp = IpAddress ?? string.Empty;
		var newPort = Port ?? string.Empty;
		var oldLogLevel = _settings.LogLevel ?? "Debug";
		var newLogLevel = SelectedLogLevel ?? "Debug";

        _settings.LogLevel = newLogLevel;

        _service.AddToHistory(_settings, newIp, newPort);
        await _service.SaveAsync(_settings);
        await LoadAsync();

		if (!string.Equals(oldIp, newIp, StringComparison.OrdinalIgnoreCase) ||
		    !string.Equals(oldPort, newPort, StringComparison.OrdinalIgnoreCase))
		{
			_logger.LogInformation("IP changed from {OldIp}:{OldPort} to {NewIp}:{NewPort}.", oldIp, oldPort, newIp, newPort);
			_logger.LogDebug("Network settings updated. Previous endpoint {OldIp}:{OldPort}, new endpoint {NewIp}:{NewPort}.", oldIp, oldPort, newIp, newPort);
		}

		if (!string.Equals(oldLogLevel, newLogLevel, StringComparison.OrdinalIgnoreCase))
		{
			_logger.LogInformation("Log level changed from {OldLevel} to {NewLevel}.", oldLogLevel, newLogLevel);
		}
    }

    private void OnSelectHistoryEntry(string? entry)
    {
        if (string.IsNullOrWhiteSpace(entry))
        {
            return;
        }

        // Format attendu: "IP:Port" (comme rempli dans LoadAsync)
        var parts = entry.Split(':', 2);
        if (parts.Length == 2)
        {
            IpAddress = parts[0];
            Port = parts[1];
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        field = value!;
        OnPropertyChanged(propertyName);
        return true;
    }
}
