using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using HL7Tester.Core;
using HL7Tester.Core.Fhir;
using HL7Tester.Core.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Graphics;

namespace HL7Tester.ViewModels;

/// <summary>
/// ViewModel for the dedicated Connections page: MLLP connection (IP/Port/Nickname +
/// History) and FHIR server profiles (base URL, auth, endpoints, TLS, timeout).
/// FHIR secrets (tokens, passwords, client secrets) are persisted through ISecretStore,
/// never in the settings JSON.
/// </summary>
public sealed class ConnectionsViewModel : INotifyPropertyChanged
{
    private readonly INetworkSettingsService _service;
    private readonly ISecretStore _secretStore;
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

    // ---------------------------------------------------------------------
    // Mode badge (global protocol toggle lives in NetworkSettingsPage)
    // ---------------------------------------------------------------------

    private static readonly Color FhirBadgeColor = Color.FromArgb("#009688"); // teal
    private static readonly Color MllpBadgeColor = Color.FromArgb("#512BD4"); // purple (Primary)

    /// <summary>
    /// True when the globally active protocol is FHIR — drives the page mode badge.
    /// </summary>
    public bool IsFhirMode { get; private set; }

    public string ModeBadgeText => IsFhirMode ? "FHIR Mode" : "HL7v2 Mode";

    public Color ModeBadgeColor => IsFhirMode ? FhirBadgeColor : MllpBadgeColor;

    // ---------------------------------------------------------------------
    // FHIR profiles
    // ---------------------------------------------------------------------

    /// <summary>All configured FHIR profiles (mirrors NetworkSettings.FhirProfiles).</summary>
    public ObservableCollection<FhirProfile> FhirProfiles { get; } = new();

    public ObservableCollection<string> AvailableEndpoints { get; } = new(
        ["Patient", "Encounter", "Observation", "ServiceRequest", "Appointment", "Bundle", "Practitioner", "Location"]);

    /// <summary>Display labels shown in the auth dropdown; mapped to FhirAuthType by index.</summary>
    public ObservableCollection<string> AvailableAuthTypes { get; } = new(
        AuthTypeOptions.Select(o => o.Display));

    private static readonly (string Display, FhirAuthType Type)[] AuthTypeOptions =
    [
        ("None", FhirAuthType.None),
        ("Bearer Token", FhirAuthType.BearerToken),
        ("Basic Auth", FhirAuthType.BasicAuth),
        ("OAuth2 Client Credentials", FhirAuthType.OAuth2ClientCredentials),
        ("OAuth2 Auth Code + PKCE", FhirAuthType.OAuth2AuthorizationCodePkce),
        ("API Key", FhirAuthType.ApiKey),
        ("mTLS", FhirAuthType.Mtls),
    ];

    /// <summary>All secret keys that may exist for a profile (used for cleanup on save/delete).</summary>
    private static readonly string[] AllSecretKeys = ["token", "password", "clientSecret", "apiKey", "certPassword"];

    /// <summary>Secret keys relevant to each auth type (values written via ISecretStore).</summary>
    private static readonly Dictionary<FhirAuthType, string[]> SecretKeysByAuthType = new()
    {
        [FhirAuthType.None] = [],
        [FhirAuthType.BearerToken] = ["token"],
        [FhirAuthType.BasicAuth] = ["password"],
        [FhirAuthType.OAuth2ClientCredentials] = ["clientSecret"],
        [FhirAuthType.OAuth2AuthorizationCodePkce] = [],
        [FhirAuthType.ApiKey] = ["apiKey"],
        [FhirAuthType.Mtls] = ["certPassword"],
    };

    private FhirProfile? _selectedProfile;
    public FhirProfile? SelectedProfile
    {
        get => _selectedProfile;
        set
        {
            if (SetField(ref _selectedProfile, value))
            {
                PopulateEditorFromSelectedProfile();
            }
        }
    }

    /// <summary>True when at least one FHIR profile exists — drives the editor visibility.</summary>
    public bool HasProfiles { get; private set; }

