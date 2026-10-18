using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Windows.Input;
using HL7Tester.Core;
using HL7Tester.Core.Fhir;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Maui.Graphics;

namespace HL7Tester.ViewModels;

/// <summary>
/// ViewModel for the read-only FHIR server explorer. Performs Search
/// (GET [base]/[Type]?params) and Read (GET [base]/[Type]/[id]) interactions
/// against the active FHIR profile, pretty-prints the JSON response, counts
/// Bundle entries and follows Bundle "next" page links for pagination.
/// </summary>
public sealed class FhirExplorerViewModel : INotifyPropertyChanged
{
    private static readonly Color ActiveSearchColor = Color.FromArgb("#009688"); // teal
    private static readonly Color ActiveReadColor = Color.FromArgb("#512BD4");   // indigo / Primary
    private static readonly Color InactiveColor = Color.FromArgb("#9CA3AF");     // gray

    private readonly FhirRestClient _restClient;
    private readonly INetworkSettingsService _settingsService;
    private readonly ILogger<FhirExplorerViewModel> _logger;

    private FhirProfile? _activeProfile;
    private string? _nextPageUrl;

    public event PropertyChangedEventHandler? PropertyChanged;

    // ── Bindable state ────────────────────────────────────────────────────────

    /// <summary>Common FHIR resource types offered in the dropdown.</summary>
    public IReadOnlyList<string> AvailableResourceTypes { get; } = new[]
    {
        "Patient",
        "Encounter",
        "Bundle",
        "ServiceRequest",
        "Appointment",
        "Observation",
        "Practitioner",
        "Organization",
        "Location",
        "AllergyIntolerance",
        "Medication",
        "Condition",
        "Procedure"
    };

    private string _selectedResourceType = "Patient";
    public string SelectedResourceType
    {
        get => _selectedResourceType;
        set => SetField(ref _selectedResourceType, value);
    }

    private bool _isSearchMode = true;
    public bool IsSearchMode
    {
        get => _isSearchMode;
        set
        {
            if (SetField(ref _isSearchMode, value))
            {
                OnPropertyChanged(nameof(IsReadMode));
                OnPropertyChanged(nameof(SearchButtonColor));
                OnPropertyChanged(nameof(ReadButtonColor));
            }
        }
    }

    public bool IsReadMode => !IsSearchMode;

    /// <summary>Background of the "Search" toggle button.</summary>
    public Color SearchButtonColor => IsSearchMode ? ActiveSearchColor : InactiveColor;

    /// <summary>Background of the "Read" toggle button.</summary>
    public Color ReadButtonColor => IsReadMode ? ActiveReadColor : InactiveColor;

    private string _searchParams = string.Empty;
    public string SearchParams
    {
        get => _searchParams;
        set => SetField(ref _searchParams, value);
    }

    private string _resourceId = string.Empty;
    public string ResourceId
    {
        get => _resourceId;
        set => SetField(ref _resourceId, value);
    }

    private string _responseBody = string.Empty;
    public string ResponseBody
    {
        get => _responseBody;
        set
        {
            if (SetField(ref _responseBody, value))
                OnPropertyChanged(nameof(HasResponse));
        }
    }

    public bool HasResponse => !string.IsNullOrEmpty(_responseBody);

