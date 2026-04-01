using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using HL7Tester.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.ApplicationModel;

namespace HL7Tester.ViewModels;

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

    private string _nickname = string.Empty;
    /// <summary>
    /// Surnom ou nom familier pour l'adresse IP (optionnel).
    /// </summary>
    public string Nickname
    {
        get => _nickname;
        set => SetField(ref _nickname, value);
    }

    public ObservableCollection<HistoryEntryViewModel> HistoryEntries { get; } = new();

    public ObservableCollection<string> AvailableLogLevels { get; } =
        new(["Debug", "Information", "Warning"]);

    private bool _autoUpdateCheck = true;
    /// <summary>
    /// Indique si l'application doit vérifier automatiquement les mises à jour au démarrage.
    /// </summary>
    public bool AutoUpdateCheck
    {
        get => _autoUpdateCheck;
        set => SetField(ref _autoUpdateCheck, value);
    }

    /// <summary>
    /// Version de l'application extraite automatiquement de l'assembly.
    /// </summary>
    public string AssemblyVersion { get; }

    private string _selectedLogLevel = "Debug";
    public string SelectedLogLevel
    {
        get => _selectedLogLevel;
        set => SetField(ref _selectedLogLevel, value);
    }

    public ICommand SaveNetworkSettingsCommand { get; }
    public ICommand SaveApplicationSettingsCommand { get; }
    public ICommand SelectHistoryEntryCommand { get; }
    public ICommand DeleteHistoryCommand { get; }
    public ICommand OpenDocumentationCommand { get; }
    public ICommand OpenOrmDocumentationCommand { get; }
    public ICommand OpenSiuDocumentationCommand { get; }

    private const string HL7_ADT_DOCUMENTATION_URL = "https://www.hl7.eu/HL7v2x/v231/std231/CH3.html#Heading3";
    private const string HL7_ORM_DOCUMENTATION_URL = "https://www.hl7.eu/HL7v2x/v231/std231/CH4.html#Heading13";
    private const string HL7_SIU_DOCUMENTATION_URL = "https://www.hl7.eu/HL7v2x/v231/std231/CH10.html#Heading53";

    public NetworkSettingsViewModel(INetworkSettingsService service, ILogger<NetworkSettingsViewModel> logger)
    {
        _service = service;
        _logger = logger;

        // Extraire la version de l'assembly
        var assembly = Assembly.GetExecutingAssembly();
        var assemblyTitleAttribute = assembly.GetCustomAttribute<AssemblyTitleAttribute>();
        var assemblyVersion = assembly.GetName().Version;
        AssemblyVersion = $"{assemblyTitleAttribute?.Title} v{assemblyVersion?.ToString(3) ?? "1.0.0"}";

        SaveNetworkSettingsCommand = new Command(async () => await SaveNetworkSettingsAsync());
        SaveApplicationSettingsCommand = new Command(async () => await SaveApplicationSettingsAsync());
        SelectHistoryEntryCommand = new Command<string>(OnSelectHistoryEntry);
        DeleteHistoryCommand = new Command(OnDeleteHistory);
        OpenDocumentationCommand = new Command(OpenAdtDocumentation);
        OpenOrmDocumentationCommand = new Command(OpenOrmDocumentation);
        OpenSiuDocumentationCommand = new Command(OpenSiuDocumentation);
    }

    private void OpenAdtDocumentation()
    {
        try
        {
            _logger.LogInformation("Opening HL7 ADT documentation: {Url}", HL7_ADT_DOCUMENTATION_URL);
            
            var uri = new Uri(HL7_ADT_DOCUMENTATION_URL);
            Launcher.Default.OpenAsync(uri);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open HL7 ADT documentation URL: {Url}", HL7_ADT_DOCUMENTATION_URL);
        }
    }

    private void OpenOrmDocumentation()
    {
        try
        {
            _logger.LogInformation("Opening HL7 ORM documentation: {Url}", HL7_ORM_DOCUMENTATION_URL);
            
            var uri = new Uri(HL7_ORM_DOCUMENTATION_URL);
            Launcher.Default.OpenAsync(uri);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open HL7 ORM documentation URL: {Url}", HL7_ORM_DOCUMENTATION_URL);
        }
    }

    private void OpenSiuDocumentation()
    {
        try
        {
            _logger.LogInformation("Opening HL7 SIU documentation: {Url}", HL7_SIU_DOCUMENTATION_URL);
            
            var uri = new Uri(HL7_SIU_DOCUMENTATION_URL);
            Launcher.Default.OpenAsync(uri);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open HL7 SIU documentation URL: {Url}", HL7_SIU_DOCUMENTATION_URL);
        }
    }

    public async Task LoadAsync()
    {
        _settings = await _service.LoadAsync();

        IpAddress = _settings.LastIpAddress ?? string.Empty;
        Port = _settings.LastPort ?? string.Empty;
        Nickname = _settings.Nickname ?? string.Empty;

        AutoUpdateCheck = _settings.AutoUpdateCheck;

        SelectedLogLevel = string.IsNullOrWhiteSpace(_settings.LogLevel)
            ? "Debug"
            : _settings.LogLevel;

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

    public class HistoryEntryViewModel
    {
        public string DisplayText { get; set; } = string.Empty;
        public ConnectionHistoryEntry? Entry { get; set; }
    }

    private async Task SaveNetworkSettingsAsync()
    {
        var oldIp = _settings.LastIpAddress ?? string.Empty;
        var oldPort = _settings.LastPort ?? string.Empty;
        
        // Trim to remove leading/trailing spaces from IP, port and nickname
        var newIp = IpAddress?.Trim() ?? string.Empty;
        var newPort = Port?.Trim() ?? string.Empty;
        var newNickname = Nickname?.Trim() ?? string.Empty;

        // Appliquer uniquement les paramètres réseau - sauvegarder aussi le nickname
        _service.AddToHistory(_settings, newIp, newPort, newNickname);
        
        // Mettre à jour les settings avec le nouveau nickname avant de sauvegarder
        _settings.Nickname = newNickname;
        
        await _service.SaveAsync(_settings);
        
        // Rafraîchir l'historique (tout en gardant IP, Port, Nickname)
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

        if (!string.Equals(oldIp, newIp, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(oldPort, newPort, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("IP changed from {OldIp}:{OldPort} to {NewIp}:{NewPort}.", oldIp, oldPort, newIp, newPort);
            _logger.LogDebug("Network settings updated. Previous endpoint {OldIp}:{OldPort}, new endpoint {NewIp}:{NewPort}.", oldIp, oldPort, newIp, newPort);
        }
    }

    private async Task SaveApplicationSettingsAsync()
    {
        var oldLogLevel = _settings.LogLevel ?? "Debug";
        var oldAutoUpdateCheck = _settings.AutoUpdateCheck;

        _settings.LogLevel = SelectedLogLevel;
        _settings.AutoUpdateCheck = AutoUpdateCheck;

        await _service.SaveAsync(_settings);
        
        if (!string.Equals(oldLogLevel, SelectedLogLevel, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("Log level changed from {OldLevel} to {NewLevel}.", oldLogLevel, SelectedLogLevel);
        }

        if (oldAutoUpdateCheck != AutoUpdateCheck)
        {
            _logger.LogInformation("Auto update check changed to {AutoUpdateCheck}.", AutoUpdateCheck);
        }
    }

    private void OnSelectHistoryEntry(string? entry)
    {
        if (string.IsNullOrWhiteSpace(entry))
        {
            return;
        }

        // Format attendu: "IP:Port" ou "IP:Port (Nickname)"
        var nicknameStart = entry.IndexOf('(');
        
        if (nicknameStart > 0)
        {
            // Il y a un nickname, extraire IP, Port et Nickname
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
            // Pas de nickname, extraire juste IP et Port
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