using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows.Input;
using HL7Tester.Core;
using HL7Tester.Core.Fhir;
using HL7Tester.Core.Sending;
using Microsoft.Extensions.Logging;

namespace HL7Tester.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> MessageTypesByFamily =
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["ADT"] =
            [
                "ADT A01 - Inpatient or Day Hospital Admission",
                "ADT A02 - Patient Movement",
                "ADT A03 - Discharge",
                "ADT A04 - Outpatient Admission",
                "ADT A05 - Pre-admission",
                "ADT A06 - Transformation of an Outpatient Visit into Admission",
                "ADT A07 - Transformation of an Admission into Outpatient Visit",
                "ADT A08 - Update Patient Stay",
                "ADT A09 - Temporary Movement",
                "ADT A10 - Return from Temporary Movement",
                "ADT A11 - Admission Cancellation",
                "ADT A12 - Movement Cancellation",
                "ADT A13 - Discharge Cancellation",
                "ADT A14 - Scheduled Admission in the Future (not used)",
                "ADT A15 - Scheduled Movement in the Future (not used)",
                "ADT A16 - Scheduled Discharge in the Future (not used)",
                "ADT A18 - Merge Patient Records",
                "ADT A21 - Leave of Absence Departure",
                "ADT A22 - Return from Leave of Absence",
                "ADT A24 - Link between Two Patients (not used)",
                "ADT A25 - Cancellation of Future Scheduled Admission (not used)",
                "ADT A26 - Cancellation of Future Scheduled Movement (not used)",
                "ADT A27 - Cancellation of Future Scheduled Discharge (not used)",
                "ADT A28 - Patient Creation",
                "ADT A31 - Update Patient",
                "ADT A32 - Cancellation of a Return from Temporary Movement",
                "ADT A33 - Cancellation of Temporary Movement",
                "ADT A37 - Cancellation of a Patient Link (not used)",
                "ADT A38 - Pre-admission Cancellation",
                "ADT A40 - Patient Record Merge"
            ],
            ["ORM"] =
            [
                "ORM O01 - General Order Message"
            ],
            ["SIU"] =
            [
                "SIU S12 - Notification of New Appointment Booking",
                "SIU S13 - Notification of Appointment Rescheduling",
                "SIU S14 - Notification of Appointment Modification",
                "SIU S15 - Notification of Appointment Cancellation"
            ]
        };

    private readonly AdtMessageGenerator _generator;
    private readonly INetworkSettingsService _networkSettingsService;
    private readonly IMessageSender _messageSender;
    private readonly IFhirResourceGenerator _fhirGenerator;
    private readonly ILogger<MainViewModel> _logger;
    private FhirProfile? _activeFhirProfile;
    private bool _isFhirMode;
    private bool _isMllpMode = true;

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<string> AvailableMessageFamilies { get; } = new(["ADT", "ORM", "SIU"]);
    public ObservableCollection<string> AvailableMessageTypes { get; } = new();

    // ---- FHIR mode properties ----
    public ObservableCollection<string> AvailableFhirResourceTypes { get; } =
        new(["Bundle (Transaction)", "Patient", "Encounter", "Observation", "ServiceRequest", "Appointment"]);

    private string _selectedFhirResourceType = "Bundle (Transaction)";
    public string SelectedFhirResourceType
    {
        get => _selectedFhirResourceType;
        set => SetField(ref _selectedFhirResourceType, value ?? "Bundle (Transaction)");
    }

    public ObservableCollection<string> AvailableFhirHttpMethods { get; } =
        new(["POST", "PUT", "PATCH", "DELETE"]);

    private string _selectedFhirHttpMethod = "POST";
    public string SelectedFhirHttpMethod
    {
        get => _selectedFhirHttpMethod;
        set => SetField(ref _selectedFhirHttpMethod, value ?? "POST");
    }

    public bool IsFhirMode
    {
        get => _isFhirMode;
        private set
        {
            if (SetField(ref _isFhirMode, value))
            {
                OnPropertyChanged(nameof(ShowJsonEditButtons));
                OnPropertyChanged(nameof(ShowJsonApplyCancel));
                OnPropertyChanged(nameof(ShowValidateButton));
            }
        }
    }

    public bool IsMllpMode
    {
        get => _isMllpMode;
        private set => SetField(ref _isMllpMode, value);
    }

    // Liste des contrôles de commande pour ORC-1 (Order Control Code)
    public IReadOnlyList<string> OrderControlList { get; } = new[]
    {
        "NW - New order",
        "CA - Cancel order",
        "XO - Cross-order"
    };

    private string _selectedMessageFamily = "ADT";
    public string SelectedMessageFamily
    {
        get => _selectedMessageFamily;
        set
        {
            var safeValue = string.IsNullOrWhiteSpace(value) ? "ADT" : value;

            if (SetField(ref _selectedMessageFamily, safeValue))
            {
                RefreshMessageTypes();
                UpdateUiForSelectedContext();
            }
        }
    }

    private string _selectedMessageType = string.Empty;
    public string SelectedMessageType
    {
        get => _selectedMessageType;
        set
        {
            var safeValue = value ?? string.Empty;

            if (SetField(ref _selectedMessageType, safeValue))
            {
                UpdateUiForSelectedContext();
            }
        }
    }

    private string _patientIdLabel = "Patient ID";
    public string PatientIdLabel
    {
        get => _patientIdLabel;
        private set => SetField(ref _patientIdLabel, value);
    }

    private string _patientId = string.Empty;
    public string PatientId
    {
        get => _patientId;
        set => SetField(ref _patientId, value);
    }

    private string? _newPatientId;
    public string? NewPatientId
    {
        get => _newPatientId;
        set => SetField(ref _newPatientId, value);
    }

    private string _patientFamilyName = string.Empty;
    public string PatientFamilyName
    {
        get => _patientFamilyName;
        set => SetField(ref _patientFamilyName, value);
    }

    private string _patientGivenName = string.Empty;
    public string PatientGivenName
    {
        get => _patientGivenName;
        set => SetField(ref _patientGivenName, value);
    }

    private string _birthDate = string.Empty;
    public string BirthDate
    {
        get => _birthDate;
        set => SetField(ref _birthDate, value);
    }

    private string _sex = string.Empty;
    public string Sex
    {
        get => _sex;
        set => SetField(ref _sex, value);
    }

    private string _unit = string.Empty;
    public string Unit
    {
        get => _unit;
        set => SetField(ref _unit, value);
    }

    private string _room = string.Empty;
    public string Room
    {
        get => _room;
        set => SetField(ref _room, value);
    }

    private string _bed = string.Empty;
    public string Bed
    {
        get => _bed;
        set => SetField(ref _bed, value);
    }

    private string _facility = string.Empty;
    public string Facility
    {
        get => _facility;
        set => SetField(ref _facility, value);
    }

    private string _floor = string.Empty;
    public string Floor
    {
        get => _floor;
        set => SetField(ref _floor, value);
    }

    private string _admissionNumber = string.Empty;
    public string AdmissionNumber
    {
        get => _admissionNumber;
        set => SetField(ref _admissionNumber, value);
    }

    private string? _eventDateTime;
    public string? EventDateTime
    {
        get => _eventDateTime;
        set => SetField(ref _eventDateTime, value);
    }

    private string _generatedMessage = string.Empty;
    public string GeneratedMessage
    {
        get => _generatedMessage;
        set => SetField(ref _generatedMessage, value);
    }

    // ---- JSON editing (FHIR mode only) ----
    private bool _isJsonEditing;
    public bool IsJsonEditing
    {
        get => _isJsonEditing;
        private set
        {
            if (SetField(ref _isJsonEditing, value))
            {
                OnPropertyChanged(nameof(ShowJsonEditButtons));
                OnPropertyChanged(nameof(ShowJsonApplyCancel));
                OnPropertyChanged(nameof(ShowValidateButton));
            }
        }
    }

    // Bool -> bool visibility flags (no Visibility-enum converters, see v2.0.21 bug)
    public bool ShowJsonEditButtons => IsFhirMode && !IsJsonEditing;
    public bool ShowJsonApplyCancel => IsFhirMode && IsJsonEditing;

    // The "Validate" button is a tool shown in FHIR mode outside JSON editing.
    public bool ShowValidateButton => IsFhirMode && !IsJsonEditing;

    // Backup of the message taken when entering JSON edit mode, restored on Cancel
    private string? _jsonBackup;

    private string _sendLog = string.Empty;
    public string SendLog
    {
        get => _sendLog;
        set => SetField(ref _sendLog, value);
    }

    private bool _isSendLogExpanded = false; // Collapsed by default as per user request
    public bool IsSendLogExpanded
    {
        get => _isSendLogExpanded;
        set => SetField(ref _isSendLogExpanded, value);
    }

    // Height calculation: ~23px per line (including padding) for up to 5 lines max
    private const double LineHeight = 23;
    private const int MaxLogLines = 5;
    
    private double _sendLogHeightRequest = LineHeight; // 1 line when collapsed
    public double SendLogHeightRequest
    {
        get => _sendLogHeightRequest;
        set => SetField(ref _sendLogHeightRequest, value);
    }

    private string? _obx1Type;
    public string? Obx1Type
    {
        get => _obx1Type;
        set => SetField(ref _obx1Type, value);
    }

    private string? _obx1Reason;
    public string? Obx1Reason
    {
        get => _obx1Reason;
        set => SetField(ref _obx1Reason, value);
    }

    private string? _obx2Type;
    public string? Obx2Type
    {
        get => _obx2Type;
        set => SetField(ref _obx2Type, value);
    }

    private string? _obx2Reason;
    public string? Obx2Reason
    {
        get => _obx2Reason;
        set => SetField(ref _obx2Reason, value);
    }

    private string? _obx3Type;
    public string? Obx3Type
    {
        get => _obx3Type;
        set => SetField(ref _obx3Type, value);
    }

    private string? _obx3Reason;
    public string? Obx3Reason
    {
        get => _obx3Reason;
        set => SetField(ref _obx3Reason, value);
    }

    private bool _isNewPatientIdVisible;
    public bool IsNewPatientIdVisible
    {
        get => _isNewPatientIdVisible;
        private set => SetField(ref _isNewPatientIdVisible, value);
    }

    private bool _isAdtMode;
    public bool IsAdtMode
    {
        get => _isAdtMode;
        private set => SetField(ref _isAdtMode, value);
    }

    private bool _isNonAdtMode;
    public bool IsNonAdtMode
    {
        get => _isNonAdtMode;
        private set => SetField(ref _isNonAdtMode, value);
    }

    private bool _isEventDateTimeVisible;
    public bool IsEventDateTimeVisible
    {
        get => _isEventDateTimeVisible;
        private set => SetField(ref _isEventDateTimeVisible, value);
    }

    private bool _isOrmSectionVisible;
    public bool IsOrmSectionVisible
    {
        get => _isOrmSectionVisible;
        private set => SetField(ref _isOrmSectionVisible, value);
    }

    private bool _isSiuSectionVisible;
    public bool IsSiuSectionVisible
    {
        get => _isSiuSectionVisible;
        private set => SetField(ref _isSiuSectionVisible, value);
    }

    private string _ormOrderControl = "NW";
    public string OrmOrderControl
    {
        get => _ormOrderControl;
        set => SetField(ref _ormOrderControl, value);
    }

    private string _ormPlacerOrderNumber = string.Empty;
    public string OrmPlacerOrderNumber
    {
        get => _ormPlacerOrderNumber;
        set => SetField(ref _ormPlacerOrderNumber, value);
    }

    private string _ormFillerOrderNumber = string.Empty;
    public string OrmFillerOrderNumber
    {
        get => _ormFillerOrderNumber;
        set => SetField(ref _ormFillerOrderNumber, value);
    }

    private string _ormOrderStatus = "Final";
    public string OrmOrderStatus
    {
        get => _ormOrderStatus;
        set => SetField(ref _ormOrderStatus, value);
    }

    private string _ormUniversalServiceId = string.Empty;
    public string OrmUniversalServiceId
    {
        get => _ormUniversalServiceId;
        set => SetField(ref _ormUniversalServiceId, value);
    }

    private string? _ormRequestedDateTime;
    public string? OrmRequestedDateTime
    {
        get => _ormRequestedDateTime;
        set => SetField(ref _ormRequestedDateTime, value);
    }

    private string _ormOrderingProviderId = string.Empty;
    public string OrmOrderingProviderId
    {
        get => _ormOrderingProviderId;
        set => SetField(ref _ormOrderingProviderId, value);
    }

    private string _ormOrderingProviderFamilyName = string.Empty;
    public string OrmOrderingProviderFamilyName
    {
        get => _ormOrderingProviderFamilyName;
        set => SetField(ref _ormOrderingProviderFamilyName, value);
    }

    private string _ormOrderingProviderGivenName = string.Empty;
    public string OrmOrderingProviderGivenName
    {
        get => _ormOrderingProviderGivenName;
        set => SetField(ref _ormOrderingProviderGivenName, value);
    }

    private string _ormDiagnosisCode = string.Empty;
    public string OrmDiagnosisCode
    {
        get => _ormDiagnosisCode;
        set => SetField(ref _ormDiagnosisCode, value);
    }

    private string _ormDiagnosisText = string.Empty;
    public string OrmDiagnosisText
    {
        get => _ormDiagnosisText;
        set => SetField(ref _ormDiagnosisText, value);
    }

    private string _siuPlacerAppointmentId = string.Empty;
    public string SiuPlacerAppointmentId
    {
        get => _siuPlacerAppointmentId;
        set => SetField(ref _siuPlacerAppointmentId, value);
    }

    private string _siuFillerAppointmentId = string.Empty;
    public string SiuFillerAppointmentId
    {
        get => _siuFillerAppointmentId;
        set => SetField(ref _siuFillerAppointmentId, value);
    }

    private string _siuOccurrenceNumber = "1";
    public string SiuOccurrenceNumber
    {
        get => _siuOccurrenceNumber;
        set => SetField(ref _siuOccurrenceNumber, value);
    }

    private string _siuAppointmentType = "OFFICE^Office visit";
    public string SiuAppointmentType
    {
        get => _siuAppointmentType;
        set => SetField(ref _siuAppointmentType, value);
    }

    private string _siuAppointmentReason = string.Empty;
    public string SiuAppointmentReason
    {
        get => _siuAppointmentReason;
        set => SetField(ref _siuAppointmentReason, value);
    }

    private string? _siuAppointmentStartDateTime;
    public string? SiuAppointmentStartDateTime
    {
        get => _siuAppointmentStartDateTime;
        set => SetField(ref _siuAppointmentStartDateTime, value);
    }

    private string? _siuAppointmentEndDateTime;
    public string? SiuAppointmentEndDateTime
    {
        get => _siuAppointmentEndDateTime;
        set => SetField(ref _siuAppointmentEndDateTime, value);
    }

    private string _siuAppointmentDuration = "60";
    public string SiuAppointmentDuration
    {
        get => _siuAppointmentDuration;
        set => SetField(ref _siuAppointmentDuration, value);
    }

    private string _siuAppointmentDurationUnits = "m";
    public string SiuAppointmentDurationUnits
    {
        get => _siuAppointmentDurationUnits;
        set => SetField(ref _siuAppointmentDurationUnits, value);
    }

    private string _siuAppointmentStatus = "Scheduled";
    public string SiuAppointmentStatus
    {
        get => _siuAppointmentStatus;
        set => SetField(ref _siuAppointmentStatus, value);
    }

    private string _siuGeneralResourceId = string.Empty;
    public string SiuGeneralResourceId
    {
        get => _siuGeneralResourceId;
        set => SetField(ref _siuGeneralResourceId, value);
    }

    private string _siuGeneralResourceName = string.Empty;
    public string SiuGeneralResourceName
    {
        get => _siuGeneralResourceName;
        set => SetField(ref _siuGeneralResourceName, value);
    }

    private string _siuGeneralResourceType = "D";
    public string SiuGeneralResourceType
    {
        get => _siuGeneralResourceType;
        set => SetField(ref _siuGeneralResourceType, value);
    }

    private string _siuLocationCode = string.Empty;
    public string SiuLocationCode
    {
        get => _siuLocationCode;
        set => SetField(ref _siuLocationCode, value);
    }

    private string _siuLocationDescription = string.Empty;
    public string SiuLocationDescription
    {
        get => _siuLocationDescription;
        set => SetField(ref _siuLocationDescription, value);
    }

    private string _siuLocationSite = string.Empty;
    public string SiuLocationSite
    {
        get => _siuLocationSite;
        set => SetField(ref _siuLocationSite, value);
    }

    private string _siuPersonnelId = string.Empty;
    public string SiuPersonnelId
    {
        get => _siuPersonnelId;
        set => SetField(ref _siuPersonnelId, value);
    }

    private string _siuPersonnelFamilyName = string.Empty;
    public string SiuPersonnelFamilyName
    {
        get => _siuPersonnelFamilyName;
        set => SetField(ref _siuPersonnelFamilyName, value);
    }

    private string _siuPersonnelGivenName = string.Empty;
    public string SiuPersonnelGivenName
    {
        get => _siuPersonnelGivenName;
        set => SetField(ref _siuPersonnelGivenName, value);
    }

    private string _siuPersonnelRole = "D";
    public string SiuPersonnelRole
    {
        get => _siuPersonnelRole;
        set => SetField(ref _siuPersonnelRole, value);
    }

    public ICommand GenerateCommand { get; }
    public ICommand SendCommand { get; }
    public ICommand RegenerateControlIdCommand { get; }
    public ICommand ToggleEditJsonCommand { get; }
    public ICommand ApplyJsonCommand { get; }
    public ICommand CancelJsonCommand { get; }
    public ICommand PasteJsonCommand { get; }
    public ICommand FormatJsonCommand { get; }
    public ICommand ValidateFhirCommand { get; }

    public MainViewModel(
        AdtMessageGenerator generator,
        INetworkSettingsService networkSettingsService,
        IMessageSender messageSender,
        IFhirResourceGenerator fhirGenerator,
        ILogger<MainViewModel> logger)
    {
        _generator = generator;
        _networkSettingsService = networkSettingsService;
        _messageSender = messageSender;
        _fhirGenerator = fhirGenerator;
        _logger = logger;

        RefreshMessageTypes();
        UpdateUiForSelectedContext();

        GenerateCommand = new Command(OnGenerate);
        SendCommand = new Command(async () => await OnSendAsync());
        RegenerateControlIdCommand = new Command(RegenerateControlId);
        ToggleEditJsonCommand = new Command(ToggleEditJson);
        ApplyJsonCommand = new Command(ApplyJson);
        CancelJsonCommand = new Command(CancelJson);
        PasteJsonCommand = new Command(async () => await PasteJsonAsync());
        FormatJsonCommand = new Command(FormatJson);
        ValidateFhirCommand = new Command(async () => await ValidateFhirAsync());
    }

    /// <summary>
    /// Loads the active protocol and FHIR profile from settings.
    /// Called from MainPage.OnAppearing() to refresh mode before user interaction.
    /// </summary>
    public async Task LoadProtocolAsync()
    {
        var settings = await _networkSettingsService.LoadAsync().ConfigureAwait(false);
        IsFhirMode = string.Equals(settings.Protocol, "FHIR", StringComparison.OrdinalIgnoreCase);
        IsMllpMode = !IsFhirMode;
        _activeFhirProfile = settings.FhirProfiles.FirstOrDefault(p => p.Id == settings.ActiveFhirProfileId);

        // Leaving FHIR mode: drop any pending JSON edit state
        if (!IsFhirMode && _isJsonEditing)
        {
            CancelJson();
        }
    }

    private void RegenerateControlId()
    {
        if (string.IsNullOrWhiteSpace(_generatedMessage)) return;

        // Normalize \n to \r for consistent hashing (same as generator)
        var normalizedMessage = _generatedMessage.Replace("\n", "\r");

        // Extract the MSH line and split on pipe to isolate MSH-10 (ControlID)
        var mshLine = normalizedMessage.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault(s => s.StartsWith("MSH"));

        if (string.IsNullOrWhiteSpace(mshLine)) return;

        var fields = mshLine.Split('|');
        if (fields.Length < 10) return;

        // Save old ControlID before modifying the message
        string oldControlId = fields[9];

        // The prefix for hashing is everything up to MSH-9 (without ControlID field)
        // MSH|field1|field2|...|field8|field9|<ControlID>
        // We need: MSH|field1|field2|...|field8|field9 (no trailing pipe)
        var prefix = string.Join("|", fields.Take(9));

        // Rebuild the message with the prefix replacing the old MSH line
        var rebuiltMessage = normalizedMessage.Replace(mshLine, prefix);

        // Compute SHA256 using Latin-1 encoding (same as GenerateControlIdFromMessage)
        var hashBytes = Encoding.Latin1.GetBytes(rebuiltMessage);
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(hashBytes);

        // Build hex string and take first 19 chars (same as GenerateControlIdFromMessage)
        var hexBuilder = new StringBuilder(hash.Length * 2);
        foreach (byte b in hash)
            hexBuilder.Append(b.ToString("x2"));
        var newControlId = hexBuilder.ToString().Substring(0, 19);

        // Build the full new MSH line with remaining fields intact
        var remainingFields = string.Join("|", fields.Skip(10));
        var newMshLine = prefix + "|" + newControlId + "|" + remainingFields;

        // Replace only the first occurrence of the old MSH line with the new one in normalized message
        _generatedMessage = normalizedMessage.Replace(mshLine, newMshLine);

        // Refresh EVN-2 (RecordedDateTime) and EVN-6 (EventOccured)
        var evnLine = _generatedMessage.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault(s => s.StartsWith("EVN"));

        if (!string.IsNullOrWhiteSpace(evnLine))
        {
            var evnFields = evnLine.Split('|');
            var newTimestamp = DateTime.Now.ToString("yyyyMMddHHmm");

            // EVN-2 (RecordedDateTime)
            if (evnFields.Length > 1)
                evnFields[2] = newTimestamp;

            // EVN-6 (EventOccured)
            var eventOccurred = string.IsNullOrWhiteSpace(_eventDateTime) ? newTimestamp : _eventDateTime;
            while (evnFields.Length < 6)
                evnFields = evnFields.Append(string.Empty).ToArray();
            evnFields[6] = eventOccurred;

            var newEvnLine = string.Join("|", evnFields);
            _generatedMessage = _generatedMessage.Replace(evnLine, newEvnLine);
        }

        // Convert all \r to Environment.NewLine (normalize line endings)
        _generatedMessage = _generatedMessage.Replace("\r", Environment.NewLine);

        OnPropertyChanged(nameof(GeneratedMessage));
    }

    // ---- JSON editing commands (FHIR mode) ----

    private void ToggleEditJson()
    {
        // Toggle acts as Cancel when already editing
        if (IsJsonEditing)
        {
            CancelJson();
            return;
        }

        // Save the current content so Cancel can restore it
        _jsonBackup = _generatedMessage;
        IsJsonEditing = true;
    }

    private void ApplyJson()
    {
        if (!TryValidateJson(_generatedMessage, out var error))
        {
            AppendToSendLog($"[{DateTime.Now:HH:mm:ss}] Apply JSON rejected - Invalid JSON: {error}");
            return;
        }

        // Valid JSON: keep the edited content and leave edit mode
        _jsonBackup = null;
        IsJsonEditing = false;
    }

    private void CancelJson()
    {
        if (_jsonBackup is not null)
        {
            GeneratedMessage = _jsonBackup;
            _jsonBackup = null;
        }

        IsJsonEditing = false;
    }

    private async Task PasteJsonAsync()
    {
        var clipboardText = await Clipboard.GetTextAsync().ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(clipboardText))
        {
            AppendToSendLog($"[{DateTime.Now:HH:mm:ss}] Paste JSON cancelled - Clipboard is empty.");
            return;
        }

        if (!TryValidateJson(clipboardText, out var error))
        {
            AppendToSendLog($"[{DateTime.Now:HH:mm:ss}] Paste JSON rejected - Invalid JSON: {error}");
            return;
        }

        // Enter edit mode so Cancel can restore the previous content
        if (!IsJsonEditing)
        {
            _jsonBackup = _generatedMessage;
            IsJsonEditing = true;
        }

        GeneratedMessage = clipboardText;
        AppendToSendLog($"[{DateTime.Now:HH:mm:ss}] Paste JSON accepted - Valid JSON loaded. Apply to keep, Cancel to revert.");
    }

    private void FormatJson()
    {
        if (string.IsNullOrWhiteSpace(_generatedMessage))
        {
            return;
        }

        try
        {
            // Pretty-print the current content (useful for single-line pasted JSON)
            using var document = JsonDocument.Parse(_generatedMessage);
            GeneratedMessage = JsonSerializer.Serialize(document, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (JsonException ex)
        {
            AppendToSendLog($"[{DateTime.Now:HH:mm:ss}] Format JSON failed - Invalid JSON: {ex.Message}");
        }
    }

    /// <summary>
    /// Validates a JSON string using System.Text.Json. Returns false and a readable error on failure.
    /// </summary>
    private static bool TryValidateJson(string? json, out string? error)
    {
        error = null;

        if (string.IsNullOrWhiteSpace(json))
        {
            error = "The message is empty.";
            return false;
        }

        try
        {
            using var _ = JsonDocument.Parse(json);
            return true;
        }
        catch (JsonException ex)
        {
            error = ex.Message;
            return false;
        }
    }

    private void RefreshMessageTypes()
    {
        AvailableMessageTypes.Clear();

        var familyKey = string.IsNullOrWhiteSpace(SelectedMessageFamily) ? "ADT" : SelectedMessageFamily;

        if (!MessageTypesByFamily.TryGetValue(familyKey, out var familyTypes))
        {
            familyTypes = MessageTypesByFamily["ADT"];
        }

        foreach (var type in familyTypes)
        {
            AvailableMessageTypes.Add(type);
        }

        if (!AvailableMessageTypes.Contains(SelectedMessageType) && AvailableMessageTypes.Count > 0)
        {
            SelectedMessageType = AvailableMessageTypes[0];
        }
        else if (AvailableMessageTypes.Count == 0)
        {
            SelectedMessageType = string.Empty;
        }
    }

    private void UpdateUiForSelectedContext()
    {
        bool isAdt = string.Equals(SelectedMessageFamily, "ADT", StringComparison.OrdinalIgnoreCase);
        bool isOrm = string.Equals(SelectedMessageFamily, "ORM", StringComparison.OrdinalIgnoreCase);
        bool isSiu = string.Equals(SelectedMessageFamily, "SIU", StringComparison.OrdinalIgnoreCase);

        IsAdtMode = isAdt;
        IsNonAdtMode = !isAdt;
        IsEventDateTimeVisible = isAdt;
        IsOrmSectionVisible = isOrm;
        IsSiuSectionVisible = isSiu;

        string selectedType = SelectedMessageType ?? string.Empty;

        bool isMergeAdtType =
            isAdt &&
            (selectedType.StartsWith("ADT A18", StringComparison.OrdinalIgnoreCase) ||
             selectedType.StartsWith("ADT A40", StringComparison.OrdinalIgnoreCase));

        PatientIdLabel = isMergeAdtType ? "Old Patient ID" : "Patient ID";
        IsNewPatientIdVisible = isMergeAdtType;
    }

    // Toggle method for Send log visibility
    public void ToggleSendLog()
    {
        IsSendLogExpanded = !IsSendLogExpanded;
        
        if (IsSendLogExpanded)
        {
            // Set height to show up to MaxLogLines when expanded
            SendLogHeightRequest = LineHeight * Math.Min(MaxLogLines, 5);
        }
        else
        {
            // Set to minimal height when collapsed
            SendLogHeightRequest = LineHeight;
        }
    }

    private void OnGenerate()
    {
        try
        {
            var messageTypeCode = IsFhirMode
                ? MapFhirResourceTypeToMessageTypeCode(SelectedFhirResourceType)
                : SelectedMessageType;

            var request = new AdtMessageRequest
            {
                MessageTypeCode = messageTypeCode,

                PatientId = PatientId ?? string.Empty,
                NewPatientId = NewPatientId,
                PatientFamilyName = PatientFamilyName ?? string.Empty,
                PatientGivenName = PatientGivenName ?? string.Empty,
                BirthDate = BirthDate ?? string.Empty,
                Sex = Sex ?? string.Empty,
                Unit = Unit ?? string.Empty,
                Room = Room ?? string.Empty,
                Bed = Bed ?? string.Empty,
                Facility = Facility ?? string.Empty,
                Floor = Floor ?? string.Empty,
                AdmissionNumber = AdmissionNumber ?? string.Empty,
                EventDateTime = IsEventDateTimeVisible && !string.IsNullOrWhiteSpace(EventDateTime) ? EventDateTime : null,

                OrderControl = ExtractOrderControlCode(OrmOrderControl),
                OrmPlacerOrderNumber = OrmPlacerOrderNumber ?? string.Empty,
                OrmFillerOrderNumber = OrmFillerOrderNumber ?? string.Empty,
                OrmOrderStatus = OrmOrderStatus ?? "Final",
                OrmUniversalServiceId = OrmUniversalServiceId ?? string.Empty,
                OrmRequestedDateTime = string.IsNullOrWhiteSpace(OrmRequestedDateTime) ? null : OrmRequestedDateTime,
                OrmOrderingProviderId = OrmOrderingProviderId ?? string.Empty,
                OrmOrderingProviderFamilyName = OrmOrderingProviderFamilyName ?? string.Empty,
                OrmOrderingProviderGivenName = OrmOrderingProviderGivenName ?? string.Empty,
                OrmDiagnosisCode = OrmDiagnosisCode ?? string.Empty,
                OrmDiagnosisText = OrmDiagnosisText ?? string.Empty,

                SiuPlacerAppointmentId = SiuPlacerAppointmentId ?? string.Empty,
                SiuFillerAppointmentId = SiuFillerAppointmentId ?? string.Empty,
                SiuOccurrenceNumber = SiuOccurrenceNumber ?? "1",
                SiuAppointmentType = SiuAppointmentType ?? "OFFICE^Office visit",
                SiuAppointmentReason = SiuAppointmentReason ?? string.Empty,
                SiuAppointmentStartDateTime = string.IsNullOrWhiteSpace(SiuAppointmentStartDateTime) ? null : SiuAppointmentStartDateTime,
                SiuAppointmentEndDateTime = string.IsNullOrWhiteSpace(SiuAppointmentEndDateTime) ? null : SiuAppointmentEndDateTime,
                SiuAppointmentDuration = SiuAppointmentDuration ?? "60",
                SiuAppointmentDurationUnits = SiuAppointmentDurationUnits ?? "m",
                SiuAppointmentStatus = SiuAppointmentStatus ?? "Scheduled",
                SiuGeneralResourceId = SiuGeneralResourceId ?? string.Empty,
                SiuGeneralResourceName = SiuGeneralResourceName ?? string.Empty,
                SiuGeneralResourceType = SiuGeneralResourceType ?? "D",
                SiuLocationCode = SiuLocationCode ?? string.Empty,
                SiuLocationDescription = SiuLocationDescription ?? string.Empty,
                SiuLocationSite = SiuLocationSite ?? string.Empty,
                SiuPersonnelId = SiuPersonnelId ?? string.Empty,
                SiuPersonnelFamilyName = SiuPersonnelFamilyName ?? string.Empty,
                SiuPersonnelGivenName = SiuPersonnelGivenName ?? string.Empty,
                SiuPersonnelRole = SiuPersonnelRole ?? "D"
            };

            request.ObxEntries.Add(new ObxEntry { Type = Obx1Type, Reason = Obx1Reason });
            request.ObxEntries.Add(new ObxEntry { Type = Obx2Type, Reason = Obx2Reason });
            request.ObxEntries.Add(new ObxEntry { Type = Obx3Type, Reason = Obx3Reason });

            if (IsFhirMode)
            {
                var resource = _fhirGenerator.Generate(request);
                var json = _fhirGenerator.ToJson(resource);
                _logger.LogDebug("FHIR resource generated. ResourceType={ResourceType}", SelectedFhirResourceType);
                GeneratedMessage = json;
            }
            else
            {
                string hl7 = _generator.Generate(request);
                _logger.LogDebug("HL7 message generated. Type={MessageType}", request.MessageTypeCode);
                GeneratedMessage = hl7.Replace("\r", Environment.NewLine);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating message.");
            GeneratedMessage = $"Error generating message: {ex.Message}";
        }
    }

    /// <summary>
    /// Maps a FHIR resource type selection to the MessageTypeCode used by FhirR4Generator.
    /// </summary>
    private static string MapFhirResourceTypeToMessageTypeCode(string resourceType) => resourceType switch
    {
        "Bundle (Transaction)" => "ADT A01",
        "Patient" => "ADT A08",
        "Encounter" => "ADT A02",
        "ServiceRequest" => "ORM O01",
        "Appointment" => "SIU S12",
        "Observation" => "ADT A01",
        _ => "ADT A01"
    };

    private async Task OnSendAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(GeneratedMessage))
            {
                _logger.LogWarning("Send requested but message is empty.");
                AppendToSendLog($"[{DateTime.Now:HH:mm:ss}] Send cancelled - Nothing to send.");
                return;
            }

            if (IsFhirMode)
            {
                await SendFhirAsync().ConfigureAwait(false);
            }
            else
            {
                await SendMllpAsync().ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while sending message.");
            AppendToSendLog($"[{DateTime.Now:HH:mm:ss}] SEND FAILED - Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Runs the FHIR $validate operation against the active server using the current
    /// GeneratedMessage. Reads the root resourceType to build POST [base]/{type}/$validate,
    /// then reports the OperationOutcome issues in the send log.
    /// </summary>
    private async Task ValidateFhirAsync()
    {
        if (string.IsNullOrWhiteSpace(_generatedMessage))
        {
            AppendToSendLog($"[{DateTime.Now:HH:mm:ss}] Validate skipped - Nothing to validate.");
            return;
        }

        if (_activeFhirProfile is null)
        {
            AppendToSendLog($"[{DateTime.Now:HH:mm:ss}] Validate failed - No active FHIR profile. Select FHIR protocol and a profile in Connections.");
            return;
        }

        string resourceType;
        try
        {
            using var doc = JsonDocument.Parse(_generatedMessage);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object ||
                !root.TryGetProperty("resourceType", out var rt) ||
                rt.ValueKind != JsonValueKind.String ||
                string.IsNullOrWhiteSpace(rt.GetString()))
            {
                AppendToSendLog($"[{DateTime.Now:HH:mm:ss}] Validate failed - Could not read resourceType from the JSON.");
                return;
            }
            resourceType = rt.GetString()!;
        }
        catch (JsonException ex)
        {
            AppendToSendLog($"[{DateTime.Now:HH:mm:ss}] Validate failed - Invalid JSON: {ex.Message}");
            return;
        }

        var sendRequest = new SendRequest
        {
            Protocol = "FHIR",
            Payload = _generatedMessage,
            Profile = _activeFhirProfile,
            ResourceType = $"{resourceType}/$validate",
            HttpMethod = FhirHttpMethod.Post
        };

        _logger.LogInformation("Validating FHIR {ResourceType} via $validate on profile '{Profile}'.",
            resourceType, _activeFhirProfile.Name);

        var result = await _messageSender.SendAsync(sendRequest).ConfigureAwait(false);
        var status = result.HttpStatusCode ?? 0;

        // Server does not advertise/support the operation.
        if (status is 404 or 405 or 501)
        {
            AppendToSendLog($"[{DateTime.Now:HH:mm:ss}] Validation not supported by server (HTTP {status}).");
            return;
        }

        var outcomeJson = result.OperationOutcomeJson ?? result.ResponseBody;
        if (TryReadOperationOutcome(outcomeJson, out var issues, out int errorCount, out int warningCount))
        {
            if (errorCount > 0)
            {
                // Keep header + up to (MaxLogLines - 1) issue lines visible in the log.
                var shown = issues.Take(Math.Max(1, MaxLogLines - 1)).ToList();
                var block = $"[{DateTime.Now:HH:mm:ss}] ✗ Validation failed - {errorCount} error(s), {warningCount} warning(s):";
                foreach (var issue in shown)
                    block += Environment.NewLine + issue;
                if (issues.Count > shown.Count)
                    block += Environment.NewLine + $"  ... and {issues.Count - shown.Count} more issue(s).";
                AppendToSendLog(block);
            }
            else
            {
                var suffix = warningCount > 0 ? $" ({warningCount} warning(s))" : string.Empty;
                AppendToSendLog($"[{DateTime.Now:HH:mm:ss}] ✓ Validation passed{suffix}");
            }
            return;
        }

        if (result.Success)
        {
            AppendToSendLog($"[{DateTime.Now:HH:mm:ss}] ✓ Validation passed (no OperationOutcome returned).");
        }
        else
        {
            var err = !string.IsNullOrWhiteSpace(result.ErrorDescription) ? result.ErrorDescription
                    : !string.IsNullOrWhiteSpace(result.ErrorMessage) ? result.ErrorMessage
                    : $"HTTP {status}";
            AppendToSendLog($"[{DateTime.Now:HH:mm:ss}] Validation failed - {err}");
        }
    }

    /// <summary>
    /// Parses an OperationOutcome JSON body. Returns false when the payload is not an
    /// OperationOutcome. On success, fills the formatted issue lines and counts errors/warnings.
    /// </summary>
    private static bool TryReadOperationOutcome(string? json, out List<string> issues, out int errorCount, out int warningCount)
    {
        issues = new List<string>();
        errorCount = 0;
        warningCount = 0;

        if (string.IsNullOrWhiteSpace(json))
            return false;

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
                return false;

            if (!root.TryGetProperty("resourceType", out var rt) ||
                rt.ValueKind != JsonValueKind.String ||
                !string.Equals(rt.GetString(), "OperationOutcome", StringComparison.OrdinalIgnoreCase))
                return false;

            if (!root.TryGetProperty("issue", out var issueArr) || issueArr.ValueKind != JsonValueKind.Array)
                return true; // OperationOutcome with no issues = valid

            foreach (var issue in issueArr.EnumerateArray())
            {
                if (issue.ValueKind != JsonValueKind.Object)
                    continue;

                var severity = GetJsonString(issue, "severity") ?? "unknown";
                if (string.Equals(severity, "error", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(severity, "fatal", StringComparison.OrdinalIgnoreCase))
                    errorCount++;
                else
                    warningCount++;

                string? details = null;
                if (issue.TryGetProperty("details", out var d) && d.ValueKind == JsonValueKind.Object)
                    details = GetJsonString(d, "text");

                string? expression = null;
                if (issue.TryGetProperty("expression", out var e) && e.ValueKind == JsonValueKind.Array && e.GetArrayLength() > 0)
                    expression = string.Join(", ", e.EnumerateArray()
                        .Where(x => x.ValueKind == JsonValueKind.String)
                        .Select(x => x.GetString()));

                var code = GetJsonString(issue, "code");
                var msg = $"  [{severity}]";
                if (!string.IsNullOrWhiteSpace(code))
                    msg += $" {code}";
                if (!string.IsNullOrWhiteSpace(details))
                    msg += $": {details}";
                if (!string.IsNullOrWhiteSpace(expression))
                    msg += $" ({expression})";
                issues.Add(msg);
            }

            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static string? GetJsonString(JsonElement obj, string prop)
        => obj.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    private async Task SendFhirAsync()
    {
        var sendRequest = new SendRequest
        {
            Protocol = "FHIR",
            Payload = GeneratedMessage ?? string.Empty,
            Profile = _activeFhirProfile,
            ResourceType = MapFhirResourceTypeToEndpoint(SelectedFhirResourceType),
            HttpMethod = ParseHttpMethod(SelectedFhirHttpMethod)
        };

        _logger.LogInformation("Sending FHIR {Method} {ResourceType} to profile '{Profile}'.",
            SelectedFhirHttpMethod, SelectedFhirResourceType, _activeFhirProfile?.Name ?? "(none)");

        var result = await _messageSender.SendAsync(sendRequest).ConfigureAwait(false);

        if (result.Success)
        {
            AppendToSendLog(
                $"[{DateTime.Now:HH:mm:ss}] FHIR {SelectedFhirHttpMethod} {SelectedFhirResourceType} -> HTTP {result.HttpStatusCode} OK");
        }
        else
        {
            var errorLine = $"[{DateTime.Now:HH:mm:ss}] FHIR {SelectedFhirHttpMethod} {SelectedFhirResourceType} -> HTTP {result.HttpStatusCode} ERROR";
            if (!string.IsNullOrWhiteSpace(result.ErrorDescription))
                errorLine += $"\n{result.ErrorDescription}";
            else if (!string.IsNullOrWhiteSpace(result.ErrorMessage))
                errorLine += $"\n{result.ErrorMessage}";
            AppendToSendLog(errorLine);
        }
    }

    private async Task SendMllpAsync()
    {
        string messageToSend = (GeneratedMessage ?? string.Empty).Replace(Environment.NewLine, "\r");

        var settings = await _networkSettingsService.LoadAsync().ConfigureAwait(false);
        string ip = settings.LastIpAddress ?? string.Empty;
        string portStr = settings.LastPort ?? string.Empty;

        if (!int.TryParse(portStr, out int port) || port < 1 || port > 65535)
        {
            _logger.LogWarning("Invalid port configured: {Port}", portStr);
            AppendToSendLog($"[{DateTime.Now:HH:mm:ss}] Send cancelled - Invalid port configured.");
            return;
        }

        _logger.LogInformation("Sending HL7 message to {Ip}:{Port}.", ip, port);

        var sendRequest = new SendRequest
        {
            Protocol = "MLLP",
            Payload = messageToSend,
            Ip = ip,
            Port = port,
            EncodingName = settings.MessageEncoding
        };

        var result = await _messageSender.SendAsync(sendRequest).ConfigureAwait(false);

        // Prefer the real message type read from MSH-9 of the sent payload;
        // fall back to the dropdown selection when MSH-9 is unreadable.
        string messageTypeStr = FormatRealMessageType(result.MessageCode) ?? SelectedMessageType ?? "Unknown";

        // Append the ControlID (MSH-10) of the actually-sent message when available,
        // producing the compact "ADT A31|b549301d43168bbf2b1" log format.
        if (!string.IsNullOrWhiteSpace(result.ControlId))
        {
            messageTypeStr = $"{messageTypeStr}|{result.ControlId.Trim()}";
        }

        // Resolve the friendly nickname linked to the ip/port being sent to,
        // so the send log shows e.g. "Staging (10.5.0.29:70)" when available.
        string destinationLabel = BuildDestinationLabel(settings, ip, portStr);

        if (result.Success)
        {
            string ackStatus = result.MsaStatus switch
            {
                "AA" => "ACK",
                "AE" => "ACK (with errors)",
                _ => "No ACK"
            };

            AppendToSendLog(
                $"[{DateTime.Now:HH:mm:ss}] Message SEND {messageTypeStr} to {destinationLabel}. -> {ackStatus}");
        }
        else if (result.MsaStatus == "AR" || result.MsaStatus == "AE")
        {
            _logger.LogWarning("Message {MsaStatus} by receiver at {Ip}:{Port}. {Desc}", result.MsaStatus, ip, port, result.ErrorDescription ?? "(no description)");
            var nackLine = $"[{DateTime.Now:HH:mm:ss}] Message SEND {messageTypeStr} to {destinationLabel}. -> NACK";
            if (!string.IsNullOrWhiteSpace(result.ErrorDescription))
            {
                nackLine += $"\n{result.ErrorDescription}";
            }
            AppendToSendLog(nackLine);
        }
        else
        {
            _logger.LogError("Failed to send HL7 message to {Ip}:{Port}: {Error}", ip, port, result.ErrorMessage);
            AppendToSendLog($"[{DateTime.Now:HH:mm:ss}] Message SEND FAILED to {ip}:{port}. Error: {result.ErrorMessage}");
        }
    }

    /// <summary>
    /// Builds the destination label used in send-log lines: "Nickname (ip:port)" when a
    /// non-empty nickname is linked to the ip/port (from the connection history, falling
    /// back to the active connection's nickname), otherwise the plain "ip:port".
    /// </summary>
    private static string BuildDestinationLabel(NetworkSettings settings, string ip, string port)
    {
        var entry = settings.History.FirstOrDefault(e =>
            string.Equals(e.Ip, ip, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(e.Port, port, StringComparison.OrdinalIgnoreCase));

        string? nickname = !string.IsNullOrWhiteSpace(entry?.Nickname) ? entry!.Nickname : settings.Nickname;

        return string.IsNullOrWhiteSpace(nickname)
            ? $"{ip}:{port}"
            : $"{nickname!.Trim()} ({ip}:{port})";
    }

    /// <summary>
    /// Formats the raw MSH-9 message code (e.g. "ADT^A31") into the compact display
    /// string "ADT A31" (family + trigger event, first two MSH-9 components).
    /// Returns null when the code is missing or unreadable so callers can fall back
    /// to the dropdown selection.
    /// </summary>
    private static string? FormatRealMessageType(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return null;

        // Keep only the first two MSH-9 components: "ADT^A31^ADT_A31" -> "ADT A31".
        var parts = code.Split('^', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
            return null;

        string normalized = parts.Length >= 2
            ? $"{parts[0].Trim()} {parts[1].Trim()}"
            : parts[0].Trim();

        return normalized.Length == 0 ? null : normalized;
    }

    private static string MapFhirResourceTypeToEndpoint(string resourceType) => resourceType switch
    {
        "Bundle (Transaction)" => "Bundle",
        "Patient" => "Patient",
        "Encounter" => "Encounter",
        "Observation" => "Observation",
        "ServiceRequest" => "ServiceRequest",
        "Appointment" => "Appointment",
        _ => "Bundle"
    };

    private static FhirHttpMethod ParseHttpMethod(string method) => method switch
    {
        "PUT" => FhirHttpMethod.Put,
        "PATCH" => FhirHttpMethod.Patch,
        "DELETE" => FhirHttpMethod.Delete,
        "GET" => FhirHttpMethod.Get,
        _ => FhirHttpMethod.Post
    };

    private void AppendToSendLog(string line)
    {
        if (string.IsNullOrWhiteSpace(_sendLog))
        {
            SendLog = line;
        }
        else
        {
            // Add new line at the beginning (most recent first)
            SendLog = line + Environment.NewLine + _sendLog;

            // Limit to MaxLogLines (keep only the most recent lines)
            var lines = SendLog.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length > MaxLogLines)
            {
                SendLog = string.Join(Environment.NewLine, lines.Take(MaxLogLines));
            }
        }

        // Auto-expand when log is updated if collapsed
        if (!_isSendLogExpanded)
        {
            IsSendLogExpanded = true;
            // Set height to show up to MaxLogLines
            SendLogHeightRequest = LineHeight * MaxLogLines;
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value!;
        OnPropertyChanged(propertyName);
        return true;
    }

    private static string ExtractOrderControlCode(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "NW";
        }

        var parts = value.Split('-', 2, StringSplitOptions.RemoveEmptyEntries);
        var code = parts.Length > 0 ? parts[0].Trim() : value.Trim();

        return string.IsNullOrWhiteSpace(code) ? "NW" : code;
    }
}