    private string _statusMessage = string.Empty;
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetField(ref _statusMessage, value);
    }

    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            _isBusy = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsNotBusy));
        }
    }

    public bool IsNotBusy => !IsBusy;

    /// <summary>True when the active transport is FHIR and a profile is selected.</summary>
    public bool IsFhirMode { get; private set; }

    /// <summary>Static header badge label.</summary>
    public string ModeBadgeText => "FHIR Explorer";

    /// <summary>Header badge color (teal).</summary>
    public Color ModeBadgeColor => Color.FromArgb("#009688");

    /// <summary>True when the last Bundle carried a "next" page link.</summary>
    public bool HasNextPage => !string.IsNullOrEmpty(_nextPageUrl);

    // ── Commands ──────────────────────────────────────────────────────────────

    public ICommand ExecuteCommand { get; }
    public ICommand NextPageCommand { get; }
    public ICommand CopyResponseCommand { get; }
    public ICommand ClearCommand { get; }
    public ICommand SendToMainPageCommand { get; }

    // ─────────────────────────────────────────────────────────────────────────

    public FhirExplorerViewModel(
        FhirRestClient restClient,
        INetworkSettingsService settingsService,
        ILogger<FhirExplorerViewModel>? logger = null)
    {
        _restClient = restClient;
        _settingsService = settingsService;
        _logger = logger ?? NullLogger<FhirExplorerViewModel>.Instance;

        ExecuteCommand = new Command(async () => await ExecuteAsync());
        NextPageCommand = new Command(async () => await NextPageAsync());
        CopyResponseCommand = new Command(async () => await CopyResponseAsync());
        ClearCommand = new Command(Clear);
        SendToMainPageCommand = new Command(async () => await SendToMainPageAsync());
    }

    // ── Load active profile ─────────────────────────────────────────────────

    /// <summary>
    /// Loads the active FHIR profile from settings. Warns when no profile is
    /// available or the app is not in FHIR mode.
    /// </summary>
    public async Task LoadAsync()
    {
        var settings = await _settingsService.LoadAsync().ConfigureAwait(false);

        IsFhirMode = string.Equals(settings.Protocol, "FHIR", StringComparison.OrdinalIgnoreCase);
        _activeProfile = settings.FhirProfiles.FirstOrDefault(p => p.Id == settings.ActiveFhirProfileId);

        // Convenience fallback: if none is explicitly active but exactly one exists, use it.
        _activeProfile ??= settings.FhirProfiles.Count == 1 ? settings.FhirProfiles[0] : null;

        OnPropertyChanged(nameof(IsFhirMode));
        OnPropertyChanged(nameof(ModeBadgeText));
        OnPropertyChanged(nameof(ModeBadgeColor));

        if (_activeProfile is null)
        {
            StatusMessage = "No active FHIR profile. Select the FHIR protocol and pick a profile in Connections.";
            _logger.LogWarning("FHIR explorer opened with no active FHIR profile.");
        }
        else if (!IsFhirMode)
        {
            StatusMessage = $"Profile '{_activeProfile.Name}' is loaded, but the app is not in FHIR mode.";
        }
    }

    // ── Execute / pagination ────────────────────────────────────────────────

    private async Task ExecuteAsync()
    {
        if (_activeProfile is null)
        {
            StatusMessage = "No active FHIR profile. Configure one in Connections and select FHIR protocol.";
            return;
        }

        if (IsReadMode && string.IsNullOrWhiteSpace(ResourceId))
        {
            StatusMessage = "Enter a resource ID to read.";
            return;
        }

        await RunGetAsync(BuildRequestPath());
    }

    private async Task NextPageAsync()
    {
        if (string.IsNullOrWhiteSpace(_nextPageUrl))
            return;

        await RunGetAsync(_nextPageUrl!);
    }

    private string BuildRequestPath()
    {
        var type = string.IsNullOrWhiteSpace(SelectedResourceType) ? "Patient" : SelectedResourceType.Trim();

        if (IsReadMode)
            return $"{type}/{ResourceId.Trim()}";

        var ps = (_searchParams ?? string.Empty).Trim();
        return ps.Length == 0 ? type : $"{type}?{ps}";
    }

    private async Task RunGetAsync(string pathOrUrl)
    {
        IsBusy = true;
        StatusMessage = string.Empty;
        SetNextPageUrl(null);

        try
        {
            var result = await _restClient.GetRawAsync(_activeProfile!, pathOrUrl).ConfigureAwait(false);
            var status = result.HttpStatusCode ?? 0;
            var body = result.ResponseBody ?? string.Empty;

            ResponseBody = PrettyPrintJson(body);

            var (entryCount, nextUrl) = InspectBundle(body);
            SetNextPageUrl(nextUrl);

            if (result.Success)
            {
                var suffix = entryCount.HasValue ? $" — {entryCount} entries" : string.Empty;
                StatusMessage = $"{status} OK{suffix}";
                _logger.LogInformation("FHIR explorer GET succeeded: {Path} ({Status}).", pathOrUrl, status);
            }
            else
            {
                var err = string.IsNullOrWhiteSpace(result.ErrorDescription)
                    ? result.ErrorMessage
                    : result.ErrorDescription;
                StatusMessage = string.IsNullOrWhiteSpace(body)
                    ? $"{err}"
                    : $"{err} — see response body";
                _logger.LogWarning("FHIR explorer GET failed: {Path} → {Error}.", pathOrUrl, err);
            }
        }
        catch (Exception ex)
        {
            ResponseBody = string.Empty;
            SetNextPageUrl(null);
            StatusMessage = $"Request failed: {ex.Message}";
            _logger.LogError(ex, "FHIR explorer request threw.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    // ── JSON helpers ────────────────────────────────────────────────────────

    /// <summary>Indent JSON when parseable; otherwise return the raw body unchanged.</summary>
    private static string PrettyPrintJson(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return string.Empty;

        try
        {
            using var doc = JsonDocument.Parse(raw);
            using var ms = new MemoryStream();
            using (var writer = new Utf8JsonWriter(ms, new JsonWriterOptions { Indented = true }))
            {
                doc.RootElement.WriteTo(writer);
            }
            return Encoding.UTF8.GetString(ms.ToArray());
        }
        catch (JsonException)
        {
            return raw;
        }
    }

    /// <summary>
    /// Counts Bundle entries and extracts the "next" page link URL (if any).
    /// Returns (null, null) for non-Bundle or non-JSON payloads.
    /// </summary>
    private static (int? entryCount, string? nextUrl) InspectBundle(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return (null, null);

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
                return (null, null);

            int? entryCount = null;
            string? nextUrl = null;

            if (root.TryGetProperty("entry", out var entries) && entries.ValueKind == JsonValueKind.Array)
                entryCount = entries.GetArrayLength();

            if (root.TryGetProperty("link", out var links) && links.ValueKind == JsonValueKind.Array)
            {
                foreach (var link in links.EnumerateArray())
                {
                    if (link.ValueKind != JsonValueKind.Object)
                        continue;

                    if (link.TryGetProperty("relation", out var rel) &&
                        rel.ValueKind == JsonValueKind.String &&
                        string.Equals(rel.GetString(), "next", StringComparison.OrdinalIgnoreCase) &&
                        link.TryGetProperty("url", out var url) &&
                        url.ValueKind == JsonValueKind.String)
                    {
                        nextUrl = url.GetString();
                        break;
                    }
                }
            }

            return (entryCount, nextUrl);
        }
        catch (JsonException)
        {
            return (null, null);
        }
    }

    private void SetNextPageUrl(string? url)
    {
        _nextPageUrl = url;
        OnPropertyChanged(nameof(HasNextPage));
    }

    // ── Footer actions ──────────────────────────────────────────────────────

    private async Task CopyResponseAsync()
    {
        if (string.IsNullOrEmpty(ResponseBody))
            return;

        await Clipboard.Default.SetTextAsync(ResponseBody);
        StatusMessage = "Response copied to clipboard.";
    }

    private void Clear()
    {
        SearchParams = string.Empty;
        ResourceId = string.Empty;
        ResponseBody = string.Empty;
        StatusMessage = string.Empty;
        SetNextPageUrl(null);
    }

    private async Task SendToMainPageAsync()
    {
        if (string.IsNullOrEmpty(ResponseBody))
        {
            StatusMessage = "Nothing to send — run a request first.";
            return;
        }

        // Reuse the existing static bridge consumed by MainPage.OnAppearing.
        Hl7InspectorViewModel.PendingParsedMessage = ResponseBody;
        await Shell.Current.GoToAsync("//MainPage");
    }

    // ── INotifyPropertyChanged ────────────────────────────────────────────────

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