    /// <summary>True when no FHIR profile exists — drives the empty-state message (bool→bool binding).</summary>
    public bool NoProfiles { get; private set; }

    // ---- Editor fields (mirrors of the selected profile, committed on Save) ----

    private string _profileName = string.Empty;
    public string ProfileName
    {
        get => _profileName;
        set => SetField(ref _profileName, value);
    }

    private string _baseUrl = string.Empty;
    public string BaseUrl
    {
        get => _baseUrl;
        set => SetField(ref _baseUrl, value);
    }

    private string _selectedAuthTypeDisplay = "None";
    public string SelectedAuthTypeDisplay
    {
        get => _selectedAuthTypeDisplay;
        set
        {
            if (SetField(ref _selectedAuthTypeDisplay, value))
            {
                UpdateAuthVisibilityFlags();
            }
        }
    }

    private FhirAuthType CurrentAuthType =>
        AuthTypeOptions.FirstOrDefault(o => o.Display == _selectedAuthTypeDisplay).Type;

    private string _token = string.Empty;
    public string Token
    {
        get => _token;
        set => SetField(ref _token, value);
    }

    private string _username = string.Empty;
    public string Username
    {
        get => _username;
        set => SetField(ref _username, value);
    }

    private string _password = string.Empty;
    public string Password
    {
        get => _password;
        set => SetField(ref _password, value);
    }

    private string _tokenUrl = string.Empty;
    public string TokenUrl
    {
        get => _tokenUrl;
        set => SetField(ref _tokenUrl, value);
    }

    private string _authorizeUrl = string.Empty;
    public string AuthorizeUrl
    {
        get => _authorizeUrl;
        set => SetField(ref _authorizeUrl, value);
    }

    private string _clientId = string.Empty;
    public string ClientId
    {
        get => _clientId;
        set => SetField(ref _clientId, value);
    }

    private string _clientSecret = string.Empty;
    public string ClientSecret
    {
        get => _clientSecret;
        set => SetField(ref _clientSecret, value);
    }

    private string _redirectUri = string.Empty;
    public string RedirectUri
    {
        get => _redirectUri;
        set => SetField(ref _redirectUri, value);
    }

    private string _scope = string.Empty;
    public string Scope
    {
        get => _scope;
        set => SetField(ref _scope, value);
    }

    private string _headerName = string.Empty;
    public string HeaderName
    {
        get => _headerName;
        set => SetField(ref _headerName, value);
    }

    private string _apiKey = string.Empty;
    public string ApiKey
    {
        get => _apiKey;
        set => SetField(ref _apiKey, value);
    }

    private string _clientCertPath = string.Empty;
    public string ClientCertPath
    {
        get => _clientCertPath;
        set => SetField(ref _clientCertPath, value);
    }

    private string _certPassword = string.Empty;
    public string CertPassword
    {
        get => _certPassword;
        set => SetField(ref _certPassword, value);
    }

    private string _defaultEndpoint = "Patient";
    public string DefaultEndpoint
    {
        get => _defaultEndpoint;
        set => SetField(ref _defaultEndpoint, value);
    }

    private bool _tlsVerify = true;
    public bool TlsVerify
    {
        get => _tlsVerify;
        set => SetField(ref _tlsVerify, value);
    }

    private string _timeoutText = "30";
    public string TimeoutText
    {
        get => _timeoutText;
        set => SetField(ref _timeoutText, value);
    }

    // ---- Conditional auth fields (bool -> bool IsVisible bindings, lesson v2.0.21) ----

    private bool _showTokenField;
    public bool ShowTokenField
    {
        get => _showTokenField;
        private set => SetField(ref _showTokenField, value);
    }

    private bool _showBasicAuthFields;
    public bool ShowBasicAuthFields
    {
        get => _showBasicAuthFields;
        private set => SetField(ref _showBasicAuthFields, value);
    }

    /// <summary>Shared OAuth2 fields (TokenUrl, ClientId, Scope) — shown for both OAuth2 flows.</summary>
    private bool _showOAuth2Fields;
    public bool ShowOAuth2Fields
    {
        get => _showOAuth2Fields;
        private set => SetField(ref _showOAuth2Fields, value);
    }

