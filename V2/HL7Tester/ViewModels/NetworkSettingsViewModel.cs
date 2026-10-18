using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using HL7Tester.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Graphics;

namespace HL7Tester.ViewModels;

/// <summary>
/// ViewModel for application settings (log level, encoding, auto-update, tools navigation).
/// Network connection management (IP/Port/History) has been extracted to ConnectionsViewModel (Phase 0.5).
/// </summary>
public sealed class NetworkSettingsViewModel : INotifyPropertyChanged
{
    private readonly INetworkSettingsService _service;
    private NetworkSettings _settings = new();
    private readonly ILogger<NetworkSettingsViewModel> _logger;

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<string> AvailableLogLevels { get; } =
        new(["Debug", "Information", "Warning"]);

    /// <summary>
    /// Liste des encodages prédéfinis pour le message HL7.
    /// </summary>
    public ObservableCollection<string> AvailableEncodings { get; } = new(
        new[] { 
            "UTF-8",           // Standard moderne, recommandé
            "ASCII",           // 7-bit pur — très courant en HL7 legacy
            "CP1252",          // Windows Latin-1 (Europe de l'Ouest)
            "ISO-8859-1",      // Latin-1 ISO (Europe de l'Ouest)
            "ISO-8859-15",     // Latin-9 avec Euro (€) — utile en Europe
            "IBM437",          // DOS original — legacy hospitalier
            "Windows-1252",    // Alias explicite pour CP1252
            "UTF-16LE"         // Unicode 2-byte little-endian
        });

    private string _selectedEncoding = "UTF-8";
    /// <summary>
    /// Encodage sélectionné dans le menu déroulant.
    /// </summary>
    public string SelectedEncoding
    {
        get => _selectedEncoding;
        set => SetField(ref _selectedEncoding, value);
    }

    private bool _useCustomEncoding = false;
    /// <summary>
    /// Indique si un encodage personnalisé doit être utilisé.
    /// </summary>
    public bool UseCustomEncoding
    {
        get => _useCustomEncoding;
        set => SetField(ref _useCustomEncoding, value);
    }

    private string _customEncoding = string.Empty;
    /// <summary>
    /// Nom de l'encodage personnalisé.
    /// </summary>
    public string CustomEncoding
    {
        get => _customEncoding;
        set => SetField(ref _customEncoding, value);
    }

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

    /// <summary>
    /// Raw protocol values bound to the protocol AppDropdown (display labels are
    /// applied through ProtocolDisplayConverter).
    /// </summary>
    public ObservableCollection<string> AvailableProtocols { get; } = new(["MLLP", "FHIR"]);

    private string _selectedProtocol = "MLLP";
    /// <summary>
    /// Active transport protocol: "MLLP" (HL7 v2) or "FHIR" (REST).
    /// </summary>
    public string SelectedProtocol
    {
        get => _selectedProtocol;
        set
        {
            if (SetField(ref _selectedProtocol, value))
            {
                OnPropertyChanged(nameof(IsFhirMode));
                OnPropertyChanged(nameof(ModeBadgeText));
                OnPropertyChanged(nameof(ModeBadgeColor));
            }
        }
    }

    /// <summary>
    /// True when the active protocol is FHIR — drives the mode badge.
    /// </summary>
    public bool IsFhirMode => SelectedProtocol == "FHIR";

    public string ModeBadgeText => IsFhirMode ? "FHIR Mode" : "HL7v2 Mode";

    public Color ModeBadgeColor => IsFhirMode
        ? Color.FromArgb("#009688") // teal
        : Color.FromArgb("#512BD4"); // purple (Primary)

    public ICommand SaveApplicationSettingsCommand { get; }
    public ICommand OpenDocumentationCommand { get; }
    public ICommand OpenOrmDocumentationCommand { get; }
    public ICommand OpenSiuDocumentationCommand { get; }
    public ICommand OpenInspectorCommand { get; }
    public ICommand OpenInspectorWebCommand { get; }
    public ICommand OpenBatchSendCommand { get; }
    public ICommand OpenConnectionsCommand { get; }
    public ICommand OpenConverterCommand { get; }

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

