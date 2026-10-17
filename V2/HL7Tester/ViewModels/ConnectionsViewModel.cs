using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using HL7Tester.Core;
using Microsoft.Extensions.Logging;

namespace HL7Tester.ViewModels;

/// <summary>
/// ViewModel for the dedicated Connections page (IP/Port/Nickname + History).
/// Extracted from NetworkSettingsViewModel as part of Phase 0.5 refactoring.
/// </summary>
public sealed class ConnectionsViewModel : INotifyPropertyChanged
{
    private readonly INetworkSettingsService _service;
    private NetworkSettings _settings = new();
    private readonly ILogger<ConnectionsViewModel> _logger;

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

    private string _nickname = string.Empty;
    /// <summary>
    /// Familiar name for the IP address (optional).
    /// </summary>
    public string Nickname
    {
        get => _nickname;
        set => SetField(ref _nickname, value);
    }

    public ObservableCollection<HistoryEntryViewModel> HistoryEntries { get; } = new();

    public ICommand SaveConnectionCommand { get; }
    public ICommand SelectHistoryEntryCommand { get; }
    public ICommand DeleteHistoryCommand { get; }

    public ConnectionsViewModel(INetworkSettingsService service, ILogger<ConnectionsViewModel> logger)
    {
        _service = service;
        _logger = logger;

        SaveConnectionCommand = new Command(async () => await SaveConnectionAsync());
        SelectHistoryEntryCommand = new Command<string>(OnSelectHistoryEntry);
        DeleteHistoryCommand = new Command(OnDeleteHistory);
    }

    public async Task LoadAsync()
    {
        _settings = await _service.LoadAsync();

        IpAddress = _settings.LastIpAddress ?? string.Empty;
        Port = _settings.LastPort ?? string.Empty;
        Nickname = _settings.Nickname ?? string.Empty;

        RefreshHistory();
    }

    private void RefreshHistory()
    {
        HistoryEntries.Clear();
        foreach (var entry in _settings.History)
        {
            if (!string.IsNullOrWhiteSpace(entry.Ip) || !string.IsNullOrWhiteSpace(entry.Port))
            {
                var displayText = string.IsNullOrEmpty(entry.Nickname)
                    ? $"{entry.Ip}:{entry.Port}"
                    : $"{entry.Ip}:{entry.Port} ({entry.Nickname})";
                HistoryEntries.Add(new HistoryEntryViewModel { DisplayText = displayText, Entry = entry });
            }
        }
    }

    private async Task SaveConnectionAsync()
    {
        var oldIp = _settings.LastIpAddress ?? string.Empty;
        var oldPort = _settings.LastPort ?? string.Empty;

        // Trim to remove leading/trailing spaces from IP, port and nickname
        var newIp = IpAddress?.Trim() ?? string.Empty;
        var newPort = Port?.Trim() ?? string.Empty;
        var newNickname = Nickname?.Trim() ?? string.Empty;

        // Apply network settings — also save the nickname
        _service.AddToHistory(_settings, newIp, newPort, newNickname);

        // Update settings with the new nickname before saving
        _settings.Nickname = newNickname;

        await _service.SaveAsync(_settings);

        // Refresh history (keeping IP, Port, Nickname)
        RefreshHistory();

        if (!string.Equals(oldIp, newIp, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(oldPort, newPort, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("IP changed from {OldIp}:{OldPort} to {NewIp}:{NewPort}.", oldIp, oldPort, newIp, newPort);
        }
    }

    private void OnSelectHistoryEntry(string? entry)
    {
        if (string.IsNullOrWhiteSpace(entry))
        {
            return;
        }

        // Expected format: "IP:Port" or "IP:Port (Nickname)"
        var nicknameStart = entry.IndexOf('(');

        if (nicknameStart > 0)
        {
            // There is a nickname — extract IP, Port and Nickname
            var beforeNickname = entry.Substring(0, nicknameStart).Trim();
            var nickname = entry.Substring(nicknameStart + 1, entry.IndexOf(')', nicknameStart) - nicknameStart - 1);

            var parts = beforeNickname.Split(':', 2);
            if (parts.Length == 2)
            {
                IpAddress = parts[0].Trim();
                Port = parts[1].Trim();
                Nickname = nickname;
            }
        }
        else
        {
            // No nickname — extract just IP and Port
            var parts = entry.Split(':', 2);
            if (parts.Length == 2)
            {
                IpAddress = parts[0].Trim();
                Port = parts[1].Trim();
            }
        }
    }

    private void OnDeleteHistory()
    {
        _settings.History.Clear();
        Task.Run(async () => await _service.SaveAsync(_settings)).Wait();
        HistoryEntries.Clear();

        _logger.LogInformation("Connection history deleted.");
    }

    public sealed class HistoryEntryViewModel
    {
        public string DisplayText { get; set; } = string.Empty;
        public ConnectionHistoryEntry? Entry { get; set; }
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