    /// <summary>Client Secret entry — Client Credentials flow only.</summary>
    private bool _showOAuth2CcFields;
    public bool ShowOAuth2CcFields
    {
        get => _showOAuth2CcFields;
        private set => SetField(ref _showOAuth2CcFields, value);
    }

    /// <summary>AuthorizeUrl + RedirectUri entries — Auth Code + PKCE flow only.</summary>
    private bool _showOAuth2PkceFields;
    public bool ShowOAuth2PkceFields
    {
        get => _showOAuth2PkceFields;
        private set => SetField(ref _showOAuth2PkceFields, value);
    }

    private bool _showApiKeyFields;
    public bool ShowApiKeyFields
    {
        get => _showApiKeyFields;
        private set => SetField(ref _showApiKeyFields, value);
    }

    private bool _showMtlsFields;
    public bool ShowMtlsFields
    {
        get => _showMtlsFields;
        private set => SetField(ref _showMtlsFields, value);
    }

    private void UpdateAuthVisibilityFlags()
    {
        var type = CurrentAuthType;
        SetField(ref _showTokenField, type == FhirAuthType.BearerToken, nameof(ShowTokenField));
        SetField(ref _showBasicAuthFields, type == FhirAuthType.BasicAuth, nameof(ShowBasicAuthFields));
        SetField(ref _showOAuth2Fields,
            type is FhirAuthType.OAuth2ClientCredentials or FhirAuthType.OAuth2AuthorizationCodePkce,
            nameof(ShowOAuth2Fields));
        SetField(ref _showOAuth2CcFields, type == FhirAuthType.OAuth2ClientCredentials, nameof(ShowOAuth2CcFields));
        SetField(ref _showOAuth2PkceFields, type == FhirAuthType.OAuth2AuthorizationCodePkce, nameof(ShowOAuth2PkceFields));
        SetField(ref _showApiKeyFields, type == FhirAuthType.ApiKey, nameof(ShowApiKeyFields));
        SetField(ref _showMtlsFields, type == FhirAuthType.Mtls, nameof(ShowMtlsFields));
    }

    public ICommand AddProfileCommand { get; }
    public ICommand SaveProfileCommand { get; }
    public ICommand DeleteProfileCommand { get; }

    public ConnectionsViewModel(
        INetworkSettingsService service,
        ISecretStore secretStore,
        ILogger<ConnectionsViewModel> logger)
    {
        _service = service;
        _secretStore = secretStore;
        _logger = logger;

        SaveConnectionCommand = new Command(async () => await SaveConnectionAsync());
        SelectHistoryEntryCommand = new Command<string>(OnSelectHistoryEntry);
        DeleteHistoryCommand = new Command(OnDeleteHistory);

        AddProfileCommand = new Command(OnAddProfile);
        SaveProfileCommand = new Command(async () => await SaveProfileAsync());
        DeleteProfileCommand = new Command(async () => await DeleteProfileAsync());
    }

    public async Task LoadAsync()
    {
        _settings = await _service.LoadAsync();

        IpAddress = _settings.LastIpAddress ?? string.Empty;
        Port = _settings.LastPort ?? string.Empty;
        Nickname = _settings.Nickname ?? string.Empty;

        RefreshHistory();

        IsFhirMode = string.Equals(_settings.Protocol, "FHIR", StringComparison.OrdinalIgnoreCase);
        OnPropertyChanged(nameof(IsFhirMode));
        OnPropertyChanged(nameof(ModeBadgeText));
        OnPropertyChanged(nameof(ModeBadgeColor));

        LoadFhirProfiles();
    }

    private void LoadFhirProfiles()
    {
        FhirProfiles.Clear();
        foreach (var profile in _settings.FhirProfiles)
        {
            FhirProfiles.Add(profile);
        }

        SetHasProfiles(FhirProfiles.Count > 0);

        var active = FhirProfiles.FirstOrDefault(p => p.Id == _settings.ActiveFhirProfileId)
                     ?? FhirProfiles.FirstOrDefault();
        SelectedProfile = active;
    }