        SaveApplicationSettingsCommand = new Command(async () => await SaveApplicationSettingsAsync());
        OpenDocumentationCommand = new Command(OpenAdtDocumentation);
        OpenOrmDocumentationCommand = new Command(OpenOrmDocumentation);
        OpenSiuDocumentationCommand = new Command(OpenSiuDocumentation);
        OpenInspectorCommand = new Command(OpenInspector);
        OpenInspectorWebCommand = new Command(OpenWebInspector);
        OpenBatchSendCommand = new Command(OpenBatchSend);
        OpenConnectionsCommand = new Command(OpenConnections);
        OpenConverterCommand = new Command(OpenConverter);
    }

    private async void OpenConverter()
    {
        try
        {
            _logger.LogInformation("Opening HL7 ⇄ FHIR Converter page.");
            await Shell.Current.GoToAsync("//FhirConverterPage");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open HL7 ⇄ FHIR Converter page.");
        }
    }

    private async void OpenBatchSend()
    {
        try
        {
            _logger.LogInformation("Opening Batch Send page.");
            await Shell.Current.GoToAsync("//BatchSendPage");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open Batch Send page.");
        }
    }

    private async void OpenConnections()
    {
        try
        {
            _logger.LogInformation("Opening Connections page.");
            await Shell.Current.GoToAsync("//ConnectionsPage");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open Connections page.");
        }
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

    private async void OpenWebInspector()
    {
        try
        {
            _logger.LogInformation("Opening local HL7 Inspector HTML file");

            var tempDir = Path.Combine(Path.GetTempPath(), "HL7Tester_Inspector");
            Directory.CreateDirectory(tempDir);

            var htmlPath = Path.Combine(tempDir, "hl7-inspector.html");
            using (var stream = await FileSystem.OpenAppPackageFileAsync("hl7-inspector.html"))
            using (var fileStream = File.Create(htmlPath))
            {
                stream.CopyTo(fileStream);
            }

            var faviconPath = Path.Combine(tempDir, "favicon.png");
            using (var stream = await FileSystem.OpenAppPackageFileAsync("favicon.png"))
            using (var fileStream = File.Create(faviconPath))
            {
                stream.CopyTo(fileStream);
            }

            Process.Start(new ProcessStartInfo(htmlPath) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open local HL7 Inspector file");
        }
    }

    private async void OpenInspector()
    {
        try
        {
            _logger.LogInformation("Opening HL7 Inspector page.");
            await Shell.Current.GoToAsync("//Hl7InspectorPage");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to navigate to HL7 Inspector page.");
        }
    }

    public async Task LoadAsync()
    {
        _settings = await _service.LoadAsync();

        AutoUpdateCheck = _settings.AutoUpdateCheck;

        SelectedProtocol = string.Equals(_settings.Protocol, "FHIR", StringComparison.OrdinalIgnoreCase)
            ? "FHIR"
            : "MLLP";

        SelectedLogLevel = string.IsNullOrWhiteSpace(_settings.LogLevel)
            ? "Debug"
            : _settings.LogLevel;

        // Load encoding from settings
        SelectedEncoding = string.IsNullOrWhiteSpace(_settings.MessageEncoding)
            ? "UTF-8"
            : _settings.MessageEncoding;
        UseCustomEncoding = false;
        CustomEncoding = string.Empty;
    }

    private async Task SaveApplicationSettingsAsync()
    {
        var oldLogLevel = _settings.LogLevel ?? "Debug";
        var oldAutoUpdateCheck = _settings.AutoUpdateCheck;
        var oldProtocol = _settings.Protocol ?? "MLLP";

        _settings.LogLevel = SelectedLogLevel;
        _settings.AutoUpdateCheck = AutoUpdateCheck;

        // Persist the global protocol toggle (MLLP / FHIR)
        _settings.Protocol = SelectedProtocol;

        // Sauvegarder l'encodage du message
        if (UseCustomEncoding && !string.IsNullOrWhiteSpace(CustomEncoding))
        {
            _settings.MessageEncoding = CustomEncoding;
        }
        else
        {
            _settings.MessageEncoding = SelectedEncoding;
        }

        await _service.SaveAsync(_settings);
        
        if (!string.Equals(oldLogLevel, SelectedLogLevel, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("Log level changed from {OldLevel} to {NewLevel}.", oldLogLevel, SelectedLogLevel);
        }

        if (oldAutoUpdateCheck != AutoUpdateCheck)
        {
            _logger.LogInformation("Auto update check changed to {AutoUpdateCheck}.", AutoUpdateCheck);
        }

        if (!string.Equals(oldProtocol, SelectedProtocol, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("Protocol changed from {OldProtocol} to {NewProtocol}.", oldProtocol, SelectedProtocol);
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