    private void SetHasProfiles(bool value)
    {
        if (!HasProfiles.Equals(value))
        {
            HasProfiles = value;
            NoProfiles = !value;
            OnPropertyChanged(nameof(HasProfiles));
            OnPropertyChanged(nameof(NoProfiles));
        }
    }

    private void PopulateEditorFromSelectedProfile()
    {
        var profile = _selectedProfile;
        if (profile is null)
        {
            ProfileName = string.Empty;
            BaseUrl = string.Empty;
            SelectedAuthTypeDisplay = "None";
            Token = Password = ClientSecret = ApiKey = CertPassword = string.Empty;
            TokenUrl = AuthorizeUrl = ClientId = RedirectUri = Scope = string.Empty;
            HeaderName = ClientCertPath = string.Empty;
            DefaultEndpoint = "Patient";
            TlsVerify = true;
            TimeoutText = "30";
            return;
        }

        ProfileName = profile.Name;
        BaseUrl = profile.BaseUrl;
        // Assignment triggers UpdateAuthVisibilityFlags through the property setter.
        SelectedAuthTypeDisplay = AuthTypeOptions.FirstOrDefault(o => o.Type == profile.Auth.Type).Display ?? "None";

        TokenUrl = profile.Auth.TokenUrl ?? string.Empty;
        AuthorizeUrl = profile.Auth.AuthorizeUrl ?? string.Empty;
        ClientId = profile.Auth.ClientId ?? string.Empty;
        RedirectUri = profile.Auth.RedirectUri ?? string.Empty;
        Scope = profile.Auth.Scope ?? string.Empty;
        HeaderName = profile.Auth.HeaderName ?? string.Empty;
        Username = profile.Auth.Username ?? string.Empty;
        ClientCertPath = profile.Auth.ClientCertPath ?? string.Empty;

        DefaultEndpoint = string.IsNullOrWhiteSpace(profile.DefaultEndpoint) ? "Patient" : profile.DefaultEndpoint;
        TlsVerify = profile.TlsVerify;
        TimeoutText = profile.TimeoutSeconds.ToString();

        // Secret values are never stored in the profile object; fetch them from the store.
        LoadSecretsIntoEditorAsync(profile).ConfigureAwait(false).GetAwaiter().GetResult();
    }

    private async Task LoadSecretsIntoEditorAsync(FhirProfile profile)
    {
        Token = await _secretStore.GetSecretAsync(profile.Id, "token") ?? string.Empty;
        Password = await _secretStore.GetSecretAsync(profile.Id, "password") ?? string.Empty;
        ClientSecret = await _secretStore.GetSecretAsync(profile.Id, "clientSecret") ?? string.Empty;
        ApiKey = await _secretStore.GetSecretAsync(profile.Id, "apiKey") ?? string.Empty;
        CertPassword = await _secretStore.GetSecretAsync(profile.Id, "certPassword") ?? string.Empty;
    }

    private void OnAddProfile()
    {
        var profile = new FhirProfile { Name = $"Profile {FhirProfiles.Count + 1}" };
        FhirProfiles.Add(profile);
        _settings.FhirProfiles.Add(profile);
        SetHasProfiles(true);

        SelectedProfile = profile;
        _settings.ActiveFhirProfileId = profile.Id;

        _logger.LogInformation("FHIR profile added: {ProfileId}.", profile.Id);
    }

    private async Task SaveProfileAsync()
    {
        var profile = SelectedProfile;
        if (profile is null)
        {
            return;
        }

        // Commit clear-text configuration onto the profile object.
        profile.Name = ProfileName?.Trim() ?? string.Empty;
        profile.BaseUrl = BaseUrl?.Trim() ?? string.Empty;

        var authType = CurrentAuthType;
        profile.Auth.Type = authType;
        profile.Auth.TokenUrl = NullIfBlank(TokenUrl);
        profile.Auth.AuthorizeUrl = NullIfBlank(AuthorizeUrl);
        profile.Auth.ClientId = NullIfBlank(ClientId);
        profile.Auth.RedirectUri = NullIfBlank(RedirectUri);
        profile.Auth.Scope = NullIfBlank(Scope);
        profile.Auth.HeaderName = NullIfBlank(HeaderName);
        profile.Auth.Username = NullIfBlank(Username);
        profile.Auth.ClientCertPath = NullIfBlank(ClientCertPath);

        profile.DefaultEndpoint = string.IsNullOrWhiteSpace(DefaultEndpoint) ? null : DefaultEndpoint;
        profile.TlsVerify = TlsVerify;
        profile.TimeoutSeconds = int.TryParse(TimeoutText, out var seconds) && seconds > 0 ? seconds : 30;

        await PersistSecretsAsync(profile, authType);

        _settings.ActiveFhirProfileId = profile.Id;
        await _service.SaveAsync(_settings);

        // FhirProfile does not implement INotifyPropertyChanged: re-insert the same
        // instance so the ObservableCollection raises Replace and the dropdown
        // caption (profile name) refreshes in both AppDropdown implementations.
        var profileIndex = FhirProfiles.IndexOf(profile);
        if (profileIndex >= 0)
        {
            FhirProfiles[profileIndex] = profile;
        }

        _logger.LogInformation("FHIR profile saved: {ProfileName} ({ProfileId}), auth {AuthType}.",
            profile.Name, profile.Id, authType);
    }

    /// <summary>
    /// Writes non-empty secret fields through ISecretStore, deletes secrets that were
    /// cleared or no longer belong to the selected auth type, and rebuilds the
    /// profile's SecretRef list. No secret value ever reaches the settings JSON.
    /// </summary>
    private async Task PersistSecretsAsync(FhirProfile profile, FhirAuthType authType)
    {
        var relevantKeys = SecretKeysByAuthType.TryGetValue(authType, out var keys) ? keys : [];
        var values = GetSecretFieldValues();

        foreach (var key in AllSecretKeys)
        {
            var value = values.TryGetValue(key, out var v) ? v : string.Empty;

            if (relevantKeys.Contains(key) && !string.IsNullOrEmpty(value))
            {
                await _secretStore.SetSecretAsync(profile.Id, key, value);
            }
            else
            {
                // Cleared by the user or not applicable to the current auth type.
                await _secretStore.DeleteSecretAsync(profile.Id, key);
            }
        }

        profile.Secrets = relevantKeys
            .Where(key => !string.IsNullOrEmpty(values.GetValueOrDefault(key)))
            .Select(key => new SecretRef { ProfileId = profile.Id, Key = key })
            .ToList();
    }

    private Dictionary<string, string> GetSecretFieldValues() => new()
    {
        ["token"] = Token?.Trim() ?? string.Empty,
        ["password"] = Password?.Trim() ?? string.Empty,
        ["clientSecret"] = ClientSecret?.Trim() ?? string.Empty,
        ["apiKey"] = ApiKey?.Trim() ?? string.Empty,
        ["certPassword"] = CertPassword?.Trim() ?? string.Empty,
    };

    private async Task DeleteProfileAsync()
    {
        var profile = SelectedProfile;
        if (profile is null)
        {
            return;
        }

        foreach (var key in AllSecretKeys)
        {
            await _secretStore.DeleteSecretAsync(profile.Id, key);
        }

        FhirProfiles.Remove(profile);
        _settings.FhirProfiles.RemoveAll(p => p.Id == profile.Id);
        if (_settings.ActiveFhirProfileId == profile.Id)
        {
            _settings.ActiveFhirProfileId = null;
        }

        await _service.SaveAsync(_settings);

        SetHasProfiles(FhirProfiles.Count > 0);
        SelectedProfile = FhirProfiles.FirstOrDefault();

        _logger.LogInformation("FHIR profile deleted: {ProfileId}.", profile.Id);
    }

    private static string? NullIfBlank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

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
